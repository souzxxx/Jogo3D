using System.Collections;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// Escuta o GameEvents e traduz tudo em feedback no mundo: flash, numero de dano, particulas, luz, tremor e pausa de impacto.
    public class CombatFeedback : MonoBehaviour
    {
        [Tooltip("Material aditivo de particula (URP Particles/Unlit).")]
        [SerializeField] Material particleMaterial;
        [Tooltip("Material transparente sem luz para anel e barras (Sprites/Default).")]
        [SerializeField] Material flatMaterial;
        [SerializeField] bool hitStop = true;

        public Material FlatMaterial => flatMaterial;
        public static Mesh RingMesh { get; private set; }
        public static Mesh DiscMesh { get; private set; }
        readonly Dictionary<GameObject, TelegraphVisual> telegraphs = new();
        readonly Dictionary<Health, float> affinityLabel = new();
        readonly Dictionary<Health, float> guardLabel = new();

        ParticleSystem fx;
        readonly Dictionary<Health, TargetDisplay> displays = new();
        readonly List<(Light light, float left, float total, float intensity)> flashes = new();
        readonly List<Light> lightPool = new();
        readonly List<(ZoneEvent zone, float left)> zones = new();
        Transform player, playerRing;
        Material playerRingMat;
        Light presence;
        Color presenceTarget = Color.white;
        float zoneEmit, presenceBase = 1.6f, quintEmit;
        bool quintessence;
        ElementType current;
        Coroutine stop;

        public void Configure(Material particle, Material flat) { particleMaterial = particle; flatMaterial = flat; }

        void Awake()
        {
            if (RingMesh == null) RingMesh = BuildRing(0.9f, 1f, 48);
            if (DiscMesh == null) DiscMesh = BuildRing(0f, 1f, 40);
            var go = new GameObject("FxParticles");
            go.transform.SetParent(transform, false);
            fx = go.AddComponent<ParticleSystem>();
            fx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = fx.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 3000;
            main.playOnAwake = false;
            main.loop = false;
            var em = fx.emission; em.enabled = false;
            var sh = fx.shape; sh.enabled = false;
            var col = fx.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0.7f, 0), new GradientAlphaKey(0.55f, 0.5f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = fx.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, 0.15f));
            var lim = fx.limitVelocityOverLifetime; lim.enabled = true; lim.dampen = 0.06f; lim.limit = 0.5f;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
            fx.Play();
            if (particleMaterial != null) accents = new ElementAccents(transform, particleMaterial);
        }

        void OnEnable()
        {
            GameEvents.DamageDealt += OnDamage;
            GameEvents.Died += OnDied;
            GameEvents.MarkApplied += OnMark;
            GameEvents.MarkCleared += OnMarkCleared;
            GameEvents.ReactionTriggered += OnReaction;
            GameEvents.StatusApplied += OnStatus;
            GameEvents.StatusEnded += OnStatusEnded;
            GameEvents.AttackPerformed += OnAttack;
            GameEvents.ElementSwitched += OnSwitch;
            GameEvents.ZoneSpawned += OnZone;
            GameEvents.HitBlocked += OnBlocked;
            GameEvents.TelegraphStarted += OnTelegraph;
            GameEvents.TelegraphCancelled += OnTelegraphCancelled;
            GameEvents.AttackMissed += OnMissed;
            GameEvents.EnemySpawned += OnEnemySpawned;
            GameEvents.WaveStarted += OnWave;
            GameEvents.ArmorChanged += OnArmor;
            GameEvents.BossSpawned += OnBoss;
            GameEvents.BossPhaseChanged += OnBossPhase;
            GameEvents.BossShieldChanged += OnBossShield;
            GameEvents.Exploded += OnExploded;
            GameEvents.PuddleLanded += OnPuddle;
            GameEvents.QuintessenceBurst += OnBurst;
            GameEvents.EnemyHealed += OnEnemyHealed;
            GameEvents.ChampionSpawned += OnChampion;
            GameEvents.Evaded += OnEvaded;
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.Shattered += OnShattered;
            GameEvents.QuintessenceActiveChanged += OnQuintessence;
        }

        void OnDisable()
        {
            GameEvents.DamageDealt -= OnDamage;
            GameEvents.Died -= OnDied;
            GameEvents.MarkApplied -= OnMark;
            GameEvents.MarkCleared -= OnMarkCleared;
            GameEvents.ReactionTriggered -= OnReaction;
            GameEvents.StatusApplied -= OnStatus;
            GameEvents.StatusEnded -= OnStatusEnded;
            GameEvents.AttackPerformed -= OnAttack;
            GameEvents.ElementSwitched -= OnSwitch;
            GameEvents.ZoneSpawned -= OnZone;
            GameEvents.HitBlocked -= OnBlocked;
            GameEvents.TelegraphStarted -= OnTelegraph;
            GameEvents.TelegraphCancelled -= OnTelegraphCancelled;
            GameEvents.AttackMissed -= OnMissed;
            GameEvents.EnemySpawned -= OnEnemySpawned;
            GameEvents.WaveStarted -= OnWave;
            GameEvents.ArmorChanged -= OnArmor;
            GameEvents.BossSpawned -= OnBoss;
            GameEvents.BossPhaseChanged -= OnBossPhase;
            GameEvents.BossShieldChanged -= OnBossShield;
            GameEvents.Exploded -= OnExploded;
            GameEvents.PuddleLanded -= OnPuddle;
            GameEvents.QuintessenceBurst -= OnBurst;
            GameEvents.EnemyHealed -= OnEnemyHealed;
            GameEvents.ChampionSpawned -= OnChampion;
            GameEvents.Evaded -= OnEvaded;
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.Shattered -= OnShattered;
            GameEvents.QuintessenceActiveChanged -= OnQuintessence;
            if (!GameClock.Paused) Time.timeScale = 1f;
        }

        // ---------- eventos ----------

        void OnDamage(Health target, DamageInfo info, float applied)
        {
            var c = ElementColors.Of(info.Element);
            bool enemy = target.Team == Team.Enemy;
            Flash(target).Flash(enemy ? new Color(1.9f, 0.5f, 0.42f) : new Color(2.2f, 2.2f, 2.2f), 0.16f);
            float size = applied >= 15f ? 1.5f : applied >= 8f ? 1.15f : applied <= 3f ? 0.7f : 0.95f;
            if (info.IsCrit) size *= 1.3f;
            bool resisted = enemy && displays.TryGetValue(target, out var shown) && shown != null && shown.Armored && !info.IsReaction;
            if (resisted) { size *= 0.8f; c = Steel; }
            // afinidade elemental: fraqueza e resistencia aparecem no numero, com um rotulo de vez em quando
            if (enemy && info.Element != ElementType.None && target.TryGetComponent(out ElementalAffinity aff))
            {
                float mult = aff.Multiplier(info.Element);
                if (mult != 1f && (!affinityLabel.TryGetValue(target, out float at) || Time.time - at > 1.2f))
                {
                    affinityLabel[target] = Time.time;
                    DamageNumber.Spawn(target.transform.position + Vector3.up * 3.5f, mult > 1f ? "FRAQUEZA" : "RESISTE",
                        mult > 1f ? new Color(1f, 0.82f, 0.4f) : new Color(0.65f, 0.66f, 0.7f), 0.75f, 0.7f);
                }
                if (mult > 1f) size *= 1.25f; else if (mult < 1f) { size *= 0.8f; c = Steel; }
            }
            // escudo do Escudeiro: numero pequeno e cinza, rotulo e o som de bloqueio de vez em quando
            if (enemy && target.TryGetComponent(out FrontGuard guard) && guard.LastBlocked)
            {
                size *= 0.75f; c = Steel;
                if (!guardLabel.TryGetValue(target, out float gt) || Time.time - gt > 0.9f)
                {
                    guardLabel[target] = Time.time;
                    DamageNumber.Spawn(target.transform.position + Vector3.up * 3.5f, "ESCUDO", new Color(0.72f, 0.76f, 0.84f), 0.75f, 0.7f);
                    Sfx.Play("block");
                }
            }
            DamageNumber.Spawn(target.transform.position + Vector3.up * 2.7f, Mathf.RoundToInt(applied) + (info.IsCrit ? "!" : ""),
                enemy ? Color.Lerp(c, Color.white, 0.35f) : new Color(1f, 0.25f, 0.25f), size);
            var p = info.Point.sqrMagnitude > 0.001f ? info.Point : target.transform.position + Vector3.up * 1.2f;
            Burst(p, info.Direction.sqrMagnitude > 0.01f ? info.Direction : Vector3.up, 70f, applied >= 8f ? 16 : 8, 3f, 9f, 0.12f, 0.3f, 0.3f, Color.Lerp(c, Color.white, 0.3f));
            if (enemy) Display(target).Touch();
            else CameraShake.Add(0.45f);
            if (applied >= 10f && !info.IsReaction) { CameraShake.Add(0.22f); Stop(0.05f); }
        }

        void OnDied(Health target, DamageInfo info)
        {
            // o estandarte caiu: o circulo da aura some junto
            if (target != null && target.TryGetComponent(out AuraSource _))
            {
                var ring = target.transform.Find("AuraRing");
                if (ring != null) Destroy(ring.gameObject);
            }
            if (target != null && target == boss)
            {
                var bp = target.transform.position + Vector3.up * 2f;
                accents?.Emit(BossElement, bp, Vector3.forward, 180f, 200, 9f);
                accents?.Emit(ElementType.Earth, bp, Vector3.forward, 180f, 120, 10f, Ash);
                foreach (var e in Elements.All) Ring(bp + Vector3.up * (0.3f * (int)e), 70, 10f + 3f * (int)e, 0.5f, 1.1f, 0.8f, ElementColors.Of(e));
                LightFlash(bp, Color.white, 22f, 24f, 1f);
                CameraShake.Add(1.2f);
                Stop(0.3f);
                if (shieldCage != null) shieldCage.gameObject.SetActive(false);
                shieldUp = false;
                boss = null;
            }
            var p = target.transform.position + Vector3.up * 1.2f;
            Burst(p, Vector3.up, 180f, 46, 3f, 11f, 0.2f, 0.55f, 0.55f, Color.Lerp(ElementColors.Of(info.Element), Color.white, 0.5f));
            LightFlash(p, Color.white, 6f, 6f, 0.25f);
            CameraShake.Add(0.3f);
            if (displays.TryGetValue(target, out var d)) { d.SetMark(ElementType.None); foreach (StatusType t in System.Enum.GetValues(typeof(StatusType))) d.SetStatus(t, false); }
            Flash(target).SetTint(Color.white);            // inimigo de verdade (anda pelo NavMesh) se desmonta nos proprios ossos; boneco de treino nao, porque ele volta
            if (target.GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
            {
                LastBonesDropped = BonePile.Scatter(target.gameObject, info.Direction);
                accents?.Emit(ElementType.Earth, target.transform.position + Vector3.up * 0.8f, Vector3.forward, 180f, 16, 4f, new Color(0.8f, 0.76f, 0.68f));
            }
        }

        /// Quantas pecas o ultimo inimigo morto soltou (para os testes).
        public int LastBonesDropped { get; private set; }

        void OnMark(ElementType element, Health target) => Display(target).SetMark(element);
        void OnMarkCleared(Health target) { if (target != null) Display(target).SetMark(ElementType.None); }

        void OnReaction(ReactionEvent e)
        {
            var a = ElementColors.Of(e.Mark);
            var b = ElementColors.Of(e.Trigger);
            var p = e.Point + Vector3.up * 0.9f;
            Ring(p, 80, e.Radius / 0.3f, 0.5f, 0.8f, 0.42f, a);
            Ring(p + Vector3.up * 0.4f, 60, e.Radius / 0.42f, 0.35f, 0.6f, 0.5f, b);
            Burst(p, Vector3.up, 60f, 50, 2f, 8f, 0.3f, 0.7f, 0.7f, Color.Lerp(a, b, 0.5f));
            LightFlash(p + Vector3.up, Color.Lerp(a, b, 0.5f), 12f, e.Radius * 3f, 0.4f);
            if (accents != null)
            {
                // a reacao mostra os dois elementos do par, cada um do seu jeito
                if (e.Mark != ElementType.None) accents.Emit(e.Mark, p, Vector3.forward, 180f, 26, e.Radius * 2.2f);
                if (e.Trigger != ElementType.None) accents.Emit(e.Trigger, p, Vector3.forward, 180f, 26, e.Radius * 2.2f);
            }
            DamageNumber.Spawn(e.Point + Vector3.up * 3.6f, (e.Name ?? "REACAO").ToUpperInvariant() + "!", Color.Lerp(Color.Lerp(a, b, 0.5f), Color.white, 0.5f), 1.25f, 1.1f);
            CameraShake.Add(0.55f);
            Stop(0.08f);
        }

        void OnStatus(Health target, StatusType type, float duration)
        {
            if (target.Team != Team.Enemy) return;
            var d = Display(target);
            bool isNew = d.SetStatus(type, true);
            var tint = StatusTint(type);
            if (tint.HasValue) Flash(target).SetTint(tint.Value);
            string label = type switch
            {
                StatusType.Stun => "ATORDOADO", StatusType.Frozen => "CONGELADO", StatusType.Rooted => "ENRAIZADO",
                StatusType.ArmorBroken => "ARMADURA QUEBRADA", StatusType.Blinded => "CEGO", _ => null,
            };
            if (isNew && label != null)
                DamageNumber.Spawn(target.transform.position + Vector3.up * 3.3f, label, tint ?? Color.white, 0.62f, 0.9f);
        }

        static Color? StatusTint(StatusType type) => type switch
        {
            StatusType.Soaked => new Color(0.6f, 0.8f, 1.5f),
            StatusType.Burn => new Color(1.35f, 0.8f, 0.65f),
            StatusType.Frozen => new Color(0.75f, 1.35f, 2.2f),
            StatusType.Rooted => new Color(0.95f, 0.7f, 0.4f),
            StatusType.Stun => new Color(1.5f, 1.4f, 0.6f),
            _ => null,
        };

        void OnTelegraph(TelegraphEvent e)
        {
            if (e.Source != null && telegraphs.TryGetValue(e.Source, out var old) && old != null) Destroy(old.gameObject);
            var v = TelegraphVisual.Create(e, flatMaterial, RingMesh, DiscMesh);
            v.transform.SetParent(transform, true);
            if (e.Source != null) telegraphs[e.Source] = v;
            // a bomba some junto com o aviso se o arremessador for interrompido
            if (e.Lobbed && e.Source != null)
                LobbedBomb.Spawn(e.Source.transform.position + Vector3.up * 1.6f, e.Point + Vector3.up * 0.2f, e.Duration, flatMaterial,
                    e.Element == ElementType.Water ? new Color(0.2f, 0.42f, 0.5f) : new Color(0.16f, 0.14f, 0.15f)).transform.SetParent(v.transform, true);
        }

        void OnTelegraphCancelled(GameObject source)
        {
            if (source == null || !telegraphs.TryGetValue(source, out var v)) return;
            if (v != null) Destroy(v.gameObject);
            telegraphs.Remove(source);
        }

        void OnMissed(Health source) =>
            DamageNumber.Spawn(source.transform.position + Vector3.up * 3.2f, "ERROU", new Color(0.8f, 0.85f, 0.9f), 0.75f, 0.7f);

        void OnEnemySpawned(Health enemy)
        {
            var p = enemy.transform.position;
            var c = new Color(0.7f, 0.3f, 1f);
            Ring(p + Vector3.up * 0.2f, 30, 5f, 0.3f, 0.6f, 0.4f, c);
            Burst(p + Vector3.up * 0.3f, Vector3.up, 25f, 24, 2f, 7f, 0.3f, 0.7f, 0.55f, c);
            // aura (Porta-Estandarte): um circulo no chao mostra quem esta fortalecido
            if (enemy.TryGetComponent(out AuraSource aura))
            {
                var ring = new GameObject("AuraRing");
                ring.transform.SetParent(enemy.transform, false);
                ring.transform.localPosition = Vector3.up * 0.06f;
                float s = aura.Radius / Mathf.Max(0.01f, enemy.transform.lossyScale.x);
                ring.transform.localScale = new Vector3(s, 1f, s);
                ring.AddComponent<MeshFilter>().sharedMesh = RingMesh;
                auraMaterial ??= new Material(flatMaterial) { color = new Color(0.6f, 0.14f, 0.2f, 0.55f) }; // carmim: nao confunde com o anel do heroi nem com aviso de ataque
                ring.AddComponent<MeshRenderer>().sharedMaterial = auraMaterial;
            }
        }

        Material auraMaterial;

        static readonly Color Steel = new(0.62f, 0.72f, 0.95f);
        float lastImmune = -10f;
        Health boss;

        void OnBoss(Health b, string name)
        {
            boss = b;
            var p = b.transform.position + Vector3.up * 2f;
            var bc = BossElement == ElementType.Fire ? new Color(1f, 0.45f, 0.1f) : ElementColors.Of(BossElement);
            Ring(p, 90, 14f, 0.6f, 1.4f, 0.7f, bc);
            LightFlash(p, bc, 16f, 18f, 0.8f);
            DamageNumber.Spawn(b.transform.position + Vector3.up * 6.5f, name.ToUpperInvariant(), new Color(1f, 0.7f, 0.3f), 1.7f, 2.4f);
            CameraShake.Add(0.8f);
            // nasce das cinzas: brasas subindo e pedra voando em volta (a Rainha, da agua)
            accents?.Emit(BossElement, b.transform.position + Vector3.up, Vector3.forward, 180f, 90, 5f);
            accents?.Emit(ElementType.Earth, b.transform.position + Vector3.up * 0.4f, Vector3.forward, 180f, 40, 6f, Ash);
        }

        static readonly Color Ash = new(0.42f, 0.38f, 0.36f);
        Transform shieldCage;
        Material shieldMat;
        bool shieldUp, shieldNeedsQuint;
        float bossEmit;

        /// Tres aros girando em volta do Guardiao enquanto o escudo esta de pe.
        void BuildShieldCage()
        {
            shieldCage = new GameObject("BossShieldCage").transform;
            shieldCage.SetParent(transform, false);
            shieldMat = new Material(flatMaterial) { color = new Color(Steel.r, Steel.g, Steel.b, 0.75f) };
            var tilts = new[] { new Vector3(0, 0, 0), new Vector3(62, 0, 0), new Vector3(62, 120, 0) };
            foreach (var t in tilts)
            {
                var ring = new GameObject("Ring");
                ring.transform.SetParent(shieldCage, false);
                ring.transform.localRotation = Quaternion.Euler(t);
                ring.transform.localScale = Vector3.one * 2.7f;
                ring.AddComponent<MeshFilter>().sharedMesh = RingMesh;
                ring.AddComponent<MeshRenderer>().sharedMaterial = shieldMat;
            }
            shieldCage.gameObject.SetActive(false);
        }

        void OnBossPhase(int phase, int total)
        {
            if (phase <= 1 || boss == null) return;
            var p = boss.transform.position + Vector3.up * 2f;
            var pc = BossElement == ElementType.Fire ? new Color(1f, 0.4f, 0.12f) : ElementColors.Of(BossElement);
            Ring(p, 100, 16f, 0.7f, 1.5f, 0.6f, pc);
            Burst(p, Vector3.up, 180f, 120, 4f, 14f, 0.4f, 1.2f, 0.7f, pc);
            LightFlash(p, pc, 18f, 20f, 0.6f);
            DamageNumber.Spawn(boss.transform.position + Vector3.up * 6.5f, $"FASE {phase}", new Color(1f, 0.6f, 0.25f), 1.6f, 1.8f);
            CameraShake.Add(0.9f);
            Stop(0.15f);
            accents?.Emit(BossElement, p, Vector3.forward, 180f, 110, 7f);
            accents?.Emit(ElementType.Earth, p + Vector3.down, Vector3.forward, 180f, 50, 8f, Ash);
        }

        void OnBossShield(bool up, bool needsQuintessence)
        {
            if (boss == null) return;
            DamageNumber.Spawn(boss.transform.position + Vector3.up * 5.6f, up ? "ESCUDO!" : "ESCUDO QUEBRADO!", Steel, 1.4f, 1.8f);
            if (up && needsQuintessence)
                DamageNumber.Spawn(boss.transform.position + Vector3.up * 4.6f, "só a Quintessência quebra", Color.white, 0.9f, 2.6f);
            if (shieldCage == null && flatMaterial != null) BuildShieldCage();
            bool was = shieldUp;
            shieldUp = up; shieldNeedsQuint = needsQuintessence;
            if (shieldCage != null) shieldCage.gameObject.SetActive(up);
            var p = boss.transform.position + Vector3.up * 2f;
            if (up)
            {
                Ring(p, 70, 10f, 0.4f, 0.8f, 0.45f, Steel);
                LightFlash(p, Steel, 9f, 10f, 0.4f);
            }
            else if (was)
            {
                // o escudo estilhaca: lascas de aco para todo lado
                accents?.Emit(ElementType.Earth, p, Vector3.forward, 180f, 120, 11f, Steel);
                Ring(p, 90, 16f, 0.5f, 1.1f, 0.5f, Color.white);
                LightFlash(p, Color.white, 16f, 16f, 0.5f);
                CameraShake.Add(0.9f);
                Stop(0.14f);
            }
        }

        void OnArmor(Health enemy, bool armored)
        {
            Display(enemy).SetArmored(armored);
            Flash(enemy).SetTint(armored ? new Color(0.75f, 0.85f, 1.25f) : Color.white);
            if (armored) return;
            var p = enemy.transform.position + Vector3.up * 1.4f;
            Burst(p, Vector3.up, 180f, 50, 4f, 11f, 0.25f, 0.6f, 0.5f, Steel);
            LightFlash(p, Steel, 8f, 6f, 0.3f);
            CameraShake.Add(0.4f);
        }

        void OnEvaded(Health target)
        {
            if (target == null) return;
            var p = target.transform.position + Vector3.up * 1.2f;
            DamageNumber.Spawn(p + Vector3.up * 1.8f, "ESQUIVA", new Color(0.7f, 0.92f, 0.8f), 0.8f, 0.7f);
            accents?.Emit(ElementType.Air, p, Vector3.up, 120f, 10, 3f);
            Sfx.Play("dash");
        }

        /// Campeao de Elite: anuncio e uma luz ambar que o acompanha.
        void OnChampion(Health champion)
        {
            if (champion == null) return;
            var amber = new Color(0.9f, 0.62f, 0.28f);
            DamageNumber.Spawn(champion.transform.position + Vector3.up * 3.6f, "CAMPEÃO", amber, 1.3f, 2f);
            Sfx.Play("champion");
            var l = new GameObject("ChampionLight").AddComponent<Light>();
            l.transform.SetParent(champion.transform, false);
            l.transform.localPosition = new Vector3(0, 2.2f, 0);
            l.type = LightType.Point; l.color = amber; l.intensity = 2.4f; l.range = 5f;
            CameraShake.Add(0.35f);
            // coroa de cristais flutuando sobre a cabeca: entra no espaco do modelo e depois segue o osso
            var crown = Resources.Load<GameObject>("Models/campeao_crown");
            var model = champion.transform.Find("Model");
            if (crown == null || model == null) return;
            Transform head = null;
            foreach (var t in model.GetComponentsInChildren<Transform>()) if (t.name == "head") head = t;
            if (head == null) return;
            var c = Instantiate(crown, model);
            c.name = "ChampionCrown";
            c.transform.localPosition = new Vector3(0, -0.3f, 0);
            c.transform.localRotation = Quaternion.identity;
            c.transform.localScale = Vector3.one;
            c.transform.SetParent(head, true);
        }

        /// Cura do Xama: numero verde e um sopro de folhas no aliado curado.
        void OnEnemyHealed(Health target, float amount)
        {
            if (target == null) return;
            var green = new Color(0.45f, 0.75f, 0.5f);
            var p = target.transform.position + Vector3.up * 1.4f;
            DamageNumber.Spawn(p + Vector3.up * 1.2f, "+" + Mathf.RoundToInt(amount), green, 0.8f, 0.9f);
            accents?.Emit(ElementType.Air, p, Vector3.up, 60f, 10, 2.5f, green);
        }

        /// Despertar da Quintessencia: o nome da explosao do heroi, um anel largo na cor dele e um clarao.
        void OnBurst(string name, Vector3 point, float radius, ElementType hero)
        {
            var c = ElementColors.Of(hero);
            var p = point + Vector3.up * 0.7f;
            Ring(p, 90, radius / 0.25f, 0.6f, 1.2f, 0.45f, c);
            Ring(p + Vector3.up * 0.3f, 70, radius / 0.35f, 0.5f, 0.9f, 0.5f, UiColorsCream);
            accents?.Emit(hero, p, Vector3.up, 180f, 60, radius * 1.4f);
            Burst(p, Vector3.up, 180f, 80, 3f, 10f, 0.35f, 1f, 0.7f, c);
            LightFlash(p + Vector3.up, c, 14f, radius * 3f, 0.45f);
            CameraShake.Add(0.7f);
            Sfx.Play("quint_burst");
            DamageNumber.Spawn(point + Vector3.up * 4.2f, name.ToUpperInvariant(), Color.Lerp(c, Color.white, 0.35f), 1.15f, 2f);
        }

        static readonly Color UiColorsCream = new(0.96f, 0.9f, 0.78f);

        /// Orbe d'agua caindo: respingo e anel da cor do elemento, sem clarao de fogo.
        void OnPuddle(Vector3 point, float radius, ElementType element, float seconds)
        {
            var c = ElementColors.Of(element);
            var p = point + Vector3.up * 0.5f;
            Ring(p, 60, radius / 0.3f, 0.5f, 0.9f, 0.4f, c);
            accents?.Emit(element, p, Vector3.up, 120f, 24, radius * 1.4f);
            Burst(p, Vector3.up, 70f, 40, 2f, 7f, 0.25f, 0.6f, 0.5f, c);
            Sfx.Play("cast_water");
            CameraShake.Add(0.25f);
        }

        /// Elemento do chefe em cena (pela afinidade elemental dele; sem afinidade, Fogo: Guardiao e Tecela).
        ElementType BossElement => boss != null && boss.TryGetComponent(out ElementalAffinity a) && a.Element != ElementType.None ? a.Element : ElementType.Fire;

        void OnExploded(Vector3 point, float radius)
        {
            var c = new Color(1f, 0.5f, 0.1f);
            var p = point + Vector3.up * 0.8f;
            Ring(p, 70, radius / 0.25f, 0.6f, 1.3f, 0.4f, c);
            accents?.Emit(ElementType.Fire, p, Vector3.forward, 180f, 36, radius * 1.6f);
            accents?.Emit(ElementType.Earth, p, Vector3.forward, 180f, 16, radius * 2f, Ash);
            Burst(p, Vector3.up, 180f, 90, 3f, 13f, 0.4f, 1.2f, 0.6f, c);
            LightFlash(p + Vector3.up, c, 14f, radius * 3.5f, 0.4f);
            CameraShake.Add(0.6f);
        }

        void OnWave(int index, int total)
        {
            FindPlayer();
            if (player != null)
                DamageNumber.Spawn(player.position + Vector3.up * 4.2f, $"ONDA {index + 1}/{total}", Color.white, 1.1f, 1.3f);
        }

        void OnRoomCleared()
        {
            FindPlayer();
            if (player == null) return;
            DamageNumber.Spawn(player.position + Vector3.up * 4.2f, "SALA LIMPA!", new Color(1f, 0.9f, 0.5f), 1.5f, 1.8f);
            Ring(player.position + Vector3.up * 0.4f, 60, 10f, 0.4f, 0.8f, 0.6f, new Color(1f, 0.85f, 0.4f));
        }

        void OnBlocked(Health target, DamageInfo info)
        {
            var c = ElementColors.Of(ElementType.Earth);
            Ring(target.transform.position + Vector3.up, 30, 6f, 0.3f, 0.5f, 0.3f, c);
            bool enemy = target.Team == Team.Enemy;
            if (enemy && Time.time - lastImmune < 0.5f) return; // inimigo com escudo: nao repete o texto a cada golpe
            if (enemy) lastImmune = Time.time;
            DamageNumber.Spawn(target.transform.position + Vector3.up * (enemy ? 5f : 3.2f), enemy ? "IMUNE" : "BLOQUEOU",
                enemy ? new Color(0.7f, 0.8f, 1f) : Color.Lerp(c, Color.white, 0.4f), enemy ? 1.1f : 0.8f, 0.7f);
        }

        void OnShattered(Health target)
        {
            var p = target.transform.position + Vector3.up * 1.2f;
            var c = new Color(0.7f, 0.95f, 1f);
            Burst(p, Vector3.up, 180f, 60, 4f, 12f, 0.25f, 0.6f, 0.45f, c);
            LightFlash(p, c, 8f, 6f, 0.25f);
            DamageNumber.Spawn(target.transform.position + Vector3.up * 3.5f, "ESTILHACOU!", c, 0.9f, 0.9f);
            CameraShake.Add(0.35f);
        }

        void OnQuintessence(bool active)
        {
            FindPlayer();
            quintessence = active;
            if (player == null) return;
            var p = player.position + Vector3.up * 1.2f;
            if (active)
            {
                foreach (var e in Elements.All) Ring(p + Vector3.up * (0.2f * (int)e), 50, 6f + 2f * (int)e, 0.4f, 0.8f, 0.6f, ElementColors.Of(e));
                LightFlash(p, Color.white, 16f, 14f, 0.6f);
                DamageNumber.Spawn(player.position + Vector3.up * 4f, "QUINTESSÊNCIA!", Color.white, 1.5f, 1.4f);
                // os quatro elementos saem juntos, cada um do seu jeito
                if (accents != null) foreach (var e in Elements.All) accents.Emit(e, p, Vector3.forward, 180f, 40, 7f);
                CameraShake.Add(0.7f);
                Stop(0.12f);
            }
            else
            {
                Ring(p, 40, 8f, 0.3f, 0.5f, 0.4f, Color.white);
                accents?.Emit(current, p, Vector3.forward, 180f, 30, 4f);
                var c = ElementColors.Of(current);
                presenceTarget = Color.Lerp(c, Color.white, 0.35f);
                if (playerRingMat != null) playerRingMat.color = new Color(c.r, c.g, c.b, 0.9f);
                if (presence != null) presence.intensity = presenceBase;
            }
        }

        void OnStatusEnded(Health target, StatusType type)
        {
            if (target == null || !displays.TryGetValue(target, out var d)) return;
            d.SetStatus(type, false);
            if (StatusTint(type).HasValue) Flash(target).SetTint(Color.white);
        }

        ElementAccents accents;

        void OnAttack(AttackEvent e)
        {
            if (accents != null && e.Element != ElementType.None)
            {
                bool radial = e.Kind == AttackKind.Skill && e.Arc >= 300f;
                int n = e.Kind switch { AttackKind.Basic => 10, AttackKind.Finisher => 24, AttackKind.Skill => 70, AttackKind.Dash => 14, _ => 0 };
                var from = e.Origin + Vector3.up * (radial ? 0.6f : 1.1f) + (radial ? Vector3.zero : e.Direction * 0.8f);
                if (radial) accents.Emit(e.Element, from, Vector3.forward, 180f, n, e.Range * 1.8f);
                else accents.Emit(e.Element, from, e.Kind == AttackKind.Dash ? -e.Direction : e.Direction, Mathf.Max(20f, e.Arc * 0.45f), n, e.Range * 1.8f);
            }
            // golpe basico e dash nao tem elemento: saem num tom de osso, sem competir com as skills
            var c = e.Element == ElementType.None ? new Color(0.8f, 0.76f, 0.68f) : ElementColors.Of(e.Element);
            var o = e.Origin + Vector3.up * 1.15f + e.Direction * 0.7f;
            switch (e.Kind)
            {
                case AttackKind.Basic:
                    Burst(o, e.Direction, e.Arc * 0.3f, 60, e.Range * 1.6f, e.Range * 4.4f, 0.7f, 1.5f, 0.3f, c);
                    LightFlash(o + e.Direction, c, 3.5f, 5f, 0.14f);
                    break;
                case AttackKind.Finisher:
                    Burst(o, e.Direction, e.Arc * 0.42f, 120, e.Range * 1.6f, e.Range * 4.8f, 0.9f, 2.0f, 0.36f, c);
                    Burst(o, e.Direction, e.Arc * 0.2f, 20, e.Range * 3f, e.Range * 5f, 0.3f, 0.5f, 0.22f, Color.Lerp(c, Color.white, 0.6f));
                    LightFlash(o + e.Direction, c, 6f, 7f, 0.2f);
                    break;
                case AttackKind.Skill when e.Arc >= 300f:
                    var center = e.Origin + Vector3.up * 0.6f;
                    Ring(center, 90, e.Range / 0.28f, 0.7f, 1.6f, 0.4f, c);
                    Ring(center + Vector3.up * 0.5f, 60, e.Range / 0.4f, 0.4f, 0.9f, 0.5f, Color.Lerp(c, Color.white, 0.5f));
                    Burst(center, Vector3.up, 50f, 60, 2f, 9f, 0.4f, 1f, 0.6f, c);
                    LightFlash(center + Vector3.up, c, 10f, e.Range * 2.5f, 0.35f);
                    CameraShake.Add(0.4f);
                    break;
                case AttackKind.Skill:
                    Burst(o, e.Direction, e.Arc * 0.5f, 260, e.Range * 1.0f, e.Range * 3.6f, 1.0f, 2.6f, 0.5f, c);
                    Burst(o, e.Direction, e.Arc * 0.3f, 50, e.Range * 2.4f, e.Range * 3.8f, 0.3f, 0.7f, 0.3f, Color.Lerp(c, Color.white, 0.6f));
                    LightFlash(o + e.Direction * 2f, c, 10f, e.Range * 2f, 0.35f);
                    CameraShake.Add(0.3f);
                    break;
                case AttackKind.Dash:
                    Burst(e.Origin + Vector3.up, -e.Direction, 40f, 22, 2f, 7f, 0.3f, 0.6f, 0.3f, c);
                    break;
            }
        }

        void OnSwitch(ElementType from, ElementType to)
        {
            FindPlayer();
            current = to;
            if (quintessence) return;
            var c = ElementColors.Of(to);
            presenceTarget = Color.Lerp(c, Color.white, 0.35f);
            if (playerRingMat != null) playerRingMat.color = new Color(c.r * 0.85f, c.g * 0.85f, c.b * 0.85f, 0.7f);
            // a cor do heroi segue a ultima skill usada; o efeito da skill ja mostra a troca
        }

        void OnZone(ZoneEvent z) => zones.Add((z, z.Duration));

        // ---------- utilitarios ----------

        HitFlash Flash(Health h) => h.TryGetComponent(out HitFlash f) ? f : h.gameObject.AddComponent<HitFlash>();

        TargetDisplay Display(Health h)
        {
            if (displays.TryGetValue(h, out var d) && d != null) return d;
            d = TargetDisplay.Create(h, this);
            d.transform.SetParent(transform, false);
            displays[h] = d;
            return d;
        }

        void FindPlayer()
        {
            if (player != null) return;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            player = go.transform;
            presence = go.GetComponentInChildren<Light>();
            if (presence != null) presenceBase = presence.intensity;
            var r = new GameObject("PlayerElementRing");
            r.transform.SetParent(transform, false);
            r.AddComponent<MeshFilter>().sharedMesh = RingMesh;
            playerRingMat = new Material(flatMaterial);
            r.AddComponent<MeshRenderer>().sharedMaterial = playerRingMat;
            playerRing = r.transform;
        }

        /// Solta particulas num cone em volta de dir.
        public void Burst(Vector3 pos, Vector3 dir, float spread, int count, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float life, Color color)
        {
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
            var basis = Quaternion.LookRotation(dir.normalized);
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var d = basis * Quaternion.Euler(Random.Range(-spread, spread) * 0.35f, Random.Range(-spread, spread), 0) * Vector3.forward;
                p.position = pos + Random.insideUnitSphere * 0.15f;
                p.velocity = d * Random.Range(speedMin, speedMax);
                p.startSize = Random.Range(sizeMin, sizeMax);
                p.startLifetime = life * Random.Range(0.6f, 1f);
                p.startColor = Color.Lerp(color, Color.white, Random.Range(0f, 0.25f));
                fx.Emit(p, 1);
            }
        }

        void Ring(Vector3 pos, int count, float speed, float sizeMin, float sizeMax, float life, Color color)
        {
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                p.position = pos + d * 0.3f;
                p.velocity = d * speed * Random.Range(0.85f, 1f);
                p.startSize = Random.Range(sizeMin, sizeMax);
                p.startLifetime = life;
                p.startColor = color;
                fx.Emit(p, 1);
            }
        }

        void LightFlash(Vector3 pos, Color color, float intensity, float range, float seconds)
        {
            Light l;
            if (lightPool.Count > 0) { l = lightPool[^1]; lightPool.RemoveAt(lightPool.Count - 1); l.gameObject.SetActive(true); }
            else
            {
                if (flashes.Count >= 6) return;
                l = new GameObject("FlashLight").AddComponent<Light>();
                l.transform.SetParent(transform, false);
                l.type = LightType.Point;
                l.shadows = LightShadows.None;
            }
            l.transform.position = pos;
            l.color = color; l.range = range; l.intensity = intensity;
            flashes.Add((l, seconds, seconds, intensity));
        }

        void Stop(float seconds)
        {
            if (!hitStop || GameClock.Paused) return;
            if (stop != null) StopCoroutine(stop);
            stop = StartCoroutine(StopRoutine(seconds));
        }

        IEnumerator StopRoutine(float seconds)
        {
            Time.timeScale = 0.05f;
            while (seconds > 0f) { seconds -= Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = GameClock.Paused ? 0f : 1f;
            stop = null;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var f = flashes[i];
                f.left -= Time.unscaledDeltaTime;
                if (f.left <= 0f) { f.light.gameObject.SetActive(false); lightPool.Add(f.light); flashes.RemoveAt(i); continue; }
                f.light.intensity = f.intensity * (f.left / f.total);
                flashes[i] = f;
            }

            if (player != null)
            {
                if (playerRing != null)
                {
                    playerRing.SetPositionAndRotation(player.position + Vector3.up * 0.05f, Quaternion.Euler(0, -Time.time * 60f, 0));
                    playerRing.localScale = Vector3.one * 1.05f;
                }
                if (presence != null) presence.color = Color.Lerp(presence.color, presenceTarget, dt * 8f);
                if (quintessence)
                {
                    // prismatico: o anel e a luz giram pelas cores dos quatro elementos
                    var prism = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.9f, 1f), 0.55f, 1f);
                    if (playerRingMat != null) playerRingMat.color = prism;
                    if (playerRing != null) playerRing.localScale = Vector3.one * (1.35f + 0.12f * Mathf.Sin(Time.time * 12f));
                    presenceTarget = Color.Lerp(prism, Color.white, 0.5f);
                    if (presence != null) presence.intensity = presenceBase * 2.6f;
                    quintEmit -= dt;
                    if (quintEmit <= 0f)
                    {
                        quintEmit = 0.03f;
                        var e = Elements.All[Random.Range(0, 4)];
                        var off = Random.insideUnitCircle.normalized * 0.9f;
                        Burst(player.position + new Vector3(off.x, Random.Range(0.2f, 1.2f), off.y), Vector3.up, 15f, 2, 2f, 5f, 0.25f, 0.55f, 0.6f, ElementColors.Of(e));
                        // aura: o detalhe de um elemento sorteado girando em volta do heroi
                        var tangent = new Vector3(-off.y, 0, off.x);
                        accents?.Emit(e, player.position + new Vector3(off.x, Random.Range(0.4f, 1.6f), off.y) * 1.2f, tangent, 25f, 2, 3.5f);
                    }
                }
            }

            if (boss != null && !boss.IsDead)
            {
                // o Guardiao solta brasa e cinza o tempo todo
                bossEmit -= dt;
                if (bossEmit <= 0f)
                {
                    bossEmit = 0.06f;
                    var bp = boss.transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(0.6f, 3f), Random.Range(-0.8f, 0.8f));
                    accents?.Emit(BossElement, bp, Vector3.up, 40f, 1, 1.2f);
                    if (Random.value < 0.35f) accents?.Emit(BossElement, bp, Vector3.up, 60f, 1, 0.8f, Ash);
                }
                if (shieldUp && shieldCage != null)
                {
                    shieldCage.position = boss.transform.position + Vector3.up * 2f;
                    shieldCage.rotation = Quaternion.Euler(Time.time * 35f, Time.time * 80f, 0);
                    // escudo que so a Quintessencia quebra gira pelas cores; o comum fica cor de aco
                    var sc = shieldNeedsQuint ? Color.Lerp(Color.HSVToRGB(Mathf.Repeat(Time.time * 0.5f, 1f), 0.45f, 1f), Steel, 0.35f) : Steel;
                    shieldMat.color = new Color(sc.r, sc.g, sc.b, 0.75f);
                }
            }

            zoneEmit -= dt;
            bool emit = zoneEmit <= 0f;
            if (emit) zoneEmit = 0.05f;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                var z = zones[i];
                z.left -= dt;
                if (z.left <= 0f) { zones.RemoveAt(i); continue; }
                zones[i] = z;
                if (!emit) continue;
                var off = Random.insideUnitCircle * z.zone.Radius;
                Burst(z.zone.Point + new Vector3(off.x, 0.15f, off.y), Vector3.up, 20f, 3, 1.5f, 3.5f, 0.3f, 0.7f, 0.5f, ElementColors.Of(z.zone.Element));
            }
        }

        static Mesh BuildRing(float inner, float outer, int segments)
        {
            var v = new Vector3[segments * 2];
            var t = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * inner;
                v[i * 2 + 1] = d * outer;
                int n = (i + 1) % segments;
                int k = i * 6;
                t[k] = i * 2; t[k + 1] = i * 2 + 1; t[k + 2] = n * 2 + 1;
                t[k + 3] = i * 2; t[k + 4] = n * 2 + 1; t[k + 5] = n * 2;
            }
            var m = new Mesh { name = "Ring", vertices = v, triangles = t };
            m.RecalculateBounds();
            return m;
        }
    }
}
