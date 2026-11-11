namespace Quintessence.Run
{
    /// Passos do treino, na ordem em que o jogo ensina.
    public enum TutorialStep { Move, Dash, Attack, Skill, Switch, React, Quintessence, Done }

    /// Em que passo o treino esta. So avanca com o sinal do passo atual: fazer outra coisa nao pula licao.
    public class TutorialState
    {
        public const int StepCount = (int)TutorialStep.Done;
        public const float MoveDistance = 6f;
        public const int HitsNeeded = 3;

        public TutorialStep Step { get; private set; } = TutorialStep.Move;
        public bool Finished => Step == TutorialStep.Done;
        public int Number => System.Math.Min((int)Step + 1, StepCount);
        float progress;

        /// Quanto falta do passo atual, de 0 a 1 (so Mover e Atacar tem meio do caminho).
        public float Progress01 => Step switch
        {
            TutorialStep.Move => System.Math.Min(1f, progress / MoveDistance),
            TutorialStep.Attack => System.Math.Min(1f, progress / HitsNeeded),
            _ => 0f,
        };

        /// Conta um sinal do passo dado. Devolve true se o treino avancou de passo.
        public bool Report(TutorialStep signal, float amount = 1f)
        {
            if (Finished || signal != Step || amount <= 0f) return false;
            progress += amount;
            float need = Step == TutorialStep.Move ? MoveDistance : Step == TutorialStep.Attack ? HitsNeeded : 1f;
            if (progress < need) return false;
            progress = 0f;
            Step++;
            return true;
        }

        public void Skip() { Step = TutorialStep.Done; progress = 0f; }

        public static string Title(TutorialStep s) => s switch
        {
            TutorialStep.Move => "Mover",
            TutorialStep.Dash => "Dash",
            TutorialStep.Attack => "Atacar e marcar",
            TutorialStep.Skill => "Skill",
            TutorialStep.Switch => "Trocar de elemento",
            TutorialStep.React => "Reação",
            TutorialStep.Quintessence => "Quintessência",
            _ => "Treino concluído",
        };

        public static string Text(TutorialStep s) => s switch
        {
            TutorialStep.Move => "Ande pela sala com W A S D. O herói mira onde o mouse aponta.",
            TutorialStep.Dash => "Aperte Espaço para dar um dash. Durante o dash você não leva dano.",
            TutorialStep.Attack => "Clique para bater num boneco três vezes. Cada golpe deixa nele a marca do seu elemento.",
            TutorialStep.Skill => "Botão direito usa a skill do elemento. Ela tem recarga: veja o ícone na barra.",
            TutorialStep.Switch => "Você despertou um segundo elemento. Aperte Q ou E para trocar.",
            TutorialStep.React => "Bata com o elemento novo num boneco que já tem a marca do outro. Marca + elemento diferente = reação.",
            TutorialStep.Quintessence => "Reações enchem a Quintessência. Com os quatro elementos e a barra cheia, aperte R.",
            _ => "É isso: marque com um elemento, troque e detone com outro.",
        };
    }
}
