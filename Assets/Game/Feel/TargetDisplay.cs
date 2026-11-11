using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// O que se le no proprio inimigo: anel da marca no chao, barra de vida e efeito dos status.
    public class TargetDisplay : MonoBehaviour
    {
        Health health;
        CombatFeedback feedback;
        Transform ring, bar, fill;
        Material ringMat, fillMat, backMat;
        ElementType mark;
        bool burning, soaked;
        float shown = 1f, emit, barTimer;

        public static TargetDisplay Create(Health health, CombatFeedback feedback)
        {
            var go = new GameObject("TargetDisplay_" + health.name);
            var d = go.AddComponent<TargetDisplay>();
            d.health = health;
            d.feedback = feedback;
            d.Build();
            return d;
        }

        void Build()
        {
            ring = new GameObject("MarkRing").transform;
            ring.SetParent(transform, false);
            ring.gameObject.AddComponent<MeshFilter>().sharedMesh = CombatFeedback.RingMesh;
            ringMat = new Material(feedback.FlatMaterial);
            ring.gameObject.AddComponent<MeshRenderer>().sharedMaterial = ringMat;
            ring.gameObject.SetActive(false);

            bar = new GameObject("HealthBar").transform;
            bar.SetParent(transform, false);
            backMat = new Material(feedback.FlatMaterial) { color = new Color(0.04f, 0.03f, 0.08f, 0.85f) };
            fillMat = new Material(feedback.FlatMaterial) { color = new Color(0.95f, 0.2f, 0.2f, 1f) };
            Quad("Back", new Vector3(1.5f, 0.2f, 1f), 0f, backMat);
            fill = Quad("Fill", new Vector3(1.42f, 0.13f, 1f), -0.01f, fillMat);
            bar.gameObject.SetActive(false);
        }

        Transform Quad(string name, Vector3 scale, float z, Material mat)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(q.GetComponent<Collider>());
            q.name = name;
            q.transform.SetParent(bar, false);
            q.transform.localScale = scale;
            q.transform.localPosition = new Vector3(0, 0, z);
            q.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return q.transform;
        }

        public void SetMark(ElementType e)
        {
            mark = e;
            if (e != ElementType.None) { var c = ElementColors.Of(e); ringMat.color = new Color(c.r, c.g, c.b, 0.95f); }
        }

        public void SetStatus(StatusType type, bool on)
        {
            if (type == StatusType.Burn) burning = on;
            if (type == StatusType.Soaked) soaked = on;
        }

        public void Touch() => barTimer = 4f;

        void LateUpdate()
        {
            if (health == null) { Destroy(gameObject); return; }
            bool alive = !health.IsDead && health.gameObject.activeInHierarchy;
            var pos = health.transform.position;

            ring.gameObject.SetActive(alive && mark != ElementType.None);
            if (ring.gameObject.activeSelf)
            {
                float pulse = 1f + 0.08f * Mathf.Sin(Time.time * 9f);
                ring.SetPositionAndRotation(pos + Vector3.up * 0.06f, Quaternion.Euler(0, Time.time * 90f, 0));
                ring.localScale = Vector3.one * (1.25f * pulse);
            }

            barTimer -= Time.deltaTime;
            float hp = health.Max > 0f ? health.Current / health.Max : 0f;
            bar.gameObject.SetActive(alive && barTimer > 0f && hp < 1f);
            if (bar.gameObject.activeSelf)
            {
                shown = Mathf.MoveTowards(shown, hp, Time.deltaTime * 1.5f);
                var cam = Camera.main;
                bar.position = pos + Vector3.up * 3.1f;
                if (cam != null) bar.rotation = cam.transform.rotation;
                fill.localScale = new Vector3(1.42f * shown, 0.13f, 1f);
                fill.localPosition = new Vector3(-0.71f * (1f - shown), 0f, -0.01f);
            }
            else shown = hp;

            if (!alive || (!burning && !soaked)) return;
            emit -= Time.deltaTime;
            if (emit > 0f) return;
            emit = 0.07f;
            if (burning)
                feedback.Burst(pos + Vector3.up * Random.Range(0.6f, 2f), Vector3.up, 35f, 2, 1f, 2.4f, 0.25f, 0.5f, 0.45f, ElementColors.Of(ElementType.Fire));
            if (soaked)
                feedback.Burst(pos + Vector3.up * Random.Range(1.4f, 2.2f), Vector3.down, 25f, 1, 1.2f, 2.2f, 0.12f, 0.2f, 0.5f, ElementColors.Of(ElementType.Water));
        }
    }
}
