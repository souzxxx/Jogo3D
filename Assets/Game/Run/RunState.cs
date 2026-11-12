using System.Collections.Generic;

namespace Quintessence.Run
{
    /// Estado da run em andamento: onde o jogador esta no mapa, o que ja visitou e o que ganhou.
    public class RunState
    {
        public RunMap Map { get; }
        public MapNode Current { get; private set; }
        public readonly HashSet<int> Visited = new();
        public int Essence { get; private set; }
        public int RoomsCleared { get; private set; }
        public readonly List<Boon> Boons = new();
        public readonly HashSet<string> BoonIds = new();
        public readonly Progression Progress = new();
        public readonly TalentBook Talents = new();
        public int Kills;
        /// Grau de cada skill (indice do ElementType). 1 = como desperta; os Santuarios podem aprofundar ate MaxSkillRank.
        public readonly int[] SkillRank = { 1, 1, 1, 1, 1 };
        public const int MaxSkillRank = 3;
        public const float RankDamage = 0.3f, RankCooldown = 0.9f;

        /// Aprofunda a skill de um elemento ja desperto: +30% de dano e recarga 10% menor por grau.
        public bool Deepen(Quintessence.Core.ElementType e, Quintessence.Core.HeroModifiers mods)
        {
            int i = (int)e;
            if (i <= 0 || SkillRank[i] >= MaxSkillRank) return false;
            SkillRank[i]++;
            mods.AddElementDamage(e, RankDamage);
            mods.ScaleSkillCooldown(e, RankCooldown);
            // grau III: a skill ganha um efeito de assinatura, nao so numero
            if (SkillRank[i] == MaxSkillRank)
                switch (e)
                {
                    case Quintessence.Core.ElementType.Fire: mods.FireTrailSeconds += 2.5f; break;
                    case Quintessence.Core.ElementType.Water: mods.WaterFreezeSeconds += 1f; break;
                    case Quintessence.Core.ElementType.Earth: mods.EarthBlocksAdd += 1; mods.EarthThorns += 10f; break;
                    case Quintessence.Core.ElementType.Air: mods.DashChargesAdd += 1; break;
                }
            return true;
        }

        /// O que o grau III acrescenta a skill de cada elemento.
        public static string Signature(Quintessence.Core.ElementType e) => e switch
        {
            Quintessence.Core.ElementType.Fire => "deixa o chão em chamas",
            Quintessence.Core.ElementType.Water => "congela quem acerta",
            Quintessence.Core.ElementType.Earth => "bloqueia 2 golpes e devolve dano",
            Quintessence.Core.ElementType.Air => "+1 carga de dash",
            _ => "",
        };

        public static string Roman(int rank) => rank switch { 1 => "I", 2 => "II", 3 => "III", _ => rank.ToString() };
        /// Trocas de oferta de talento que ainda restam nesta run.
        public int Rerolls;

        public void AddBoon(Boon b) { Boons.Add(b); BoonIds.Add(b.id); }

        public RunState(RunMap map)
        {
            Map = map;
            Current = map.Start;
            Visited.Add(Current.Id);
        }

        public int Depth => Current.Layer;
        public bool AtBoss => Current.Type == RoomType.Boss;

        public List<MapNode> Choices
        {
            get
            {
                var list = new List<MapNode>();
                foreach (int id in Current.Next) list.Add(Map.Get(id));
                return list;
            }
        }

        public bool CanAdvanceTo(int id) => Current.Next.Contains(id);

        public bool Advance(int id)
        {
            if (!CanAdvanceTo(id)) return false;
            Current = Map.Get(id);
            Visited.Add(id);
            return true;
        }

        /// Volta uma run salva: sala atual, salas visitadas, Essencia e salas vencidas.
        public void Restore(int currentId, IEnumerable<int> visited, int essence, int roomsCleared)
        {
            if (currentId >= 0 && currentId < Map.Nodes.Count) Current = Map.Get(currentId);
            Visited.Clear();
            if (visited != null) foreach (int v in visited) Visited.Add(v);
            Visited.Add(Current.Id);
            Essence = System.Math.Max(0, essence);
            RoomsCleared = System.Math.Max(0, roomsCleared);
        }

        public void RoomCleared() => RoomsCleared++;
        public void AddEssence(int amount) { if (amount > 0) Essence += amount; }

        /// Gasta essencia se houver o bastante. Nunca fica negativo.
        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > Essence) return false;
            Essence -= amount;
            return true;
        }
    }

    /// Quantos inimigos cada onda de uma sala tem. Cresce com a profundidade.
    public static class EncounterPlanner
    {
        public struct WavePlan
        {
            public int Melee, Ranged, Brute, Bomber, Boss, Charger, Lobber, Shaman, MiniBoss, Swarm, Ember, Sentinel, Guard, Drowned;
            public int Total => Melee + Ranged + Brute + Bomber + Boss + Charger + Lobber + Shaman + MiniBoss + Swarm + Ember + Sentinel + Guard + Drowned;
        }

        public const int MiniBossDepth = 5;
        /// O Escudeiro aparece da quarta sala em diante, no lugar de um Rastejante da primeira onda.
        public const int GuardDepth = 3;

        /// Tamanho do bando de Cinzentos: nenhum nas duas primeiras salas, depois 3 e, do meio em diante, 4.
        public static int SwarmSize(int depth) => depth < 2 ? 0 : depth < 5 ? 3 : 4;

        public static List<WavePlan> Plan(RoomType type, int depth)
        {
            var waves = new List<WavePlan>();
            switch (type)
            {
                case RoomType.Combat:
                    // os tipos novos entram aos poucos: Ceifador na segunda sala, Arremessador na quarta, Xama na quinta
                    // Cinzentos chegam em bando da terceira sala em diante
                    waves.Add(new WavePlan { Melee = 2 + depth / 2 - (depth >= 1 ? 1 : 0), Charger = depth >= 1 ? 1 + depth / 6 : 0, Bomber = depth >= 2 ? 1 + depth / 5 : 0, Swarm = SwarmSize(depth) });
                    waves.Add(new WavePlan
                    {
                        Melee = 2 + depth / 3, Ranged = depth >= 1 ? 1 + depth / 3 - (depth >= 3 ? 1 : 0) : 0, Brute = depth >= 3 ? 1 : 0,
                        Lobber = depth >= 3 ? 1 + depth / 7 : 0, Shaman = depth >= 4 ? 1 : 0,
                    });
                    // do meio da run em diante as salas comuns ganham uma terceira onda
                    if (depth >= 5) waves.Add(new WavePlan { Melee = 1 + depth / 4, Ranged = 1, Charger = 1, Bomber = depth >= 7 ? 1 : 0 });
                    break;
                case RoomType.Elite:
                    waves.Add(new WavePlan { Melee = 1 + depth / 3, Ranged = 1, Bomber = 1, Charger = 1, Swarm = SwarmSize(depth) });
                    if (depth >= MiniBossDepth)
                    {
                        // da sexta sala em diante: a onda pesada do meio e o Carrasco no fim
                        waves.Add(new WavePlan { Melee = 2 + depth / 3, Ranged = 1 + depth / 3, Brute = 1, Shaman = 1 });
                        waves.Add(new WavePlan { Melee = 2, Ranged = 1 + depth / 4, Bomber = 2, Lobber = 1, MiniBoss = 1 });
                    }
                    else // a primeira Elite chega com 1 ou 2 elementos: duas ondas leves e o Campeao
                        waves.Add(new WavePlan { Melee = 3, Ranged = 1 + depth / 4, Bomber = 1, Lobber = 1, Charger = 1 });
                    break;
                case RoomType.Boss: // o Guardiao das Cinzas chama os proprios reforcos
                    waves.Add(new WavePlan { Boss = 1 });
                    break;
            }
            // o Escudeiro toma o lugar de um Rastejante na primeira onda: a sala nao fica maior, fica diferente
            if ((type == RoomType.Combat || type == RoomType.Elite) && depth >= GuardDepth && waves.Count > 0 && waves[0].Melee >= 2)
            {
                var first = waves[0];
                first.Melee--; first.Guard++;
                waves[0] = first;
            }
            return waves;
        }

        /// O trecho da run puxa a mistura de inimigos para um lado, sem mudar o total da onda:
        /// a Cripta troca corpo a corpo por quem ataca de longe; o Jardim troca por quem bate pesado.
        public static List<WavePlan> Plan(RoomType type, int depth, Biome biome)
        {
            var waves = Plan(type, depth);
            if (type != RoomType.Combat && type != RoomType.Elite) return waves;
            for (int i = 0; i < waves.Count; i++)
            {
                var w = waves[i];
                if (w.Melee >= 2)
                {
                    if (biome == Biome.Crypt) { w.Melee--; if (i % 2 == 0) w.Ranged++; else w.Lobber++; }
                    else if (biome == Biome.Garden) { w.Melee--; if (i % 2 == 0) w.Charger++; else w.Brute++; }
                }
                // os elementais do trecho: Brasa Viva nos Saloes de Cinza, Afogado na Cripta, Sentinela de Pedra no Jardim
                if (i == 1 && biome == Biome.Ashes && depth >= 1 && w.Melee >= 1) { w.Melee--; w.Ember++; }
                if (i == 1 && biome == Biome.Garden && w.Ranged >= 1) { w.Ranged--; w.Sentinel++; }
                if (i == 1 && biome == Biome.Crypt && w.Lobber >= 1) { w.Lobber--; w.Drowned++; }
                waves[i] = w;
            }
            return waves;
        }

        public static int EssenceReward(RoomType type, int depth) => type switch
        {
            RoomType.Elite => 2 * (10 + 2 * depth),
            RoomType.Combat or RoomType.Trial => 10 + 2 * depth,
            RoomType.Boss => 50,
            _ => 0,
        };
    }
}
