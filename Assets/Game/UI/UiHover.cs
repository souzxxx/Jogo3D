using UnityEngine;
using UnityEngine.EventSystems;

namespace Quintessence.UI
{
    /// Botao cresce um pouco sob o mouse e afunda ao clicar. Usa tempo real para funcionar com o jogo pausado.
    public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public float hoverScale = 1.04f;
        bool over, down;

        public void OnPointerEnter(PointerEventData e) => over = true;
        public void OnPointerExit(PointerEventData e) { over = false; down = false; }
        public void OnPointerDown(PointerEventData e) => down = true;
        public void OnPointerUp(PointerEventData e) => down = false;

        void OnDisable() { over = down = false; transform.localScale = Vector3.one; }

        void Update()
        {
            float want = down ? 0.97f : over ? hoverScale : 1f;
            float s = Mathf.Lerp(transform.localScale.x, want, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
