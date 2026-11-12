using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Run
{
    /// Todas as bencaos do jogo. Os numeros sao editados no asset, nao no codigo.
    [CreateAssetMenu(menuName = "Quintessence/Boon Catalog")]
    public class BoonCatalog : ScriptableObject
    {
        public List<Boon> boons = new();

        /// Preenche com o conjunto inicial (usado ao criar o asset).
        public void FillDefaults()
        {
            boons.Clear();
            void Add(string id, string name, string text, ElementType patron, BoonStat stat, float value) =>
                boons.Add(new Boon { id = id, name = name, description = text, patron = patron, stat = stat, value = value });

            Add("fire_dmg", "Brasa Viva", "+20% de dano de Fogo", ElementType.Fire, BoonStat.DamageElement, 0.2f);
            Add("fire_react", "Fôlego do Forno", "Reações causam +15%", ElementType.Fire, BoonStat.ReactionPower, 0.15f);
            Add("fire_leech", "Sangue Quente", "Cura 3 a cada reação", ElementType.Fire, BoonStat.HealOnReaction, 3f);

            Add("water_dmg", "Corrente Fria", "+20% de dano de Água", ElementType.Water, BoonStat.DamageElement, 0.2f);
            Add("water_status", "Maré Longa", "Status duram +30%", ElementType.Water, BoonStat.StatusDuration, 0.3f);
            Add("water_rest", "Fonte Calma", "Cura 12% ao limpar a sala", ElementType.Water, BoonStat.HealOnRoomClear, 0.12f);

            Add("earth_dmg", "Punho de Granito", "+20% de dano de Terra", ElementType.Earth, BoonStat.DamageElement, 0.2f);
            Add("earth_hp", "Casca de Pedra", "+25 de vida máxima", ElementType.Earth, BoonStat.MaxHealth, 25f);
            Add("earth_skill", "Pulso da Montanha", "Skills recarregam 15% mais rápido", ElementType.Earth, BoonStat.SkillCooldown, 0.15f);

            Add("air_dmg", "Lâmina do Vento", "+20% de dano de Ar", ElementType.Air, BoonStat.DamageElement, 0.2f);
            Add("air_crit", "Olho do Falcão", "+10% de chance de crítico", ElementType.Air, BoonStat.CritChance, 0.1f);
            Add("air_speed", "Passo Leve", "+12% de velocidade", ElementType.Air, BoonStat.MoveSpeed, 0.12f);
            Add("air_dash", "Rajada", "Dash recarrega 25% mais rápido", ElementType.Air, BoonStat.DashCooldown, 0.25f);

            Add("fire_burning", "Carvão Vivo", "+20% de dano em quem está Queimando", ElementType.Fire, BoonStat.BurningBonus, 0.2f);
            Add("water_heal", "Orvalho", "Maré cura mais 4% da vida", ElementType.Water, BoonStat.WaterHeal, 0.04f);
            Add("earth_thorns", "Casca de Espinhos", "Golpe bloqueado devolve 10 de dano de Terra", ElementType.Earth, BoonStat.EarthThorns, 10f);
            Add("air_haste", "Rabo de Vento", "Vendaval dá 2 s de pressa", ElementType.Air, BoonStat.AirHaste, 2f);
            Add("all_basic", "Mão Firme", "Combo básico causa +12%", ElementType.None, BoonStat.BasicDamage, 0.12f);
            Add("all_finisher", "Golpe Final", "Terceiro golpe causa +25%", ElementType.None, BoonStat.Finisher, 0.25f);
            Add("all_xp", "Curiosidade", "+20% de experiência", ElementType.None, BoonStat.XpGain, 0.2f);
            Add("all_essence", "Faro de Ouro", "+25% de Essência", ElementType.None, BoonStat.EssenceGain, 0.25f);
            Add("all_refund", "Sede de Luta", "Cada abate adianta 0,3 s das skills", ElementType.None, BoonStat.SkillRefund, 0.3f);

            Add("all_dmg", "Golpe Certeiro", "+10% de dano em tudo", ElementType.None, BoonStat.DamageAll, 0.1f);
            Add("all_switch", "Mão Pesada", "Golpe básico em inimigo marcado causa +25%", ElementType.None, BoonStat.SwitchStrike, 0.25f);
            Add("all_quint", "Centelha Prismática", "Quintessência enche 30% mais rápido", ElementType.None, BoonStat.QuintessenceGain, 0.3f);
            Add("all_hp", "Coração Firme", "+15 de vida máxima", ElementType.None, BoonStat.MaxHealth, 15f);
            Add("all_react", "Eco Elemental", "Reações causam +10%", ElementType.None, BoonStat.ReactionPower, 0.1f);
            Add("all_skill", "Foco", "Skills recarregam 10% mais rápido", ElementType.None, BoonStat.SkillCooldown, 0.1f);
            Add("all_crit", "Instinto", "+6% de chance de crítico", ElementType.None, BoonStat.CritChance, 0.06f);
        }
    }
}
