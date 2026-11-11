using UnityEngine;
using UnityEngine.InputSystem;

namespace Quintessence.Combat
{
    /// Teclado + mouse: WASD move, mouse mira, clique esquerdo ataca, Espaco da dash.
    public class PlayerInputReader : MonoBehaviour, IPlayerInput
    {
        Camera cam;
        Vector3 aim;

        public Vector2 Move
        {
            get
            {
                var k = Keyboard.current;
                if (k == null) return Vector2.zero;
                var v = new Vector2(
                    (k.dKey.isPressed ? 1 : 0) - (k.aKey.isPressed ? 1 : 0),
                    (k.wKey.isPressed ? 1 : 0) - (k.sKey.isPressed ? 1 : 0));
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public Vector3 AimPoint
        {
            get
            {
                if (cam == null) cam = Camera.main;
                var m = Mouse.current;
                if (cam == null || m == null) return aim;
                var ray = cam.ScreenPointToRay(m.position.ReadValue());
                var plane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
                if (plane.Raycast(ray, out float d)) aim = ray.GetPoint(d);
                return aim;
            }
        }

        public bool AttackHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;

        public bool ConsumeDash() => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    }
}
