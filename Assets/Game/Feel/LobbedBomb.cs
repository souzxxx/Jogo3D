using UnityEngine;

namespace Quintessence.Feel
{
    /// A bomba do Arremessador: sai da mao dele, sobe em arco e cai no centro do circulo avisado.
    public class LobbedBomb : MonoBehaviour
    {
        Vector3 from, to;
        float duration, t, height;
        Material material;

        public static LobbedBomb Spawn(Vector3 from, Vector3 to, float duration, Material flat) => Spawn(from, to, duration, flat, new Color(0.16f, 0.14f, 0.15f));

        public static LobbedBomb Spawn(Vector3 from, Vector3 to, float duration, Material flat, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "LobbedBomb";
            go.transform.localScale = Vector3.one * 0.55f;
            var m = new Material(flat) { color = color };
            go.GetComponent<Renderer>().material = m;
            var b = go.AddComponent<LobbedBomb>();
            b.material = m;
            b.from = from; b.to = to; b.duration = Mathf.Max(0.1f, duration);
            b.height = 2.5f + Vector3.Distance(from, to) * 0.25f;
            go.transform.position = from;
            return b;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            var p = Vector3.Lerp(from, to, k);
            p.y += Mathf.Sin(k * Mathf.PI) * height;
            transform.position = p;
            transform.Rotate(360f * Time.deltaTime, 200f * Time.deltaTime, 0f);
            if (k >= 1f) Destroy(gameObject);
        }

        void OnDestroy() { if (material != null) Destroy(material); }
    }
}
