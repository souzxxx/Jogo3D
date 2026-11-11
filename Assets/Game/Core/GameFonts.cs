using UnityEngine;

namespace Quintessence.Core
{
    /// Fontes do jogo (Assets/Resources/Fonts). Display: numeros e rotulos. Body: texto corrido. Title: logotipo.
    public static class GameFonts
    {
        static Font display, body, title;
        public static Font Display => display != null ? display : display = Load("LilitaOne-Regular");
        public static Font Body => body != null ? body : body = Load("TitilliumWeb-SemiBold");
        public static Font Title => title != null ? title : title = Load("CinzelDecorative-Bold");

        static Font Load(string name)
        {
            var f = Resources.Load<Font>("Fonts/" + name);
            return f != null ? f : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
