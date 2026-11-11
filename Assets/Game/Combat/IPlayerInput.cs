using UnityEngine;

namespace Quintessence.Combat
{
    /// De onde vem o comando do heroi: teclado+mouse, gamepad ou roteiro de teste.
    public interface IPlayerInput
    {
        /// x = direita, y = frente (mundo), magnitude ate 1.
        Vector2 Move { get; }
        /// Ponto no chao para onde o heroi mira.
        Vector3 AimPoint { get; }
        bool AttackHeld { get; }
        /// True uma vez por aperto.
        bool ConsumeDash();
        bool ConsumeSkill();
        bool ConsumeQuintessence();
        bool ConsumeInteract();
        /// -1 = elemento anterior, +1 = proximo, 0 = nada.
        int ConsumeSwitch();
    }
}
