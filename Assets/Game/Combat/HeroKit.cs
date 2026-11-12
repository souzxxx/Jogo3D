using Quintessence.Core;

namespace Quintessence.Combat
{
    /// O golpe basico de cada heroi. Continua sem elemento; o que muda e o ritmo, o alcance e a abertura.
    public class HeroKit
    {
        public ComboStep[] Steps;
        public float Range, Arc, CritChance;
        public string Style;

        public float ComboDamage { get { float d = 0f; foreach (var s in Steps) d += s.Damage; return d; } }
        public float ComboTime { get { float t = 0f; foreach (var s in Steps) t += s.Duration; return t; } }

        public static HeroKit Of(ElementType hero) => hero switch
        {
            // Rax: garras. Curto e rapido, quase em cima do alvo.
            ElementType.Fire => new HeroKit
            {
                Steps = new[] { new ComboStep(6f, 0.23f, 0.09f, 1.5f), new ComboStep(6f, 0.23f, 0.09f, 1.5f), new ComboStep(11f, 0.36f, 0.14f, 4f) },
                Range = 2.8f, Arc = 130f, CritChance = 0.05f, Style = "Garras: golpes curtos e rápidos",
            },
            // Liora: chicote d'agua saindo do orbe. Alcanca longe, numa faixa estreita.
            ElementType.Water => new HeroKit
            {
                Steps = new[] { new ComboStep(7f, 0.32f, 0.13f, 2f), new ComboStep(7f, 0.32f, 0.13f, 2f), new ComboStep(12f, 0.48f, 0.2f, 5f) },
                Range = 4.8f, Arc = 50f, CritChance = 0.05f, Style = "Chicote: alcança longe, numa faixa estreita",
            },
            // Brann: martelo. Lento, largo e pesado.
            ElementType.Earth => new HeroKit
            {
                Steps = new[] { new ComboStep(11f, 0.42f, 0.18f, 4f), new ComboStep(11f, 0.42f, 0.18f, 4f), new ComboStep(19f, 0.6f, 0.26f, 9f) },
                Range = 3.5f, Arc = 150f, CritChance = 0.05f, Style = "Martelo: lento, largo e pesado",
            },
            // Vey: duas adagas. O mais rapido, com mais critico.
            ElementType.Air => new HeroKit
            {
                Steps = new[] { new ComboStep(5f, 0.2f, 0.08f, 1f), new ComboStep(5f, 0.2f, 0.08f, 1f), new ComboStep(9f, 0.3f, 0.12f, 3f) },
                Range = 3f, Arc = 95f, CritChance = 0.15f, Style = "Adagas: o mais rápido, mais crítico",
            },
            _ => new HeroKit
            {
                Steps = new[] { new ComboStep(8f, 0.30f, 0.12f, 2f), new ComboStep(8f, 0.30f, 0.12f, 2f), new ComboStep(13f, 0.45f, 0.18f, 5f) },
                Range = 3.2f, Arc = 120f, CritChance = 0.05f, Style = "",
            },
        };
    }
}
