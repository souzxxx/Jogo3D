using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// Numero (ou palavra) que sobe do alvo e some. Sempre virado para a camera.
    public class DamageNumber : MonoBehaviour
    {
        static Font font;
        // textos vivos: o novo nasce acima dos que estao perto. Ele sobe mais rapido que os antigos (que ja desaceleraram),
        // entao a distancia so aumenta; nascer abaixo fazia o novo alcancar o antigo e escrever por cima dele
        static readonly List<DamageNumber> live = new();
        /// Quanto o novo sobe a mais que um antigo ja lento antes de desacelerar tambem (2,2 m/s caindo a 5 m/s2 ate 0,4).
        const float CatchUp = 0.5f;
        TextMesh front, shadow;
        Vector3 velocity;
        float life, total, baseSize;
        bool isNumber;
        bool still; // nasceu no alto da tela: fica parado em vez de subir
        Color color;

        /// No maximo tantos textos vivos: no climax (Quintessencia, reacoes em cadeia, chefe) passavam de 15 e viravam sopa.
        public const int MaxLive = 8;
        static readonly System.Collections.Generic.Dictionary<string, float> lastWord = new();

        /// Some com todos os textos no ar (vitoria: so o VITORIA! fica).
        /// Tira os textos no ar sem calar os proximos (painel de talento: o "NIVEL 2!" do mundo ficava parado atras do
        /// titulo "NIVEL 2" do painel, e numeros vazavam por cima dele com o jogo pausado).
        public static void ClearLive()
        {
            foreach (var n in live) if (n != null) Destroy(n.gameObject);
            live.Clear();
        }

        /// So os numeros de dano no ar (as palavras ficam): ao abrir as portas, os "7 7" da luta cobriam o texto delas.
        public static void ClearNumbers()
        {
            for (int i = live.Count - 1; i >= 0; i--)
            {
                var n = live[i];
                if (n == null) { live.RemoveAt(i); continue; }
                if (!n.isNumber || (n.front != null && n.front.text.StartsWith("+"))) continue; // "+25% de vida" e curas ficam
                Destroy(n.gameObject);
                live.RemoveAt(i);
            }
        }

        public static void ClearAll()
        {
            foreach (var n in live) if (n != null) Destroy(n.gameObject);
            live.Clear();
            Hush = true;
        }

        /// Depois do ClearAll da vitoria: nenhum texto novo ate a cena trocar.
        public static bool Hush;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetHush() { Hush = false; lastWord.Clear(); }

        /// Quantos textos estao no ar (para os testes).
        public static int LiveCount { get { live.RemoveAll(x => x == null); return live.Count; } }

        /// Mesma palavra (nome de reacao, status) ha menos de 'seconds': nao repete. Numeros nunca sao filtrados aqui.
        public static bool Recently(string word, float seconds)
        {
            float now = Time.unscaledTime;
            // (now < at: relogio de outra sessao de Play sem recarga de dominio; nao conta como recente)
            if (lastWord.TryGetValue(word, out float at) && now >= at && now - at < seconds) return true;
            lastWord[word] = now;
            return false;
        }

        public static void Spawn(Vector3 position, string text, Color color, float size = 1f, float seconds = 0.75f)
        {
            if (Hush) return;
            if (font == null) font = GameFonts.Display;
            live.RemoveAll(x => x == null);
            // lotado: sai primeiro o rotulo mais velho (palavra), so depois numero: o dano e o que o jogador precisa ler
            while (live.Count >= MaxLive)
            {
                int victim = live.FindIndex(x => x != null && !x.isNumber);
                if (victim < 0) victim = 0;
                var old = live[victim]; live.RemoveAt(victim);
                if (old != null) Destroy(old.gameObject);
            }
            var spot = position + new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f);
            // nascendo no alto da tela (sobre o chefe no fundo da sala): desce para dentro dela, nao sobe e empilha para baixo,
            // senao varios textos do mesmo instante (FASE 3, ESCUDO!) ficavam todos presos na mesma linha do limite
            bool atTop = false;
            var cam = Camera.main;
            if (cam != null)
            {
                var vp = cam.WorldToViewportPoint(spot);
                if (vp.z > 0f && vp.y > TopLimit - 0.08f)
                {
                    spot = cam.ViewportToWorldPoint(new Vector3(vp.x, Mathf.Min(vp.y, TopLimit - 0.03f), vp.z));
                    atTop = true;
                }
            }
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                foreach (var other in live)
                {
                    var o = other.transform.position;
                    // altura de uma linha no tamanho 1 (com o salto inicial de 1,25): ~0,72; a folga considera os dois tamanhos
                    float gap = 0.8f * (other.baseSize + size) * 0.5f + 0.12f;
                    // largura pelo tamanho do texto: duas palavras longas lado a lado (MAGMA! e BLOQUEOU) encostavam
                    float wide = Mathf.Max(1.8f * Mathf.Max(size, other.baseSize), 0.2f * (text.Length * size + other.textLen * other.baseSize));
                    if (Mathf.Abs(o.x - spot.x) < wide && Mathf.Abs(o.z - spot.z) < 1.6f && o.y - spot.y < gap + CatchUp && spot.y - o.y < gap)
                    { spot.y = atTop ? o.y - gap : o.y + gap; moved = true; }
                }
                if (!moved) break;
            }
            var go = new GameObject("DamageNumber");
            go.transform.position = spot;
            var n = go.AddComponent<DamageNumber>();
            n.shadow = Make(go.transform, text, new Vector3(0.05f, -0.05f, 0.02f));
            n.front = Make(go.transform, text, Vector3.zero);
            n.color = color; n.total = n.life = seconds; n.baseSize = size; n.textLen = text.Length;
            n.isNumber = text.Length > 0 && (char.IsDigit(text[0]) || text[0] == '+');
            n.velocity = new Vector3(Random.Range(-0.3f, 0.3f), 2.2f, 0f);
            n.still = atTop;
            if (atTop) n.velocity.y = 0f;
            n.Apply();
            live.Add(n);
        }

        void OnDestroy() => live.Remove(this);
        // cena nova (titulo, run nova): textos voltam a aparecer
        static DamageNumber() { UnityEngine.SceneManagement.SceneManager.sceneLoaded += (_, _) => { Hush = false; lastWord.Clear(); }; }

        static TextMesh Make(Transform parent, string text, Vector3 offset)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = offset;
            var t = go.AddComponent<TextMesh>();
            t.font = font;
            t.text = text;
            t.fontSize = 96;
            t.characterSize = 0.06f;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            return t;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            transform.position += velocity * dt;
            velocity.y = still ? 0f : Mathf.Max(0.4f, velocity.y - 5f * dt);
            KeepOnScreen();
            Apply();
        }

        /// Texto que nasce sobre algo no alto da tela (o chefe la no fundo da sala) saia cortado pela borda de cima e
        /// caia por cima da barra do chefe: fica abaixo de 80% da altura e dentro das laterais.
        void KeepOnScreen()
        {
            var cam = Camera.main;
            if (cam == null) return;
            var vp = cam.WorldToViewportPoint(transform.position);
            if (vp.z <= 0f) return;
            // a borda considera a metade da largura do texto (o centro preso a 6% ainda deixava "TEMPESTADE DE FOGO!" cortado)
            float half = 0.06f;
            if (front != null)
            {
                var r = front.GetComponent<Renderer>();
                if (r != null)
                {
                    var b = r.bounds;
                    var l = cam.WorldToViewportPoint(b.center - cam.transform.right * b.extents.magnitude * 0.7f);
                    var rr = cam.WorldToViewportPoint(b.center + cam.transform.right * b.extents.magnitude * 0.7f);
                    half = Mathf.Clamp(Mathf.Abs(rr.x - l.x) * 0.5f + 0.015f, 0.06f, 0.45f);
                }
            }
            float x = Mathf.Clamp(vp.x, half, 1f - half), y = Mathf.Min(vp.y, TopLimit);
            if (Mathf.Approximately(x, vp.x) && Mathf.Approximately(y, vp.y)) return;
            transform.position = cam.ViewportToWorldPoint(new Vector3(x, y, vp.z));
            if (y < vp.y)
            {
                velocity.y = 0.4f;
                // numero que subiu ate o teto e encostou numa palavra parada ali (MAGMA! sobre o chefe no fundo):
                // some logo em vez de ficar escrito por cima dela
                // (vale tambem para outro texto que subiu antes e ja esta parado no teto: o primeiro fica, o seguinte some)
                if (!still && !parked && life > CeilingFade)
                    foreach (var other in live)
                    {
                        if (other == null || other == this || !(other.still || other.parked)) continue;
                        var o = cam.WorldToViewportPoint(other.transform.position);
                        if (Mathf.Abs(o.y - y) < 0.05f && Mathf.Abs(o.x - x) < 0.16f) { life = CeilingFade; CeilingFades++; break; }
                    }
                parked = true;
            }
        }

        const float CeilingFade = 0.12f;
        bool parked;
        int textLen;
        /// Numeros que encostaram numa palavra parada no teto e sumiram (para os testes).
        public static int CeilingFades { get; private set; }

        // 0,8: abaixo da barra do chefe e da linha do escudo sob ela ("ESCUDO — aguente firme" ficava atras de "ESCUDO!")
        public const float TopLimit = 0.8f;

        void Apply()
        {
            float k = 1f - life / total;
            float pop = k < 0.15f ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, Mathf.Clamp01((k - 0.15f) / 0.2f));
            transform.localScale = Vector3.one * (baseSize * pop);
            float a = Mathf.Clamp01((1f - k) / 0.35f);
            front.color = new Color(color.r, color.g, color.b, a);
            shadow.color = new Color(0.03f, 0.02f, 0.06f, a * 0.9f);
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
