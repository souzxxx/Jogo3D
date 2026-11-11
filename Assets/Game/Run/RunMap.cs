using System;
using System.Collections.Generic;
using UnityEngine;

namespace Quintessence.Run
{
    public enum RoomType { Combat, Elite, Shrine, Rest, Boss }
    public enum RewardType { None, Essence, Health, Element }

    public class MapNode
    {
        public int Id, Layer, Lane;
        public RoomType Type;
        public RewardType Reward;
        public readonly List<int> Next = new();
    }

    /// Mapa da run: camadas da esquerda para a direita; o jogador escolhe um caminho.
    public class RunMap
    {
        public readonly List<MapNode> Nodes = new();
        public int LayerCount;
        public MapNode Start => Nodes[0];
        public MapNode Get(int id) => Nodes[id];

        public List<MapNode> Layer(int layer)
        {
            var list = new List<MapNode>();
            foreach (var n in Nodes) if (n.Layer == layer) list.Add(n);
            return list;
        }
    }

    public static class RoomInfo
    {
        public static string Name(RoomType t) => t switch
        {
            RoomType.Combat => "Combate", RoomType.Elite => "Elite", RoomType.Shrine => "Santuário",
            RoomType.Rest => "Descanso", RoomType.Boss => "Chefe", _ => t.ToString(),
        };

        public static string Name(RewardType r) => r switch
        {
            RewardType.Essence => "Essência", RewardType.Health => "Vida", RewardType.Element => "Novo elemento", _ => "",
        };

        public static Color Color(RoomType t) => t switch
        {
            RoomType.Combat => new Color(0.95f, 0.35f, 0.3f), RoomType.Elite => new Color(0.75f, 0.35f, 1f),
            RoomType.Shrine => new Color(0.5f, 0.95f, 1f), RoomType.Rest => new Color(0.45f, 0.95f, 0.5f),
            RoomType.Boss => new Color(1f, 0.6f, 0.15f), _ => UnityEngine.Color.white,
        };
    }

    public static class RunMapGenerator
    {
        /// layers: quantas salas o jogador atravessa (a primeira e Combate, a ultima e o Chefe).
        /// shrines: camadas inteiras de Santuario, para que qualquer caminho desperte o mesmo numero de elementos.
        public static RunMap Generate(int seed, int layers = 8, int shrines = 3)
        {
            if (layers < 3) throw new ArgumentException("a run precisa de pelo menos 3 camadas");
            shrines = Math.Min(shrines, layers - 2);
            var rng = new System.Random(seed);
            var map = new RunMap { LayerCount = layers };
            var shrineLayers = new HashSet<int>();
            for (int k = 0; k < shrines; k++) shrineLayers.Add((k + 1) * (layers - 2) / shrines);

            var byLayer = new List<List<MapNode>>();
            for (int layer = 0; layer < layers; layer++)
            {
                bool edge = layer == 0 || layer == layers - 1;
                bool shrine = shrineLayers.Contains(layer);
                int width = edge ? 1 : shrine ? 1 + rng.Next(2) : 2 + rng.Next(2);
                var list = new List<MapNode>();
                bool restUsed = false;
                for (int lane = 0; lane < width; lane++)
                {
                    var n = new MapNode { Id = map.Nodes.Count, Layer = layer, Lane = lane };
                    if (layer == 0) { n.Type = RoomType.Combat; n.Reward = RewardType.Essence; }
                    else if (layer == layers - 1) { n.Type = RoomType.Boss; n.Reward = RewardType.None; }
                    else if (shrine) { n.Type = RoomType.Shrine; n.Reward = RewardType.Element; }
                    else
                    {
                        int roll = rng.Next(100);
                        if (roll < 20 && !restUsed && lane > 0) { n.Type = RoomType.Rest; n.Reward = RewardType.Health; restUsed = true; }
                        else if (roll < 40 && layer >= 3) { n.Type = RoomType.Elite; n.Reward = RewardType.Essence; }
                        else { n.Type = RoomType.Combat; n.Reward = rng.Next(3) == 0 ? RewardType.Health : RewardType.Essence; }
                    }
                    map.Nodes.Add(n);
                    list.Add(n);
                }
                byLayer.Add(list);
            }

            for (int layer = 0; layer < layers - 1; layer++)
            {
                var a = byLayer[layer];
                var b = byLayer[layer + 1];
                var incoming = new HashSet<int>();
                for (int i = 0; i < a.Count; i++)
                {
                    int main = a.Count == 1 ? rng.Next(b.Count) : (int)Math.Round(i * (b.Count - 1) / (double)(a.Count - 1));
                    Link(a[i], b[main], incoming);
                    if (a.Count == 1) foreach (var other in b) Link(a[i], other, incoming); // da primeira sala sai tudo
                    else if (b.Count > 1 && rng.Next(2) == 0)
                        Link(a[i], b[Math.Clamp(main + (rng.Next(2) == 0 ? -1 : 1), 0, b.Count - 1)], incoming);
                }
                // ninguem fica sem entrada
                for (int j = 0; j < b.Count; j++)
                    if (!incoming.Contains(b[j].Id))
                        Link(a[Math.Clamp((int)Math.Round(j * (a.Count - 1) / (double)Math.Max(1, b.Count - 1)), 0, a.Count - 1)], b[j], incoming);
            }
            return map;
        }

        static void Link(MapNode from, MapNode to, HashSet<int> incoming)
        {
            if (!from.Next.Contains(to.Id)) from.Next.Add(to.Id);
            incoming.Add(to.Id);
        }
    }
}
