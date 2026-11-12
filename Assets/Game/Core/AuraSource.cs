using UnityEngine;

namespace Quintessence.Core
{
    /// Marca quem projeta uma aura em volta (Porta-Estandarte). O efeito mora em quem a usa; o feedback so le o raio para desenhar o circulo.
    public class AuraSource : MonoBehaviour
    {
        [SerializeField] float radius = 6f;
        public float Radius => radius;
        public void Configure(float r) => radius = r;
    }
}
