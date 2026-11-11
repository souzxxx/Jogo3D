using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Heroi: anda, mira livre do movimento, combo de ataque e dash com invulnerabilidade.
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
            new(6f, 0.30f, 0.12f, 3f),
            new(6f, 0.30f, 0.12f, 3f),
            new(10f, 0.45f, 0.18f, 7f),
        };
        [SerializeField] float attackRange = 2.6f;
        [SerializeField, Range(10f, 360f)] float attackArc = 130f;
        [SerializeField] ElementType element = ElementType.None;

        public IPlayerInput Input { get; set; }
        public Vector3 AimDirection { get; private set; } = Vector3.forward;
        public bool IsMoving { get; private set; }
        public bool IsDashing => dash.IsDashing;
        public bool IsAttacking => combo.IsAttacking;
        public DashState Dash => dash;
        public ComboState Combo => combo;
        public ElementType Element { get => element; set => element = value; }

        /// indice do golpe no combo
        public event Action<int> AttackStarted;
        /// (indice do golpe, alvos atingidos)
        public event Action<int, int> AttackHit;
        public event Action<Vector3> DashStarted;

        CharacterController body;
        Health health;
        DashState dash;
        ComboState combo;
        Vector3 dashDir;
        bool wasDashing;
        readonly HashSet<IDamageable> hitSet = new();
        static readonly Collider[] overlap = new Collider[32];

        void Awake()
        {
            body = GetComponent<CharacterController>();
            health = GetComponent<Health>();
            dash = new DashState(dashDuration, dashCooldown, 1);
            combo = new ComboState(comboSteps);
            Input ??= GetComponent<IPlayerInput>();
        }

        void Update()
        {
            if (health.IsDead || Input == null) return;
            float dt = Time.deltaTime;

            var mv = Input.Move;
            var moveDir = Vector3.ClampMagnitude(new Vector3(mv.x, 0f, mv.y), 1f);
            var toAim = Input.AimPoint - transform.position;
            toAim.y = 0f;
            if (toAim.sqrMagnitude > 0.04f) AimDirection = toAim.normalized;

            dash.Tick(dt);
            bool canCancelIntoDash = !combo.IsAttacking || combo.PastHit;
            if (Input.ConsumeDash() && canCancelIntoDash && dash.TryStart())
            {
                dashDir = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : AimDirection;
                combo.Cancel();
                health.Pool.Invulnerable = true;
                DashStarted?.Invoke(dashDir);
            }
            if (wasDashing && !dash.IsDashing) health.Pool.Invulnerable = false;
            wasDashing = dash.IsDashing;

            Vector3 velocity;
            if (dash.IsDashing)
            {
                velocity = dashDir * (dashDistance / dashDuration);
            }
            else
            {
                if (Input.AttackHeld && combo.TryAttack()) AttackStarted?.Invoke(combo.CurrentIndex);
                combo.Tick(dt, out bool hit, out bool chained);
                if (hit) DoHit();
                if (chained) AttackStarted?.Invoke(combo.CurrentIndex);
                velocity = moveDir * (moveSpeed * (combo.IsAttacking ? attackMoveFactor : 1f));
            }

            IsMoving = !dash.IsDashing && moveDir.sqrMagnitude > 0.01f;
            velocity.y = -10f;
            body.Move(velocity * dt);

            var face = dash.IsDashing ? dashDir : AimDirection;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), turnSpeed * dt);
        }

        void DoHit()
        {
            var step = combo.Current;
            var origin = transform.position + Vector3.up;
            int n = Physics.OverlapSphereNonAlloc(origin, attackRange, overlap, ~0, QueryTriggerInteraction.Collide);
            hitSet.Clear();
            int hits = 0;
            for (int i = 0; i < n; i++)
            {
                var target = overlap[i].GetComponentInParent<IDamageable>();
                if (target == null || target.Team == health.Team || target.IsDead || !hitSet.Add(target)) continue;
                var to = overlap[i].transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.6f && Vector3.Angle(AimDirection, to) > attackArc * 0.5f) continue;
                var dir = to.sqrMagnitude > 0.001f ? to.normalized : AimDirection;
                var info = new DamageInfo(step.Damage, element, health.Team, gameObject)
                {
                    Point = overlap[i].ClosestPoint(origin),
                    Direction = dir,
                    Knockback = step.Knockback,
                };
                if (target.TakeDamage(info) > 0f) hits++;
            }
            AttackHit?.Invoke(combo.CurrentIndex, hits);
        }
    }
}
