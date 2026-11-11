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
        [Header("Segundo status (ex.: Lama enraiza e encharca)")]
        public bool appliesSecondStatus;
        public StatusType secondStatus;
        public float secondStatusDuration = 3f;
        [Header("Area que fica no chao (ex.: poca de Magma, nuvem de Vapor)")]
        public float zoneDuration;
        public float zoneInterval = 0.5f;
        public float zoneDamage;
        public bool zoneAppliesStatus;
        public StatusType zoneStatus;
        public float zoneStatusDuration = 1f;
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
