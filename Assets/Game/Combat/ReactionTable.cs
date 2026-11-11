using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    [Serializable]
    public class Reaction
    {
        public string name;
        public ElementType a, b;
        public float damage = 10f;
        public float radius = 2.5f;
        public float knockback = 4f;
        public bool appliesStatus;
        public StatusType status;
        public float statusDuration = 3f;
    }

    /// Tabela de reacoes. Simetrica: Fogo+Agua = Agua+Fogo.
    [CreateAssetMenu(menuName = "Quintessence/Reaction Table")]
    public class ReactionTable : ScriptableObject
    {
        public List<Reaction> reactions = new();

        public Reaction Find(ElementType x, ElementType y)
        {
            foreach (var r in reactions)
                if ((r.a == x && r.b == y) || (r.a == y && r.b == x)) return r;
            return null;
        }
    }
}
