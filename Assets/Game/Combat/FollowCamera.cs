using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Quintessence.Combat
{
    /// Camera top-down 3/4 fixa que segue o alvo com suavizacao e olha um pouco na direcao da mira.
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

        void LateUpdate()
        {
            if (target == null) return;
            var want = target.position;
            if (player != null) want += player.AimDirection * lookAhead;
            focus = Vector3.SmoothDamp(focus, want, ref velocity, smoothTime);
            Apply();
            HideOccluders();
        }

        // ---------- pecas na frente do heroi ----------
        // Pilar, caixote ou escombro entre a camera e o heroi some enquanto atrapalha; a sombra fica no chao,
        // entao o jogador continua sabendo que a peca esta ali. Inimigos nunca somem.
        readonly Dictionary<Renderer, ShadowCastingMode> hidden = new();
        readonly HashSet<Renderer> blocking = new();
        readonly List<Renderer> restore = new();
        readonly RaycastHit[] hits = new RaycastHit[24];

        /// Quantas pecas estao escondidas agora (para os testes).
        public int HiddenCount => hidden.Count;
        public bool IsHidden(Renderer r) => hidden.ContainsKey(r);

        void HideOccluders()
        {
            blocking.Clear();
            var chest = target.position + Vector3.up;
            var from = transform.position;
            var d = chest - from;
            float len = d.magnitude - 0.9f;
            if (len > 0.1f)
            {
                int n = Physics.SphereCastNonAlloc(from, 0.45f, d / d.magnitude, hits, len, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++) Collect(hits[i].collider);
            }
            foreach (var r in blocking)
                if (!hidden.ContainsKey(r)) { hidden[r] = r.shadowCastingMode; r.shadowCastingMode = ShadowCastingMode.ShadowsOnly; }
            restore.Clear();
            foreach (var kv in hidden) if (!blocking.Contains(kv.Key)) restore.Add(kv.Key);
            foreach (var r in restore) { if (r != null) r.shadowCastingMode = hidden[r]; hidden.Remove(r); }
        }

        void Collect(Collider c)
        {
            if (c == null || c.GetComponentInParent<PlayerController>() != null || c.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() != null) return;
            var rs = c.GetComponentsInChildren<Renderer>();
            // colisor sem malha propria (peca em L): a malha e irma dele, no mesmo pai; barreira invisivel (pai e a cena toda) nao tem o que esconder
            if (rs.Length == 0 && c.transform.parent != null && c.transform.parent.parent != null && c.transform.parent.childCount <= 8)
                rs = c.transform.parent.GetComponentsInChildren<Renderer>();
            foreach (var r in rs) if (r is MeshRenderer || r is SkinnedMeshRenderer) blocking.Add(r);
        }

        void OnDisable()
        {
            foreach (var kv in hidden) if (kv.Key != null) kv.Key.shadowCastingMode = kv.Value;
            hidden.Clear();
        }

        Camera cam;
        /// Quanto da parede lateral pode aparecer na borda da tela antes de a camera parar de seguir.
        const float WallMargin = 1.2f;

        void Apply()
        {
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            var center = focus;
            center.x = ClampX(center.x);
            transform.SetPositionAndRotation(center - rot * Vector3.forward * distance, rot);
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
