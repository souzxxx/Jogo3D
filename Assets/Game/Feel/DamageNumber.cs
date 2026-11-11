using UnityEngine;

namespace Quintessence.Feel
{
    /// Numero (ou palavra) que sobe do alvo e some. Sempre virado para a camera.
    public class DamageNumber : MonoBehaviour
    {
        static Font font;
        TextMesh front, shadow;
        Vector3 velocity;
        float life, total, baseSize;
        Color color;

        public static void Spawn(Vector3 position, string text, Color color, float size = 1f, float seconds = 0.75f)
        {
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var go = new GameObject("DamageNumber");
            go.transform.position = position + new Vector3(Random.Range(-0.35f, 0.35f), 0f, 0f);
            var n = go.AddComponent<DamageNumber>();
            n.shadow = Make(go.transform, text, new Vector3(0.035f, -0.035f, 0.02f));
            n.front = Make(go.transform, text, Vector3.zero);
            n.color = color; n.total = n.life = seconds; n.baseSize = size;
            n.velocity = new Vector3(Random.Range(-0.5f, 0.5f), 2.6f, 0f);
            n.Apply();
        }

        static TextMesh Make(Transform parent, string text, Vector3 offset)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var t = go.AddComponent<TextMesh>();
            t.font = font;
            t.text = text;
            t.fontSize = 72;
            t.characterSize = 0.075f;
            t.fontStyle = FontStyle.Bold;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return t;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            transform.position += velocity * dt;
            velocity.y = Mathf.Max(0.4f, velocity.y - 5f * dt);
            Apply();
        }

        void Apply()
        {
            float k = 1f - life / total;
            float pop = k < 0.15f ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            transform.localScale = Vector3.one * (baseSize * pop);
            float a = Mathf.Clamp01((1f - k) / 0.35f);
            front.color = new Color(color.r, color.g, color.b, a);
            shadow.color = new Color(0.03f, 0.02f, 0.06f, a * 0.9f);
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
