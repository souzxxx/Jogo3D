using System.Collections.Generic;
using UnityEngine;

namespace Quintessence.Game
{
    /// Materiais criados para uma peca so (medalhao): saem junto com ela, em vez de ficar na memoria ate a cena acabar.
    public class OwnedMaterials : MonoBehaviour
    {
        readonly List<Material> owned = new();

        public static void Add(GameObject holder, Material m)
        {
            if (holder == null || m == null) return;
            var o = holder.GetComponent<OwnedMaterials>() ?? holder.AddComponent<OwnedMaterials>();
            o.owned.Add(m);
        }

        void OnDestroy()
        {
            foreach (var m in owned) if (m != null) Destroy(m);
            owned.Clear();
        }
    }
}
