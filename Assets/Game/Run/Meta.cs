using System;
using Quintessence.Core;

namespace Quintessence.Run
{
    /// Melhorias permanentes do Refugio, compradas com Fragmentos entre uma run e outra.
    public enum MetaUpgrade { Vigor, Power, Purse, Focus, Recovery, Wisdom, Insight }

    public static class MetaInfo
    {
        public const int Count = 7;
        static readonly string[] names = { "Vigor", "Força", "Bolsa", "Foco", "Fôlego", "Sabedoria", "Intuição" };
        static readonly string[] icons = { "boon_health", "boon_damage", "essence", "boon_quint", "boon_rest", "boon_skill", "boon_reaction" };
        static readonly int[] maxLevel = { 5, 5, 3, 3, 3, 3, 2 };
        static readonly int[] baseCost = { 10, 12, 8, 10, 14, 10, 16 };
        static readonly int[] costStep = { 8, 10, 8, 10, 12, 10, 14 };
        /// Quanto cada nivel da: vida, fracao de dano, essencia, fracao de ganho, fracao de cura.
        static readonly float[] perLevel = { 8f, 0.04f, 8f, 0.1f, 0.03f, 0.08f, 1f };

        public static string Name(MetaUpgrade u) => names[(int)u];
        public static string Icon(MetaUpgrade u) => icons[(int)u];
        public static int MaxLevel(MetaUpgrade u) => maxLevel[(int)u];
        public static float PerLevel(MetaUpgrade u) => perLevel[(int)u];
        /// Preco do proximo nivel, partindo do nivel atual.
        public static int Cost(MetaUpgrade u, int level) => baseCost[(int)u] + costStep[(int)u] * level;

        public static string Describe(MetaUpgrade u, int level)
        {
            float v = perLevel[(int)u] * level;
            return u switch
            {
                MetaUpgrade.Vigor => $"+{v:0} de vida máxima",
                MetaUpgrade.Power => $"+{v * 100:0}% de dano",
                MetaUpgrade.Purse => $"começa com {v:0} de Essência",
                MetaUpgrade.Focus => $"Quintessência enche {v * 100:0}% mais rápido",
                MetaUpgrade.Recovery => $"cura {v * 100:0}% ao limpar a sala",
                MetaUpgrade.Wisdom => $"+{v * 100:0}% de experiência",
                _ => v <= 1f ? "1 troca de talentos por run" : $"{v:0} trocas de talentos por run",
            };
        }
    }

    /// O que sobrevive entre as runs. So regra: quem grava em disco e outro.
    public class MetaProgress
    {
        public const int ShardsPerRoom = 3;
        public const int ShardsForVictory = 15;

        readonly int[] levels = new int[MetaInfo.Count];
        public int Shards { get; private set; }
        public int Runs { get; private set; }
        public int Wins { get; private set; }
        public int BestRooms { get; private set; }

        public int Level(MetaUpgrade u) => levels[(int)u];
        public bool Maxed(MetaUpgrade u) => Level(u) >= MetaInfo.MaxLevel(u);
        public int Cost(MetaUpgrade u) => MetaInfo.Cost(u, Level(u));
        public bool CanBuy(MetaUpgrade u) => !Maxed(u) && Shards >= Cost(u);

        public bool TryBuy(MetaUpgrade u)
        {
            if (!CanBuy(u)) return false;
            Shards -= Cost(u);
            levels[(int)u]++;
            return true;
        }

        public void AddShards(int amount) { if (amount > 0) Shards += amount; }

        public static int Reward(int roomsCleared, bool victory) =>
            Math.Max(0, roomsCleared) * ShardsPerRoom + (victory ? ShardsForVictory : 0);

        /// Fecha uma run: soma os Fragmentos ganhos e atualiza os recordes. Devolve quanto ganhou.
        public int EndRun(int roomsCleared, bool victory, float shardMult = 1f)
        {
            int earned = (int)Math.Round(Reward(roomsCleared, victory) * Math.Max(1f, shardMult));
            Shards += earned;
            Runs++;
            if (victory) Wins++;
            if (roomsCleared > BestRooms) BestRooms = roomsCleared;
            return earned;
        }

        /// Quantas vezes por run da para trocar as tres opcoes de talento.
        public int Rerolls => (int)(MetaInfo.PerLevel(MetaUpgrade.Insight) * Level(MetaUpgrade.Insight));

        public int StartEssence => (int)(MetaInfo.PerLevel(MetaUpgrade.Purse) * Level(MetaUpgrade.Purse));

        public void ApplyTo(HeroModifiers m)
        {
            m.MaxHealthAdd += MetaInfo.PerLevel(MetaUpgrade.Vigor) * Level(MetaUpgrade.Vigor);
            m.DamageAll += MetaInfo.PerLevel(MetaUpgrade.Power) * Level(MetaUpgrade.Power);
            m.QuintessenceGainMult += MetaInfo.PerLevel(MetaUpgrade.Focus) * Level(MetaUpgrade.Focus);
            m.HealOnRoomClear += MetaInfo.PerLevel(MetaUpgrade.Recovery) * Level(MetaUpgrade.Recovery);
            m.XpMult += MetaInfo.PerLevel(MetaUpgrade.Wisdom) * Level(MetaUpgrade.Wisdom);
        }

        /// "v1;fragmentos;runs;vitorias;recorde;n0,n1,n2,n3,n4"
        public string Serialize() => $"v1;{Shards};{Runs};{Wins};{BestRooms};{string.Join(",", levels)}";

        /// Le um save. Texto vazio, cortado ou adulterado vira progresso zerado ou valores dentro dos limites, nunca erro.
        public static MetaProgress Parse(string text)
        {
            var p = new MetaProgress();
            if (string.IsNullOrEmpty(text)) return p;
            var parts = text.Split(';');
            if (parts.Length < 6 || parts[0] != "v1") return p;
            p.Shards = Read(parts[1], 0, 999999);
            p.Runs = Read(parts[2], 0, 999999);
            p.Wins = Read(parts[3], 0, p.Runs);
            p.BestRooms = Read(parts[4], 0, 999);
            var lv = parts[5].Split(',');
            for (int i = 0; i < MetaInfo.Count && i < lv.Length; i++)
                p.levels[i] = Read(lv[i], 0, MetaInfo.MaxLevel((MetaUpgrade)i));
            return p;
        }

        static int Read(string s, int min, int max) =>
            int.TryParse(s, out int v) ? Math.Min(max, Math.Max(min, v)) : min;
    }
}
