using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Some depois de um tempo ou quando a vida acaba (pilar de pedra).
    public class TimedLife : MonoBehaviour
    {
        [SerializeField] float seconds = 10f;
        Health health;

        void Awake() => health = GetComponent<Health>();

        void Update()
        {
            seconds -= Time.deltaTime;
            if (seconds <= 0f || (health != null && health.IsDead)) Destroy(gameObject);
        }
    }
}
