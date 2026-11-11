using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Heroi: anda, mira livre do movimento, combo, skill e dash do elemento atual, troca de elemento e Quintessencia.
    [RequireComponent(typeof(CharacterController), typeof(Health))]
    public class PlayerController : MonoBehaviour, IHeroStatus
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
        [SerializeField] float critMultiplier = 1.5f;

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
        public QuintessenceState Quintessence => quint;
        public ElementType Element => loadout.Current;

        // IHeroStatus
        public Health Health => health;
        public ElementType CurrentElement => loadout.Current;
        public IReadOnlyList<ElementType> AwakenedElements => loadout.Awakened;
        public int DashCharges => dash.Charges;
        public int DashMaxCharges => dash.MaxCharges;
        public float Quintessence01 => quint.Normalized;
        public bool QuintessenceActive => quint.Active;
        public bool QuintessenceReady => quint.Ready && loadout.HasAll;

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
        readonly QuintessenceState quint = new();
        ElementDefinition fallback, castDef;
        Vector3 dashDir, castPoint;
        bool wasDashing, castHit;
        float castLeft, castT, dashSpeed, armorLeft;
        int quintCycle;
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

        void OnEnable() => GameEvents.ReactionTriggered += OnReaction;
        void OnDisable() => GameEvents.ReactionTriggered -= OnReaction;

        void Start()
        {
            foreach (var e in loadout.Awakened) GameEvents.RaiseElementAwakened(e);
            dash.SetMaxCharges(Def(Element).dashCharges);
            GameEvents.RaiseElementSwitched(ElementType.None, Element);
            GameEvents.RaiseQuintessenceMeter(0f);
        }

        void OnReaction(ReactionEvent e)
        {
            if (e.Source != gameObject || quint.Active) return;
            quint.AddReaction();
            GameEvents.RaiseQuintessenceMeter(quint.Normalized);
        }

        /// Enche o medidor por fora das reacoes (bencaos, cena de teste).
        public void AddQuintessence(float amount)
        {
            quint.AddMeter(amount);
            GameEvents.RaiseQuintessenceMeter(quint.Normalized);
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

        /// Comeco de run: o heroi volta a ter so o elemento inicial.
        public void ResetLoadout(ElementType starting)
        {
            loadout = new LoadoutState(starting);
            dash.SetMaxCharges(Def(starting).dashCharges);
            GameEvents.RaiseElementAwakened(starting);
            GameEvents.RaiseElementSwitched(ElementType.None, starting);
        }

        /// Move o heroi de uma vez (troca de sala).
        public void Teleport(Vector3 position, Vector3 facing)
        {
            body.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(facing));
            body.enabled = true;
            AimDirection = facing;
            combo.Cancel();
            castLeft = 0f;
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
            if (armorLeft > 0f && (armorLeft -= dt) <= 0f) health.Pool.Blocks = 0;

            if (Input.ConsumeQuintessence() && quint.TryActivate(loadout.HasAll))
            {
                health.Pool.ExtraDamageTakenMultiplier = QuintessenceState.DamageTaken;
                GameEvents.RaiseQuintessenceActive(true);
            }
            if (quint.Active) GameEvents.RaiseQuintessenceMeter(quint.Normalized);
            if (quint.Tick(dt))
            {
                health.Pool.ExtraDamageTakenMultiplier = 1f;
                GameEvents.RaiseQuintessenceActive(false);
                GameEvents.RaiseQuintessenceMeter(0f);
            }

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
                velocity = dashDir * dashSpeed;
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
                    castPoint = transform.position + AimDirection * castDef.skillSummonDistance;
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
            float distance = dashDistance * def.dashDistanceMultiplier;
            dashSpeed = distance / dashDuration;
            switch (def.dash)
            {
                case DashVariant.FireTrail:
                    foreach (float d in new[] { 0.15f, 0.5f, 0.85f })
                        DamageZone.Spawn(transform.position + dashDir * (distance * d), 1.1f, 1.5f, 0.5f, 3f, Element, health.Team, gameObject, reactions);
                    break;
                case DashVariant.Cleanse:
                    if (status != null) status.ClearNegative();
                    break;
                case DashVariant.Armor:
                    health.Pool.Blocks = 1;
                    armorLeft = dashDuration + 1f;
                    break;
            }
            DashStarted?.Invoke(dashDir);
            GameEvents.RaiseAttackPerformed(new AttackEvent
            {
                Origin = transform.position, Direction = dashDir, Element = Element, Range = distance,
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
            int hits = ShapedHit(transform.position, AimDirection, attackRange, attackArc, damage, step.Knockback * def.knockbackMultiplier,
                finisher ? AttackKind.Finisher : AttackKind.Basic, strike, def.critChance,
                finisher && def.finisherAppliesStatus, def.finisherStatus, def.finisherStatusDuration);
            if (hits > 0) loadout.NotifyHitLanded();
            AttackHit?.Invoke(combo.CurrentIndex, hits);
        }

        void DoSkillHit()
        {
            var d = castDef;
            var origin = transform.position;
            float arc = d.skillArc;
            if (d.skillShape == SkillShape.Radial) arc = 360f;
            if (d.skillShape == SkillShape.Summon)
            {
                origin = castPoint;
                arc = 360f;
                if (d.skillSummonPrefab != null)
                    Instantiate(d.skillSummonPrefab, castPoint, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0));
            }
            int hits = ShapedHit(origin, AimDirection, d.skillRange, arc, d.skillDamage, d.skillKnockback,
                AttackKind.Skill, false, 0f, d.skillAppliesStatus, d.skillStatus, d.skillStatusDuration);
            if (hits > 0) loadout.NotifyHitLanded();
        }

        int ShapedHit(Vector3 from, Vector3 direction, float range, float arc, float damage, float knockback, AttackKind kind,
            bool strike, float critChance, bool applyStatus, StatusType statusType, float statusDuration)
        {
            // Quintessencia: cada golpe usa o proximo elemento e reage como se o alvo ja estivesse marcado
            var element = Element;
            var force = ElementType.None;
            if (quint.Active)
            {
                element = Elements.All[quintCycle % 4];
                force = Elements.All[(quintCycle + 1) % 4];
                quintCycle++;
            }

            GameEvents.RaiseAttackPerformed(new AttackEvent
            {
                Origin = from, Direction = direction, Element = element, Range = range, Arc = arc,
                Kind = kind, SwitchStrike = strike, Source = gameObject,
            });

            var origin = from + Vector3.up;
            int n = Physics.OverlapSphereNonAlloc(origin, range, overlap, ~0, QueryTriggerInteraction.Collide);
            hitSet.Clear();
            int hits = 0;
            for (int i = 0; i < n; i++)
            {
                var target = overlap[i].GetComponentInParent<Health>();
                if (target == null || target.Team == health.Team || target.IsDead || !hitSet.Add(target)) continue;
                var to = target.transform.position - from;
                to.y = 0f;
                if (arc < 359f && to.sqrMagnitude > 0.6f && Vector3.Angle(direction, to) > arc * 0.5f) continue;
                bool crit = critChance > 0f && UnityEngine.Random.value < critChance;
                var info = new DamageInfo(damage * (crit ? critMultiplier : 1f), element, health.Team, gameObject)
                {
                    Point = overlap[i].ClosestPoint(origin),
                    Direction = to.sqrMagnitude > 0.001f ? to.normalized : direction,
                    Knockback = knockback,
                    IsCrit = crit,
                };
                if (ElementCombat.Deliver(target, info, reactions, 1f, force) <= 0f) continue;
                hits++;
                if (applyStatus && !target.IsDead)
                    StatusHolder.GetOrAdd(target).Apply(statusType, statusDuration, health.Team, gameObject);
            }
            return hits;
        }
    }
}
