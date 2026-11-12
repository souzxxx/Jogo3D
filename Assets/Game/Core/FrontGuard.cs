using UnityEngine;

namespace Quintessence.Core
{
    /// Escudo frontal (Escudeiro): golpe direto que vem de dentro do arco a frente leva so uma fracao do dano.
    /// Passam inteiros: golpes pelas costas ou de lado, reacoes, queimadura e zonas no chao.
    /// O escudo cai enquanto o portador esta atordoado, congelado, enraizado ou com a armadura quebrada.
    public class FrontGuard : MonoBehaviour
    {
        [SerializeField] float arc = 130f;
        [SerializeField, Range(0f, 1f)] float taken = 0.15f;
        public float Arc => arc;
        public float Taken => taken;
        /// O ultimo golpe que passou por Multiplier bateu no escudo.
        public bool LastBlocked { get; private set; }
        public int Blocks { get; private set; }

        public void Configure(float arcDegrees, float damageTaken)
        {
            arc = Mathf.Clamp(arcDegrees, 0f, 360f);
            taken = Mathf.Clamp01(damageTaken);
        }

        /// Regra pura: quem ataca de toAttacker esta dentro do arco a frente de forward? So o plano do chao conta.
        public static bool Covers(Vector3 forward, Vector3 toAttacker, float arcDegrees)
        {
            forward.y = 0f; toAttacker.y = 0f;
            if (arcDegrees <= 0f || forward.sqrMagnitude < 1e-4f || toAttacker.sqrMagnitude < 1e-4f) return false;
            return Vector3.Angle(forward, toAttacker) <= arcDegrees * 0.5f;
        }

        public static bool Drops(StatusState s) =>
            s != null && (s.Has(StatusType.Stun) || s.Has(StatusType.Frozen) || s.Has(StatusType.Rooted) || s.Has(StatusType.ArmorBroken));

        /// Escudo erguido agora (nao caiu por status).
        public bool Up => !(TryGetComponent(out StatusHolder holder) && Drops(holder.State));

        /// So golpe direto tem direcao: reacao, queimadura e zona chegam sem ela e passam.
        public float Multiplier(in DamageInfo info)
        {
            LastBlocked = false;
            if (info.IsReaction || info.Direction.sqrMagnitude < 1e-4f || !Up) return 1f;
            // de onde o golpe vem, nao onde esta quem bateu: o Pilar de Pedra nasce a frente do heroi e pode pegar pelas costas
            var to = -info.Direction;
            if (!Covers(transform.forward, to, arc)) return 1f;
            LastBlocked = true;
            Blocks++;
            return taken;
        }
    }
}
