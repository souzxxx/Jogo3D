using UnityEngine;

namespace Quintessence.Core
{
    /// Graficos completos (perfil PC: sombras suaves, oclusao de ambiente, resolucao cheia) ou leves para PC fraco
    /// (perfil Mobile: sem oclusao de ambiente, sombra simples, 80% da resolucao). Guardado entre sessoes.
    public static class GfxPrefs
    {
        const string Key = "gfx.light";
        static int light = -1;

        public static bool Light
        {
            get { if (light < 0) light = PlayerPrefs.GetInt(Key, 0); return light == 1; }
            set { light = value ? 1 : 0; PlayerPrefs.SetInt(Key, light); PlayerPrefs.Save(); Apply(); }
        }

        /// Troca o perfil de qualidade (e com ele o pipeline) pelo nome: "Mobile" nos leves, "PC" nos completos.
        public static void Apply()
        {
            string want = Light ? "Mobile" : "PC";
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
                if (names[i] == want && QualitySettings.GetQualityLevel() != i) { QualitySettings.SetQualityLevel(i, true); return; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            light = -1;
            // so mexe quando o jogador escolheu os leves: no editor, a qualidade escolhida por quem desenvolve continua valendo
            if (Light) Apply();
        }
    }
}
