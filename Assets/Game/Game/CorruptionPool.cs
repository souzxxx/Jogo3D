using System.Collections.Generic;
using Quintessence.Core;
using Quintessence.Run;
using UnityEngine;

namespace Quintessence.Game
{
    /// Poca de corrupcao no chao: fere quem estiver dentro, heroi ou inimigo, a cada meio segundo.
    /// Toma a cor do trecho; queima nos Saloes e no Jardim, encharca (deixa lento) na Cripta.
    public class CorruptionPool : MonoBehaviour
    {
        [SerializeField] Vector3 size = new(6f, 1.2f, 1.8f);
        [SerializeField] float interval = 0.5f;
        [SerializeField] float damage = 2.5f;
        [Tooltip("Dano no heroi por pulso: menor que nos inimigos, a poca e para atrair o bando.")]
        [SerializeField] float heroDamage = 1.5f;
        [SerializeField] Renderer[] surfaces = new Renderer[0];
        [SerializeField] Renderer[] veins = new Renderer[0];

        float tick;
        StatusType status = StatusType.Burn;
        static readonly Collider[] buffer = new Collider[24];
        readonly HashSet<Health> seen = new();
        public int Pulses { get; private set; }
        public StatusType Status => status;

        public void Bind(Vector3 area, Renderer[] ground, Renderer[] glowVeins) { size = area; surfaces = ground; veins = glowVeins; }

        void Start()
        {
            var dresser = FindFirstObjectByType<BiomeDresser>();
            var c = dresser != null ? dresser.CrackColor : new Color(1f, 0.16f, 0.02f);
            if (dresser != null && dresser.Current == Biome.Crypt) status = StatusType.Soaked;
            // chao chamuscado com um fundo da cor do trecho; os veios brilham como as rachaduras da sala
            var block = new MaterialPropertyBlock();
            void Tint(Renderer[] list, Color baseColor, Color glow)
            {
                foreach (var r in list)
                {
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", baseColor);
                    block.SetColor("_EmissionColor", glow);
                    r.SetPropertyBlock(block);
                }
            }
            Tint(surfaces, Color.Lerp(new Color(0.12f, 0.08f, 0.08f), c, 0.12f), c * 0.12f);
            Tint(veins, c * 0.6f, c * 1.1f);
        }

        void Update()
        {
            if ((tick -= Time.deltaTime) > 0f) return;
            tick = interval;
            Pulses++;
            int n = Physics.OverlapBoxNonAlloc(transform.position + Vector3.up * 0.6f, size * 0.5f, buffer, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            seen.Clear();
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h.IsDead || !seen.Add(h)) continue;
                // so criatura: barril e boneco nao se queimam na poca
                if (h.Team == Team.Enemy && h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue;
                var from = h.Team == Team.Player ? Team.Enemy : Team.Player;
                bool hero = h.Team == Team.Player;
                h.TakeDamage(new DamageInfo(hero ? heroDamage : damage, ElementType.None, from, gameObject) { Point = h.transform.position + Vector3.up * 0.4f, IsReaction = true });
                // no heroi so a Cripta deixa efeito (encharca); a queimadura e para os inimigos
                if (!h.IsDead && (!hero || status == StatusType.Soaked)) StatusHolder.GetOrAdd(h).Apply(status, 1.2f, from, gameObject);
            }
        }

        /// O ponto esta dentro da area que fere (para a sonda de balanceamento desviar).
        public bool Contains(Vector3 p)
        {
            var local = Quaternion.Inverse(transform.rotation) * (p - transform.position);
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.z * 0.5f;
        }

        /// Direcao (no mundo) para sair da poca pela borda mais perto.
        public Vector3 ExitDirection(Vector3 p)
        {
            var local = Quaternion.Inverse(transform.rotation) * (p - transform.position);
            float toX = size.x * 0.5f - Mathf.Abs(local.x), toZ = size.z * 0.5f - Mathf.Abs(local.z);
            var dir = toX < toZ ? new Vector3(local.x >= 0f ? 1f : -1f, 0, 0) : new Vector3(0, 0, local.z >= 0f ? 1f : -1f);
            return transform.rotation * dir;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.up * 0.6f, size);
        }
    }
}
