using System;
using System.Collections.Generic;
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
    }
}
