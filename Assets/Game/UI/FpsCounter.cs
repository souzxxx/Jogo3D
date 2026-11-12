using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Quintessence.UI
{
    /// Medidor de desempenho ligado no F3: quadros por segundo (media de meio segundo) e o pior quadro do periodo.
    /// Usa tempo real, entao continua valendo com o jogo pausado ou em pausa de impacto.
    public class FpsCounter : MonoBehaviour
    {
        Text label;
        float elapsed, worst;
        int frames;

        public static FpsCounter Attach(Canvas canvas)
        {
            var rt = UiKit.Rect("Fps", canvas.transform, new Vector2(1, 1), new Vector2(-40, -112), new Vector2(360, 30));
            var c = rt.gameObject.AddComponent<FpsCounter>();
            c.label = UiKit.Text(UiKit.Rect("Text", rt, new Vector2(1, 0.5f), Vector2.zero, new Vector2(360, 30)), "", 22, UiKit.Dim, TextAnchor.MiddleRight, false);
            c.label.enabled = false;
            return c;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f3Key.wasPressedThisFrame) { label.enabled = !label.enabled; elapsed = worst = 0f; frames = 0; }
            if (!label.enabled) return;
            float dt = Time.unscaledDeltaTime;
            elapsed += dt; frames++;
            if (dt > worst) worst = dt;
            if (elapsed < 0.5f) return;
            label.text = $"{frames / elapsed:0} fps  ·  pior quadro {worst * 1000f:0} ms";
            elapsed = worst = 0f; frames = 0;
        }
    }
}
