using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Marca elemental de um alvo. Adicionada sozinha no primeiro golpe elemental.
    [RequireComponent(typeof(Health))]
    public class ElementMark : MonoBehaviour
    {
        public MarkState State { get; } = new();
        /// Contagio (talento do heroi): raio em que a marca de quem cai pula para o vizinho. O heroi atualiza a cada quadro.
        public static float SpreadRadius;
        static readonly Collider[] buffer = new Collider[24];
        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void OnDestroy() { if (health != null) health.Died -= OnDied; }

        void OnDied(DamageInfo _)
        {
            if (!State.HasMark) return;
            var element = State.Element;
            State.Clear();
            GameEvents.RaiseMarkCleared(health);
            if (SpreadRadius > 0f) Spread(element);
        }

        /// A marca pula para o inimigo vivo mais perto (sem trocar a marca que ele ja tem de outro elemento).
        void Spread(ElementType element)
        {
            int n = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up, SpreadRadius, buffer, ~0, QueryTriggerInteraction.Ignore);
            Health best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var h = buffer[i].GetComponentInParent<Health>();
                if (h == null || h == health || h.IsDead || h.Team != health.Team) continue;
                if (h.GetComponent<UnityEngine.AI.NavMeshAgent>() == null) continue; // so criatura
                float d = (h.transform.position - transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = h; }
            }
            if (best == null) return;
            var mark = best.GetComponent<ElementMark>() ?? best.gameObject.AddComponent<ElementMark>();
            if (mark.State.HasMark && mark.State.Element != element) return;
            mark.State.Hit(element, out _);
            GameEvents.RaiseMarkApplied(element, best);
            Spreads++;
        }

        /// Quantas vezes uma marca pulou nesta sessao (para os testes).
        public static int Spreads;

        void Update()
        {
            if (State.Tick(Time.deltaTime)) GameEvents.RaiseMarkCleared(health);
        }
    }
}
