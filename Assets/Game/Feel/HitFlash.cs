using UnityEngine;

namespace Quintessence.Feel
{
    /// Pisca o modelo numa cor ao apanhar e mantem um tom enquanto ha status (ex.: azulado se Encharcado).
    public class HitFlash : MonoBehaviour
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        Renderer[] renderers;
        MaterialPropertyBlock block;
        Color flash, tint = Color.white;
        float left, duration;
        bool dirty;

        void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            block = new MaterialPropertyBlock();
        }

        public void Flash(Color color, float seconds = 0.14f)
        {
            flash = color; duration = seconds; left = seconds; dirty = true;
        }

        public void SetTint(Color color) { tint = color; dirty = true; }

        void LateUpdate()
        {
            if (!dirty) return;
            left -= Time.unscaledDeltaTime;
            float k = duration > 0f ? Mathf.Clamp01(left / duration) : 0f;
            var c = Color.Lerp(tint, flash, k);
            foreach (var r in renderers)
            {
                if (r == null || r is ParticleSystemRenderer) continue;
                r.GetPropertyBlock(block);
                block.SetColor(BaseColor, c);
                r.SetPropertyBlock(block);
            }
            if (left <= 0f) dirty = false;
        }
    }
}
