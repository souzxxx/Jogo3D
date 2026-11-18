using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Quintessence.UI
{
    /// Mora no EventSystem e cuida do foco para quem joga so com o controle:
    /// - painel que acabou de abrir nao aceita o A por um instante (o A tambem e o dash: quem apertava o dash
    ///   no fim da sala escolhia o talento ou o "Jogar de novo" sem ver);
    /// - sem nada escolhido (controle ligado depois, clique do mouse no vazio), mexer o direcional ou apertar A
    ///   devolve o foco ao ultimo botao focado que ainda esta na tela.
    public class FocusGuard : MonoBehaviour
    {
        /// Quanto tempo o A fica sem efeito depois que um painel pede foco.
        public const float SubmitDelay = 0.35f;

        /// Ultimo botao que um painel pediu para focar (mesmo sem controle: o controle pode chegar depois).
        public static GameObject LastFocused { get; private set; }
        /// Vezes que o foco voltou sozinho (para os testes).
        public static int Recovered { get; private set; }

        float blockedUntil;
        EventSystem system;

        public static void Remember(GameObject go) => LastFocused = go;

        /// Painel pediu foco com o controle ligado: segura o A (e o direcional) por SubmitDelay.
        public static void Hold(EventSystem es)
        {
            if (es == null) return;
            var guard = es.GetComponent<FocusGuard>();
            if (guard == null) guard = es.gameObject.AddComponent<FocusGuard>();
            guard.system = es;
            guard.blockedUntil = Time.unscaledTime + SubmitDelay;
            es.sendNavigationEvents = false;
        }

        public bool Holding => system != null && !system.sendNavigationEvents;

        void Update()
        {
            if (system == null) system = GetComponent<EventSystem>();
            if (system == null) return;
            if (!system.sendNavigationEvents && Time.unscaledTime >= blockedUntil) system.sendNavigationEvents = true;
            var pad = Gamepad.current;
            if (pad == null) return;
            TryRecover(pad.dpad.ReadValue().sqrMagnitude > 0.25f || pad.leftStick.ReadValue().sqrMagnitude > 0.25f || pad.buttonSouth.wasPressedThisFrame);
        }

        /// O controle mexeu (direcional, analogico ou A) com nada escolhido: volta o foco ao ultimo botao focado
        /// que ainda esta na tela. Devolve se voltou.
        public bool TryRecover(bool padMoved)
        {
            if (!padMoved || system == null || !system.sendNavigationEvents) return false;
            var current = system.currentSelectedGameObject;
            if (current != null && current.activeInHierarchy) return false;
            if (LastFocused == null || !LastFocused.activeInHierarchy) return false;
            var sel = LastFocused.GetComponent<Selectable>();
            if (sel != null && !sel.IsInteractable()) return false;
            system.SetSelectedGameObject(LastFocused);
            Recovered++;
            return true;
        }

        void OnDisable()
        {
            if (system != null) system.sendNavigationEvents = true;
        }
    }
}
