using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Marca elemental de um alvo. Adicionada sozinha no primeiro golpe elemental.
    [RequireComponent(typeof(Health))]
    public class ElementMark : MonoBehaviour
    {
        public MarkState State { get; } = new();
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
            State.Clear();
            GameEvents.RaiseMarkCleared(health);
        }

        void Update()
        {
            if (State.Tick(Time.deltaTime)) GameEvents.RaiseMarkCleared(health);
        }
    }
}
