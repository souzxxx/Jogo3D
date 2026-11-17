using UnityEngine;

namespace Quintessence.Core
{
    /// Coloca o modelo do heroi e dos inimigos nas camadas que o renderer desenha como silhueta quando ficam atras
    /// de uma peca do cenario (Assets/Game/Feel/Shaders/Silhouette.shader, RenderObjects do PC_Renderer).
    /// So o modelo animado entra (o que esta abaixo do Animator): aneis, auras e colisores ficam na camada padrao.
    public static class Silhouette
    {
        public const string HeroLayer = "Hero";
        public const string EnemyLayer = "Enemy";

        public static void MarkHero(GameObject root) => Set(root, LayerMask.NameToLayer(HeroLayer));
        public static void MarkEnemy(GameObject root) => Set(root, LayerMask.NameToLayer(EnemyLayer));

        /// Corpo caido nao precisa de silhueta atras da parede.
        public static void Clear(GameObject root) => Set(root, 0);

        static void Set(GameObject root, int layer)
        {
            if (root == null || layer < 0) return;
            var animator = root.GetComponentInChildren<Animator>(true);
            var scope = animator != null ? animator.transform : root.transform;
            foreach (var r in scope.GetComponentsInChildren<Renderer>(true))
                if ((r is SkinnedMeshRenderer || r is MeshRenderer) && r.GetComponent<Collider>() == null)
                    r.gameObject.layer = layer;
        }
    }
}
