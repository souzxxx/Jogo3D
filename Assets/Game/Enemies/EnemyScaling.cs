namespace Quintessence.Enemies
{
    /// Multiplicadores que a sala aplica aos inimigos que nascem nela. 1 = ficha do inimigo.
    public struct EnemyScaling
    {
        public float Health, Damage, Speed, Cooldown, Size;
        public static EnemyScaling None => new() { Health = 1f, Damage = 1f, Speed = 1f, Cooldown = 1f, Size = 1f };
    }
}
