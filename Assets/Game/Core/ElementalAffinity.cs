using UnityEngine;

namespace Quintessence.Core
{
    /// Inimigo elemental: resiste ao proprio elemento e e fraco ao elemento que o vence.
    /// Ciclo: Agua apaga Fogo, Terra absorve Agua, Ar desgasta Terra, Fogo consome Ar.
    public class ElementalAffinity : MonoBehaviour
    {
        public const float Resist = 0.5f, Weak = 1.6f;
        [SerializeField] ElementType element;
        public ElementType Element => element;
        public void Set(ElementType e) => element = e;

        /// O elemento que vence e.
        public static ElementType WeakTo(ElementType e) => e switch
        {
            ElementType.Fire => ElementType.Water,
            ElementType.Water => ElementType.Earth,
            ElementType.Earth => ElementType.Air,
            ElementType.Air => ElementType.Fire,
            _ => ElementType.None,
        };

        public static float Multiplier(ElementType own, ElementType attack)
        {
            if (own == ElementType.None || attack == ElementType.None) return 1f;
            if (attack == own) return Resist;
            return attack == WeakTo(own) ? Weak : 1f;
        }

        public float Multiplier(ElementType attack) => Multiplier(element, attack);

        /// O elemental de Fogo nao queima.
        public bool Immune(StatusType status) => element == ElementType.Fire && status == StatusType.Burn;
    }
}
