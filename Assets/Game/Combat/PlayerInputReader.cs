using UnityEngine;
using UnityEngine.InputSystem;

namespace Quintessence.Combat
{
    /// Teclado + mouse e controle ao mesmo tempo; vale o ultimo que mexeu.
    /// Teclado: WASD move, mouse mira, clique esquerdo ataca, 1-4 skills, Espaco dash, R Quintessencia, E interage.
    /// Controle: analogico esquerdo move, direito mira, RT ou X ataca, direcional (cima, direita, baixo, esquerda) = skills 1-4,
    /// LT = primeira skill pronta, A dash, Y Quintessencia, B interage.
    public class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        const float StickDeadzone = 0.25f, TriggerPoint = 0.45f;
        Camera cam;
        Vector3 aim;
        Vector2 lastMouse;
        bool usingPad, leftTriggerWas;

        /// O jogador esta no controle (o HUD troca as dicas de tecla).
        public bool UsingGamepad => usingPad;

        void Update()
        {
            // quem mexeu por ultimo manda na mira
            var pad = Gamepad.current;
            if (pad != null && (pad.leftStick.ReadValue().sqrMagnitude > 0.1f || pad.rightStick.ReadValue().sqrMagnitude > 0.1f || pad.buttonSouth.isPressed))
                usingPad = true;
            var m = Mouse.current;
            if (m != null)
            {
                var pos = m.position.ReadValue();
                if ((pos - lastMouse).sqrMagnitude > 4f || m.leftButton.isPressed) usingPad = false;
                lastMouse = pos;
            }
            var k = Keyboard.current;
            if (k != null && k.anyKey.isPressed) usingPad = false;
        }

        public Vector2 Move
        {
            get
            {
                var v = Vector2.zero;
                var k = Keyboard.current;
                if (k != null)
                    v = new Vector2(
                        (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                        (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                var pad = Gamepad.current;
                if (pad != null)
                {
                    var stick = pad.leftStick.ReadValue();
                    if (stick.magnitude > StickDeadzone) v += stick;
                }
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public Vector3 AimPoint
        {
            get
            {
                var pad = Gamepad.current;
                if (usingPad && pad != null)
                {
                    // analogico direito aponta; sem ele, o heroi mira para onde anda
                    var stick = pad.rightStick.ReadValue();
                    if (stick.magnitude < StickDeadzone) stick = pad.leftStick.ReadValue();
                    if (stick.magnitude >= StickDeadzone)
                        aim = transform.position + new Vector3(stick.x, 0f, stick.y).normalized * 6f;
                    return aim;
                }
                if (cam == null) cam = Camera.main;
                var m = Mouse.current;
                if (cam == null || m == null) return aim;
                var ray = cam.ScreenPointToRay(m.position.ReadValue());
                var plane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                if (plane.Raycast(ray, out float d)) aim = ray.GetPoint(d);
                return aim;
            }
        }

        public bool AttackHeld
        {
            get
            {
                if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
                var pad = Gamepad.current;
                return pad != null && (pad.rightTrigger.ReadValue() > TriggerPoint || pad.buttonWest.isPressed);
            }
        }

        public bool ConsumeDash() =>
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);

        public bool ConsumeSkill()
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
            var pad = Gamepad.current;
            if (pad == null) return false;
            bool down = pad.leftTrigger.ReadValue() > TriggerPoint;
            bool pressed = down && !leftTriggerWas;
            leftTriggerWas = down;
            return pressed;
        }

        public int ConsumeElementSkill()
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) return 1;
                if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) return 2;
                if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) return 3;
                if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) return 4;
            }
            var pad = Gamepad.current;
            if (pad != null)
            {
                if (pad.dpad.up.wasPressedThisFrame) return 1;
                if (pad.dpad.right.wasPressedThisFrame) return 2;
                if (pad.dpad.down.wasPressedThisFrame) return 3;
                if (pad.dpad.left.wasPressedThisFrame) return 4;
            }
            return 0;
        }

        public bool ConsumeInteract() =>
            (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        public bool ConsumeQuintessence() =>
            (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
    }

    /// Atalhos fora do combate (pausa, mapa, inventario), no teclado e no controle.
    public static class MenuInput
    {
        public static bool PausePressed =>
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        public static bool InventoryPressed =>
            (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame) ||
            (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);

        public static bool MapHeld =>
            (Keyboard.current != null && Keyboard.current.tabKey.isPressed) ||
            (Gamepad.current != null && Gamepad.current.rightStickButton.isPressed);

        public static bool GamepadConnected => Gamepad.current != null;
    }
}
