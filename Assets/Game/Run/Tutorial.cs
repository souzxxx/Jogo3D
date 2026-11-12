namespace Quintessence.Run
{
    /// Passos do treino, na ordem em que o jogo ensina.
    public enum TutorialStep { Move, Dash, Attack, Skill, React, Quintessence, Done }

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
            TutorialStep.Attack => "Atacar",
            TutorialStep.Skill => "Skill elemental",
            TutorialStep.React => "Reação",
            TutorialStep.Quintessence => "Quintessência",
            _ => "Treino concluído",
        };

        public static string Text(TutorialStep s) => s switch
        {
            TutorialStep.Move => "Ande pela sala com W A S D. O herói mira onde o mouse aponta.",
            TutorialStep.Dash => "Aperte Espaço (A no controle) para dar um dash. Durante o dash você não leva dano.",
            TutorialStep.Attack => "Clique esquerdo (RT no controle) para bater num boneco três vezes. O golpe básico não tem elemento.",
            TutorialStep.Skill => "Aperte 1 perto de um boneco: é a skill de Fogo. Ela deixa a marca do elemento no inimigo e tem recarga.",
            TutorialStep.React => "Você despertou a Água. Marque um boneco com 1 e acerte o mesmo boneco com 2: skills de elementos diferentes reagem.",
            TutorialStep.Quintessence => "Reações enchem a Quintessência. Com os quatro elementos e a barra cheia, aperte R.",
            _ => "É isso: marque com uma skill e detone com a de outro elemento.",
        };
    }
}
