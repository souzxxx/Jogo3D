using Quintessence.Core;

namespace Quintessence.Run
{
    /// O que cada heroi traz de berco, alem da primeira skill. O heroi e definido pelo elemento inicial.
    public static class HeroPassives
    {
        public static string HeroName(ElementType e) => e switch
        {
            ElementType.Fire => "Rax", ElementType.Water => "Liora", ElementType.Earth => "Brann", ElementType.Air => "Vey", _ => "",
        };

        public static string Title(ElementType e) => e switch
        {
            ElementType.Fire => "Garras do Caçador", ElementType.Water => "Seiva da Fonte",
            ElementType.Earth => "Couro Velho", ElementType.Air => "Passo de Brisa", _ => "",
        };

        public static string Describe(ElementType e) => e switch
        {
            ElementType.Fire => "Combo básico causa +15%",
            ElementType.Water => "Cura 8% ao limpar a sala; Maré cura mais 3%",
            ElementType.Earth => "+20 de vida máxima",
            ElementType.Air => "+10% de velocidade, dash 20% mais rápido, 20% de esquiva, +10 de vida; cada esquiva cura 3%",
            _ => "",
        };

        /// Como o heroi golpeia (o kit em si mora no combate; aqui fica o texto para os menus).
        public static string Style(ElementType e) => e switch
        {
            ElementType.Fire => "Garras: golpes curtos e rápidos",
            ElementType.Water => "Chicote: alcança longe, numa faixa estreita",
            ElementType.Earth => "Martelo: lento, largo e pesado",
            ElementType.Air => "Adagas: o mais rápido, mais crítico",
            _ => "",
        };

        public static void Apply(ElementType hero, HeroModifiers m)
        {
            switch (hero)
            {
                case ElementType.Fire: m.BasicDamageMult += 0.15f; break;
                case ElementType.Water: m.HealOnRoomClear += 0.08f; m.WaterHealAdd += 0.03f; break;
                case ElementType.Earth: m.MaxHealthAdd += 20f; break;
                case ElementType.Air: m.MoveSpeedMult += 0.1f; m.DashCooldownMult *= 0.8f; m.Evasion += 0.2f; m.MaxHealthAdd += 10f; m.DodgeHeal += 0.03f; break;
            }
        }
    }
}
