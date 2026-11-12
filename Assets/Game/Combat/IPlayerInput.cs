using UnityEngine;

namespace Quintessence.Combat
{
    /// De onde vem o comando do heroi: teclado+mouse, controle ou roteiro de teste.
    public interface IPlayerInput
    {
        /// x = direita, y = frente (mundo), magnitude ate 1.
        Vector2 Move { get; }
        /// Ponto no chao para onde o heroi mira.
        Vector3 AimPoint { get; }
        bool AttackHeld { get; }
        /// True uma vez por aperto.
        bool ConsumeDash();
        /// Atalho generico: a primeira skill pronta.
        bool ConsumeSkill();
        /// 1 a 4 = skill de Fogo, Agua, Terra ou Ar; 0 = nada.
        int ConsumeElementSkill();
        bool ConsumeQuintessence();
        bool ConsumeInteract();
    }
}
