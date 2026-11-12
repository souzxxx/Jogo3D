using UnityEngine;

namespace Quintessence.Feel
{
    /// Um osso solto: voa, quica no chao, para, e depois de um tempo encolhe e some.
    public class BonePiece : MonoBehaviour
    {
        Vector3 velocity, spin;
        float rest, life, floor;
        Vector3 baseScale;
        const float Gravity = 22f, Fade = 0.45f;

        public void Launch(Vector3 v, Vector3 angular, float seconds, float halfHeight)
        {
            velocity = v; spin = angular; life = seconds; floor = Mathf.Max(0.04f, halfHeight);
            baseScale = transform.localScale;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            life -= dt;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (life < Fade) transform.localScale = baseScale * (life / Fade);
            if (rest > 0.25f) return; // ja parou: nao gasta mais nada

            velocity.y -= Gravity * dt;
            var p = transform.position + velocity * dt;
            if (p.y < floor)
            {
                p.y = floor;
                if (velocity.y < -1.5f) { velocity.y *= -0.35f; spin *= 0.6f; }
                else velocity.y = 0f;
                velocity.x *= 0.7f; velocity.z *= 0.7f;
                spin *= 0.8f;
            }
            // nao atravessa as paredes da sala
            p.x = Mathf.Clamp(p.x, -9.6f, 9.6f); p.z = Mathf.Clamp(p.z, -7.6f, 7.6f);
            transform.position = p;
            transform.Rotate(spin * dt, Space.World);
            rest = p.y <= floor + 0.01f && velocity.sqrMagnitude < 0.2f ? rest + dt : 0f;
        }
    }

    /// Desmonta um inimigo morto nas proprias pecas: cada malha do modelo vira um osso solto.
    public static class BonePile
    {
        /// Devolve quantas pecas soltou. Esconde o modelo original.
        public static int Scatter(GameObject enemy, Vector3 hitDirection, float seconds = 3.2f)
        {
            if (enemy == null) return 0;
            var origin = enemy.transform.position;
            hitDirection.y = 0f;
            int count = 0;
            var holder = new GameObject("Bones_" + enemy.name).transform;

            foreach (var smr in enemy.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (!smr.enabled || smr.sharedMesh == null) continue;
                var mesh = new Mesh();
                smr.BakeMesh(mesh, true);
                // recentra a malha no proprio meio, para o osso girar em torno de si
                var c = mesh.bounds.center;
                var verts = mesh.vertices;
                for (int i = 0; i < verts.Length; i++) verts[i] -= c;
                mesh.vertices = verts;
                mesh.RecalculateBounds();
                var world = smr.transform.position + smr.transform.rotation * c;
                Piece(holder, mesh, smr.sharedMaterials, world, smr.transform.rotation, Vector3.one, origin, hitDirection, seconds, true);
                smr.enabled = false;
                count++;
            }
            foreach (var mr in enemy.GetComponentsInChildren<MeshRenderer>())
            {
                var mf = mr.GetComponent<MeshFilter>();
                // so pecas do modelo (elmo, arma, cristais): aneis e marcadores de interface ficam de fora
                if (!mr.enabled || mf == null || mf.sharedMesh == null || mr.GetComponentInParent<Animator>() == null) continue;
                Piece(holder, mf.sharedMesh, mr.sharedMaterials, mr.bounds.center, mr.transform.rotation, mr.transform.lossyScale, origin, hitDirection, seconds, false,
                    mr.transform.position - mr.bounds.center);
                mr.enabled = false;
                count++;
            }
            Object.Destroy(holder.gameObject, seconds + 0.5f);
            return count;
        }

        static void Piece(Transform holder, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, Vector3 scale, Vector3 origin,
            Vector3 hitDir, float seconds, bool ownsMesh, Vector3 meshOffset = default)
        {
            var go = new GameObject("Bone");
            go.transform.SetParent(holder, true);
            Transform visual = go.transform;
            if (meshOffset != default)
            {
                // malha com o pivo fora do centro: um filho segura a malha e o pai gira em torno do meio dela
                go.transform.position = pos;
                visual = new GameObject("Mesh").transform;
                visual.SetParent(go.transform, false);
                visual.position = pos + meshOffset;
                visual.rotation = rot;
                visual.localScale = scale;
            }
            else
            {
                go.transform.SetPositionAndRotation(pos, rot);
                go.transform.localScale = scale;
            }
            visual.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = visual.gameObject.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (ownsMesh) go.AddComponent<OwnedMesh>().mesh = mesh;

            var away = pos - origin; away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = Random.insideUnitSphere; away.y = 0f;
            var v = away.normalized * Random.Range(1.5f, 4f) + hitDir.normalized * Random.Range(1.5f, 4.5f) + Vector3.up * Random.Range(3.5f, 7f);
            float half = Mathf.Min(mesh.bounds.extents.x, Mathf.Min(mesh.bounds.extents.y, mesh.bounds.extents.z)) * Mathf.Abs(scale.y);
            go.AddComponent<BonePiece>().Launch(v, Random.insideUnitSphere * 540f, seconds * Random.Range(0.8f, 1f), half);
        }
    }

    /// Solta a malha gerada quando o osso some (malha feita em tempo de jogo nao e recolhida sozinha).
    public class OwnedMesh : MonoBehaviour
    {
        public Mesh mesh;
        void OnDestroy() { if (mesh != null) Destroy(mesh); }
    }
}
