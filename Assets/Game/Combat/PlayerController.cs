using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Heroi: anda, mira livre do movimento, combo, skill e dash do elemento atual, troca de elemento.
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] float moveSpeed = 7f;
        [SerializeField, Range(0f, 1f)] float attackMoveFactor = 0.6f;
        [SerializeField] float turnSpeed = 1080f;

        [Header("Dash")]
        [SerializeField] float dashDistance = 4f;
        [SerializeField] float dashDuration = 0.15f;
        [SerializeField] float dashCooldown = 0.6f;

        [Header("Ataque")]
        [SerializeField] ComboStep[] comboSteps =
        {
            new(6f, 0.30f, 0.12f, 2f),
            new(6f, 0.30f, 0.12f, 2f),
            new(10f, 0.45f, 0.18f, 5f),
        };
        [SerializeField] float attackRange = 3.2f;
        [SerializeField, Range(10f, 360f)] float attackArc = 120f;

        [Header("Elementos")]
        [SerializeField] ElementDefinition[] elements = Array.Empty<ElementDefinition>();
        [SerializeField] ReactionTable reactions;
        [SerializeField] ElementType startingElement = ElementType.Fire;
        [Tooltip("Elementos ja despertos alem do inicial (cena de teste).")]
        [SerializeField] ElementType[] alsoAwakened = Array.Empty<ElementType>();

        public IPlayerInput Input { get; set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool IsMoving { get; private set; }
        public bool IsDashing => dash.IsDashing;
        public bool IsAttacking => combo.IsAttacking;
        public bool IsCasting => castLeft > 0f;
        public DashState Dash => dash;
        public ComboState Combo => combo;
        public LoadoutState Loadout => loadout;
        public ElementType Element => loadout.Current;

        /// indice do golpe no combo
        public event Action<int> AttackStarted;
        /// (indice do golpe, alvos atingidos)
        public event Action<int, int> AttackHit;
        public event Action<Vector3> DashStarted;
        public event Action<ElementType> SkillStarted;

        CharacterController body;
        Health health;
        DashState dash;
        ComboState combo;
        LoadoutState loadout;
        ElementDefinition fallback, castDef;
        Vector3 dashDir;
        bool wasDashing, castHit;
        float castLeft, castT;
        const float InputBuffer = 0.25f;
        float dashBuffer, skillBuffer, switchBuffer;
        int switchDir;
        readonly float[] skillCd = new float[5];
        readonly HashSet<Health> hitSet = new();
        static readonly Collider[] overlap = new Collider[32];

        void Awake()
        {
            body = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            dash = new DashState(dashDuration, dashCooldown, 1);
            combo = new ComboState(comboSteps);
            loadout = new LoadoutState(startingElement);
            foreach (var e in alsoAwakened) loadout.Awaken(e);
            fallback = ScriptableObject.CreateInstance<ElementDefinition>();
            Input ??= GetComponent<IPlayerInput>();
        }

        void Start()
        {
            foreach (var e in loadout.Awakened) GameEvents.RaiseElementAwakened(e);
            dash.SetMaxCharges(Def(Element).dashCharges);
            GameEvents.RaiseElementSwitched(ElementType.None, Element);
        }

        ElementDefinition Def(ElementType e)
        {
            foreach (var d in elements) if (d != null && d.element == e) return d;
            return fallback;
        }

        /// 0 = pronta, 1 = acabou de usar.
        public float SkillCooldown01(ElementType e)
        {
            float cd = Def(e).skillCooldown;
            return cd <= 0f ? 0f : Mathf.Clamp01(skillCd[(int)e] / cd);
        }

        /// Desperta um elemento (santuario).
        public bool Awaken(ElementType e)
        {
            if (!loadout.Awaken(e)) return false;
            GameEvents.RaiseElementAwakened(e);
            return true;
        }

        void Update()
        {
            if (health.IsDead || Input == null) return;
            float dt = Time.deltaTime;
            loadout.Tick(dt);
            for (int i = 0; i < skillCd.Length; i++) if (skillCd[i] > 0f) skillCd[i] -= dt;

            float moveMul = 1f;
            bool canAct = true;
            if (TryGetComponent(out StatusHolder status)) { moveMul = status.State.MoveMultiplier; canAct = status.State.CanAct; }

            var mv = Input.Move;
            var moveDir = Vector3.ClampMagnitude(new Vector3(mv.x, 0f, mv.y), 1f);
            var toAim = Input.AimPoint - transform.position;
            toAim.y = 0f;
            if (toAim.sqrMagnitude > 0.04f) AimDirection = toAim.normalized;

            // comandos apertados no meio de um golpe ficam guardados por um instante em vez de se perderem
            if (Input.ConsumeDash()) dashBuffer = InputBuffer;
            if (Input.ConsumeSkill()) skillBuffer = InputBuffer;
            int pressed = Input.ConsumeSwitch();
            if (pressed != 0) { switchBuffer = InputBuffer; switchDir = pressed; }
            dashBuffer -= dt; skillBuffer -= dt; switchBuffer -= dt;
            bool dashPressed = dashBuffer > 0f;
            bool skillPressed = skillBuffer > 0f;
            int sw = switchBuffer > 0f ? switchDir : 0;
            bool free = canAct && castLeft <= 0f && (!combo.IsAttacking || combo.PastHit);

            if (sw != 0 && free)
            {
                var from = Element;
                if (loadout.TrySwitch(sw))
                {
                    switchBuffer = 0f;
                    dash.SetMaxCharges(Def(Element).dashCharges);
                    GameEvents.RaiseElementSwitched(from, Element);
                }
            }

            dash.Tick(dt);
            if (dashPressed && free && dash.TryStart()) { dashBuffer = 0f; StartDash(moveDir, status); }
            if (wasDashing && !dash.IsDashing) health.Pool.Invulnerable = false;
            wasDashing = dash.IsDashing;

            Vector3 velocity = Vector3.zero;
            if (dash.IsDashing)
            {
                velocity = dashDir * (dashDistance / dashDuration);
            }
            else if (castLeft > 0f)
            {
                castLeft -= dt;
                castT += dt;
                if (!castHit && castT >= castDef.skillHitTime) { castHit = true; DoSkillHit(); }
                velocity = moveDir * (moveSpeed * attackMoveFactor * moveMul);
            }
            else if (canAct)
            {
                int ei = (int)Element;
                if (skillPressed && skillCd[ei] <= 0f && (!combo.IsAttacking || combo.PastHit))
                {
                    skillBuffer = 0f;
                    combo.Cancel();
                    castDef = Def(Element);
                    castLeft = castDef.skillCastTime;
                    castT = 0f;
                    castHit = false;
                    skillCd[ei] = castDef.skillCooldown;
                    SkillStarted?.Invoke(Element);
                }
                else
                {
                    if (Input.AttackHeld && combo.TryAttack()) AttackStarted?.Invoke(combo.CurrentIndex);
                    combo.Tick(dt, out bool hit, out bool chained);
                    if (hit) DoBasicHit();
                    if (chained) AttackStarted?.Invoke(combo.CurrentIndex);
                }
                velocity = moveDir * (moveSpeed * moveMul * (combo.IsAttacking || castLeft > 0f ? attackMoveFactor : 1f));
            }

            IsMoving = !dash.IsDashing && velocity.sqrMagnitude > 0.01f;
            velocity.y = -10f;
            body.Move(velocity * dt);

            var face = dash.IsDashing ? dashDir : AimDirection;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), turnSpeed * dt);
        }

        void StartDash(Vector3 moveDir, StatusHolder status)
        {
            dashDir = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : AimDirection;
            combo.Cancel();
            health.Pool.Invulnerable = true;
            var def = Def(Element);
            switch (def.dash)
            {
                case DashVariant.FireTrail:
                    foreach (float d in new[] { 0.6f, 2f, 3.4f })
                        DamageZone.Spawn(transform.position + dashDir * d, 1.1f, 1.5f, 0.5f, 3f, Element, health.Team, gameObject, reactions);
                    break;
                case DashVariant.Cleanse:
                    if (status != null) status.ClearNegative();
                    break;
            }
            DashStarted?.Invoke(dashDir);
            GameEvents.RaiseAttackPerformed(new AttackEvent
            {
                Origin = transform.position, Direction = dashDir, Element = Element, Range = dashDistance,
                Kind = AttackKind.Dash, Source = gameObject,
            });
        }

        void DoBasicHit()
        {
            var step = combo.Current;
            var def = Def(Element);
            bool finisher = combo.CurrentIndex == combo.StepCount - 1;
            bool strike = loadout.ConsumeStrike();
            float damage = step.Damage * def.damageMultiplier * (strike ? LoadoutState.StrikeBonus : 1f);
            int hits = ShapedHit(attackRange, attackArc, damage, step.Knockback * def.knockbackMultiplier,
                finisher ? AttackKind.Finisher : AttackKind.Basic, strike,
                finisher && def.finisherAppliesStatus, def.finisherStatus, def.finisherStatusDuration);
            if (hits > 0) loadout.NotifyHitLanded();
            AttackHit?.Invoke(combo.CurrentIndex, hits);
        }

        void DoSkillHit()
        {
            int hits = ShapedHit(castDef.skillRange, castDef.skillArc, castDef.skillDamage, castDef.skillKnockback,
                AttackKind.Skill, false, castDef.skillAppliesStatus, castDef.skillStatus, castDef.skillStatusDuration);
            if (hits > 0) loadout.NotifyHitLanded();
        }

        int ShapedHit(float range, float arc, float damage, float knockback, AttackKind kind, bool strike,
            bool applyStatus, StatusType statusType, float statusDuration)
        {
            var element = Element;
            var origin = transform.position + Vector3.up;
            GameEvents.RaiseAttackPerformed(new AttackEvent
            {
                Origin = transform.position, Direction = AimDirection, Element = element, Range = range, Arc = arc,
                Kind = kind, SwitchStrike = strike, Source = gameObject,
            });

            int n = Physics.OverlapSphereNonAlloc(origin, range, overlap, ~0, QueryTriggerInteraction.Collide);
            hitSet.Clear();
            int hits = 0;
            for (int i = 0; i < n; i++)
            {
                var target = overlap[i].GetComponentInParent<Health>();
                if (target == null || target.Team == health.Team || target.IsDead || !hitSet.Add(target)) continue;
                var to = target.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.6f && Vector3.Angle(AimDirection, to) > arc * 0.5f) continue;
                var info = new DamageInfo(damage, element, health.Team, gameObject)
                {
                    Point = overlap[i].ClosestPoint(origin),
                    Direction = to.sqrMagnitude > 0.001f ? to.normalized : AimDirection,
                    Knockback = knockback,
                };
                if (ElementCombat.Deliver(target, info, reactions) <= 0f) continue;
                hits++;
                if (applyStatus && !target.IsDead)
                    StatusHolder.GetOrAdd(target).Apply(statusType, statusDuration, health.Team, gameObject);
            }
            return hits;
        }
    }
}
