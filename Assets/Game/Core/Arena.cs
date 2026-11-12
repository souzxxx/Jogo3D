using UnityEngine;

namespace Quintessence.Core
{
    /// Limites da sala (centrada na origem) com folga. Quem sair daqui e trazido de volta.
    /// O padrao e a sala de 20 x 16; a run configura o tamanho dela ao abrir e volta ao padrao ao fechar.
    public static class Arena
    {
        public const float DefaultHalfX = 10.6f, DefaultHalfZ = 8.6f, MinY = -1.5f, MaxY = 8f;
        public static float HalfX { get; private set; } = DefaultHalfX;
        public static float HalfZ { get; private set; } = DefaultHalfZ;

        public static void Configure(float halfX, float halfZ) { HalfX = Mathf.Max(4f, halfX); HalfZ = Mathf.Max(4f, halfZ); }
        public static void ResetSize() { HalfX = DefaultHalfX; HalfZ = DefaultHalfZ; }

        public static bool Contains(Vector3 p) =>
            Mathf.Abs(p.x) <= HalfX && Mathf.Abs(p.z) <= HalfZ && p.y >= MinY && p.y <= MaxY && !float.IsNaN(p.x + p.y + p.z);

        /// Ponto mais proximo dentro da sala, no chao, com um passo de folga da parede.
        public static Vector3 Clamp(Vector3 p)
        {
            if (float.IsNaN(p.x + p.y + p.z)) return Vector3.zero;
            return new Vector3(Mathf.Clamp(p.x, -HalfX + 2f, HalfX - 2f), 0f, Mathf.Clamp(p.z, -HalfZ + 2f, HalfZ - 2f));
        }
    }

    /// Conta o tempo em que uma luta nao anda (ninguem leva dano). Passou do limite, dispara uma vez e recomeca.
    public class StallTimer
    {
        readonly float limit;
        float idle;
        public int Fired { get; private set; }

        public StallTimer(float limit) { this.limit = Mathf.Max(1f, limit); }

        public void NoteProgress() => idle = 0f;

        /// fighting = ha inimigo vivo e o jogo nao esta pausado. Devolve true quando a luta travou.
        public bool Tick(float dt, bool fighting)
        {
            if (!fighting) { idle = 0f; return false; }
            idle += Mathf.Max(0f, dt);
            if (idle < limit) return false;
            idle = 0f;
            Fired++;
            return true;
        }
    }
}
