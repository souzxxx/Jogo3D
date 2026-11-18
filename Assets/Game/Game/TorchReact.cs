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

        /// Apaga (ou reacende) as tochas a ate reach do ponto, medindo no chao.
        public void Touch(Vector3 at, float reach, bool fire)
        {
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

        void SetFlame(int i, bool on)
        {
            var f = flames[i];
            if (f == null) return;
            if (on) f.Play(); else f.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
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
