using UnityEngine;

namespace Quintessence.Combat
{
    /// Liga o estado do heroi aos estados do Animator (Idle, Run, Dash, Attack0..2).
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] Animator animator;
        string current;

        void OnEnable()
        {
            controller.AttackStarted += OnAttack;
            controller.DashStarted += OnDash;
        }

        void OnDisable()
        {
            controller.AttackStarted -= OnAttack;
            controller.DashStarted -= OnDash;
        }

        public void Bind(PlayerController c, Animator a) { controller = c; animator = a; }

        void OnAttack(int index) => Play("Attack" + index, 0.05f, true);
        void OnDash(Vector3 _) => Play("Dash", 0.03f, true);

        void Update()
        {
            if (controller.IsAttacking || controller.IsDashing) return;
            Play(controller.IsMoving ? "Run" : "Idle", 0.1f, false);
        }

        void Play(string state, float fade, bool restart)
        {
            if (!restart && current == state) return;
            current = state;
            animator.CrossFadeInFixedTime(state, fade, 0, 0f);
        }
    }
}
