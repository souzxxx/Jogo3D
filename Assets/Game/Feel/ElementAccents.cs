using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// A assinatura de cada elemento nos efeitos, alem da cor: brasas que sobem (Fogo), gotas que caem em arco (Agua),
    /// lascas de pedra pesadas (Terra) e riscos rapidos de vento (Ar). Um sistema de particulas por elemento.
    public class ElementAccents
    {
        readonly ParticleSystem[] systems = new ParticleSystem[5];
        static Mesh cube;

        public ElementAccents(Transform parent, Material additive)
        {
            // material solido para as lascas: o mesmo shader das particulas, sem somar luz e sem textura
            var solid = new Material(additive);
            solid.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            solid.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (cube == null)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube = tmp.GetComponent<MeshFilter>().sharedMesh;
                Object.Destroy(tmp);
            }

            systems[(int)ElementType.Fire] = Make(parent, "Embers", additive, -0.35f, 700, r => { }, noise: 0.7f);
            systems[(int)ElementType.Water] = Make(parent, "Droplets", additive, 1.5f, 700, r =>
            {
                r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.07f; r.lengthScale = 1.6f;
            });
            systems[(int)ElementType.Earth] = Make(parent, "Debris", solid, 2.6f, 400, r =>
            {
                r.renderMode = ParticleSystemRenderMode.Mesh; r.mesh = cube;
            }, spin: true);
            systems[(int)ElementType.Air] = Make(parent, "Wisps", additive, 0f, 700, r =>
            {
                r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.16f; r.lengthScale = 3.2f;
            }, noise: 1.6f);
        }

        static ParticleSystem Make(Transform parent, string name, Material mat, float gravity, int max,
            System.Action<ParticleSystemRenderer> render, float noise = 0f, bool spin = false)
        {
            var go = new GameObject("Fx" + name);
            go.transform.SetParent(parent, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = max;
            main.playOnAwake = false;
            main.loop = false;
            main.gravityModifier = gravity;
            main.startRotation3D = spin;
            var em = ps.emission; em.enabled = false;
            var sh = ps.shape; sh.enabled = false;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 1, 1, spin ? 0.6f : 0.2f));
            if (noise > 0f)
            {
                var n = ps.noise; n.enabled = true; n.strength = noise; n.frequency = 0.9f; n.scrollSpeed = 0.6f;
            }
            if (spin)
            {
                var rot = ps.rotationOverLifetime; rot.enabled = true; rot.separateAxes = true;
                rot.x = new ParticleSystem.MinMaxCurve(-6f, 6f); rot.y = new ParticleSystem.MinMaxCurve(-6f, 6f); rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            }
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            render(r);
            ps.Play();
            return ps;
        }

        /// Solta o detalhe do elemento a partir de pos, na direcao dir (spread em graus para os lados), com forca power.
        public void Emit(ElementType element, Vector3 pos, Vector3 dir, float spread, int count, float power, Color? tint = null)
        {
            var ps = systems[(int)element];
            if (ps == null || count <= 0) return;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.up;
            var basis = Quaternion.LookRotation(dir.normalized, Mathf.Abs(dir.normalized.y) > 0.95f ? Vector3.forward : Vector3.up);
            // tint troca a cor mantendo o comportamento (lascas de aco do escudo, cinzas do Guardiao)
            var c = tint ?? ElementColors.Of(element);
            if (element == ElementType.Earth) count = Mathf.Max(1, count / 2);
            var p = new ParticleSystem.EmitParams();
            for (int i = 0; i < count; i++)
            {
                var d = basis * Quaternion.Euler(Random.Range(-spread, spread) * 0.4f, Random.Range(-spread, spread), 0) * Vector3.forward;
                p.position = pos + Random.insideUnitSphere * 0.25f;
                switch (element)
                {
                    case ElementType.Fire: // brasas pequenas que sobem e tremem
                        p.velocity = d * power * Random.Range(0.25f, 0.9f) + Vector3.up * Random.Range(0.6f, 2.2f);
                        p.startSize = Random.Range(0.18f, 0.42f);
                        p.startLifetime = Random.Range(0.8f, 1.4f);
                        p.startColor = Color.Lerp(c, new Color(1f, 0.85f, 0.5f), Random.Range(0f, 0.5f));
                        break;
                    case ElementType.Water: // gotas alongadas que sobem e caem
                        p.velocity = d * power * Random.Range(0.45f, 1f) + Vector3.up * Random.Range(2f, 4.5f);
                        p.startSize = Random.Range(0.2f, 0.36f);
                        p.startLifetime = Random.Range(0.6f, 0.95f);
                        p.startColor = Color.Lerp(c, Color.white, Random.Range(0.2f, 0.55f));
                        break;
                    case ElementType.Earth: // lascas de pedra: poucas, grandes, pesadas e girando
                        p.velocity = d * power * Random.Range(0.3f, 0.8f) + Vector3.up * Random.Range(2.5f, 5f);
                        p.startSize = Random.Range(0.22f, 0.5f);
                        p.startLifetime = Random.Range(0.8f, 1.1f);
                        p.rotation3D = Random.insideUnitSphere * 180f;
                        float shade = Random.Range(0.5f, 0.85f);
                        p.startColor = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                        break;
                    default: // Ar: riscos finos e rapidos, sem peso
                        var side = Vector3.Cross(Vector3.up, d).normalized * Random.Range(-1.5f, 1.5f);
                        p.position = pos + Random.insideUnitSphere * 0.7f;
                        p.velocity = d * power * Random.Range(0.9f, 1.5f) + side;
                        p.startSize = Random.Range(0.12f, 0.2f);
                        p.startLifetime = Random.Range(0.4f, 0.65f);
                        p.startColor = Color.Lerp(c, Color.white, Random.Range(0.4f, 0.7f));
                        break;
                }
                ps.Emit(p, 1);
            }
        }
    }
}
