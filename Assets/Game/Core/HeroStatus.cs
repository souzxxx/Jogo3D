using System.Collections.Generic;

namespace Quintessence.Core
{
    /// O que o HUD e a run precisam saber do heroi, sem depender da frente de combate.
    public interface IHeroStatus
    {
        Health Health { get; }
        ElementType CurrentElement { get; }
        IReadOnlyList<ElementType> AwakenedElements { get; }
        /// 0 = pronta, 1 = acabou de usar.
        float SkillCooldown01(ElementType element);
        int DashCharges { get; }
        int DashMaxCharges { get; }
        /// Inativa: quanto do medidor esta cheio. Ativa: tempo restante.
        float Quintessence01 { get; }
        bool QuintessenceActive { get; }
        bool QuintessenceReady { get; }
        HeroModifiers Modifiers { get; }
    }

    /// Pausa do jogo. Quem mexe em Time.timeScale (pausa de impacto) consulta aqui antes de devolver o tempo.
    public static class GameClock
    {
        public static bool Paused { get; private set; }
        /// Quadro em que a pausa terminou: a tecla ou o botao que fechou o menu nao pode virar comando de jogo.
        public static int ResumedFrame { get; private set; } = -1;

        public static void SetPaused(bool paused)
        {
            if (Paused && !paused) ResumedFrame = UnityEngine.Time.frameCount;
            Paused = paused;
            UnityEngine.Time.timeScale = paused ? 0f : 1f;
        }
    }
}
