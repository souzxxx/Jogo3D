using Quintessence.UI;
using UnityEngine;

namespace Quintessence.Game
{
    /// Cena de titulo: liga o menu ao inicio da run.
    public class TitleController : MonoBehaviour
    {
        void Start()
        {
            var menu = FindFirstObjectByType<TitleMenu>();
            if (menu == null) menu = new GameObject("TitleMenu").AddComponent<TitleMenu>();
            menu.OnPick = GameFlow.StartRun;
            menu.BindMeta(MetaStore.Current, MetaStore.Save, MetaStore.Reset, () => MetaStore.Current);
        }
    }
}
