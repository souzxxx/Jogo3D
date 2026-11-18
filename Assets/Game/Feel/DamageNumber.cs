using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// Numero (ou palavra) que sobe do alvo e some. Sempre virado para a camera.
    public class DamageNumber : MonoBehaviour
    {
        static Font font;
        // textos vivos: o novo nasce acima dos que estao perto. Ele sobe mais rapido que os antigos (que ja desaceleraram),
        // entao a distancia so aumenta; nascer abaixo fazia o novo alcancar o antigo e escrever por cima dele
        static readonly List<DamageNumber> live = new();
        /// Quanto o novo sobe a mais que um antigo ja lento antes de desacelerar tambem (2,2 m/s caindo a 5 m/s2 ate 0,4).
        const float CatchUp = 0.5f;
        TextMesh front, shadow;
        Vector3 velocity;
        float life, total, baseSize;
        Color color;

        public static void Spawn(Vector3 position, string text, Color color, float size = 1f, float seconds = 0.75f)
        {
            if (font == null) font = GameFonts.Display;
            live.RemoveAll(x => x == null);
            var spot = position + new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f);
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                foreach (var other in live)
                {
                    var o = other.transform.position;
                    // altura de uma linha no tamanho 1 (com o salto inicial de 1,25): ~0,72; a folga considera os dois tamanhos
                    float gap = 0.8f * (other.baseSize + size) * 0.5f + 0.12f;
                    if (Mathf.Abs(o.x - spot.x) < 1.8f * Mathf.Max(size, other.baseSize) && Mathf.Abs(o.z - spot.z) < 1.6f && o.y - spot.y < gap + CatchUp && spot.y - o.y < gap)
                    { spot.y = o.y + gap; moved = true; }
                }
                if (!moved) break;
            }
            var go = new GameObject("DamageNumber");
            go.transform.position = spot;
            var n = go.AddComponent<DamageNumber>();
            n.shadow = Make(go.transform, text, new Vector3(0.05f, -0.05f, 0.02f));
            n.front = Make(go.transform, text, Vector3.zero);
            n.color = color; n.total = n.life = seconds; n.baseSize = size;
            n.velocity = new Vector3(Random.Range(-0.3f, 0.3f), 2.2f, 0f);
            n.Apply();
            live.Add(n);
        }

        void OnDestroy() => live.Remove(this);

        static TextMesh Make(Transform parent, string text, Vector3 offset)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var t = go.AddComponent<TextMesh>();
            t.font = font;
            t.text = text;
            t.fontSize = 96;
            t.characterSize = 0.06f;
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
            KeepOnScreen();
            Apply();
        }

        /// Texto que nasce sobre algo no alto da tela (o chefe la no fundo da sala) saia cortado pela borda de cima e
        /// caia por cima da barra do chefe: fica abaixo de 85% da altura e dentro das laterais.
        void KeepOnScreen()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var vp = cam.WorldToViewportPoint(transform.position);
            if (vp.z <= 0f) return;
            float x = Mathf.Clamp(vp.x, 0.06f, 0.94f), y = Mathf.Min(vp.y, TopLimit);
            if (Mathf.Approximately(x, vp.x) && Mathf.Approximately(y, vp.y)) return;
            transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, vp.z));
            if (y < vp.y) velocity.y = 0.4f;
        }

        public const float TopLimit = 0.85f;

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
