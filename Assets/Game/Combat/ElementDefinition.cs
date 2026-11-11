using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    public enum DashVariant { Plain, FireTrail, Cleanse, Armor }

    /// Numeros de um elemento: ataque basico, skill e dash.
    [CreateAssetMenu(menuName = "Quintessence/Element Definition")]
    public class ElementDefinition : ScriptableObject
    {
        public ElementType element;

        [Header("Ataque basico")]
        public float damageMultiplier = 1f;
        public float knockbackMultiplier = 1f;
        public bool finisherAppliesStatus;
        public StatusType finisherStatus;
        public float finisherStatusDuration = 3f;

        [Header("Skill")]
        public string skillName = "Skill";
        public float skillDamage = 15f;
        public float skillCooldown = 4f;
        public float skillRange = 5f;
        [Range(10f, 360f)] public float skillArc = 70f;
        public float skillKnockback = 4f;
        public bool skillAppliesStatus;
        public StatusType skillStatus;
        public float skillStatusDuration = 3f;
        public float skillCastTime = 0.35f;
        public float skillHitTime = 0.15f;

        [Header("Dash")]
        public DashVariant dash = DashVariant.Plain;
        public int dashCharges = 1;
    }
}
