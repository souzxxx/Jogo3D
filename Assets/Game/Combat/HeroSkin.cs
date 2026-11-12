using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat
{
    /// Os quatro herois dividem o mesmo corpo de jogo: este componente mostra o modelo do heroi escolhido
    /// (um por elemento inicial) e pinta a parte magica dele com a cor da ultima skill usada.
    public class HeroSkin : MonoBehaviour
    {
        public static readonly string[] Names = { "", "Rax", "Liora", "Brann", "Vey" };
        public static string NameOf(ElementType e) => Names[(int)e];

        [Tooltip("Um modelo por elemento, na ordem Fogo, Agua, Terra, Ar.")]
        [SerializeField] GameObject[] models = new GameObject[0];
        [SerializeField] PlayerAnimator heroAnimator;
        [SerializeField] PlayerController controller;

        Renderer[] magic = new Renderer[0];
        int[] magicIndex = new int[0];
        MaterialPropertyBlock block;
        public ElementType Shown { get; private set; }
        public GameObject Model => Shown == ElementType.None ? null : models[(int)Shown - 1];
        public int MagicParts => magic.Length;

        public void Bind(GameObject[] list, PlayerAnimator anim, PlayerController pc) { models = list; heroAnimator = anim; controller = pc; }

        void OnEnable() => GameEvents.ElementSwitched += OnSwitch;
        void OnDisable() => GameEvents.ElementSwitched -= OnSwitch;
        void OnSwitch(ElementType from, ElementType to) => Tint(to);

        /// Troca o modelo para o heroi daquele elemento.
        public void Show(ElementType hero)
        {
            int index = (int)hero - 1;
            if (index < 0 || index >= models.Length || models[index] == null) return;
            for (int i = 0; i < models.Length; i++) if (models[i] != null) models[i].SetActive(i == index);
            Shown = hero;
            var anim = models[index].GetComponent<Animator>();
            if (heroAnimator != null && anim != null) heroAnimator.Bind(controller, anim);

            var rs = new System.Collections.Generic.List<Renderer>();
            var idx = new System.Collections.Generic.List<int>();
            foreach (var r in models[index].GetComponentsInChildren<Renderer>(true))
            {
                var mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                    if (mats[m] != null && mats[m].name.StartsWith("Magic")) { rs.Add(r); idx.Add(m); }
            }
            magic = rs.ToArray(); magicIndex = idx.ToArray();
            Tint(controller != null ? controller.Element : hero);
        }

        void Tint(ElementType e)
        {
            if (e == ElementType.None || magic.Length == 0) return;
            block ??= new MaterialPropertyBlock();
            var c = ElementColors.Of(e);
            for (int i = 0; i < magic.Length; i++)
            {
                if (magic[i] == null) continue;
                magic[i].GetPropertyBlock(block, magicIndex[i]);
                block.SetColor("_BaseColor", c);
                block.SetColor("_EmissionColor", c * 0.35f);
                magic[i].SetPropertyBlock(block, magicIndex[i]);
            }
        }
    }
}
