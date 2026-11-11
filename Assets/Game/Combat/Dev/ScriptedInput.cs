using UnityEngine;

namespace Quintessence.Combat
{
    /// Entrada roteirizada para testes e gravacoes.
    public class ScriptedInput : MonoBehaviour, IPlayerInput
    {
        public Vector2 Move { get; set; }
        public Vector3 AimPoint { get; set; }
        public bool AttackHeld { get; set; }
        bool dash;
        public void PressDash() => dash = true;
        public bool ConsumeDash() { bool d = dash; dash = false; return d; }
    }
}
