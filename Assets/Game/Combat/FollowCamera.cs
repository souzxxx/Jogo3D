using UnityEngine;

namespace Quintessence.Combat
{
    /// Camera top-down 3/4 fixa que segue o alvo com suavizacao e olha um pouco na direcao da mira.
    /// O cenario nunca some: heroi e inimigos atras de uma peca aparecem como silhueta (Core.Silhouette).
    public class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float pitch = 41f;
        [SerializeField] float distance = 24f;
        [SerializeField] float smoothTime = 0.12f;
        [SerializeField] float lookAhead = 1.2f;

        Vector3 focus, velocity;
        PlayerController player;

        public void SetTarget(Transform t)
        {
            target = t;
            player = t != null ? t.GetComponent<PlayerController>() : null;
            if (t != null) focus = t.position;
            Apply();
        }

        void Start() { if (target != null) SetTarget(target); }

        Transform peek;
        float peekLeft, peekTotal;
        /// Quantas vezes a camera olhou para outra coisa (para os testes).
        public int Peeks { get; private set; }

        /// Apresenta o chefe: por alguns segundos a camera vai ate o meio do caminho entre o heroi e ele e volta.
        /// O heroi continua no quadro e o jogador continua no controle.
        public void Peek(Transform other, float seconds = 2f)
        {
            if (other == null) return;
            peek = other; peekLeft = peekTotal = seconds; Peeks++;
        }

        void LateUpdate()
        {
            if (target == null) return;
            var want = target.position;
            if (player != null) want += player.AimDirection * lookAhead;
            if (peekLeft > 0f && peek != null)
            {
                peekLeft -= Time.unscaledDeltaTime;
                float done = peekTotal - peekLeft;
                float w = Mathf.Clamp01(Mathf.Min(done / 0.45f, peekLeft / 0.6f)); // entra e sai sem tranco
                want = Vector3.Lerp(want, (target.position + peek.position) * 0.5f, w * 0.9f);
            }
            focus = Vector3.SmoothDamp(focus, want, ref velocity, smoothTime);
            Apply();
        }

        Camera cam;
        /// Quanto da parede lateral pode aparecer na borda da tela antes de a camera parar de seguir.
        const float WallMargin = 1.2f;

        void Apply()
        {
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            var center = focus;
            center.x = ClampX(center.x);
            center.z = ClampSouth(center.z);
            transform.SetPositionAndRotation(center - rot * Vector3.forward * distance, rot);
        }

        /// Quanto do chao alem da borda sul pode aparecer: o bastante para o heroi na porta nao ficar embaixo da barra de skills.
        const float SouthMargin = 2.0f;

        /// Na borda sul (onde toda sala comeca) a metade de baixo da tela mostrava o chao de fora da sala: a camera para antes.
        float ClampSouth(float z)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null || cam.orthographic) return z;
            float height = distance * Mathf.Sin(pitch * Mathf.Deg2Rad);
            float back = distance * Mathf.Cos(pitch * Mathf.Deg2Rad);
            float bottom = (pitch + cam.fieldOfView * 0.5f) * Mathf.Deg2Rad; // raio da borda de baixo da tela
            if (bottom >= Mathf.PI * 0.5f) return z;
            float reach = back - height / Mathf.Tan(bottom); // do foco ate o chao visto na borda de baixo
            return Mathf.Max(z, -Quintessence.Core.Arena.HalfZ - SouthMargin + reach);
        }

        /// Nao mostra o vazio preto do lado de fora da sala: a camera segue em x so ate a parede lateral chegar na borda.
        float ClampX(float x)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null || cam.orthographic) return x;
            float halfV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float halfWidth = distance * halfV * cam.aspect; // largura visivel no chao, na altura do foco
            float limit = Quintessence.Core.Arena.HalfX + WallMargin - halfWidth;
            return limit <= 0f ? 0f : Mathf.Clamp(x, -limit, limit);
        }
    }
}
