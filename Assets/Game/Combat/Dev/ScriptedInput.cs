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
        public void PressSkill() => skill = true;
        int elementSkill;
        /// 1 a 4: pede a skill daquele elemento.
        public void PressElementSkill(int slot) => elementSkill = slot;
        public int ConsumeElementSkill() { int d = elementSkill; elementSkill = 0; return d; }
        bool interact;
        public void PressInteract() => interact = true;
        public bool ConsumeInteract() { bool d = interact; interact = false; return d; }
        bool quint;
        public void PressQuintessence() => quint = true;
        public bool ConsumeQuintessence() { bool d = quint; quint = false; return d; }
        public bool ConsumeSkill() { bool d = skill; skill = false; return d; }
    }
}
