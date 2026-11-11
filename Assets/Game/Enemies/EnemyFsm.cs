namespace Quintessence.Enemies
{
    public enum EnemyPhase { Spawning, Chase, Windup, Recover, Dead }
    public enum EnemyAction { None, StartWindup, Strike, Cancel }

    /// Maquina de estados do inimigo (sem Unity): nasce, persegue, avisa o golpe, golpeia, recupera.
    public class EnemyFsm
    {
        readonly float spawnTime, windupTime, recoverTime, cooldown, attackRange;
        float t, cd;

        public EnemyPhase Phase { get; private set; } = EnemyPhase.Spawning;

        public EnemyFsm(float windupTime, float recoverTime, float cooldown, float attackRange, float spawnTime = 0.7f)
        {
            this.windupTime = windupTime; this.recoverTime = recoverTime; this.cooldown = cooldown;
            this.attackRange = attackRange; this.spawnTime = spawnTime;
        }

        public bool CanMove => Phase == EnemyPhase.Chase;

        /// distance: ate o heroi. canAct: falso se Atordoado ou Congelado. hasTarget: heroi vivo e visivel.
        public EnemyAction Tick(float dt, float distance, bool canAct, bool hasTarget = true)
        {
            if (Phase == EnemyPhase.Dead) return EnemyAction.None;
            if (cd > 0f) cd -= dt;
            switch (Phase)
            {
                case EnemyPhase.Spawning:
                    if ((t += dt) >= spawnTime) { Phase = EnemyPhase.Chase; t = 0f; }
                    break;
                case EnemyPhase.Chase:
                    if (canAct && hasTarget && cd <= 0f && distance <= attackRange)
                    {
                        Phase = EnemyPhase.Windup; t = 0f;
                        return EnemyAction.StartWindup;
                    }
                    break;
                case EnemyPhase.Windup:
                    if (!canAct)
                    {
                        // atordoar no meio do aviso interrompe o golpe
                        Phase = EnemyPhase.Chase; cd = cooldown * 0.5f;
                        return EnemyAction.Cancel;
                    }
                    if ((t += dt) >= windupTime)
                    {
                        Phase = EnemyPhase.Recover; t = 0f; cd = cooldown;
                        return EnemyAction.Strike;
                    }
                    break;
                case EnemyPhase.Recover:
                    if ((t += dt) >= recoverTime) { Phase = EnemyPhase.Chase; t = 0f; }
                    break;
            }
            return EnemyAction.None;
        }

        /// Devolve true se havia um golpe sendo avisado.
        public bool Kill()
        {
            bool wasWindup = Phase == EnemyPhase.Windup;
            Phase = EnemyPhase.Dead;
            return wasWindup;
        }
    }
}
