using Quintessence.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Quintessence.Enemies
{
    /// Inimigo base: persegue o heroi pelo NavMesh, avisa o golpe no chao, golpeia e morre.
    /// Respeita os status (Atordoado, Congelado, Enraizado, Encharcado, Cego).
    [RequireComponent(typeof(Health), typeof(NavMeshAgent))]
    public class EnemyBrain : MonoBehaviour
    {
        [SerializeField] EnemyDefinition definition;
        [SerializeField] Animator animator;

        public EnemyDefinition Definition => definition;
        public EnemyPhase Phase => fsm.Phase;

        Health health, target;
        NavMeshAgent agent;
        Collider body;
        EnemyFsm fsm;
        Vector3 knock, strikePoint, strikeDir;
        string anim;
        float repath, chargeLeft;
        bool chargeHit, healing;

        public void Bind(EnemyDefinition def, Animator anim) { definition = def; animator = anim; }

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<Collider>();
            // a sala (WaveSpawner pai) pode escalar os numeros da ficha: profundidade e traco de Elite
            var spawner = GetComponentInParent<WaveSpawner>();
            Scaling = spawner != null ? spawner.Scaling : EnemyScaling.None;
            if (Scaling.Health != 1f) health.Pool.SetMax(health.Max * Scaling.Health);
            if (Scaling.Size != 1f) transform.localScale *= Scaling.Size;
            fsm = new EnemyFsm(definition.telegraphTime, definition.recoverTime, definition.cooldown * Scaling.Cooldown, definition.attackRange);
            agent.speed = definition.moveSpeed * Scaling.Speed;
            agent.updateRotation = false;
            agent.acceleration = 40f;
            agent.stoppingDistance = 0f;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            GameEvents.StatusApplied += OnStatusApplied;
        }

        public bool Armored { get; private set; }
        public EnemyScaling Scaling { get; private set; } = EnemyScaling.None;
        float Damage => definition.damage * Scaling.Damage;

        void OnStatusApplied(Health target, StatusType type, float duration)
        {
            if (target != health || type != StatusType.ArmorBroken || !Armored) return;
            Armored = false;
            health.Pool.ExtraDamageTakenMultiplier = 1f;
            GameEvents.RaiseArmorChanged(health, false);
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.GetComponent<Health>();
            Play("Spawn", 0f);
            GameEvents.RaiseEnemySpawned(health);
            if (definition.armored)
            {
                Armored = true;
                health.Pool.ExtraDamageTakenMultiplier = definition.armoredDamageTaken;
                GameEvents.RaiseArmorChanged(health, true);
            }
        }

        void OnDestroy()
        {
            GameEvents.StatusApplied -= OnStatusApplied;
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        /// Traz o inimigo de volta para um ponto valido do chao perto de around. Usado quando ele sai da sala,
        /// cai do NavMesh ou a luta trava: inimigo preso fora de alcance nunca pode segurar a sala.
        public bool Rescue(Vector3 around)
        {
            if (health.IsDead) return false;
            var want = Arena.Clamp(around);
            if (!NavMesh.SamplePosition(want, out var hit, 6f, NavMesh.AllAreas) &&
                !NavMesh.SamplePosition(Vector3.zero, out hit, 12f, NavMesh.AllAreas)) return false;
            knock = Vector3.zero;
            if (!agent.enabled) agent.enabled = true;
            if (!agent.Warp(hit.position)) transform.position = hit.position;
            Rescues++;
            return true;
        }

        public int Rescues { get; private set; }
        float safetyCheck;

        void OnDamaged(DamageInfo info, float applied)
        {
            knock += info.Direction * info.Knockback;
            // recua ao apanhar, mas so enquanto persegue: golpe ja avisado nao e cortado pela animacao
            if (health.IsDead || fsm.Phase != EnemyPhase.Chase || applied <= 0f) return;
            anim = null;
            Play("Hit", 0.03f);
            hitHold = 0.25f;
        }

        float hitHold;

        void OnDied(DamageInfo info)
        {
            if (fsm.Kill()) GameEvents.RaiseTelegraphCancelled(gameObject);
            if (agent.enabled) agent.enabled = false;
            if (body != null) body.enabled = false;
            Play("Death", 0.05f);
            Destroy(gameObject, 1.5f);
        }

        void Update()
        {
            if (health.IsDead) return;
            float dt = Time.deltaTime;
            if ((safetyCheck -= dt) <= 0f)
            {
                safetyCheck = 0.5f;
                if (!Arena.Contains(transform.position) || !agent.enabled || !agent.isOnNavMesh) Rescue(transform.position);
            }

            float moveMul = 1f, miss = 0f;
            bool canAct = true;
            if (TryGetComponent(out StatusHolder status))
            {
                moveMul = status.State.MoveMultiplier; canAct = status.State.CanAct; miss = status.State.MissChance;
            }

            bool hasTarget = target != null && !target.IsDead;
            var to = hasTarget ? target.transform.position - transform.position : transform.forward;
            to.y = 0f;
            float distance = hasTarget ? to.magnitude : float.MaxValue;
            var toDir = distance > 0.01f && hasTarget ? to / distance : transform.forward;

            switch (fsm.Tick(dt, distance, canAct, hasTarget))
            {
                case EnemyAction.StartWindup:
                    strikeDir = toDir;
                    strikePoint = transform.position + strikeDir * (definition.attack == EnemyAttackKind.Explode ? 0f : definition.strikeOffset);
                    var kind = definition.attack;
                    healing = false;
                    if (kind == EnemyAttackKind.Heal)
                    {
                        // so cura se houver alguem ferido por perto; sozinho, o Xama atira
                        healing = AnyWoundedAlly();
                        kind = healing ? EnemyAttackKind.Heal : EnemyAttackKind.Projectile;
                    }
                    if (kind == EnemyAttackKind.Lob && hasTarget) strikePoint = target.transform.position;
                    if (kind == EnemyAttackKind.Heal) strikePoint = transform.position;
                    bool line = kind == EnemyAttackKind.Projectile || kind == EnemyAttackKind.Charge;
                    bool charge = kind == EnemyAttackKind.Charge;
                    GameEvents.RaiseTelegraph(new TelegraphEvent
                    {
                        Point = line ? transform.position : strikePoint, Direction = strikeDir,
                        Radius = charge ? definition.chargeWidth : line ? 0.35f : kind == EnemyAttackKind.Heal ? definition.healRadius : definition.strikeRadius,
                        Length = charge ? ClearDistance(strikeDir, definition.chargeDistance) : line ? ClearDistance(strikeDir, definition.projectileRange) : 0f,
                        Duration = definition.telegraphTime, IsLine = line, Friendly = kind == EnemyAttackKind.Heal, Source = gameObject,
                    });
                    Play("Attack", 0.08f);
                    break;
                case EnemyAction.Strike:
                    Strike(miss);
                    break;
                case EnemyAction.Cancel:
                    GameEvents.RaiseTelegraphCancelled(gameObject);
                    break;
            }

            Move(dt, hasTarget, distance, toDir, canAct, moveMul);
            if (chargeLeft > 0f) Charge(dt, hasTarget, canAct);

            if (knock.sqrMagnitude > 0.01f)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.Move(knock * dt);
                knock = Vector3.Lerp(knock, Vector3.zero, 9f * dt);
            }

            var face = fsm.Phase == EnemyPhase.Windup || fsm.Phase == EnemyPhase.Recover ? strikeDir : toDir;
            if (canAct && face.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 540f * dt);

            if (hitHold > 0f) hitHold -= dt;
            else if (fsm.Phase == EnemyPhase.Chase)
                Play(!canAct ? "Idle" : agent.velocity.sqrMagnitude > 0.3f ? "Run" : "Idle", 0.12f);
        }

        void Move(float dt, bool hasTarget, float distance, Vector3 toDir, bool canAct, float moveMul)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            bool move = fsm.CanMove && canAct && hasTarget && moveMul > 0f;
            agent.speed = definition.moveSpeed * Scaling.Speed * moveMul;
            if (!move) { agent.isStopped = true; agent.velocity = Vector3.zero; return; }

            Vector3 goal = target.transform.position;
            bool stop = false;
            if (definition.attack == EnemyAttackKind.Projectile || definition.attack == EnemyAttackKind.Lob || definition.attack == EnemyAttackKind.Heal)
            {
                if (distance < definition.keepDistance) goal = transform.position - toDir * 4f;   // recua
                else if (distance <= definition.attackRange * 0.9f) stop = true;                 // ja esta em posicao
            }
            else if (distance <= Mathf.Min(definition.attackRange * 0.8f, definition.attack == EnemyAttackKind.Charge ? 3.5f : 999f)) stop = true;

            agent.isStopped = stop;
            if (stop) { agent.velocity = Vector3.zero; return; }
            if ((repath -= dt) > 0f) return;
            repath = 0.15f;
            agent.SetDestination(goal);
        }

        void Strike(float missChance)
        {
            if (missChance > 0f && Random.value < missChance) { GameEvents.RaiseAttackMissed(health); return; }
            if (definition.attack == EnemyAttackKind.Explode) { Explode(); return; }
            if (definition.attack == EnemyAttackKind.Charge)
            {
                chargeLeft = ClearDistance(strikeDir, definition.chargeDistance) / Mathf.Max(1f, definition.chargeSpeed);
                chargeHit = false;
                return;
            }
            if (definition.attack == EnemyAttackKind.Heal && healing) { HealAllies(); return; }
            if (definition.attack == EnemyAttackKind.Lob)
            {
                GameEvents.RaiseExploded(strikePoint, definition.strikeRadius);
                if (target == null || target.IsDead) return;
                var off = target.transform.position - strikePoint; off.y = 0f;
                if (off.magnitude > definition.strikeRadius + 0.3f) return;
                target.TakeDamage(new DamageInfo(Damage, definition.element, health.Team, gameObject)
                {
                    Point = target.transform.position + Vector3.up * 1.2f,
                    Direction = off.sqrMagnitude > 0.01f ? off.normalized : strikeDir, Knockback = definition.knockback,
                });
                return;
            }
            if (definition.attack == EnemyAttackKind.Projectile || definition.attack == EnemyAttackKind.Heal)
            {
                Projectile.Spawn(definition.projectilePrefab, transform.position + Vector3.up * 1.3f + strikeDir * 0.8f, strikeDir,
                    definition.projectileSpeed, definition.projectileRange, Damage, definition.knockback,
                    definition.element, health.Team, gameObject);
                return;
            }
            if (target == null || target.IsDead) return;
            var d = target.transform.position - strikePoint;
            d.y = 0f;
            if (d.magnitude > definition.strikeRadius + 0.4f) return; // o heroi saiu da area avisada
            target.TakeDamage(new DamageInfo(Damage, definition.element, health.Team, gameObject)
            {
                Point = target.transform.position + Vector3.up * 1.2f, Direction = strikeDir, Knockback = definition.knockback,
            });
        }

        /// Corre pela linha avisada. Fere o heroi uma vez se ele estiver no corredor.
        void Charge(float dt, bool hasTarget, bool canAct)
        {
            if (!canAct) { chargeLeft = 0f; return; } // atordoar ou congelar para a investida
            chargeLeft -= dt;
            if (agent.enabled && agent.isOnNavMesh) agent.Move(strikeDir * (definition.chargeSpeed * dt));
            if (chargeHit || !hasTarget) return;
            var d = target.transform.position - transform.position; d.y = 0f;
            if (d.magnitude > definition.chargeWidth + 0.7f) return;
            chargeHit = true;
            target.TakeDamage(new DamageInfo(Damage, definition.element, health.Team, gameObject)
            {
                Point = target.transform.position + Vector3.up * 1.2f, Direction = strikeDir, Knockback = definition.knockback,
            });
        }

        public bool IsCharging => chargeLeft > 0f;
        static readonly Collider[] allies = new Collider[32];

        bool AnyWoundedAlly()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, definition.healRadius, allies, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = allies[i].GetComponentInParent<Health>();
                if (h != null && h != health && h.Team == health.Team && !h.IsDead && h.Current < h.Max - 0.5f) return true;
            }
            return false;
        }

        void HealAllies()
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, definition.healRadius, allies, ~0, QueryTriggerInteraction.Ignore);
            var seen = new System.Collections.Generic.HashSet<Health>();
            for (int i = 0; i < n; i++)
            {
                var h = allies[i].GetComponentInParent<Health>();
                if (h == null || h == health || h.Team != health.Team || h.IsDead || !seen.Add(h)) continue;
                float got = h.Heal(definition.healAmount * Scaling.Health);
                if (got > 0f) GameEvents.RaiseEnemyHealed(h, got);
            }
        }

        static readonly Collider[] blast = new Collider[32];

        void Explode()
        {
            var center = transform.position;
            GameEvents.RaiseExploded(center, definition.strikeRadius);
            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, definition.strikeRadius, blast, ~0, QueryTriggerInteraction.Ignore);
            var seen = new System.Collections.Generic.HashSet<Health>();
            for (int i = 0; i < n; i++)
            {
                var h = blast[i].GetComponentInParent<Health>();
                if (h == null || h == health || h.IsDead || !seen.Add(h)) continue;
                var dir = h.transform.position - center; dir.y = 0f;
                dir = dir.sqrMagnitude > 0.01f ? dir.normalized : transform.forward;
                bool ally = h.Team == health.Team;
                // contra aliados o dano entra como se fosse do heroi: da pra usar o Explosivo contra o proprio bando
                h.TakeDamage(new DamageInfo(ally ? definition.explosionDamageToEnemies : Damage, definition.element,
                    ally ? Team.Player : health.Team, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, Direction = dir, Knockback = definition.knockback, IsReaction = true,
                });
            }
            health.TakeDamage(new DamageInfo(99999f, ElementType.None, Team.Player, gameObject) { IsReaction = true });
        }

        static readonly RaycastHit[] rayHits = new RaycastHit[16];

        /// Ate onde o tiro vai antes de bater em parede ou objeto do cenario (para o aviso nao atravessar a sala).
        float ClearDistance(Vector3 dir, float max)
        {
            int n = Physics.RaycastNonAlloc(transform.position + Vector3.up * 1.3f, dir, rayHits, max, ~0, QueryTriggerInteraction.Ignore);
            float best = max;
            for (int i = 0; i < n; i++)
            {
                if (rayHits[i].collider.GetComponentInParent<Health>() != null) continue;
                if (rayHits[i].distance < best) best = rayHits[i].distance;
            }
            return best;
        }

        void Play(string state, float fade)
        {
            if (animator == null || anim == state) return;
            anim = state;
            animator.CrossFadeInFixedTime(state, fade, 0, 0f);
        }
    }
}
