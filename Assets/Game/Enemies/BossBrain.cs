using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Quintessence.Enemies
{
    /// Guardiao das Cinzas: tres fases por vida, tres ataques avisados, invoca reforcos
    /// e, na ultima fase, um escudo que so cai com um golpe durante a Quintessencia.
    [RequireComponent(typeof(Health), typeof(NavMeshAgent))]
    public class BossBrain : MonoBehaviour
    {
        [SerializeField] BossDefinition definition;
        [SerializeField] Animator animator;

        public int Phase { get; private set; }
        public BossShield Shield { get; private set; }
        public BossDefinition Definition => definition;

        struct Strike { public float Time; public Vector3 Point, Direction; public float Radius, Damage; public bool Projectile; }

        Health health, target;
        IHeroStatus hero;
        NavMeshAgent agent;
        readonly List<Strike> strikes = new();
        readonly List<GameObject> adds = new();
        float spawnLeft = 1.2f, cooldown = 1.5f, busy, addTimer, chargeWait, chargeLeft;
        Vector3 chargeDir;
        bool chargeHit;
        public bool IsCharging => chargeLeft > 0f;
        int attackIndex;
        bool quintessence;
        string anim;

        public void Bind(BossDefinition def, Animator a) { definition = def; animator = a; }

        void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            agent.speed = definition.moveSpeed;
            agent.updateRotation = false;
            agent.acceleration = 30f;
            Shield = new BossShield(definition.shieldTimeout);
            health.Died += OnDied;
        }

        void OnEnable()
        {
            GameEvents.HitBlocked += OnBlocked;
            GameEvents.QuintessenceActiveChanged += OnQuintessence;
        }

        void OnDisable()
        {
            GameEvents.HitBlocked -= OnBlocked;
            GameEvents.QuintessenceActiveChanged -= OnQuintessence;
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) { target = p.GetComponent<Health>(); hero = p.GetComponent<IHeroStatus>(); }
            if (hero != null) quintessence = hero.QuintessenceActive;
            Play("Spawn", 0f);
            GameEvents.RaiseEnemySpawned(health);
            GameEvents.RaiseBossSpawned(health, definition.displayName);
            GameEvents.RaiseBossPhase(1, BossRules.Phases);
        }

        void OnQuintessence(bool active) => quintessence = active;

        void OnBlocked(Health who, DamageInfo info)
        {
            if (who != health || !Shield.Up) return;
            if (Shield.Hit(quintessence)) DropShield();
            else health.Pool.Blocks = 9999;
        }

        void DropShield()
        {
            health.Pool.Blocks = 0;
            GameEvents.RaiseArmorChanged(health, false);
            GameEvents.RaiseBossShield(false, false);
        }

        void OnDied(DamageInfo info)
        {
            strikes.Clear();
            chargeWait = chargeLeft = 0f;
            if (agent.enabled) agent.enabled = false;
            if (TryGetComponent(out Collider c)) c.enabled = false;
            // os reforcos caem junto com o chefe: a sala nunca fica presa esperando um inimigo esquecido
            foreach (var a in adds)
                if (a != null && a.TryGetComponent(out Health h) && !h.IsDead)
                    h.TakeDamage(new DamageInfo(99999f, ElementType.None, Team.Player, gameObject) { IsReaction = true });
            if (Shield.Up) GameEvents.RaiseBossShield(false, false);
            Play("Death", 0.05f);
            Destroy(gameObject, 2.5f);
        }

        void Update()
        {
            if (health.IsDead) return;
            float dt = Time.deltaTime;
            if (spawnLeft > 0f) { spawnLeft -= dt; return; }

            int phase = BossRules.PhaseFor(health.Current / health.Max);
            if (phase > Phase) EnterPhase(phase);
            if (Shield.Tick(dt)) DropShield();
            if (Shield.Up && (addTimer -= dt) <= 0f) { addTimer = definition.addInterval; Summon(1); }

            bool hasTarget = target != null && !target.IsDead;
            var to = hasTarget ? target.transform.position - transform.position : transform.forward;
            to.y = 0f;
            float distance = hasTarget ? to.magnitude : 999f;
            var dir = distance > 0.01f ? to.normalized : transform.forward;

            for (int i = strikes.Count - 1; i >= 0; i--)
            {
                var s = strikes[i];
                s.Time -= dt;
                if (s.Time > 0f) { strikes[i] = s; continue; }
                strikes.RemoveAt(i);
                Resolve(s);
            }

            if (chargeWait > 0f && (chargeWait -= dt) <= 0f)
            {
                chargeLeft = ClearLength(chargeDir, definition.chargeLength) / Mathf.Max(1f, definition.chargeSpeed);
                chargeHit = false;
            }
            if (chargeLeft > 0f)
            {
                chargeLeft -= dt;
                if (agent.enabled && agent.isOnNavMesh) agent.Move(chargeDir * (definition.chargeSpeed * dt));
                if (!chargeHit && hasTarget && distance < definition.chargeWidth + 1.2f)
                {
                    chargeHit = true;
                    target.TakeDamage(new DamageInfo(definition.chargeDamage, ElementType.None, health.Team, gameObject)
                    {
                        Point = target.transform.position + Vector3.up * 1.2f, Direction = chargeDir, Knockback = 10f,
                    });
                }
            }

            if (busy > 0f)
            {
                busy -= dt;
                Stop();
            }
            else
            {
                cooldown -= dt;
                if (hasTarget && cooldown <= 0f) Attack(dir, distance);
                else Chase(hasTarget, distance);
                if (busy <= 0f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 240f * dt);
                Play(agent.enabled && agent.velocity.sqrMagnitude > 0.2f ? "Run" : "Idle", 0.15f);
            }
        }

        void EnterPhase(int phase)
        {
            Phase = phase;
            GameEvents.RaiseBossPhase(phase + 1, BossRules.Phases);
            Summon(definition.addsPerPhaseChange);
            cooldown = 1.2f;
            // rugido na troca de fase: ele para um instante, e e a deixa para o jogador se reposicionar
            Play("Roar", 0.08f);
            busy = Mathf.Max(busy, 0.9f);
            if (phase < BossRules.Phases - 1 || !definition.lastPhaseShield) return;
            bool canUse = hero != null && hero.AwakenedElements.Count >= 4;
            Shield.Raise(canUse);
            health.Pool.Blocks = 9999;
            addTimer = definition.addInterval;
            GameEvents.RaiseArmorChanged(health, true);
            GameEvents.RaiseBossShield(true, canUse);
        }

        void Summon(int count)
        {
            adds.RemoveAll(a => a == null);
            if (definition.addPrefabs == null || definition.addPrefabs.Length == 0) return;
            for (int i = 0; i < count && adds.Count < definition.maxAdds; i++)
            {
                var around = Quaternion.Euler(0, Random.Range(0f, 360f), 0) * Vector3.forward * Random.Range(4f, 6f);
                var pos = transform.position + around;
                if (NavMesh.SamplePosition(pos, out var hit, 3f, NavMesh.AllAreas)) pos = hit.position;
                var prefab = definition.addPrefabs[Random.Range(0, definition.addPrefabs.Length)];
                adds.Add(Instantiate(prefab, pos, Quaternion.identity, transform.parent));
            }
        }

        void Chase(bool hasTarget, float distance)
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            if (!hasTarget || distance < definition.slamRange * 0.7f) { Stop(); return; }
            agent.isStopped = false;
            agent.SetDestination(target.transform.position);
        }

        void Stop()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        void Attack(Vector3 dir, float distance)
        {
            var attack = BossRules.Next(Phase, attackIndex++, distance <= definition.slamRange);
            cooldown = definition.cooldownByPhase[Mathf.Clamp(Phase, 0, definition.cooldownByPhase.Length - 1)];
            transform.rotation = Quaternion.LookRotation(dir);
            switch (attack)
            {
                case BossAttack.Slam:
                {
                    var p = transform.position + dir * definition.slamOffset;
                    Circle(p, definition.slamRadius, definition.slamWindup, definition.slamDamage, dir);
                    busy = definition.slamWindup + 0.5f;
                    Play("Attack", 0.08f);
                    break;
                }
                case BossAttack.Fan:
                {
                    int count = Phase >= BossRules.Phases - 1 ? definition.fanCountLastPhase : definition.fanCount;
                    foreach (var d in BossRules.FanDirections(dir, count, definition.fanSpread))
                    {
                        GameEvents.RaiseTelegraph(new TelegraphEvent
                        {
                            Point = transform.position, Direction = d, Radius = 0.4f, Length = definition.projectileRange,
                            Duration = definition.fanWindup, IsLine = true,
                        });
                        strikes.Add(new Strike { Time = definition.fanWindup, Direction = d, Damage = definition.fanDamage, Projectile = true });
                    }
                    busy = definition.fanWindup + 0.4f;
                    Play("Cast", 0.08f);
                    break;
                }
                case BossAttack.Charge:
                {
                    chargeDir = dir;
                    float len = ClearLength(dir, definition.chargeLength);
                    GameEvents.RaiseTelegraph(new TelegraphEvent
                    {
                        Point = transform.position, Direction = dir, Radius = definition.chargeWidth, Length = len,
                        Duration = definition.chargeWindup, IsLine = true, Source = gameObject,
                    });
                    chargeWait = definition.chargeWindup;
                    busy = definition.chargeWindup + len / Mathf.Max(1f, definition.chargeSpeed) + 0.9f; // fica exposto depois de atravessar
                    Play("Roar", 0.08f);
                    break;
                }
                case BossAttack.Ring:
                {
                    // dois aneis: o de dentro primeiro, o de fora logo depois. Entre eles ha uma faixa segura
                    float turn = Random.Range(0f, 45f);
                    foreach (var p in BossRules.RingPoints(transform.position, definition.ringRadius, definition.ringCount, turn))
                        Circle(p, definition.ringCircleRadius, definition.ringWindup, definition.ringDamage, Vector3.up);
                    foreach (var p in BossRules.RingPoints(transform.position, definition.ringRadius * 2.05f, definition.ringCount + 4, turn + 20f))
                        Circle(p, definition.ringCircleRadius, definition.ringWindup + 0.55f, definition.ringDamage, Vector3.up);
                    busy = definition.ringWindup + 0.9f;
                    Play("Summon", 0.08f);
                    break;
                }
                case BossAttack.Eruption:
                {
                    int count = definition.eruptionCount + Phase;
                    for (int i = 0; i < count; i++)
                    {
                        var off = i == 0 ? Vector2.zero : Random.insideUnitCircle * definition.eruptionScatter;
                        var p = target.transform.position + new Vector3(off.x, 0f, off.y);
                        Circle(p, definition.eruptionRadius, definition.eruptionWindup + i * 0.12f, definition.eruptionDamage, Vector3.up);
                    }
                    busy = definition.eruptionWindup + 0.3f;
                    Play("Summon", 0.08f);
                    break;
                }
            }
        }

        static readonly RaycastHit[] rayHits = new RaycastHit[16];

        /// Ate onde a investida vai antes de bater na parede.
        float ClearLength(Vector3 dir, float max)
        {
            int n = Physics.RaycastNonAlloc(transform.position + Vector3.up * 1.3f, dir, rayHits, max, ~0, QueryTriggerInteraction.Ignore);
            float best = max;
            for (int i = 0; i < n; i++)
            {
                if (rayHits[i].collider.GetComponentInParent<Health>() != null) continue;
                if (rayHits[i].distance < best) best = rayHits[i].distance;
            }
            return Mathf.Max(2f, best - 1.5f);
        }

        void Circle(Vector3 point, float radius, float windup, float damage, Vector3 dir)
        {
            GameEvents.RaiseTelegraph(new TelegraphEvent { Point = point, Direction = dir, Radius = radius, Duration = windup });
            strikes.Add(new Strike { Time = windup, Point = point, Radius = radius, Damage = damage, Direction = dir });
        }

        void Resolve(Strike s)
        {
            if (s.Projectile)
            {
                Projectile.Spawn(definition.projectilePrefab, transform.position + Vector3.up * 1.6f + s.Direction * 1.4f, s.Direction,
                    definition.projectileSpeed, definition.projectileRange, s.Damage, 3f, ElementType.Fire, health.Team, gameObject);
                return;
            }
            GameEvents.RaiseExploded(s.Point, s.Radius);
            if (target == null || target.IsDead) return;
            var d = target.transform.position - s.Point;
            d.y = 0f;
            if (d.magnitude > s.Radius + 0.3f) return;
            target.TakeDamage(new DamageInfo(s.Damage, ElementType.Fire, health.Team, gameObject)
            {
                Point = target.transform.position + Vector3.up * 1.2f, Direction = d.sqrMagnitude > 0.01f ? d.normalized : transform.forward, Knockback = 6f,
            });
        }

        void Play(string state, float fade)
        {
            if (animator == null || anim == state) return;
            anim = state;
            animator.CrossFadeInFixedTime(state, fade, 0, 0f);
        }
    }
}
