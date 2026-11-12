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

    /// Maestria de cada heroi: experiencia acumulada em todas as runs com ele, em cinco niveis.
    public static class Mastery
    {
        public const int MaxLevel = 5;
        static readonly int[] thresholds = { 0, 15, 40, 80, 140, 220 };

        /// Experiencia que uma run da: uma por sala vencida, uma a cada 10 abates e 10 pela vitoria.
        public static int RunXp(int roomsCleared, int kills, bool victory) => Math.Max(0, roomsCleared) + Math.Max(0, kills) / 10 + (victory ? 10 : 0);

        public static int LevelFor(int xp)
        {
            int level = 0;
            for (int i = 1; i < thresholds.Length; i++) if (xp >= thresholds[i]) level = i;
            return level;
        }

        /// Experiencia que falta para o proximo nivel (0 no maximo) e a fracao andada nele.
        public static (int needed, float fraction) Progress(int xp)
        {
            int level = LevelFor(xp);
            if (level >= MaxLevel) return (0, 1f);
            int from = thresholds[level], to = thresholds[level + 1];
            return (to - xp, (xp - from) / (float)(to - from));
        }

        public static string Perk(int level) => level switch
        {
            1 => "+10 de vida máxima",
            2 => "um talento à escolha no começo da run",
            3 => "+8% de dano",
            4 => "+1 troca de talentos por run",
            5 => "começa com 30 de Essência",
            _ => "",
        };

        public static void ApplyTo(int level, HeroModifiers m)
        {
            if (level >= 1) m.MaxHealthAdd += 10f;
            if (level >= 3) m.DamageAll += 0.08f;
        }

        public static int StartTalents(int level) => level >= 2 ? 1 : 0;
        public static int ExtraRerolls(int level) => level >= 4 ? 1 : 0;
        public static int StartEssence(int level) => level >= 5 ? 30 : 0;
    }

    public enum Feat { FirstWin, AllHeroes, MasteryFive, Ascended, Quintessence, Trial, Fusion, Horde, ShieldBreak, Collector }

    /// O que uma run fez, para conferir os Feitos no fim dela.
    public struct RunRecord
    {
        public bool Victory;
        public int Ascension, Kills, Talents, Quintessences, Fusions, TrialsPassed, ShieldBreaks;
    }

    /// Feitos: metas permanentes, cada uma paga Fragmentos uma vez so.
    public static class Feats
    {
        public const int Count = 10;
        public const int Reward = 10;

        public static string Name(Feat f) => f switch
        {
            Feat.FirstWin => "Primeira Chama", Feat.AllHeroes => "Quatro Caminhos", Feat.MasteryFive => "Mestre",
            Feat.Ascended => "Ascendido", Feat.Quintessence => "O Quinto Estado", Feat.Trial => "Provado",
            Feat.Fusion => "Alquimista", Feat.Horde => "Ceifa", Feat.ShieldBreak => "Escudo Partido", Feat.Collector => "Arsenal",
            _ => "",
        };

        public static string Describe(Feat f) => f switch
        {
            Feat.FirstWin => "Vença uma run",
            Feat.AllHeroes => "Vença com os quatro heróis",
            Feat.MasteryFive => "Leve um herói à maestria 5",
            Feat.Ascended => "Vença na Ascensão 3 ou acima",
            Feat.Quintessence => "Desperte a Quintessência",
            Feat.Trial => "Cumpra uma Provação",
            Feat.Fusion => "Faça uma Fusão com o terceiro elemento",
            Feat.Horde => "Derrote 100 inimigos numa run",
            Feat.ShieldBreak => "Quebre o escudo de um chefe final",
            Feat.Collector => "Termine uma run com 6 talentos",
            _ => "",
        };

        /// Quais feitos a run cumpriu (com o progresso ja somado da run).
        public static System.Collections.Generic.List<Feat> Earned(RunRecord r, MetaProgress p)
        {
            var list = new System.Collections.Generic.List<Feat>();
            void Check(Feat f, bool ok) { if (ok && !p.Has(f)) list.Add(f); }
            Check(Feat.FirstWin, r.Victory);
            Check(Feat.AllHeroes, p.HeroWins(ElementType.Fire) > 0 && p.HeroWins(ElementType.Water) > 0 && p.HeroWins(ElementType.Earth) > 0 && p.HeroWins(ElementType.Air) > 0);
            Check(Feat.MasteryFive, p.MasteryLevel(ElementType.Fire) >= Mastery.MaxLevel || p.MasteryLevel(ElementType.Water) >= Mastery.MaxLevel
                || p.MasteryLevel(ElementType.Earth) >= Mastery.MaxLevel || p.MasteryLevel(ElementType.Air) >= Mastery.MaxLevel);
            Check(Feat.Ascended, r.Victory && r.Ascension >= 3);
            Check(Feat.Quintessence, r.Quintessences > 0);
            Check(Feat.Trial, r.TrialsPassed > 0);
            Check(Feat.Fusion, r.Fusions > 0);
            Check(Feat.Horde, r.Kills >= 100);
            Check(Feat.ShieldBreak, r.ShieldBreaks > 0);
            Check(Feat.Collector, r.Talents >= 6);
            return list;
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
        readonly int[] heroWins = new int[5];
        readonly int[] heroXp = new int[5];
        /// Experiencia de maestria acumulada com o heroi.
        public int HeroXp(ElementType hero) => (int)hero >= 1 && (int)hero <= 4 ? heroXp[(int)hero] : 0;
        public int MasteryLevel(ElementType hero) => Mastery.LevelFor(HeroXp(hero));

        /// Soma a experiencia da run ao heroi. Devolve quantos niveis de maestria ele subiu.
        public int AddHeroXp(ElementType hero, int xp)
        {
            if ((int)hero < 1 || (int)hero > 4 || xp <= 0) return 0;
            int before = MasteryLevel(hero);
            heroXp[(int)hero] += xp;
            return MasteryLevel(hero) - before;
        }
        /// Vitorias com o heroi daquele elemento.
        public int HeroWins(ElementType hero) => (int)hero >= 1 && (int)hero <= 4 ? heroWins[(int)hero] : 0;

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
        public int EndRun(int roomsCleared, bool victory, float shardMult = 1f, ElementType hero = ElementType.None)
        {
            if (victory && (int)hero >= 1 && (int)hero <= 4) heroWins[(int)hero]++;
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

        /// "v1;fragmentos;runs;vitorias;recorde;niveis;vitorias por heroi;maestria por heroi" (os dois ultimos sao opcionais: saves antigos nao tem)
        public string Serialize() => $"v1;{Shards};{Runs};{Wins};{BestRooms};{string.Join(",", levels)};{heroWins[1]},{heroWins[2]},{heroWins[3]},{heroWins[4]};{heroXp[1]},{heroXp[2]},{heroXp[3]},{heroXp[4]};{feats}";

        int feats;
        public bool Has(Feat f) => (feats & (1 << (int)f)) != 0;
        public int FeatCount { get { int n = 0; for (int i = 0; i < Feats.Count; i++) if ((feats & (1 << i)) != 0) n++; return n; } }

        /// Marca os feitos novos da run e paga os Fragmentos de cada um. Devolve os novos.
        public System.Collections.Generic.List<Feat> GrantFeats(RunRecord r)
        {
            var earned = Feats.Earned(r, this);
            foreach (var f in earned) { feats |= 1 << (int)f; Shards += Feats.Reward; }
            return earned;
        }

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
            if (parts.Length >= 7)
            {
                var hw = parts[6].Split(',');
                for (int i = 0; i < 4 && i < hw.Length; i++) p.heroWins[i + 1] = Read(hw[i], 0, p.Wins);
            }
            if (parts.Length >= 8)
            {
                var hx = parts[7].Split(',');
                for (int i = 0; i < 4 && i < hx.Length; i++) p.heroXp[i + 1] = Read(hx[i], 0, 99999);
            }
            if (parts.Length >= 9) p.feats = Read(parts[8], 0, (1 << Feats.Count) - 1);
            return p;
        }

        static int Read(string s, int min, int max) =>
            int.TryParse(s, out int v) ? Math.Min(max, Math.Max(min, v)) : min;
    }
}
