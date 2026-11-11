namespace Quintessence.Enemies
{
    /// Regra das ondas (sem Unity): a proxima onda so vem quando a anterior morreu; depois da ultima, sala limpa.
    public class WaveState
    {
        readonly int[] sizes;
        readonly float delayBetween;
        int wave = -1, alive;
        float delay;
        bool started;

        public WaveState(int[] sizes, float delayBetween = 1f)
        {
            this.sizes = sizes;
            this.delayBetween = delayBetween;
        }

        public int CurrentWave => wave;
        public int TotalWaves => sizes.Length;
        public int Alive => alive;
        public bool Cleared { get; private set; }

        public void Start() => started = true;

        /// Devolve o indice da onda que deve nascer agora, ou -1.
        public int Tick(float dt)
        {
            if (!started || Cleared || alive > 0) return -1;
            if (wave >= sizes.Length - 1) { Cleared = true; return -1; }
            if (wave >= 0 && (delay -= dt) > 0f) return -1;
            wave++;
            alive = sizes[wave];
            delay = delayBetween;
            return wave;
        }

        public void NotifyDeath() { if (alive > 0) alive--; }
    }
}
