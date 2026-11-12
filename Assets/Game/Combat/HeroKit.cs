using Quintessence.Core;

namespace Quintessence.Combat
{
    /// O golpe basico de cada heroi. Continua sem elemento; o que muda e o ritmo, o alcance e a abertura.
    public class HeroKit
    {
        public ComboStep[] Steps;
        public float Range, Arc, CritChance;
        public string Style;
        /// Despertar da Quintessencia: a explosao do heroi ao apertar R (o clímax da run).
        public QuintBurst Burst;

        public float ComboDamage { get { float d = 0f; foreach (var s in Steps) d += s.Damage; return d; } }
        public float ComboTime { get { float t = 0f; foreach (var s in Steps) t += s.Duration; return t; } }

        public static HeroKit Of(ElementType hero) => hero switch
        {
            // Rax: garras. Curto e rapido, quase em cima do alvo.
            ElementType.Fire => new HeroKit
            {
                Steps = new[] { new ComboStep(6f, 0.23f, 0.09f, 1.5f), new ComboStep(6f, 0.23f, 0.09f, 1.5f), new ComboStep(11f, 0.36f, 0.14f, 4f) },
                Range = 2.8f, Arc = 130f, CritChance = 0.05f, Style = "Garras: golpes curtos e rápidos",
                Burst = new QuintBurst { Name = "Erupção do Caçador", Damage = 30f, Radius = 5f, Knockback = 4f, Applies = true, Status = StatusType.Burn, StatusTime = 4f },
            },
            // Liora: chicote d'agua saindo do orbe. Alcanca longe, numa faixa estreita.
            ElementType.Water => new HeroKit
            {
                Steps = new[] { new ComboStep(7f, 0.32f, 0.13f, 2f), new ComboStep(7f, 0.32f, 0.13f, 2f), new ComboStep(12f, 0.48f, 0.2f, 5f) },
                Range = 4.8f, Arc = 65f, CritChance = 0.05f, Style = "Chicote: alcança longe, numa faixa estreita",
                Burst = new QuintBurst { Name = "Maré da Fonte", Damage = 18f, Radius = 5.5f, Knockback = 3f, Heal = 0.15f, Applies = true, Status = StatusType.Soaked, StatusTime = 3f },
            },
            // Brann: martelo. Lento, largo e pesado.
            ElementType.Earth => new HeroKit
            {
                Steps = new[] { new ComboStep(11f, 0.42f, 0.18f, 4f), new ComboStep(11f, 0.42f, 0.18f, 4f), new ComboStep(19f, 0.6f, 0.26f, 9f) },
                Range = 3.5f, Arc = 150f, CritChance = 0.05f, Style = "Martelo: lento, largo e pesado",
                Burst = new QuintBurst { Name = "Rugido da Montanha", Damage = 24f, Radius = 4.5f, Knockback = 6f, Applies = true, Status = StatusType.Stun, StatusTime = 1.4f },
            },
            // Vey: duas adagas. O mais rapido, com mais critico.
            ElementType.Air => new HeroKit
            {
                Steps = new[] { new ComboStep(5.5f, 0.2f, 0.08f, 1f), new ComboStep(5.5f, 0.2f, 0.08f, 1f), new ComboStep(10f, 0.3f, 0.12f, 3f) },
                Range = 3.2f, Arc = 110f, CritChance = 0.22f, Style = "Adagas: o mais rápido, mais crítico",
                Burst = new QuintBurst { Name = "Olho do Vendaval", Damage = 18f, Radius = 6f, Knockback = 11f, Applies = true, Status = StatusType.Blinded, StatusTime = 3f },
            },
            _ => new HeroKit
            {
                Steps = new[] { new ComboStep(8f, 0.30f, 0.12f, 2f), new ComboStep(8f, 0.30f, 0.12f, 2f), new ComboStep(13f, 0.45f, 0.18f, 5f) },
                Range = 3.2f, Arc = 120f, CritChance = 0.05f, Style = "",
            },
        };
    }

    /// Explosao do despertar da Quintessencia, propria de cada heroi.
    public struct QuintBurst
    {
        public string Name;
        public float Damage, Radius, Knockback, StatusTime;
        /// Fracao da vida maxima curada na hora (Liora).
        public float Heal;
        public bool Applies;
        public StatusType Status;
    }
}
