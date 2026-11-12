using System;
using System.Collections.Generic;
using Quintessence.Core;

namespace Quintessence.Run
{
    /// Nivel do heroi dentro da run: inimigos derrotados dao experiencia, cada nivel da um talento para escolher.
    public class Progression
    {
        public const int MaxLevel = 12;
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        /// Talentos ganhos e ainda nao escolhidos.
        public int PendingPicks { get; private set; }
        public int TotalXp { get; private set; }

        public static int XpFor(int level) => 18 + 10 * (level - 1);
        public int XpToNext => XpFor(Level);
        public bool AtMax => Level >= MaxLevel;
        public float Normalized => AtMax ? 1f : (float)Xp / XpToNext;

        /// Soma experiencia. Devolve quantos niveis subiu.
        public int AddXp(int amount)
        {
            if (amount <= 0 || AtMax) return 0;
            TotalXp += amount;
            Xp += amount;
            int gained = 0;
            while (!AtMax && Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                gained++;
            }
            if (AtMax) Xp = 0;
            PendingPicks += gained;
            return gained;
        }

        /// Volta um save: nivel, experiencia, escolhas pendentes e total.
        public void Restore(int level, int xp, int pending, int total)
        {
            Level = Math.Max(1, Math.Min(MaxLevel, level));
            Xp = Math.Max(0, xp); PendingPicks = Math.Max(0, pending); TotalXp = Math.Max(0, total);
        }

        /// Talento extra sem subir de nivel (pacto).
        public void GrantPick() => PendingPicks++;

        public bool ConsumePick()
        {
            if (PendingPicks <= 0) return false;
            PendingPicks--;
            return true;
        }

        /// Experiencia de um inimigo, pela vida que ele tem na ficha (sem a escala da sala).
        public static int XpForEnemy(float baseMaxHealth, bool boss = false)
        {
            if (boss) return 60;
            return Math.Max(2, (int)Math.Round(baseMaxHealth / 7f));
        }
    }

    public enum TalentEffect
    {
        BasicDamage, Finisher, MaxHealth, SkillRefundOnKill, Essence,
        FireTrail, FireRange, BurningBonus,
        WaterHeal, WaterFreeze, WaterCooldown,
        EarthBlocks, EarthRange, EarthThorns,
        AirDashCharge, AirRange, AirHaste,
        ReactionBoost,
        Flank, SureFooted, BurstPower, Contagion, Alchemy,
    }

    public class Talent
    {
        public string Id, Name, Description;
        /// None = talento de corpo, sempre disponivel. Os de elemento so aparecem com o elemento desperto.
        public ElementType Element;
        /// Talento de reacao: o segundo elemento do par. Aparece so com os dois despertos.
        public ElementType Second;
        /// Nome curto para o cartao (o da reacao, nos talentos de reacao).
        public string Tag = "";
        public TalentEffect Effect;
        public float Value;
        public int MaxRank = 1;
    }

    /// O que o heroi ja escolheu na run.
    public class TalentBook
    {
        readonly Dictionary<string, int> ranks = new();
        public readonly List<Talent> Taken = new();

        public int Rank(string id) => ranks.TryGetValue(id, out int r) ? r : 0;
        public int Count => Taken.Count;
        public bool CanTake(Talent t) => Rank(t.Id) < t.MaxRank;

        /// Volta um talento de um save sem aplicar de novo (os modificadores ja vem na foto).
        public void Restore(Talent t)
        {
            if (t == null) return;
            ranks[t.Id] = Rank(t.Id) + 1;
            Taken.Add(t);
        }

        public bool Take(Talent t, HeroModifiers mods)
        {
            if (!CanTake(t)) return false;
            ranks[t.Id] = Rank(t.Id) + 1;
            Taken.Add(t);
            TalentCatalog.Apply(mods, t);
            return true;
        }
    }

    public static class TalentCatalog
    {
        static List<Talent> all;
        public static IReadOnlyList<Talent> All => all ??= Build();

        static List<Talent> Build()
        {
            var list = new List<Talent>();
            void Add(string id, string name, string text, ElementType e, TalentEffect fx, float value, int max) =>
                list.Add(new Talent { Id = id, Name = name, Description = text, Element = e, Effect = fx, Value = value, MaxRank = max });

            Add("body_basic", "Golpe Treinado", "Combo básico causa +12%", ElementType.None, TalentEffect.BasicDamage, 0.12f, 3);
            Add("body_finisher", "Finalizador Brutal", "Terceiro golpe causa +30% e empurra mais", ElementType.None, TalentEffect.Finisher, 0.3f, 2);
            Add("body_health", "Vigor", "+12 de vida máxima", ElementType.None, TalentEffect.MaxHealth, 12f, 3);
            Add("body_refund", "Ímpeto", "Cada inimigo derrotado adianta 0,4 s das skills", ElementType.None, TalentEffect.SkillRefundOnKill, 0.4f, 2);
            Add("body_essence", "Colecionador", "+20% de Essência", ElementType.None, TalentEffect.Essence, 0.2f, 2);
            Add("body_flank", "Flanqueador", "Golpes pelas costas do alvo causam +25%", ElementType.None, TalentEffect.Flank, 0.25f, 2);
            Add("body_footing", "Pés Firmes", "Encharcado, você não fica lento", ElementType.None, TalentEffect.SureFooted, 1f, 1);
            Add("body_awaken", "Despertar Intenso", "A explosão da Quintessência causa +50%", ElementType.None, TalentEffect.BurstPower, 0.5f, 2);
            Add("body_contagion", "Contágio", "Quando um inimigo marcado cai, a marca pula para o vizinho mais perto", ElementType.None, TalentEffect.Contagion, 5f, 1);
            Add("body_alchemy", "Alquimia", "Fusões causam +40%", ElementType.None, TalentEffect.Alchemy, 0.4f, 2);

            Add("fire_trail", "Rastro de Brasa", "Labareda deixa o chão em chamas por 3 s", ElementType.Fire, TalentEffect.FireTrail, 3f, 1);
            Add("fire_range", "Fornalha", "Labareda alcança 20% mais longe", ElementType.Fire, TalentEffect.FireRange, 0.2f, 2);
            Add("fire_burning", "Combustão", "+15% de dano em quem está Queimando", ElementType.Fire, TalentEffect.BurningBonus, 0.15f, 2);

            Add("water_heal", "Maré Viva", "Maré cura mais 4% da vida", ElementType.Water, TalentEffect.WaterHeal, 0.04f, 3);
            Add("water_freeze", "Água Gelada", "Maré congela por 1,2 s quem ela acerta", ElementType.Water, TalentEffect.WaterFreeze, 1.2f, 1);
            Add("water_cooldown", "Ressaca", "Maré recarrega 20% mais rápido", ElementType.Water, TalentEffect.WaterCooldown, 0.2f, 2);

            Add("earth_blocks", "Muralha", "Pilar de Pedra bloqueia mais um golpe", ElementType.Earth, TalentEffect.EarthBlocks, 1f, 2);
            Add("earth_range", "Tremor", "Pilar de Pedra atinge uma área 25% maior", ElementType.Earth, TalentEffect.EarthRange, 0.25f, 2);
            Add("earth_thorns", "Espinhos de Pedra", "Golpe bloqueado devolve 14 de dano de Terra em volta", ElementType.Earth, TalentEffect.EarthThorns, 14f, 2);

            Add("air_dash", "Asas", "+1 carga de dash", ElementType.Air, TalentEffect.AirDashCharge, 1f, 1);
            Add("air_range", "Olho da Tormenta", "Vendaval atinge uma área 25% maior", ElementType.Air, TalentEffect.AirRange, 0.25f, 2);
            void Pair(string id, string name, string reaction, ElementType a, ElementType b) =>
                list.Add(new Talent
                {
                    Id = id, Name = name, Description = reaction + " causa +40% e pega uma área maior", Element = a, Second = b,
                    Effect = TalentEffect.ReactionBoost, Value = 0.4f, MaxRank = 2, Tag = reaction,
                });
            Pair("react_storm", "Olho da Fornalha", "Tempestade de Fogo", ElementType.Fire, ElementType.Air);
            Pair("react_steam", "Névoa Densa", "Vapor", ElementType.Fire, ElementType.Water);
            Pair("react_magma", "Coração de Lava", "Magma", ElementType.Fire, ElementType.Earth);
            Pair("react_mud", "Atoleiro", "Lama", ElementType.Water, ElementType.Earth);
            Pair("react_blizzard", "Inverno Profundo", "Nevasca", ElementType.Water, ElementType.Air);
            Pair("react_shards", "Chuva de Pedra", "Estilhaços", ElementType.Earth, ElementType.Air);
            Add("air_haste", "Corrente de Ar", "Depois do Vendaval, +35% de velocidade por 3 s", ElementType.Air, TalentEffect.AirHaste, 3f, 1);
            return list;
        }

        public static Talent Find(string id)
        {
            foreach (var t in All) if (t.Id == id) return t;
            return null;
        }

        static bool Has(IReadOnlyList<ElementType> list, ElementType e)
        {
            foreach (var a in list) if (a == e) return true;
            return false;
        }

        public static void Apply(HeroModifiers m, Talent t)
        {
            switch (t.Effect)
            {
                case TalentEffect.BasicDamage: m.BasicDamageMult += t.Value; break;
                case TalentEffect.Finisher: m.FinisherMult += t.Value; break;
                case TalentEffect.MaxHealth: m.MaxHealthAdd += t.Value; break;
                case TalentEffect.SkillRefundOnKill: m.SkillRefundOnKill += t.Value; break;
                case TalentEffect.Essence: m.EssenceMult += t.Value; break;
                case TalentEffect.FireTrail: m.FireTrailSeconds += t.Value; break;
                case TalentEffect.FireRange: m.AddSkillRange(ElementType.Fire, t.Value); break;
                case TalentEffect.BurningBonus: m.BurningBonus += t.Value; break;
                case TalentEffect.WaterHeal: m.WaterHealAdd += t.Value; break;
                case TalentEffect.WaterFreeze: m.WaterFreezeSeconds += t.Value; break;
                case TalentEffect.WaterCooldown: m.ScaleSkillCooldown(ElementType.Water, 1f - t.Value); break;
                case TalentEffect.EarthBlocks: m.EarthBlocksAdd += (int)t.Value; break;
                case TalentEffect.EarthRange: m.AddSkillRange(ElementType.Earth, t.Value); break;
                case TalentEffect.EarthThorns: m.EarthThorns += t.Value; break;
                case TalentEffect.AirDashCharge: m.DashChargesAdd += (int)t.Value; break;
                case TalentEffect.AirRange: m.AddSkillRange(ElementType.Air, t.Value); break;
                case TalentEffect.AirHaste: m.AirHasteSeconds += t.Value; break;
                case TalentEffect.ReactionBoost: m.AddReactionBonus(t.Element, t.Second, t.Value); break;
                case TalentEffect.Flank: m.FlankBonus += t.Value; break;
                case TalentEffect.SureFooted: m.SureFooted = true; break;
                case TalentEffect.BurstPower: m.BurstBonus += t.Value; break;
                case TalentEffect.Contagion: m.MarkSpread = System.Math.Max(m.MarkSpread, t.Value); break;
                case TalentEffect.Alchemy: m.FusionBonus += t.Value; break;
            }
        }

        /// Ate count talentos diferentes que ainda cabem, de corpo ou de elementos ja despertos.
        public static List<Talent> Offer(TalentBook book, IReadOnlyList<ElementType> awakened, Random rng, int count = 3)
        {
            var pool = new List<Talent>();
            foreach (var t in All)
            {
                if (!book.CanTake(t)) continue;
                if (t.Element != ElementType.None && !Has(awakened, t.Element)) continue;
                if (t.Second != ElementType.None && !Has(awakened, t.Second)) continue;
                // o despertar so vale com os quatro elementos: oferece a partir do terceiro
                if (t.Effect == TalentEffect.BurstPower && awakened.Count < 3) continue;
                // Contagio so serve com reacao possivel (dois elementos); Alquimia so com Fusao (tres)
                if (t.Effect == TalentEffect.Contagion && awakened.Count < 2) continue;
                if (t.Effect == TalentEffect.Alchemy && awakened.Count < 3) continue;
                pool.Add(t);
            }
            var offer = new List<Talent>();
            while (offer.Count < count && pool.Count > 0)
            {
                int i = rng.Next(pool.Count);
                offer.Add(pool[i]);
                pool.RemoveAt(i);
            }
            return offer;
        }
    }
}
