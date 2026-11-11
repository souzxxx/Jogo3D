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
        float repath;

        public void Bind(EnemyDefinition def, Animator anim) { definition = def; animator = anim; }

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            body = GetComponent<Collider>();
            fsm = new EnemyFsm(definition.telegraphTime, definition.recoverTime, definition.cooldown, definition.attackRange);
            agent.speed = definition.moveSpeed;
            agent.updateRotation = false;
            agent.acceleration = 40f;
            agent.stoppingDistance = 0f;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.GetComponent<Health>();
            Play("Spawn", 0f);
            GameEvents.RaiseEnemySpawned(health);
        }

        void OnDestroy()
        {
            if (health == null) return;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        void OnDamaged(DamageInfo info, float applied) => knock += info.Direction * info.Knockback;

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
                    strikePoint = transform.position + strikeDir * definition.strikeOffset;
                    bool line = definition.attack == EnemyAttackKind.Projectile;
                    GameEvents.RaiseTelegraph(new TelegraphEvent
                    {
                        Point = line ? transform.position : strikePoint, Direction = strikeDir,
                        Radius = line ? 0.35f : definition.strikeRadius, Length = line ? ClearDistance(strikeDir, definition.projectileRange) : 0f,
                        Duration = definition.telegraphTime, IsLine = line, Source = gameObject,
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

            if (knock.sqrMagnitude > 0.01f)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.Move(knock * dt);
                knock = Vector3.Lerp(knock, Vector3.zero, 9f * dt);
            }

            var face = fsm.Phase == EnemyPhase.Windup || fsm.Phase == EnemyPhase.Recover ? strikeDir : toDir;
            if (canAct && face.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face), 540f * dt);

            if (fsm.Phase == EnemyPhase.Chase)
                Play(!canAct ? "Idle" : agent.velocity.sqrMagnitude > 0.3f ? "Run" : "Idle", 0.12f);
        }

        void Move(float dt, bool hasTarget, float distance, Vector3 toDir, bool canAct, float moveMul)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            bool move = fsm.CanMove && canAct && hasTarget && moveMul > 0f;
            agent.speed = definition.moveSpeed * moveMul;
            if (!move) { agent.isStopped = true; agent.velocity = Vector3.zero; return; }

            Vector3 goal = target.transform.position;
            bool stop = false;
            if (definition.attack == EnemyAttackKind.Projectile)
            {
                if (distance < definition.keepDistance) goal = transform.position - toDir * 4f;   // recua
                else if (distance <= definition.attackRange * 0.9f) stop = true;                 // ja esta em posicao
            }
            else if (distance <= definition.attackRange * 0.8f) stop = true;

            agent.isStopped = stop;
            if (stop) { agent.velocity = Vector3.zero; return; }
            if ((repath -= dt) > 0f) return;
            repath = 0.15f;
            agent.SetDestination(goal);
        }

        void Strike(float missChance)
        {
            if (missChance > 0f && Random.value < missChance) { GameEvents.RaiseAttackMissed(health); return; }
            if (definition.attack == EnemyAttackKind.Projectile)
            {
                Projectile.Spawn(definition.projectilePrefab, transform.position + Vector3.up * 1.3f + strikeDir * 0.8f, strikeDir,
                    definition.projectileSpeed, definition.projectileRange, definition.damage, definition.knockback,
                    definition.element, health.Team, gameObject);
                return;
            }
            if (target == null || target.IsDead) return;
            var d = target.transform.position - strikePoint;
            d.y = 0f;
            if (d.magnitude > definition.strikeRadius + 0.4f) return; // o heroi saiu da area avisada
            target.TakeDamage(new DamageInfo(definition.damage, definition.element, health.Team, gameObject)
            {
                Point = target.transform.position + Vector3.up * 1.2f, Direction = strikeDir, Knockback = definition.knockback,
            });
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
