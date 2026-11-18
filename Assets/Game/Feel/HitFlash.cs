using UnityEngine;

namespace Quintessence.Feel
{
    /// Pisca o modelo numa cor ao apanhar e mantem um tom enquanto ha status (ex.: azulado se Encharcado).
    public class HitFlash : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        Renderer[] renderers;
        Color[] baseColors;
        MaterialPropertyBlock block;
        Color flash, tint = Color.white;
        float left, duration;
        bool dirty;

        void Awake()
        {
            // o circulo da aura (Porta-Estandarte) e um marcador no chao: nao pisca nem toma a tinta do status
            renderers = System.Array.FindAll(GetComponentsInChildren<Renderer>(true), r => r.gameObject.name != "AuraRing");
            block = new MaterialPropertyBlock();
            // a cor propria do material (cinza do Guardiao, tom quente do Explosivo) e preservada: o piscar multiplica por ela
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                var m = renderers[i] != null ? renderers[i].sharedMaterial : null;
                baseColors[i] = m != null && m.HasProperty(BaseColor) ? m.GetColor(BaseColor) : Color.white;
            }
        }

        float strength = 1f;

        /// strength: quanto da cor do clarao cobre a do material no pico (1 = toda).
        public void Flash(Color color, float seconds = 0.14f, float strength = 1f)
        {
            flash = color; duration = seconds; left = seconds; dirty = true; this.strength = Mathf.Clamp01(strength);
        }

        public void SetTint(Color color) { tint = color; dirty = true; }

        void LateUpdate()
        {
            if (!dirty) return;
            left -= Time.unscaledDeltaTime;
            float k = (duration > 0f ? Mathf.Clamp01(left / duration) : 0f) * strength;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(block);
                // o tom de status multiplica a cor do material; o clarao do golpe cobre tudo
                block.SetColor(BaseColor, Color.Lerp(baseColors[i] * tint, flash, k));
                r.SetPropertyBlock(block);
            }
            if (left <= 0f) dirty = false;
        }
    }
}
