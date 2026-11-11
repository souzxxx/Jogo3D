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
    }

    /// Quantos inimigos cada onda de uma sala tem. Cresce com a profundidade.
    public static class EncounterPlanner
    {
        public struct WavePlan
        {
            public int Melee, Ranged, Brute, Bomber;
            public int Total => Melee + Ranged + Brute + Bomber;
        }

        public static List<WavePlan> Plan(RoomType type, int depth)
        {
            var waves = new List<WavePlan>();
            switch (type)
            {
                case RoomType.Combat:
                    waves.Add(new WavePlan { Melee = 2 + depth / 2, Bomber = depth >= 2 ? 1 + depth / 5 : 0 });
                    waves.Add(new WavePlan { Melee = 2 + depth / 3, Ranged = depth >= 1 ? 1 + depth / 3 : 0, Brute = depth >= 3 ? 1 : 0 });
                    break;
                case RoomType.Elite:
                    waves.Add(new WavePlan { Melee = 3 + depth / 2, Ranged = 1, Bomber = 1 });
                    waves.Add(new WavePlan { Melee = 2 + depth / 3, Ranged = 2 + depth / 3, Brute = 1 });
                    waves.Add(new WavePlan { Melee = 2, Ranged = 2 + depth / 4, Brute = 1 + depth / 5, Bomber = 2 });
                    break;
                case RoomType.Boss: // provisorio ate o Guardiao das Cinzas existir
                    waves.Add(new WavePlan { Melee = 4, Ranged = 1, Bomber = 2 });
                    waves.Add(new WavePlan { Melee = 3, Ranged = 3, Brute = 1 });
                    waves.Add(new WavePlan { Melee = 3, Ranged = 2, Brute = 2, Bomber = 2 });
                    break;
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
