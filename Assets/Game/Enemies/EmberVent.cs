using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Enemies
{
    /// Perigo do cenario: respiradouro de brasa. De tempos em tempos avisa um circulo e entra em erupcao.
    /// Queima quem estiver em cima, heroi ou inimigo: da para usar contra o bando.
    public class EmberVent : MonoBehaviour
    {
        [SerializeField] float interval = 4.5f;
        [SerializeField] float windup = 1.1f;
        [SerializeField] float radius = 1.9f;
        [SerializeField] float damage = 6f;

        float timer;
        bool warned;
        static readonly Collider[] buffer = new Collider[24];
        readonly HashSet<Health> seen = new();

        void OnEnable()
        {
            timer = Random.Range(1.5f, interval);
            GameEvents.AttackPerformed += OnAttack;
            GameEvents.ReactionTriggered += OnReaction;
        }

        // ---- o respiradouro reage aos elementos (como as tochas): Agua apaga por um tempo, Fogo faz entrar em erupcao ja
        public const float QuenchSeconds = 6f, Reach = 4.5f;
        float quenched;
        public bool Quenched => quenched > 0f;
        /// Quantas vezes apagou e quantas erupcoes o Fogo adiantou (para os testes).
        public int Quenches { get; private set; }
        public int FireTriggers { get; private set; }

        void OnAttack(AttackEvent e)
        {
            if (e.Kind != AttackKind.Skill) return;
            if (e.Element == ElementType.Water) Touch(e.Origin, Mathf.Max(Reach, e.Range + 1f), false);
            else if (e.Element == ElementType.Fire) Touch(e.Origin, Mathf.Max(Reach, e.Range + 1f), true);
        }

        void OnReaction(ReactionEvent e)
        {
            if (e.Mark == ElementType.Water || e.Trigger == ElementType.Water) Touch(e.Point, e.Radius + 2f, false);
        }

        /// Agua a ate reach do respiradouro: apaga por QuenchSeconds (chiado, sem aviso nem queimadura).
        /// Fogo: reacende se estava apagado e adianta a erupcao, sempre com o aviso inteiro (1,1 s): aviso curto seria
        /// injusto, e o circulo e o que deixa ler o perigo.
        public void Touch(Vector3 at, float reach, bool fire)
        {
            var d = transform.position - at; d.y = 0f;
            if (d.magnitude > reach) return;
            if (fire)
            {
                quenched = 0f;
                SetEmbers(true);
                if (timer > windup) { timer = windup; warned = false; GameEvents.RaiseTelegraphCancelled(gameObject); FireTriggers++; }
                return;
            }
            if (quenched <= 0f) { Quenches++; Sfx.Play("cast_water"); }
            quenched = QuenchSeconds;
            warned = false;
            GameEvents.RaiseTelegraphCancelled(gameObject);
            SetEmbers(false);
        }

        MaterialPropertyBlock dark;

        /// Apagado: o miolo em brasa ("Core") fica cinza e sem brilho; aceso, volta ao material dele.
        void SetEmbers(bool on)
        {
            foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                if (on) { if (!ps.isPlaying) ps.Play(); } else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            var core = transform.Find("Core");
            if (core == null || !core.TryGetComponent(out Renderer r)) return;
            if (on) { r.SetPropertyBlock(null); return; }
            dark ??= new MaterialPropertyBlock();
            dark.SetColor("_BaseColor", new Color(0.16f, 0.15f, 0.15f));
            dark.SetColor("_EmissionColor", Color.black);
            r.SetPropertyBlock(dark);
        }
        public bool CoreDark => transform.Find("Core") is Transform c && c.TryGetComponent(out Renderer r) && r.HasPropertyBlock();

        void Update()
        {
            if (quenched > 0f)
            {
                if ((quenched -= Time.deltaTime) > 0f) return;
                SetEmbers(true);
                timer = interval; // volta do comeco do ciclo: nao estoura logo ao reacender
            }
            timer -= Time.deltaTime;
            if (!warned && timer <= windup)
            {
                warned = true;
                GameEvents.RaiseTelegraph(new TelegraphEvent
                {
                    Point = transform.position, Direction = Vector3.forward, Radius = radius, Duration = Mathf.Max(0f, timer), Source = gameObject,
                });
            }
            if (timer > 0f) return;
            timer = interval;
            warned = false;
            Erupt();
        }

        void OnDisable()
        {
            GameEvents.AttackPerformed -= OnAttack;
            GameEvents.ReactionTriggered -= OnReaction;
            GameEvents.RaiseTelegraphCancelled(gameObject);
        }

        public int Eruptions { get; private set; }

        void Erupt()
        {
            Eruptions++;
            GameEvents.RaiseExploded(transform.position, radius);
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || !seen.Add(h)) continue;
                // so criatura: cristal, barril e boneco nao sao feridos pelo chao
                if (h.Team == Team.Enemy && h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue;
                var flat = h.transform.position - transform.position;
                flat.y = 0f;
                if (flat.magnitude > radius + 0.3f) continue;
                // o dano entra pelo time oposto ao do alvo, para valer nos dois lados
                var from = h.Team == Team.Player ? Team.Enemy : Team.Player;
                h.TakeDamage(new DamageInfo(damage, ElementType.Fire, from, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, Direction = flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward,
                    Knockback = 4f, IsReaction = true,
                });
            }
            // quem sobreviveu em cima fica marcado com Fogo (como a poca marca com Agua): o diretor aplica a marca
            GameEvents.RaiseVentErupted(transform.position, radius);
        }
    }
}
