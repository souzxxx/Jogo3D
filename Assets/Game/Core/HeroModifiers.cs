namespace Quintessence.Core
{
    /// Modificadores do heroi acumulados na run (bencaos, talentos, upgrades). O combate le daqui; quem da bencao escreve aqui.
    public class HeroModifiers
    {
        readonly float[] elementDamage = { 1f, 1f, 1f, 1f, 1f };
        readonly float[] skillRange = { 1f, 1f, 1f, 1f, 1f };
        readonly float[] skillCooldown = { 1f, 1f, 1f, 1f, 1f };
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

        // ---- talentos (nivel) ----
        /// Dano do combo basico.
        public float BasicDamageMult = 1f;
        /// Dano e empurrao so do terceiro golpe.
        public float FinisherMult = 1f;
        /// Segundos de chao em chamas deixado pela Labareda (0 = nao deixa).
        public float FireTrailSeconds;
        /// Bonus de dano contra quem esta Queimando.
        public float BurningBonus;
        /// Fracao extra de vida curada pela Mare.
        public float WaterHealAdd;
        /// Segundos de Congelado que a Mare aplica (0 = nao congela).
        public float WaterFreezeSeconds;
        /// Golpes extras que o Pilar de Pedra bloqueia.
        public int EarthBlocksAdd;
        /// Dano de Terra em volta do heroi quando um golpe e bloqueado.
        public float EarthThorns;
        /// Cargas extras de dash.
        public int DashChargesAdd;
        /// Segundos de pressa depois do Vendaval.
        public float AirHasteSeconds;
        /// Segundos devolvidos as recargas das skills a cada inimigo morto.
        public float SkillRefundOnKill;
        public float EssenceMult = 1f;
        public float XpMult = 1f;

        public float Damage(ElementType e) => DamageAll * elementDamage[(int)e];
        public void AddElementDamage(ElementType e, float fraction) => elementDamage[(int)e] += fraction;
        public float SkillRange(ElementType e) => skillRange[(int)e];
        public void AddSkillRange(ElementType e, float fraction) => skillRange[(int)e] += fraction;
        /// Recarga da skill daquele elemento, ja com o multiplicador geral.
        public float SkillCooldown(ElementType e) => SkillCooldownMult * skillCooldown[(int)e];
        public void ScaleSkillCooldown(ElementType e, float factor) => skillCooldown[(int)e] *= factor;

        public void Reset()
        {
            for (int i = 0; i < elementDamage.Length; i++) elementDamage[i] = skillRange[i] = skillCooldown[i] = 1f;
            DamageAll = ReactionPower = MoveSpeedMult = DashCooldownMult = SkillCooldownMult = QuintessenceGainMult = StatusDurationMult = 1f;
            CritChanceAdd = MaxHealthAdd = HealOnReaction = HealOnRoomClear = SwitchStrikeAdd = 0f;
            BasicDamageMult = FinisherMult = EssenceMult = XpMult = 1f;
            FireTrailSeconds = BurningBonus = WaterHealAdd = WaterFreezeSeconds = EarthThorns = AirHasteSeconds = SkillRefundOnKill = 0f;
            EarthBlocksAdd = DashChargesAdd = 0;
        }
    }
}
