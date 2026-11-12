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
            public Color Sky, Equator, Ground, Fog, Sun, Torch, Crack, Crystal;
            public float SunIntensity, CrackGlow, CrystalGlow;
        }

        static Look Of(Biome b) => b switch
        {
            Biome.Crypt => new Look
            {
                Sky = new Color(0.14f, 0.24f, 0.36f), Equator = new Color(0.1f, 0.17f, 0.25f), Ground = new Color(0.05f, 0.08f, 0.11f),
                Fog = new Color(0.03f, 0.06f, 0.09f), Sun = new Color(0.62f, 0.78f, 0.95f), SunIntensity = 0.62f,
                Torch = new Color(0.72f, 0.82f, 0.95f), Crack = new Color(0.2f, 0.52f, 0.62f), CrackGlow = 0.7f,
                Crystal = new Color(0.45f, 0.68f, 0.74f), CrystalGlow = 0.4f,
            },
            Biome.Garden => new Look
            {
                Sky = new Color(0.2f, 0.27f, 0.2f), Equator = new Color(0.15f, 0.19f, 0.14f), Ground = new Color(0.07f, 0.09f, 0.06f),
                Fog = new Color(0.04f, 0.06f, 0.04f), Sun = new Color(0.95f, 0.88f, 0.62f), SunIntensity = 0.8f,
                Torch = new Color(1f, 0.78f, 0.45f), Crack = new Color(0.42f, 0.6f, 0.26f), CrackGlow = 0.6f,
                Crystal = new Color(0.5f, 0.7f, 0.5f), CrystalGlow = 0.4f,
            },
            Biome.Heart => new Look
            {
                Sky = new Color(0.3f, 0.16f, 0.2f), Equator = new Color(0.22f, 0.12f, 0.14f), Ground = new Color(0.1f, 0.05f, 0.05f),
                Fog = new Color(0.08f, 0.03f, 0.03f), Sun = new Color(1f, 0.5f, 0.36f), SunIntensity = 0.8f,
                Torch = new Color(1f, 0.45f, 0.18f), Crack = new Color(1f, 0.2f, 0.04f), CrackGlow = 1.4f,
                Crystal = new Color(0.9f, 0.4f, 0.14f), CrystalGlow = 0.7f,
            },
            _ => new Look
            {
                Sky = new Color(0.22f, 0.2f, 0.36f), Equator = new Color(0.17f, 0.15f, 0.26f), Ground = new Color(0.09f, 0.07f, 0.11f),
                Fog = new Color(0.05f, 0.04f, 0.09f), Sun = new Color(1f, 0.66f, 0.46f), SunIntensity = 0.75f,
                Torch = new Color(1f, 0.55f, 0.2f), Crack = new Color(1f, 0.16f, 0.02f), CrackGlow = 1.15f,
                Crystal = new Color(0.85f, 0.5f, 0.16f), CrystalGlow = 0.55f,
            },
        };

        readonly List<Light> torches = new(), cracksLights = new(), crystalLights = new();
        readonly List<Renderer> cracks = new(), crystals = new();
        readonly List<ParticleSystem> flames = new(), sparks = new();
        Light sun;
        MaterialPropertyBlock block;
        public Biome Current { get; private set; }
        public Color CrackColor { get; private set; }

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
            }
            foreach (var p in FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (p.name == "TorchFlame") flames.Add(p);
                else if (p.name == "EmberSparks") sparks.Add(p);
            }
            block = new MaterialPropertyBlock();
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
            foreach (var p in flames) if (p != null) { var m = p.main; m.startColor = Color.Lerp(k.Torch, new Color(1f, 0.45f, 0.08f), biome == Biome.Crypt ? 0.25f : 0.8f); }
            foreach (var p in sparks) if (p != null) { var m = p.main; m.startColor = k.Crack; }
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
