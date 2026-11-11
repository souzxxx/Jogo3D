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
        bool skill;
        int sw;
        public void PressSkill() => skill = true;
        public void PressSwitch(int dir) => sw = dir;
        bool quint;
        public void PressQuintessence() => quint = true;
        public bool ConsumeQuintessence() { bool d = quint; quint = false; return d; }
        public bool ConsumeSkill() { bool d = skill; skill = false; return d; }
        public int ConsumeSwitch() { int d = sw; sw = 0; return d; }
    }
}
