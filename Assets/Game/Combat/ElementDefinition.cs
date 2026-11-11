using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    public enum DashVariant { Plain, FireTrail, Cleanse, Armor }
    /// Cone: na frente do heroi. Radial: em volta do heroi. Summon: invoca um objeto na mira e explode em volta dele.
    public enum SkillShape { Cone, Radial, Summon }

    /// Numeros de um elemento: ataque basico, skill e dash.
    [CreateAssetMenu(menuName = "Quintessence/Element Definition")]
    public class ElementDefinition : ScriptableObject
    {
        public ElementType element;

        [Header("Ataque basico")]
        public float damageMultiplier = 1f;
        public float knockbackMultiplier = 1f;
        [Range(0f, 1f)] public float critChance;
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
        public SkillShape skillShape = SkillShape.Cone;
        public GameObject skillSummonPrefab;
        public float skillSummonDistance = 3.5f;
        public float skillCastTime = 0.35f;
        public float skillHitTime = 0.15f;

        [Header("Dash")]
        public DashVariant dash = DashVariant.Plain;
        public int dashCharges = 1;
        public float dashDistanceMultiplier = 1f;
    }
}
