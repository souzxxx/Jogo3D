using System.Collections.Generic;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Run;
using UnityEngine;

namespace Quintessence.Game
{
    /// Cristal de Ressonancia: um golpe estoura o cristal e ele solta o elemento do trecho em volta.
    /// Fere pouco, aplica o efeito do trecho e MARCA os inimigos com o elemento: uma skill de outro elemento logo depois
    /// detona reacoes no bando inteiro. Nao fere o heroi e nao conta como abate (nao anda pelo NavMesh).
    [RequireComponent(typeof(Health))]
    public class ResonanceCrystal : MonoBehaviour
    {
        [SerializeField] float radius = 3.6f;
        [SerializeField] float damage = 6f;
        [SerializeField] Renderer[] tinted = new Renderer[0];
        Health health;
        ElementType element = ElementType.Fire;
        static readonly Collider[] buffer = new Collider[32];
        public ElementType Element => element;
        public float Radius => radius;
        /// Quantos inimigos o estouro marcou (para os testes).
        public int Marked { get; private set; }

        public static ElementType ElementFor(Biome biome) => biome switch
        {
            Biome.Crypt => ElementType.Water,
            Biome.Garden => ElementType.Earth,
            _ => ElementType.Fire,
        };

        /// O efeito do trecho: Fogo queima, Agua encharca, Terra enraiza.
        public static StatusType StatusFor(ElementType e) => e switch
        {
            ElementType.Water => StatusType.Soaked,
            ElementType.Earth => StatusType.Rooted,
            _ => StatusType.Burn,
        };

        public void Bind(Renderer[] renderers) => tinted = renderers;

        /// Elemento do cristal e a cor dele (fosca, com um brilho baixo: nada de neon).
        public void SetElement(ElementType e)
        {
            element = e;
            var c = ElementColors.Of(e);
            var block = new MaterialPropertyBlock();
            foreach (var r in tinted)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", Color.Lerp(c, new Color(0.25f, 0.24f, 0.28f), 0.35f));
                block.SetColor("_EmissionColor", c * 0.35f);
                r.SetPropertyBlock(block);
            }
        }

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += _ => Burst();
        }

        void Burst()
        {
            var center = transform.position;
            // o estouro desenha como uma skill do elemento, em roda
            GameEvents.RaiseAttackPerformed(new AttackEvent { Origin = center, Direction = Vector3.forward, Element = element, Range = radius, Arc = 360f, Kind = AttackKind.Skill, Source = gameObject });
            Sfx.Play("shatter");
            int n = Physics.OverlapSphereNonAlloc(center + Vector3.up, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            var seen = new HashSet<Health>();
            var status = StatusFor(element);
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h == health || h.IsDead || h.Team != Team.Enemy || !seen.Add(h)) continue;
                if (h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue; // barril, outro cristal: nao
                var dir = h.transform.position - center; dir.y = 0f;
                if (dir.magnitude > radius + 0.4f) continue;
                h.TakeDamage(new DamageInfo(damage, element, Team.Player, gameObject)
                {
                    Point = h.transform.position + Vector3.up * 1.2f, IsReaction = true,
                });
                if (h.IsDead) continue;
                StatusHolder.GetOrAdd(h).Apply(status, 1.5f, Team.Player, gameObject);
                var mark = h.GetComponent<ElementMark>() ?? h.gameObject.AddComponent<ElementMark>();
                if (mark.State.HasMark && mark.State.Element != element) continue;
                mark.State.Hit(element, out _);
                GameEvents.RaiseMarkApplied(element, h);
                Marked++;
            }
            Shatter(center);
            Destroy(gameObject, 0.05f);
        }

        /// Quantas lascas o ultimo estouro soltou (para os testes).
        public int Shards { get; private set; }

        /// O cristal se parte: lascas finas na cor dele saltam e caem (mesma fisica dos ossos), fora do objeto que some.
        void Shatter(Vector3 center)
        {
            Material mat = null;
            foreach (var r in GetComponentsInChildren<MeshRenderer>()) if (r.sharedMaterial != null) { mat = r.sharedMaterial; break; }
            if (mat == null) return;
            for (int i = 0; i < 7; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(shard.GetComponent<Collider>());
                shard.name = "CrystalShard";
                float s = Random.Range(0.1f, 0.2f);
                shard.transform.SetPositionAndRotation(center + Vector3.up * Random.Range(0.4f, 1.3f) + Random.insideUnitSphere * 0.25f, Random.rotation);
                shard.transform.localScale = new Vector3(s * 0.6f, s * Random.Range(1.6f, 3f), s * 0.6f);
                var r = shard.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var v = Random.insideUnitSphere * Random.Range(2.5f, 5f) + Vector3.up * Random.Range(3f, 6f);
                shard.AddComponent<Quintessence.Feel.BonePiece>().Launch(v, Random.insideUnitSphere * 700f, Random.Range(1.1f, 1.6f), s * 0.3f);
            }
            Shards = 7;
        }
    }
}
