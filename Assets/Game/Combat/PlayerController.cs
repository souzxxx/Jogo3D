using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Heroi: anda, mira livre do movimento, combo neutro, dash, uma skill por elemento desperto e Quintessencia.
    /// O golpe basico nao tem elemento: quem marca e reage sao as skills, combinadas entre si.
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
            // sem elemento no golpe basico, o combo carrega mais dano proprio (era 6, 6, 10)
            new(8f, 0.30f, 0.12f, 2f),
            new(8f, 0.30f, 0.12f, 2f),
            new(13f, 0.45f, 0.18f, 5f),
        };
        [SerializeField] float attackRange = 3.2f;
        [SerializeField, Range(10f, 360f)] float attackArc = 120f;
        [SerializeField] float critMultiplier = 1.5f;
        [SerializeField, Range(0f, 1f)] float baseCritChance = 0.05f;
        [Tooltip("Bonus do golpe basico em inimigo que ja carrega a marca de um elemento.")]
        [SerializeField] float markedTargetBonus = 0.15f;

        [Header("Papel de cada skill")]
        [Tooltip("Mare: fracao da vida maxima curada ao lancar. Tambem limpa status negativos.")]
        [SerializeField, Range(0f, 0.5f)] float waterSkillHeal = 0.08f;
        [Tooltip("Pilar de Pedra: por quantos segundos o proximo golpe recebido e bloqueado.")]
        [SerializeField] float earthSkillGuard = 3f;

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
        /// Kit de golpe basico em uso (nulo ate a run escolher o heroi: vale o que esta no Inspector).
        public HeroKit Kit { get; private set; }
        public float AttackRange => attackRange;
        public float AttackArc => attackArc;
        public DashState Dash => dash;
        public ComboState Combo => combo;
        public LoadoutState Loadout => loadout;
        public QuintessenceState Quintessence => quint;
        /// Elemento da ultima skill usada (cor do heroi, HUD). O golpe basico nao depende dele.
        public ElementType Element { get; private set; } = ElementType.Fire;
        /// Alguma skill desperta esta fora de recarga.
        public bool AnySkillReady
        {
            get { foreach (var e in loadout.Awakened) if (skillCd[(int)e] <= 0f) return true; return false; }
        }

        // IHeroStatus
        public Health Health => health;
        public ElementType CurrentElement => Element;
        public IReadOnlyList<ElementType> AwakenedElements => loadout.Awakened;
        public int DashCharges => dash.Charges;
        public int DashMaxCharges => dash.MaxCharges;
        public float Quintessence01 => quint.Normalized;
        public bool QuintessenceActive => quint.Active;
        public bool QuintessenceReady => quint.Ready && loadout.HasAll;
        public HeroModifiers Modifiers => mods;
        readonly HeroModifiers mods = new();

        /// indice do golpe no combo
        public event Action<int> AttackStarted;
        /// (indice do golpe, alvos atingidos)
        public event Action<int, int> AttackHit;
        public event Action<Vector3> DashStarted;
        public event Action<ElementType> SkillStarted;
        /// Pediu a skill de um elemento que ainda nao despertou.
        public event Action<ElementType> SkillDenied;

        CharacterController body;
        Health health;
        DashState dash;
        ComboState combo;
        LoadoutState loadout;
        readonly QuintessenceState quint = new();
        ElementDefinition fallback, castDef;
        Vector3 dashDir, castPoint;
        bool wasDashing, castHit;
        float castLeft, castT, dashSpeed, armorLeft, hasteLeft;
        int quintCycle;
        const float InputBuffer = 0.25f;
        float dashBuffer, skillBuffer;
        ElementType skillWanted, castElement;
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
            Element = startingElement;
            foreach (var e in alsoAwakened) loadout.Awaken(e);
            fallback = ScriptableObject.CreateInstance<ElementDefinition>();
            Input ??= GetComponent<IPlayerInput>();
            if (TryGetComponent(out HeroSkin skin)) skin.Show(startingElement);
        }

        void OnEnable()
        {
            GameEvents.ReactionTriggered += OnReaction;
            GameEvents.QuintessenceCharge += OnCharge;
            GameEvents.HitBlocked += OnBlocked;
            GameEvents.Died += OnAnyDied;
        }

        void OnDisable()
        {
            ElementMark.SpreadRadius = 0f;
            GameEvents.ReactionTriggered -= OnReaction;
            GameEvents.QuintessenceCharge -= OnCharge;
            GameEvents.HitBlocked -= OnBlocked;
            GameEvents.Died -= OnAnyDied;
        }

        /// Espinhos de Pedra: o golpe que o Pilar bloqueou volta como dano de Terra em volta do heroi.
        void OnBlocked(Health target, DamageInfo info)
        {
            if (target != health || mods.EarthThorns <= 0f) return;
            thorns = true;
            ShapedHit(ElementType.Earth, transform.position, AimDirection, 3.2f, 360f, mods.EarthThorns, 5f, AttackKind.Skill, 0f, false, default, 0f);
            thorns = false;
        }

        bool thorns;
        /// A explosao do despertar usa o caminho das skills, mas nao leva efeitos da ultima skill (Agua Gelada).
        bool bursting;

        /// Impeto: cada inimigo derrotado adianta as recargas.
        void OnAnyDied(Health target, DamageInfo info)
        {
            if (mods.SkillRefundOnKill <= 0f || target == health || target.Team == health.Team) return;
            // so criatura conta como abate (barril de polvora e boneco de treino nao andam pelo NavMesh)
            if (target.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) return;
            for (int i = 0; i < skillCd.Length; i++) if (skillCd[i] > 0f) skillCd[i] -= mods.SkillRefundOnKill;
        }

        void Start()
        {
            foreach (var e in loadout.Awakened) GameEvents.RaiseElementAwakened(e);
            GameEvents.RaiseElementSwitched(ElementType.None, Element);
            GameEvents.RaiseQuintessenceMeter(0f);
        }

        void OnReaction(ReactionEvent e)
        {
            if (e.Source != gameObject) return;
            if (mods.HealOnReaction > 0f) health.Heal(mods.HealOnReaction);
            if (quint.Active) return;
            quint.AddMeter(QuintessenceState.PerReaction * mods.QuintessenceGainMult);
            GameEvents.RaiseQuintessenceMeter(quint.Normalized);
        }

        void OnCharge(float amount)
        {
            if (quint.Active || health.IsDead) return;
            quint.AddMeter(amount * mods.QuintessenceGainMult);
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
            float cd = Def(e).skillCooldown * mods.SkillCooldown(e);
            return cd <= 0f ? 0f : Mathf.Clamp01(skillCd[(int)e] / cd);
        }

        /// Comeco de run: o heroi volta a ter so o elemento inicial.
        ElementType hero;
        /// Quantas explosoes de despertar sairam (para os testes).
        public int Bursts { get; private set; }

        /// Despertar da Quintessencia: a explosao do heroi em volta dele. Ja com a Quintessencia ativa, reage com tudo que pega.
        void QuintessenceBurst()
        {
            // sem kit escolhido (cenas de teste), vale o do elemento inicial do Inspector
            var who = hero != ElementType.None ? hero : startingElement;
            var b = (Kit ?? HeroKit.Of(who)).Burst;
            if (b.Damage <= 0f) return;
            Bursts++;
            bursting = true;
            ShapedHit(who, transform.position, AimDirection, b.Radius, 360f, b.Damage * (1f + mods.BurstBonus), b.Knockback, AttackKind.Skill, 0f, b.Applies, b.Status, b.StatusTime);
            bursting = false;
            if (b.Heal > 0f) health.Heal(health.Max * b.Heal);
            GameEvents.RaiseQuintessenceBurst(b.Name, transform.position, b.Radius, who);
        }

        public void ResetLoadout(ElementType starting)
        {
            loadout = new LoadoutState(starting);
            Element = starting;
            hero = starting;
            mods.Reset();
            for (int i = 0; i < skillCd.Length; i++) skillCd[i] = 0f;
            if (TryGetComponent(out HeroSkin skin)) skin.Show(starting);
            // cada heroi golpeia do seu jeito: ritmo, alcance e abertura do combo vem do kit dele
            Kit = HeroKit.Of(starting);
            combo = new ComboState(Kit.Steps);
            attackRange = Kit.Range; attackArc = Kit.Arc; baseCritChance = Kit.CritChance;
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
            // pausado (menu, inventario, escolha de talento) ou no quadro em que a pausa saiu: nenhum comando vira acao
            ElementMark.SpreadRadius = mods.MarkSpread; // Contagio: as marcas dos inimigos leem daqui
            if (GameClock.Paused || Time.frameCount == GameClock.ResumedFrame) { dashBuffer = skillBuffer = 0f; return; }
            float dt = Time.deltaTime;
            for (int i = 0; i < skillCd.Length; i++) if (skillCd[i] > 0f) skillCd[i] -= dt;
            if (armorLeft > 0f && (armorLeft -= dt) <= 0f) health.Pool.Blocks = 0;

            if (Input.ConsumeQuintessence() && quint.TryActivate(loadout.HasAll))
            {
                health.Pool.ExtraDamageTakenMultiplier = QuintessenceState.DamageTaken;
                GameEvents.RaiseQuintessenceActive(true);
                QuintessenceBurst();
            }
            if (quint.Active) GameEvents.RaiseQuintessenceMeter(quint.Normalized);
            if (quint.Tick(dt))
            {
                health.Pool.ExtraDamageTakenMultiplier = 1f;
                GameEvents.RaiseQuintessenceActive(false);
                GameEvents.RaiseQuintessenceMeter(0f);
            }

            dash.Cooldown = dashCooldown * mods.DashCooldownMult;
            if (dash.MaxCharges != 1 + mods.DashChargesAdd) dash.SetMaxCharges(1 + mods.DashChargesAdd);
            health.Pool.Evasion = mods.Evasion;
            float moveMul = mods.MoveSpeedMult;
            if (hasteLeft > 0f) { hasteLeft -= dt; moveMul *= 1.35f; }
            bool canAct = true;
            if (TryGetComponent(out StatusHolder status))
            {
                // Pes Firmes: encharcado nao deixa lento (o resto do status vale)
                float statusMove = status.State.MoveMultiplier;
                if (mods.SureFooted && statusMove > 0f && statusMove < 1f) statusMove = 1f;
                moveMul *= statusMove; canAct = status.State.CanAct;
            }

            var mv = Input.Move;
            var moveDir = Vector3.ClampMagnitude(new Vector3(mv.x, 0f, mv.y), 1f);
            var toAim = Input.AimPoint - transform.position;
            toAim.y = 0f;
            if (toAim.sqrMagnitude > 0.04f) AimDirection = toAim.normalized;

            // comandos apertados no meio de um golpe ficam guardados por um instante em vez de se perderem
            if (Input.ConsumeDash()) dashBuffer = InputBuffer;
            // 1 a 4 pedem a skill daquele elemento; o atalho generico pede a primeira que estiver pronta
            int slot = Input.ConsumeElementSkill();
            if (slot >= 1 && slot <= 4) { skillBuffer = InputBuffer; skillWanted = Elements.All[slot - 1]; }
            else if (Input.ConsumeSkill()) { skillBuffer = InputBuffer; skillWanted = ElementType.None; }
            dashBuffer -= dt; skillBuffer -= dt;
            bool dashPressed = dashBuffer > 0f;
            bool skillPressed = skillBuffer > 0f;
            bool free = canAct && castLeft <= 0f && (!combo.IsAttacking || combo.PastHit);

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
                var cast = skillPressed ? PickSkill(skillWanted) : ElementType.None;
                // pediu skill de elemento ainda nao desperto: o pedido cai na hora, sem ficar guardado
                if (skillPressed && skillWanted != ElementType.None && !Has(skillWanted)) { skillBuffer = 0f; SkillDenied?.Invoke(skillWanted); }
                int ei = (int)cast;
                if (cast != ElementType.None && (!combo.IsAttacking || combo.PastHit))
                {
                    skillBuffer = 0f;
                    combo.Cancel();
                    castElement = cast;
                    if (cast != Element)
                    {
                        var from = Element;
                        Element = cast;
                        GameEvents.RaiseElementSwitched(from, cast);
                    }
                    castDef = Def(cast);
                    castPoint = transform.position + AimDirection * castDef.skillSummonDistance;
                    castLeft = castDef.skillCastTime;
                    castT = 0f;
                    castHit = false;
                    skillCd[ei] = castDef.skillCooldown * mods.SkillCooldown(cast);
                    SkillStarted?.Invoke(cast);
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
            float distance = dashDistance;
            dashSpeed = distance / dashDuration;
            DashStarted?.Invoke(dashDir);
            GameEvents.RaiseAttackPerformed(new AttackEvent
            {
                Origin = transform.position, Direction = dashDir, Element = ElementType.None, Range = distance,
                Kind = AttackKind.Dash, Source = gameObject,
            });
        }

        void DoBasicHit()
        {
            var step = combo.Current;
            bool finisher = combo.CurrentIndex == combo.StepCount - 1;
            // golpe basico: sem elemento, sem marca e sem status. Ele bate mais forte em quem ja esta marcado.
            float boost = mods.BasicDamageMult * (finisher ? mods.FinisherMult : 1f);
            int hits = ShapedHit(ElementType.None, transform.position, AimDirection, attackRange, attackArc, step.Damage * boost,
                step.Knockback * (finisher ? mods.FinisherMult : 1f),
                finisher ? AttackKind.Finisher : AttackKind.Basic, baseCritChance, false, default, 0f);
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
            // alem do proprio status, a skill aplica o efeito de assinatura do elemento (a Terra quebra armadura)
            bool second = d.finisherAppliesStatus && (!d.skillAppliesStatus || d.finisherStatus != d.skillStatus);
            float reach = d.skillRange * mods.SkillRange(castElement);
            ShapedHit(castElement, origin, AimDirection, reach, arc, d.skillDamage * d.damageMultiplier, d.skillKnockback,
                AttackKind.Skill, d.critChance, d.skillAppliesStatus, d.skillStatus, d.skillStatusDuration,
                second, d.finisherStatus, d.finisherStatusDuration);

            TryFuse(origin, reach, arc);

            // o papel de cada elemento alem do dano: Agua cuida, Terra protege, Ar e a fuga
            switch (castElement)
            {
                case ElementType.Fire:
                    if (mods.FireTrailSeconds > 0f)
                        DamageZone.Spawn(origin + AimDirection * (reach * 0.55f), reach * 0.45f, mods.FireTrailSeconds, 0.5f, 4f * mods.Damage(ElementType.Fire),
                            ElementType.Fire, health.Team, gameObject, reactions).WithStatus(StatusType.Burn, 2f).WithModifiers(mods);
                    break;
                case ElementType.Water:
                    if (waterSkillHeal + mods.WaterHealAdd > 0f) health.Heal(health.Max * (waterSkillHeal + mods.WaterHealAdd));
                    if (TryGetComponent(out StatusHolder own)) own.ClearNegative();
                    break;
                case ElementType.Earth:
                    health.Pool.Blocks = Mathf.Max(health.Pool.Blocks, 1 + mods.EarthBlocksAdd);
                    armorLeft = earthSkillGuard;
                    break;
                case ElementType.Air:
                    dash.Refill();
                    if (mods.AirHasteSeconds > 0f) hasteLeft = mods.AirHasteSeconds;
                    break;
            }
        }

        /// Quantas fusoes este heroi ja fez (para os testes).
        public int Fusions { get; private set; }

        /// A skill pegou uma area de reacao de outros dois elementos: funde.
        void TryFuse(Vector3 origin, float reach, float arc)
        {
            var zones = new List<DamageZone>(DamageZone.Active);
            foreach (var z in zones)
            {
                if (z == null || !z.Fusable) continue;
                var to = z.transform.position - origin; to.y = 0f;
                if (to.magnitude > reach + z.Radius * 0.6f) continue;
                if (arc < 359f && to.sqrMagnitude > 1f && Vector3.Angle(AimDirection, to) > arc * 0.5f + 15f) continue;
                if (ElementCombat.Fuse(z, castElement, health.Team, gameObject, mods.ReactionPower * (1f + mods.FusionBonus))) Fusions++;
            }
        }

        bool Has(ElementType e)
        {
            foreach (var a in loadout.Awakened) if (a == e) return true;
            return false;
        }

        /// A skill pedida, se estiver desperta e pronta; sem pedido especifico, a primeira pronta.
        ElementType PickSkill(ElementType wanted)
        {
            if (wanted != ElementType.None) return Has(wanted) && skillCd[(int)wanted] <= 0f ? wanted : ElementType.None;
            foreach (var e in loadout.Awakened) if (skillCd[(int)e] <= 0f) return e;
            return ElementType.None;
        }

        int ShapedHit(ElementType element, Vector3 from, Vector3 direction, float range, float arc, float damage, float knockback, AttackKind kind,
            float critChance, bool applyStatus, StatusType statusType, float statusDuration,
            bool applySecond = false, StatusType secondType = default, float secondDuration = 0f)
        {
            bool basic = element == ElementType.None;
            // Quintessencia: cada golpe usa o proximo elemento e reage como se o alvo ja estivesse marcado
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
                Kind = kind, Source = gameObject,
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
                float chance = critChance + mods.CritChanceAdd;
                bool crit = chance > 0f && UnityEngine.Random.value < chance;
                float marked = basic && target.TryGetComponent(out ElementMark m) && m.State.HasMark ? 1f + markedTargetBonus + mods.SwitchStrikeAdd : 1f;
                if (mods.BurningBonus > 0f && target.TryGetComponent(out StatusHolder burning) && burning.State.Has(StatusType.Burn)) marked *= 1f + mods.BurningBonus;
                // Flanqueador: o heroi esta atras de quem ele acerta (medido do heroi, nao do ponto da skill: o Pilar nasce longe)
                if (mods.FlankBonus > 0f && target.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
                {
                    var fromHero = target.transform.position - transform.position; fromHero.y = 0f;
                    if (fromHero.sqrMagnitude > 0.01f && Vector3.Dot(target.transform.forward, fromHero.normalized) > 0.35f) marked *= 1f + mods.FlankBonus;
                }
                var info = new DamageInfo(damage * mods.Damage(element) * marked * (crit ? critMultiplier : 1f), element, health.Team, gameObject)
                {
                    Point = overlap[i].ClosestPoint(origin),
                    Direction = to.sqrMagnitude > 0.001f ? to.normalized : direction,
                    Knockback = knockback,
                    IsCrit = crit,
                };
                if (ElementCombat.Deliver(target, info, reactions, mods.ReactionPower, force, mods) <= 0f) continue;
                hits++;
                if (applyStatus && !target.IsDead)
                    StatusHolder.GetOrAdd(target).Apply(statusType, statusDuration * mods.StatusDurationMult, health.Team, gameObject);
                if (kind == AttackKind.Skill && !thorns && !bursting && castElement == ElementType.Water && mods.WaterFreezeSeconds > 0f && !target.IsDead)
                    StatusHolder.GetOrAdd(target).Apply(StatusType.Frozen, mods.WaterFreezeSeconds, health.Team, gameObject);
                if (applySecond && !target.IsDead)
                    StatusHolder.GetOrAdd(target).Apply(secondType, secondDuration * mods.StatusDurationMult, health.Team, gameObject);
            }
            return hits;
        }
    }
}
