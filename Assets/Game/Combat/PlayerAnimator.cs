using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Liga o estado do heroi aos estados do Animator: combo de tres golpes, uma skill por elemento,
    /// esquiva para o lado do dash, reacao ao apanhar, morte e a entrada na Quintessencia.
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] PlayerController controller;
        [SerializeField] Animator animator;
        string current;
        float hold;
        bool dead;

        /// Estado de animacao atual (para os testes).
        public string Current => current;

        void OnEnable()
        {
            controller.AttackStarted += OnAttack;
            controller.DashStarted += OnDash;
            controller.SkillStarted += OnSkill;
            GameEvents.DamageDealt += OnDamage;
            GameEvents.Died += OnDied;
            GameEvents.QuintessenceActiveChanged += OnQuintessence;
        }

        void OnDisable()
        {
            controller.AttackStarted -= OnAttack;
            controller.DashStarted -= OnDash;
            controller.SkillStarted -= OnSkill;
            GameEvents.DamageDealt -= OnDamage;
            GameEvents.Died -= OnDied;
            GameEvents.QuintessenceActiveChanged -= OnQuintessence;
        }

        public void Bind(PlayerController c, Animator a) { controller = c; animator = a; }

        void OnAttack(int index) { if (!dead) Play("Attack" + Mathf.Clamp(index, 0, 2), 0.05f, true); }

        void OnDash(Vector3 direction)
        {
            if (dead) return;
            var local = Quaternion.Inverse(controller.transform.rotation) * direction;
            string state = Mathf.Abs(local.x) > Mathf.Abs(local.z) ? (local.x < 0f ? "DashL" : "DashR") : (local.z < 0f ? "DashB" : "DashF");
            Play(state, 0.03f, true);
        }

        void OnSkill(ElementType element)
        {
            if (dead) return;
            Play(element switch
            {
                ElementType.Water => "SkillWater",
                ElementType.Earth => "SkillEarth",
                ElementType.Air => "SkillAir",
                _ => "SkillFire",
            }, 0.04f, true);
        }

        void OnDamage(Health target, DamageInfo info, float applied)
        {
            // so reage se nao estiver no meio de um golpe, dash ou skill: apanhar nao corta a acao do jogador
            if (dead || target != controller.Health || controller.IsAttacking || controller.IsDashing || controller.IsCasting) return;
            Play("Hit", 0.03f, true);
            hold = 0.22f;
        }

        void OnDied(Health target, DamageInfo info)
        {
            if (target != controller.Health) return;
            dead = true;
            Play("Death", 0.05f, true);
        }

        void OnQuintessence(bool active)
        {
            if (!active || dead) return;
            Play("Quint", 0.05f, true);
            hold = 0.55f;
        }

        void Update()
        {
            if (dead) return;
            if (controller.IsAttacking || controller.IsDashing || controller.IsCasting) { hold = 0f; return; }
            if (hold > 0f) { hold -= Time.deltaTime; return; }
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
