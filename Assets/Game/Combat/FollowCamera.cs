using UnityEngine;

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
        }

        void Apply()
        {
            var rot = Quaternion.Euler(pitch, 0f, 0f);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
        }
    }
}
