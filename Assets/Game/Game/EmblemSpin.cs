using UnityEngine;

namespace Quintessence.Game
{
    /// Emblema de elemento sobre um totem: gira devagar e flutua um palmo, para ler como coisa viva sem piscar.
    public class EmblemSpin : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 28f;
        [SerializeField] float bob = 0.06f;
        Vector3 home;
        float phase;

        void Start()
        {
            home = transform.localPosition;
            phase = Random.value * 6.28f; // os quatro do titulo nao sobem e descem juntos
        }

        void Update()
        {
            float t = Time.unscaledTime;
            transform.localPosition = home + Vector3.up * (bob * Mathf.Sin(t * 1.6f + phase));
            transform.localRotation = Quaternion.Euler(0f, t * degreesPerSecond + phase * 57f, 0f);
        }
    }
}
