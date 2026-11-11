namespace Quintessence.Core
{
    /// Modificadores do heroi acumulados na run (bencaos, upgrades). O combate le daqui; quem da bencao escreve aqui.
    public class HeroModifiers
    {
        readonly float[] elementDamage = { 1f, 1f, 1f, 1f, 1f };
        public float DamageAll = 1f;
        public float ReactionPower = 1f;
        public float CritChanceAdd;
        public float MoveSpeedMult = 1f;
        public float DashCooldownMult = 1f;
        public float SkillCooldownMult = 1f;
        public float MaxHealthAdd;
        public float QuintessenceGainMult = 1f;
        public float HealOnReaction;
        /// Fracao da vida maxima curada ao limpar uma sala.
        public float HealOnRoomClear;
        public float SwitchStrikeAdd;
        public float StatusDurationMult = 1f;

        public float Damage(ElementType e) => DamageAll * elementDamage[(int)e];
        public void AddElementDamage(ElementType e, float fraction) => elementDamage[(int)e] += fraction;

        public void Reset()
        {
            for (int i = 0; i < elementDamage.Length; i++) elementDamage[i] = 1f;
            DamageAll = ReactionPower = MoveSpeedMult = DashCooldownMult = SkillCooldownMult = QuintessenceGainMult = StatusDurationMult = 1f;
            CritChanceAdd = MaxHealthAdd = HealOnReaction = HealOnRoomClear = SwitchStrikeAdd = 0f;
        }
    }
}
