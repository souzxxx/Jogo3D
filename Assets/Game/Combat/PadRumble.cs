using Quintessence.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quintessence.Combat
{
    /// Vibracao do controle (so quando o jogador esta no controle): forte e curta ao levar golpe, leve nos golpes pesados
    /// do heroi e nas reacoes, longa no chefe caindo. Para tudo na pausa, ao perder o foco e ao sair da cena.
    [RequireComponent(typeof(PlayerController))]
    public class PadRumble : MonoBehaviour
    {
        Health self;
        float low, high, left;
        bool running;
        Gamepad rumblePad; // o controle que esta vibrando (se outro virar o atual, este ainda precisa parar)

        /// Ultimo pedido de vibracao (para os testes): motor grave, motor agudo, duracao.
        public Vector3 Last { get; private set; }

        void Awake() => self = GetComponent<Health>();

        void OnEnable()
        {
            GameEvents.DamageDealt += OnDamage;
            GameEvents.ReactionTriggered += OnReaction;
            GameEvents.Died += OnDied;
        }

        void OnDisable()
        {
            GameEvents.DamageDealt -= OnDamage;
            GameEvents.ReactionTriggered -= OnReaction;
            GameEvents.Died -= OnDied;
            Stop();
        }

        void OnApplicationFocus(bool focused) { if (!focused) Stop(); }

        void OnDamage(Health target, DamageInfo info, float applied)
        {
            if (applied <= 0f) return;
            if (target == self) Pulse(0.55f, 0.35f, 0.2f);
            else if (info.SourceTeam == Team.Player && applied >= 10f && !info.IsReaction) Pulse(0.12f, 0.3f, 0.07f);
        }

        void OnReaction(ReactionEvent e) => Pulse(0.3f, 0.45f, 0.14f);

        void OnDied(Health target, DamageInfo info)
        {
            if (target == self) Pulse(0.8f, 0.5f, 0.45f);
            else if (target != null && target.GetComponent("BossBrain") != null) Pulse(0.9f, 0.6f, 0.7f);
        }

        /// Soma o pedido ao que ja esta vibrando (fica o mais forte e o mais longo).
        public void Pulse(float lowMotor, float highMotor, float seconds)
        {
            if (!InputMode.Gamepad || !FxPrefs.ScreenShake) return; // a mesma opcao do tremor de tela (Tremor e vibracao)
            low = Mathf.Max(low, lowMotor); high = Mathf.Max(high, highMotor); left = Mathf.Max(left, seconds);
            Last = new Vector3(lowMotor, highMotor, seconds);
        }

        void Update()
        {
            var pad = Gamepad.current;
            if (running && pad != rumblePad) Stop(); // controle trocado ou desligado: para o antigo e esquece o pulso
            if (pad == null) { low = high = left = 0f; return; }
            if (GameClock.Paused || !InputMode.Gamepad || !FxPrefs.ScreenShake || left <= 0f) { Stop(); return; }
            left -= Time.unscaledDeltaTime;
            pad.SetMotorSpeeds(low, high);
            rumblePad = pad;
            running = true;
            if (left <= 0f) Stop();
        }

        void Stop()
        {
            low = high = left = 0f;
            if (!running) return;
            running = false;
            rumblePad?.SetMotorSpeeds(0f, 0f);
            rumblePad = null;
        }
    }
}
