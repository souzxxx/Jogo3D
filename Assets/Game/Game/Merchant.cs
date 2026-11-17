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

        float speaking;
        Transform nameTag;

        void Say(string line)
        {
            LastLine = line;
            // a fala ocupa o lugar do nome enquanto dura (os dois na mesma altura se sobrepunham)
            if (nameTag == null) nameTag = transform.Find("Label");
            if (nameTag != null) nameTag.gameObject.SetActive(false);
            speaking = 1.7f;
            // perto da parede a frase centrada no personagem passava da borda da tela: entra na sala o quanto precisar
            var at = transform.position + Vector3.up * 3.1f; // rente ao chapeu: mais alto, a fala subia na tela ate os rotulos dos baus
            // frase longa em duas linhas: fica estreita, junto do personagem, sem cobrir os rotulos vizinhos
            string shown = line;
            if (line.Length > 18)
            {
                int mid = line.Length / 2, cut = -1;
                for (int d = 0; d < mid && cut < 0; d++)
                {
                    if (mid + d < line.Length && line[mid + d] == ' ') cut = mid + d;
                    else if (line[mid - d] == ' ') cut = mid - d;
                }
                if (cut > 0) shown = line.Substring(0, cut) + "\n" + line.Substring(cut + 1);
            }
            int widest = 0;
            foreach (var part in shown.Split('\n')) widest = Mathf.Max(widest, part.Length);
            float half = widest * 0.12f; // meia largura aproximada da linha mais longa no tamanho 0,8
            float limit = Mathf.Max(0f, Quintessence.Core.Arena.HalfX - half - 0.6f);
            at.x = Mathf.Clamp(at.x, -limit, limit);
            // e dentro do que a camera mostra (a sala e mais larga que a tela quando o heroi esta no meio)
            var cam = Camera.main;
            if (cam != null)
            {
                float l = cam.WorldToViewportPoint(at - Vector3.right * half).x, r = cam.WorldToViewportPoint(at + Vector3.right * half).x;
                float perView = r > l + 0.001f ? 2f * half / (r - l) : 0f;
                if (l < 0.03f) at.x += (0.03f - l) * perView;
                else if (r > 0.97f) at.x -= (r - 0.97f) * perView;
            }
            Feel.DamageNumber.Spawn(at, shown, Voice, 0.8f, 1.6f);
        }

        void Update()
        {
            if (speaking > 0f && (speaking -= Time.deltaTime) <= 0f && nameTag != null) nameTag.gameObject.SetActive(true);
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
