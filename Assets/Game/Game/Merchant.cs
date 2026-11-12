using UnityEngine;

namespace Quintessence.Game
{
    /// Personagem que atende o heroi (Mercador da loja, Mestre das bencaos): acompanha com o corpo, cumprimenta quando ele chega perto e reage.
    public class Merchant : MonoBehaviour
    {
        static readonly Color Voice = new(0.93f, 0.88f, 0.78f);
        [SerializeField] string greetLine = "Bem-vindo! Essência compra tudo aqui.";
        [SerializeField] string thankLine = "Boa escolha!";
        [SerializeField] string refuseLine = "Falta Essência...";

        /// Troca as falas (o Mestre usa o mesmo comportamento do Mercador).
        public void SetLines(string greet, string thank, string refuse) { greetLine = greet; thankLine = thank; refuseLine = refuse; }
        Animator animator;
        Transform customer;
        float hold;
        string current = "Idle";

        public bool Greeted { get; private set; }
        /// Ultima fala (para os testes).
        public string LastLine { get; private set; } = "";

        public void Bind(Transform hero)
        {
            customer = hero;
            animator = GetComponent<Animator>();
        }

        public void Thank() => React("Thanks", 1.1f, thankLine);
        public void Refuse() => React("Refuse", 0.6f, refuseLine);

        void React(string state, float seconds, string line)
        {
            Say(line);
            if (animator == null) return;
            current = state;
            animator.CrossFadeInFixedTime(state, 0.08f, 0, 0f);
            hold = seconds;
        }

        void Say(string line)
        {
            LastLine = line;
            Feel.DamageNumber.Spawn(transform.position + Vector3.up * 3.9f, line, Voice, 0.8f, 1.6f);
        }

        void Update()
        {
            if (customer == null) return;
            var to = customer.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude > 0.05f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 220f * Time.deltaTime);
            if (!Greeted && to.magnitude < 10.5f)
            {
                Greeted = true;
                React("Greet", 1.1f, greetLine);
            }
            if (hold > 0f && (hold -= Time.deltaTime) <= 0f && animator != null && current != "Idle")
            {
                current = "Idle";
                animator.CrossFadeInFixedTime("Idle", 0.2f, 0, 0f);
            }
        }
    }
}
