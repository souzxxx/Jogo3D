using UnityEngine;

namespace Quintessence.Feel
{
    /// Tremor de camera. Roda depois de quem posiciona a camera e soma um deslocamento.
    [DefaultExecutionOrder(100)]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }
        [SerializeField] float maxOffset = 0.45f;
        [SerializeField] float decay = 3.2f;
        float trauma;

        void OnEnable() => Instance = this;
        void OnDisable() { if (Instance == this) Instance = null; }

        /// 0..1; valores somam e saturam em 1.
        public static void Add(float amount) { if (Instance != null && Quintessence.Core.FxPrefs.ScreenShake) Instance.trauma = Mathf.Clamp01(Instance.trauma + amount); }

        void LateUpdate()
        {
            if (trauma <= 0f) return;
            float s = trauma * trauma * maxOffset;
            transform.position += transform.right * Random.Range(-s, s) + transform.up * Random.Range(-s, s);
            trauma = Mathf.Max(0f, trauma - decay * Time.unscaledDeltaTime);
        }
    }
}
