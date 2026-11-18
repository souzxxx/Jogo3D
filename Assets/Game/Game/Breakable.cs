using System.Collections.Generic;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Feel;
using UnityEngine;

namespace Quintessence.Game
{
    /// Caixote pequeno que quebra: golpe, skill ou dash do heroi que alcance o caixote, ou o heroi passando por cima.
    /// Sem vida e sem colisor de proposito: nao conta abate, nao da experiencia, nao entra na mira, nao mexe na fisica
    /// nem na malha de navegacao (rodada de criticas de 18/11). So o efeito: lascas, estalo e uma tremida leve.
    public class Breakable : MonoBehaviour
    {
        [SerializeField] float radius = 0.55f;
        static readonly List<Breakable> active = new();
        public static IReadOnlyList<Breakable> Active => active;
        /// Quantos quebraram desde o inicio da cena (para os testes).
        public static int Broken { get; private set; }
        public bool IsBroken { get; private set; }
        static Transform hero;
        static Material chips;

        void OnEnable()
        {
            active.Add(this);
            GameEvents.AttackPerformed += OnAttack;
            GameEvents.Exploded += OnBlast;
            GameEvents.ReactionTriggered += OnReaction;
        }

        void OnDisable()
        {
            active.Remove(this);
            GameEvents.AttackPerformed -= OnAttack;
            GameEvents.Exploded -= OnBlast;
            GameEvents.ReactionTriggered -= OnReaction;
        }

        // explosao (barril de polvora, Explosivo) e reacao de area tambem quebram o que estiver no raio
        void OnBlast(Vector3 point, float blastRadius) => BreakIfInside(point, blastRadius);
        void OnReaction(ReactionEvent e) => BreakIfInside(e.Point, e.Radius);

        void BreakIfInside(Vector3 point, float blastRadius)
        {
            if (IsBroken) return;
            var d = transform.position - point; d.y = 0f;
            if (d.magnitude <= blastRadius + radius) Break(d.sqrMagnitude > 0.001f ? d.normalized : Vector3.forward);
        }

        void OnAttack(AttackEvent e)
        {
            // golpes, skills e dash do heroi; e o estouro do Cristal de Ressonancia (em roda)
            if (IsBroken || e.Source == null) return;
            if (e.Source.GetComponent<PlayerController>() == null && e.Source.GetComponent<ResonanceCrystal>() == null) return;
            var to = transform.position - e.Origin; to.y = 0f;
            if (to.magnitude > e.Range + radius) return;
            if (e.Arc < 359f && to.sqrMagnitude > 0.36f && Vector3.Angle(e.Direction, to) > e.Arc * 0.5f + 12f) return;
            Break(to.sqrMagnitude > 0.001f ? to.normalized : e.Direction);
        }

        void Update()
        {
            if (IsBroken) return;
            if (hero == null) { var go = GameObject.FindGameObjectWithTag("Player"); if (go == null) return; hero = go.transform; }
            var d = hero.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude < (radius + 0.3f) * (radius + 0.3f)) { Break(-d.normalized); return; }
            // inimigo passando por cima (ou empurrado para cima) tambem quebra; nao da dano nem credito a ninguem
            if (Time.frameCount % 3 != 0) return;
            foreach (var h in Health.Active)
            {
                if (h == null || h.IsDead || h.Team != Team.Enemy || h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue;
                var e = h.transform.position - transform.position; e.y = 0f;
                if (e.sqrMagnitude < (radius + 0.35f) * (radius + 0.35f)) { Break(-e.normalized); return; }
            }
        }

        public void Break(Vector3 push)
        {
            if (IsBroken) return;
            IsBroken = true;
            Broken++;
            Sfx.Play("crate");
            CameraShake.Add(0.06f);
            if (chips == null)
            {
                var src = GetComponentInChildren<Renderer>();
                chips = src != null ? new Material(src.sharedMaterial) : null;
                if (chips != null)
                {
                    if (chips.HasProperty("_BaseMap")) chips.SetTexture("_BaseMap", null);
                    if (chips.HasProperty("_BaseColor")) chips.SetColor("_BaseColor", new Color(0.55f, 0.36f, 0.22f));
                }
            }
            var center = transform.position + Vector3.up * 0.35f;
            for (int i = 0; i < 7; i++)
            {
                var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(chip.GetComponent<Collider>());
                chip.name = "Chip";
                float s = Random.Range(0.08f, 0.2f);
                chip.transform.SetPositionAndRotation(center + Random.insideUnitSphere * 0.25f, Random.rotation);
                chip.transform.localScale = new Vector3(s * Random.Range(1f, 2.4f), s * 0.5f, s);
                var r = chip.GetComponent<Renderer>();
                if (chips != null) r.sharedMaterial = chips;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var v = (Random.insideUnitSphere + push * 1.2f) * Random.Range(2f, 4f) + Vector3.up * Random.Range(3f, 5.5f);
                chip.AddComponent<BonePiece>().Launch(v, Random.insideUnitSphere * 600f, Random.Range(1f, 1.5f), s * 0.25f);
            }
            Destroy(gameObject);
        }

        /// Zera a contagem (testes).
        public static void ResetCount() => Broken = 0;
    }
}
