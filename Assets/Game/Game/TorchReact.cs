using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Game
{
    /// Tochas de parede reagem aos elementos (so visual): Agua perto apaga com um chiado e a luz cai a zero;
    /// reacende sozinha depois de alguns segundos, ou na hora com Fogo perto. Fica num objeto so da sala e cuida de todas.
    /// A intensidade e a cor que o BiomeDresser poe continuam valendo: aqui so se multiplica a luz e liga/desliga a chama.
    public class TorchReact : MonoBehaviour
    {
        public const float Relight = 8f, Reach = 4.5f;
        Light[] lights = new Light[0];
        ParticleSystem[] flames = new ParticleSystem[0];
        float[] baseIntensity = new float[0], outFor = new float[0], level = new float[0];
        /// Quantas vezes uma tocha apagou (para os testes).
        public int Doused { get; private set; }
        public bool IsOut(int i) => outFor[i] > 0f;
        public int Count => lights.Length;

        /// Velas do pack em pares (acesa, apagada): a chama faz parte da malha, entao apagar e trocar a malha.
        /// So Agua apaga (Ar apagando seria regra nova); Fogo ou 8 s reacendem.
        public static Mesh[] CandlePairs = new Mesh[0];
        readonly System.Collections.Generic.List<(MeshFilter filter, Mesh lit, float left)> candlesOut = new();
        /// Quantas velas apagaram (para os testes).
        public int CandlesDoused { get; private set; }
        public int CandlesOut => candlesOut.Count;

        void Start()
        {
            var ls = new System.Collections.Generic.List<Light>();
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) if (l.name == "TorchLight") ls.Add(l);
            lights = ls.ToArray();
            flames = new ParticleSystem[lights.Length];
            var fs = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                float best = 1.2f;
                foreach (var f in fs)
                {
                    if (f.name != "TorchFlame") continue;
                    float d = Vector3.Distance(f.transform.position, lights[i].transform.position);
                    if (d < best) { best = d; flames[i] = f; }
                }
            }
            baseIntensity = new float[lights.Length];
            outFor = new float[lights.Length];
            level = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) { baseIntensity[i] = lights[i].intensity; level[i] = 1f; }
        }

        void OnEnable() { GameEvents.AttackPerformed += OnAttack; GameEvents.ReactionTriggered += OnReaction; }
        void OnDisable() { GameEvents.AttackPerformed -= OnAttack; GameEvents.ReactionTriggered -= OnReaction; }

        void OnAttack(AttackEvent e)
        {
            if (e.Kind != AttackKind.Skill) return;
            if (e.Element == ElementType.Water) Touch(e.Origin, Mathf.Max(Reach, e.Range + 1f), false);
            else if (e.Element == ElementType.Fire) Touch(e.Origin, Mathf.Max(Reach, e.Range + 1f), true);
        }

        void OnReaction(ReactionEvent e)
        {
            if (e.Mark == ElementType.Water || e.Trigger == ElementType.Water) Touch(e.Point, e.Radius + 2f, false);
        }

        /// Apaga (ou reacende) as tochas e as velas a ate reach do ponto, medindo no chao.
        public void Touch(Vector3 at, float reach, bool fire)
        {
            TouchCandles(at, reach, fire);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                var d = lights[i].transform.position - at; d.y = 0f;
                if (d.magnitude > reach) continue;
                if (fire) { if (outFor[i] > 0f) { outFor[i] = 0f; SetFlame(i, true); Sfx.Play("cast_fire"); } }
                else if (outFor[i] <= 0f)
                {
                    outFor[i] = Relight;
                    SetFlame(i, false);
                    Doused++;
                    Sfx.Play("cast_water");
                }
            }
        }

        static bool Near(Vector3 p, Vector3 at, float reach) { var d = p - at; d.y = 0f; return d.magnitude <= reach; }

        Mesh UnlitOf(Mesh m)
        {
            for (int i = 0; i + 1 < CandlePairs.Length; i += 2) if (CandlePairs[i] != null && CandlePairs[i] == m) return CandlePairs[i + 1];
            return null;
        }

        void TouchCandles(Vector3 at, float reach, bool fire)
        {
            if (fire)
            {
                for (int i = candlesOut.Count - 1; i >= 0; i--)
                {
                    var c = candlesOut[i];
                    if (c.filter == null) { candlesOut.RemoveAt(i); continue; }
                    if (!Near(c.filter.transform.position, at, reach)) continue;
                    c.filter.sharedMesh = c.lit;
                    SetCandleFlame(c.filter, true);
                    candlesOut.RemoveAt(i);
                }
                return;
            }
            if (CandlePairs.Length < 2) return;
            RefreshCandles();
            bool any = false;
            foreach (var f in candles)
            {
                if (f == null) continue;
                var unlit = UnlitOf(f.sharedMesh);
                if (unlit == null || !Near(f.transform.position, at, reach)) continue;
                candlesOut.Add((f, f.sharedMesh, Relight));
                f.sharedMesh = unlit;
                SetCandleFlame(f, false);
                CandlesDoused++;
                any = true;
                if (feedback == null) feedback = FindFirstObjectByType<Feel.CombatFeedback>();
                feedback?.Burst(f.transform.position + Vector3.up * 0.5f, Vector3.up, 25f, 3, 0.4f, 1f, 0.08f, 0.16f, 0.9f, new Color(0.6f, 0.6f, 0.62f));
            }
            if (any) Sfx.Play("cast_water"); // o chiado de apagar
        }

        Feel.CombatFeedback feedback;
        readonly System.Collections.Generic.List<MeshFilter> candles = new();
        float scannedAt = -10f;

        /// A decoracao muda a cada sala: refaz a lista de velas quando alguma sumiu (troca de sala) ou a cada segundo,
        /// nunca a cada reacao (uma skill de area dispara varias reacoes de Agua no mesmo quadro).
        void RefreshCandles()
        {
            bool stale = Time.time - scannedAt > 1f;
            foreach (var c in candles) if (c == null) { stale = true; break; }
            if (!stale) return;
            scannedAt = Time.time;
            candles.Clear();
            foreach (var f in FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (f.sharedMesh == null) continue;
                for (int i = 0; i < CandlePairs.Length; i++) if (CandlePairs[i] == f.sharedMesh) { candles.Add(f); break; }
            }
            // velas da sala anterior (destruidas) saem do dicionario
            var gone = new System.Collections.Generic.List<MeshFilter>();
            foreach (var k in candleFlames.Keys) if (k == null) gone.Add(k);
            foreach (var k in gone) candleFlames.Remove(k);
            foreach (var c in candles) if (UnlitOf(c.sharedMesh) != null && !candleFlames.ContainsKey(c)) AddCandleFlame(c);
        }

        /// A chama da vela do pack tem uns 6 pixels na camera: apagar nao se via. Cada vela acesa ganha uma copia pequena
        /// e mais fraca da chama das tochas (o mesmo estilo ja aprovado, sem luz nova); ela some quando a vela apaga.
        readonly System.Collections.Generic.Dictionary<MeshFilter, ParticleSystem> candleFlames = new();
        public int CandleFlames => candleFlames.Count;

        void AddCandleFlame(MeshFilter candle)
        {
            ParticleSystem template = null;
            foreach (var f in flames) if (f != null) { template = f; break; }
            if (template == null) return;
            var r = candle.GetComponent<Renderer>();
            var top = r != null ? new Vector3(r.bounds.center.x, r.bounds.max.y, r.bounds.center.z) : candle.transform.position + Vector3.up * 0.5f;
            var ps = Instantiate(template, top + Vector3.up * 0.04f, template.transform.rotation, candle.transform);
            ps.name = "CandleFlame";
            ps.transform.localScale = Vector3.one; // a escala vem das curvas abaixo (a vela do pack tem escala propria)
            var main = ps.main;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            // a cor vem da chama das tochas (o vestidor tinge por bioma; na sala da Rainha fica fria como elas)
            var tc = template.main.startColor;
            Color lo = tc.mode == ParticleSystemGradientMode.TwoColors ? tc.colorMin : tc.color;
            Color hi = tc.mode == ParticleSystemGradientMode.TwoColors ? tc.colorMax : tc.color;
            main.startColor = new ParticleSystem.MinMaxGradient(lo * 0.9f, hi * 0.95f);
            var em = ps.emission; em.rateOverTime = 18f;
            var sh = ps.shape; sh.radius = 0.02f;
            ps.Clear(); ps.Play();
            candleFlames[candle] = ps;
        }

        public string CandleFlameInfo(MeshFilter candle) => candle != null && candleFlames.TryGetValue(candle, out var ps) && ps != null
            ? $"n={ps.particleCount} pos={ps.transform.position} vela={candle.transform.position} escala={ps.transform.lossyScale} pai={candle.transform.lossyScale} emit={ps.isEmitting} rend={ps.GetComponent<ParticleSystemRenderer>().enabled}" : "sem chama";
        public bool CandleFlameOn(MeshFilter candle) => candle != null && candleFlames.TryGetValue(candle, out var ps) && ps != null && ps.isEmitting;

        void SetCandleFlame(MeshFilter candle, bool on)
        {
            if (candle == null || !candleFlames.TryGetValue(candle, out var ps) || ps == null) return;
            if (on) ps.Play(); else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void SetFlame(int i, bool on)
        {
            var f = flames[i];
            if (f == null) return;
            if (on) f.Play(); else f.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = candlesOut.Count - 1; i >= 0; i--)
            {
                var c = candlesOut[i];
                if (c.filter == null) { candlesOut.RemoveAt(i); continue; }
                c.left -= dt;
                if (c.left <= 0f) { c.filter.sharedMesh = c.lit; SetCandleFlame(c.filter, true); candlesOut.RemoveAt(i); }
                else candlesOut[i] = c;
            }
            if (CandlePairs.Length >= 2) RefreshCandles(); // sala nova: as velas acesas ganham a chama (no maximo 1 busca por segundo)
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                if (outFor[i] > 0f && (outFor[i] -= dt) <= 0f) SetFlame(i, true);
                float want = outFor[i] > 0f ? 0f : 1f;
                level[i] = Mathf.MoveTowards(level[i], want, dt / 0.3f);
                lights[i].intensity = baseIntensity[i] * level[i];
            }
        }
    }
}
