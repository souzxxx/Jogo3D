using System.Collections.Generic;
using Quintessence.Run;
using UnityEngine;

namespace Quintessence.Game
{
    /// Veste a sala com as cores do trecho da run: luz ambiente, neblina, tochas e a cor da corrupcao no chao.
    /// A sala e a mesma; o que muda e o clima. Tudo fosco, no registro do pack.
    public class BiomeDresser : MonoBehaviour
    {
        struct Look
        {
            public Color Sky, Equator, Ground, Fog, Sun, Torch, Crack, Crystal, Stone;
            public float SunIntensity, CrackGlow, CrystalGlow;
        }

        static Look Of(Biome b) => b switch
        {
            Biome.Crypt => new Look
            {
                Sky = new Color(0.14f, 0.24f, 0.36f), Equator = new Color(0.1f, 0.17f, 0.25f), Ground = new Color(0.05f, 0.08f, 0.11f),
                Fog = new Color(0.03f, 0.06f, 0.09f), Sun = new Color(0.62f, 0.78f, 0.95f), SunIntensity = 0.62f,
                Torch = new Color(0.72f, 0.82f, 0.95f), Crack = new Color(0.2f, 0.46f, 0.55f), CrackGlow = 0.3f,
                Crystal = new Color(0.45f, 0.68f, 0.74f), CrystalGlow = 0.4f, Stone = new Color(0.4f, 0.54f, 0.66f),
            },
            Biome.Garden => new Look
            {
                Sky = new Color(0.2f, 0.27f, 0.2f), Equator = new Color(0.15f, 0.19f, 0.14f), Ground = new Color(0.07f, 0.09f, 0.06f),
                Fog = new Color(0.04f, 0.06f, 0.04f), Sun = new Color(0.95f, 0.88f, 0.62f), SunIntensity = 0.8f,
                Torch = new Color(1f, 0.78f, 0.45f), Crack = new Color(0.4f, 0.52f, 0.26f), CrackGlow = 0.25f,
                Crystal = new Color(0.5f, 0.7f, 0.5f), CrystalGlow = 0.4f, Stone = new Color(0.52f, 0.58f, 0.46f),
            },
            Biome.Heart => new Look
            {
                // a sala do chefe era toda vermelha (chao, luz, chefe e heroi de Fogo no mesmo tom): luz principal
                // quase neutra e pedra menos tingida; o vermelho fica nas rachaduras, nos cristais e nas tochas
                Sky = new Color(0.25f, 0.19f, 0.22f), Equator = new Color(0.18f, 0.14f, 0.16f), Ground = new Color(0.08f, 0.05f, 0.05f),
                Fog = new Color(0.07f, 0.035f, 0.035f), Sun = new Color(1f, 0.8f, 0.68f), SunIntensity = 0.82f,
                Torch = new Color(1f, 0.5f, 0.22f), Crack = new Color(0.86f, 0.32f, 0.12f), CrackGlow = 0.35f,
                Crystal = new Color(0.9f, 0.4f, 0.14f), CrystalGlow = 0.6f, Stone = new Color(0.55f, 0.5f, 0.55f),
            },
            _ => new Look
            {
                Sky = new Color(0.22f, 0.2f, 0.36f), Equator = new Color(0.17f, 0.15f, 0.26f), Ground = new Color(0.09f, 0.07f, 0.11f),
                Fog = new Color(0.05f, 0.04f, 0.09f), Sun = new Color(1f, 0.66f, 0.46f), SunIntensity = 0.75f,
                Torch = new Color(1f, 0.55f, 0.2f), Crack = new Color(0.8f, 0.3f, 0.12f), CrackGlow = 0.245f, // rachadura 30% mais fraca: era o que mais puxava o olho na luta
                Crystal = new Color(0.85f, 0.5f, 0.16f), CrystalGlow = 0.55f, Stone = new Color(0.5f, 0.52f, 0.68f),
            },
        };

        readonly List<Light> torches = new(), cracksLights = new(), crystalLights = new();
        readonly List<Renderer> cracks = new(), crystals = new(), stones = new();
        readonly List<ParticleSystem> flames = new(), sparks = new();
        Light sun;
        MaterialPropertyBlock block;
        public Biome Current { get; private set; }
        public Color CrackColor { get; private set; }
        public int StoneCount { get; private set; }

        void Awake()
        {
            // so o que ja esta na cena ao abrir: o que nasce depois (chefe, perigos) tem cor propria
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.name == "TorchLight") torches.Add(l);
                else if (l.name == "EmberLight") cracksLights.Add(l);
                else if (l.name == "AmberLight") crystalLights.Add(l);
                else if (l.type == LightType.Directional) sun = l;
            }
            foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r.name == "EmberCrack") cracks.Add(r);
                else if (r.name == "AmberCrystal") crystals.Add(r);
                // piso e paredes usam o material "Dungeon": a pedra da sala muda de tom com o trecho
                else if (r.sharedMaterial != null && r.sharedMaterial.name == "Dungeon") stones.Add(r);
            }
            foreach (var p in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (p.name == "TorchFlame") flames.Add(p);
                else if (p.name == "EmberSparks") sparks.Add(p);
            }
            block = new MaterialPropertyBlock();
            BuildMotes();
        }

        ParticleSystem motes;
        /// Particulas de ar da sala (para os testes).
        public ParticleSystem Motes => motes;

        /// Particulas miudas soltas no ar da sala inteira: cinza caindo nos Saloes, nevoa subindo na Cripta, polen no Jardim
        /// e brasas subindo no Coracao. Poucas, pequenas e foscas: so para o ar nao parecer parado.
        void BuildMotes()
        {
            Material mat = null;
            foreach (var p in sparks) if (p != null) { mat = p.GetComponent<ParticleSystemRenderer>().sharedMaterial; break; }
            if (mat == null) return; // cena sem as fagulhas do chao (cenas de teste): sem particulas de ar
            var go = new GameObject("AirMotes");
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(new Vector3(0f, 2.2f, 0f), Quaternion.identity);
            motes = go.AddComponent<ParticleSystem>();
            motes.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = motes.main;
            main.loop = true; main.prewarm = true; main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.22f); // a camera fica a 24 m: menor que isso some
            main.maxParticles = 260;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = motes.emission; em.rateOverTime = 32f;
            var sh = motes.shape; sh.shapeType = ParticleSystemShapeType.Box;
            var noise = motes.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.25f; noise.scrollSpeed = 0.1f;
            var col = motes.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(1, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void DressMotes(Biome biome)
        {
            if (motes == null) return;
            var sh = motes.shape;
            sh.scale = new Vector3(Quintessence.Core.Arena.HalfX * 2f, 3.6f, Quintessence.Core.Arena.HalfZ * 2f);
            var main = motes.main;
            var vel = motes.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            (Color color, float alpha, float rise) = biome switch
            {
                Biome.Crypt => (new Color(0.62f, 0.78f, 0.86f), 0.35f, 0.08f), // nevoa fina subindo da agua parada
                Biome.Garden => (new Color(0.86f, 0.88f, 0.6f), 0.45f, 0.03f),  // polen quase parado
                Biome.Heart => (new Color(1f, 0.5f, 0.2f), 0.6f, 0.25f),       // brasas subindo
                _ => (new Color(0.72f, 0.66f, 0.62f), 0.4f, -0.12f),           // cinza caindo devagar
            };
            color.a = alpha;
            main.startColor = color;
            vel.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            vel.y = new ParticleSystem.MinMaxCurve(rise * 0.6f, rise * 1.4f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            if (!motes.isPlaying) motes.Play();
        }

        public void Apply(Biome biome)
        {
            Current = biome;
            var k = Of(biome);
            CrackColor = k.Crack;
            RenderSettings.ambientSkyColor = k.Sky;
            RenderSettings.ambientEquatorColor = k.Equator;
            RenderSettings.ambientGroundColor = k.Ground;
            RenderSettings.fogColor = k.Fog;
            if (sun != null) { sun.color = k.Sun; sun.intensity = k.SunIntensity; }
            foreach (var l in torches) if (l != null) l.color = k.Torch;
            foreach (var l in cracksLights) if (l != null) l.color = k.Crack;
            foreach (var l in crystalLights) if (l != null) l.color = k.Crystal;
            Tint(cracks, k.Crack * 0.6f, k.Crack * k.CrackGlow);
            Tint(crystals, k.Crystal * 0.6f, k.Crystal * k.CrystalGlow);
            foreach (var r in stones)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", k.Stone);
                r.SetPropertyBlock(block);
            }
            StoneCount = stones.Count;
            foreach (var p in flames) if (p != null) { var m = p.main; m.startColor = Color.Lerp(k.Torch, new Color(1f, 0.45f, 0.08f), biome == Biome.Crypt ? 0.25f : 0.8f); }
            foreach (var p in sparks) if (p != null) { var m = p.main; m.startColor = k.Crack; }
            DressMotes(biome);
        }

        void Tint(List<Renderer> list, Color baseColor, Color emission)
        {
            baseColor.a = 1f; emission.a = 1f;
            foreach (var r in list)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor("_BaseColor", baseColor);
                block.SetColor("_EmissionColor", emission);
                r.SetPropertyBlock(block);
            }
        }
    }
}
