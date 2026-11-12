using System;
using System.Collections.Generic;
using Quintessence.Run;
using UnityEngine;

namespace Quintessence.Game
{
    [Serializable]
    public class LayoutPiece
    {
        public GameObject prefab;
        public Vector3 position;
        public float rotation;
    }

    /// Arranjo de cobertura e perigos de uma sala de combate.
    [CreateAssetMenu(menuName = "Quintessence/Room Layout")]
    public class RoomLayout : ScriptableObject
    {
        public string displayName = "Sala";
        public List<LayoutPiece> pieces = new();
        [Tooltip("Trechos a que o arranjo pertence: so aparece neles, e com o triplo de chance. Vazio = qualquer trecho.")]
        public List<Biome> biomes = new();

        /// Peso no sorteio do arranjo para uma sala deste trecho.
        public int WeightFor(Biome biome) => biomes == null || biomes.Count == 0 ? 1 : biomes.Contains(biome) ? 3 : 0;

        /// Tem perigo que fere o heroi sozinho (brasa, espinhos, poca). A primeira sala da run nao usa estes.
        public bool HasHazard
        {
            get
            {
                foreach (var p in pieces)
                    if (p.prefab != null && (p.prefab.GetComponent<Quintessence.Enemies.EmberVent>() != null || p.prefab.GetComponent<Quintessence.Enemies.SpikeTrap>() != null
                        || p.prefab.GetComponent<CorruptionPool>() != null)) return true;
                return false;
            }
        }
    }
}
