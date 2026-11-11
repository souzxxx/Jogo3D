using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// Aviso de ataque no chao: contorno fixo da area e um preenchimento que cresce ate o golpe sair.
    public class TelegraphVisual : MonoBehaviour
    {
        Transform fill;
        Material outlineMat, fillMat;
        TelegraphEvent data;
        float t;

        public static TelegraphVisual Create(TelegraphEvent e, Material flat, Mesh ring, Mesh disc)
        {
            var go = new GameObject("Telegraph");
            var v = go.AddComponent<TelegraphVisual>();
            v.data = e;
            // vermelho-tijolo fosco: le como perigo sem brilhar mais que o cenario
            v.outlineMat = new Material(flat) { color = new Color(0.66f, 0.17f, 0.13f, e.IsLine ? 0.2f : 0.85f) };
            v.fillMat = new Material(flat) { color = new Color(0.66f, 0.15f, 0.11f, 0.3f) };
            var dir = e.Direction.sqrMagnitude > 0.01f ? e.Direction.normalized : Vector3.forward;
            if (e.IsLine)
            {
                var rot = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
                go.transform.SetPositionAndRotation(e.Point + Vector3.up * 0.08f, rot);
                Part(go.transform, "Outline", null, v.outlineMat, true).localScale = new Vector3(e.Radius * 2f, e.Length, 1f);
                v.fill = Part(go.transform, "Fill", null, v.fillMat, true);
            }
            else
            {
                go.transform.position = e.Point + Vector3.up * 0.08f;
                Part(go.transform, "Outline", ring, v.outlineMat, false).localScale = Vector3.one * e.Radius;
                v.fill = Part(go.transform, "Fill", disc, v.fillMat, false);
            }
            v.Apply(0f);
            return v;
        }

        static Transform Part(Transform parent, string name, Mesh mesh, Material mat, bool quad)
        {
            GameObject go;
            if (quad)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(go.GetComponent<Collider>());
                go.name = name;
            }
            else
            {
                go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>();
            }
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = data.Duration > 0f ? Mathf.Clamp01(t / data.Duration) : 1f;
            Apply(k);
            if (t >= data.Duration + 0.1f) Destroy(gameObject);
        }

        void Apply(float k)
        {
            if (data.IsLine)
            {
                // a linha enche do inimigo para frente; o quad tem o centro no meio, entao desloca junto
                fill.localScale = new Vector3(data.Radius * 2f, data.Length * k, 1f);
                fill.localPosition = new Vector3(0f, data.Length * k * 0.5f, -0.01f);
                fill.parent.Find("Outline").localPosition = new Vector3(0f, data.Length * 0.5f, 0f);
            }
            else
            {
                fill.localScale = Vector3.one * (data.Radius * k);
                fill.localPosition = new Vector3(0f, 0.01f, 0f);
            }
            // pisca claro no instante do golpe
            float flash = Mathf.InverseLerp(0.9f, 1f, k);
            fillMat.color = Color.Lerp(new Color(0.66f, 0.15f, 0.11f, 0.3f), new Color(0.85f, 0.4f, 0.28f, 0.5f), flash);
        }

        void OnDestroy()
        {
            if (outlineMat != null) Destroy(outlineMat);
            if (fillMat != null) Destroy(fillMat);
        }
    }
}
