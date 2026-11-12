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
            public int Melee, Ranged, Brute, Bomber, Boss, Charger, Lobber, Shaman;
            public int Total => Melee + Ranged + Brute + Bomber + Boss + Charger + Lobber + Shaman;
        }

        public static List<WavePlan> Plan(RoomType type, int depth)
        {
            var waves = new List<WavePlan>();
            switch (type)
            {
                case RoomType.Combat:
                    // os tipos novos entram aos poucos: Ceifador na segunda sala, Arremessador na quarta, Xama na quinta
                    waves.Add(new WavePlan { Melee = 2 + depth / 2 - (depth >= 1 ? 1 : 0), Charger = depth >= 1 ? 1 + depth / 6 : 0, Bomber = depth >= 2 ? 1 + depth / 5 : 0 });
                    waves.Add(new WavePlan
                    {
                        Melee = 2 + depth / 3, Ranged = depth >= 1 ? 1 + depth / 3 - (depth >= 3 ? 1 : 0) : 0, Brute = depth >= 3 ? 1 : 0,
                        Lobber = depth >= 3 ? 1 + depth / 7 : 0, Shaman = depth >= 4 ? 1 : 0,
                    });
                    // do meio da run em diante as salas comuns ganham uma terceira onda
                    if (depth >= 5) waves.Add(new WavePlan { Melee = 1 + depth / 4, Ranged = 1, Charger = 1, Bomber = depth >= 7 ? 1 : 0 });
                    break;
                case RoomType.Elite:
                    waves.Add(new WavePlan { Melee = 1 + depth / 2, Ranged = 1, Bomber = 1, Charger = 1 });
                    waves.Add(new WavePlan { Melee = 2 + depth / 3, Ranged = 1 + depth / 3, Brute = 1, Shaman = 1 });
                    waves.Add(new WavePlan { Melee = 2, Ranged = 1 + depth / 4, Brute = 1 + depth / 5, Bomber = 2, Lobber = 1 });
                    break;
                case RoomType.Boss: // o Guardiao das Cinzas chama os proprios reforcos
                    waves.Add(new WavePlan { Boss = 1 });
                    break;
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
                if (w.Melee < 2) continue;
                if (biome == Biome.Crypt) { w.Melee--; if (i % 2 == 0) w.Ranged++; else w.Lobber++; }
                else if (biome == Biome.Garden) { w.Melee--; if (i % 2 == 0) w.Charger++; else w.Brute++; }
                waves[i] = w;
            }
            return waves;
        }

        public static int EssenceReward(RoomType type, int depth) => type switch
        {
            RoomType.Elite => 2 * (10 + 2 * depth),
            RoomType.Combat => 10 + 2 * depth,
            RoomType.Boss => 50,
            _ => 0,
        };
    }
}
