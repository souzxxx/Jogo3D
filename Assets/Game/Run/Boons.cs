using System;
using System.Collections.Generic;
using Quintessence.Core;

namespace Quintessence.Run
{
    public enum BoonStat
    {
        DamageAll, DamageElement, ReactionPower, CritChance, MoveSpeed, DashCooldown, SkillCooldown,
        MaxHealth, QuintessenceGain, HealOnReaction, HealOnRoomClear, SwitchStrike, StatusDuration,
        BasicDamage, Finisher, XpGain, EssenceGain, BurningBonus, WaterHeal, EarthThorns, AirHaste, SkillRefund,
    }

    [Serializable]
    public class Boon
    {
        public string id;
        public string name;
        public string description;
        /// Mestre que oferece. None = qualquer um. Bencao de um elemento so aparece se ele ja esta desperto.
        public ElementType patron;
        public BoonStat stat;
        public float value;
    }

    public static class BoonEffects
    {
        /// Nome do icone (Resources/Icons) que representa o efeito da bencao.
        public static string Icon(BoonStat stat) => stat switch
        {
            BoonStat.DamageAll => "boon_damage",
            BoonStat.DamageElement => "boon_element",
            BoonStat.ReactionPower => "boon_reaction",
            BoonStat.CritChance => "boon_crit",
            BoonStat.MoveSpeed => "boon_speed",
            BoonStat.DashCooldown => "boon_dash",
            BoonStat.SkillCooldown => "boon_skill",
            BoonStat.MaxHealth => "boon_health",
            BoonStat.QuintessenceGain => "boon_quint",
            BoonStat.HealOnReaction => "boon_leech",
            BoonStat.HealOnRoomClear => "boon_rest",
            BoonStat.SwitchStrike => "boon_switch",
            BoonStat.StatusDuration => "boon_status",
            BoonStat.BasicDamage => "boon_damage", BoonStat.Finisher => "boon_crit", BoonStat.XpGain => "boon_skill",
            BoonStat.EssenceGain => "essence", BoonStat.BurningBonus => "boon_element", BoonStat.WaterHeal => "boon_leech",
            BoonStat.EarthThorns => "boon_status", BoonStat.AirHaste => "boon_speed", BoonStat.SkillRefund => "boon_skill",
            _ => "boon",
        };

        public static void Apply(HeroModifiers m, Boon b)
        {
            switch (b.stat)
            {
                case BoonStat.DamageAll: m.DamageAll += b.value; break;
                case BoonStat.DamageElement: m.AddElementDamage(b.patron, b.value); break;
                case BoonStat.ReactionPower: m.ReactionPower += b.value; break;
                case BoonStat.CritChance: m.CritChanceAdd += b.value; break;
                case BoonStat.MoveSpeed: m.MoveSpeedMult += b.value; break;
                case BoonStat.DashCooldown: m.DashCooldownMult *= 1f - b.value; break;
                case BoonStat.SkillCooldown: m.SkillCooldownMult *= 1f - b.value; break;
                case BoonStat.MaxHealth: m.MaxHealthAdd += b.value; break;
                case BoonStat.QuintessenceGain: m.QuintessenceGainMult += b.value; break;
                case BoonStat.HealOnReaction: m.HealOnReaction += b.value; break;
                case BoonStat.HealOnRoomClear: m.HealOnRoomClear += b.value; break;
                case BoonStat.SwitchStrike: m.SwitchStrikeAdd += b.value; break;
                case BoonStat.StatusDuration: m.StatusDurationMult += b.value; break;
                case BoonStat.BasicDamage: m.BasicDamageMult += b.value; break;
                case BoonStat.Finisher: m.FinisherMult += b.value; break;
                case BoonStat.XpGain: m.XpMult += b.value; break;
                case BoonStat.EssenceGain: m.EssenceMult += b.value; break;
                case BoonStat.BurningBonus: m.BurningBonus += b.value; break;
                case BoonStat.WaterHeal: m.WaterHealAdd += b.value; break;
                case BoonStat.EarthThorns: m.EarthThorns += b.value; break;
                case BoonStat.AirHaste: m.AirHasteSeconds += b.value; break;
                case BoonStat.SkillRefund: m.SkillRefundOnKill += b.value; break;
            }
        }
    }

    public static class BoonPicker
    {
        /// Ate count bencaos diferentes, ainda nao pegas, de mestres neutros ou de elementos ja despertos.
        public static List<Boon> Offer(IReadOnlyList<Boon> catalog, ICollection<string> taken, IReadOnlyList<ElementType> awakened,
            System.Random rng, int count = 3)
        {
            var pool = new List<Boon>();
            foreach (var b in catalog)
            {
                if (taken.Contains(b.id)) continue;
                if (b.patron != ElementType.None && !Has(awakened, b.patron)) continue;
                pool.Add(b);
            }
            var offer = new List<Boon>();
            while (offer.Count < count && pool.Count > 0)
            {
                int i = rng.Next(pool.Count);
                offer.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return offer;
        }

        static bool Has(IReadOnlyList<ElementType> list, ElementType e)
        {
            foreach (var x in list) if (x == e) return true;
            return false;
        }
    }
}
