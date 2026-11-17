using UnityEngine;

namespace Quintessence.Feel
{
    /// Luz de fogueira: a intensidade oscila devagar em volta da base (ruido, sem piscar), como chama de lenha.
    [RequireComponent(typeof(Light))]
    public class FireFlicker : MonoBehaviour
    {
        [SerializeField] float amount = 0.18f;
        [SerializeField] float speed = 3.2f;
        Light lamp;
        float baseIntensity, seed;

        void Awake()
        {
            lamp = GetComponent<Light>();
            baseIntensity = lamp.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * speed) - 0.5f;
            lamp.intensity = baseIntensity * (1f + n * 2f * amount);
        }
    }
}
