using System;
using UnityEngine;

namespace Quintessence.Core
{
    /// Pedido de som sem depender de quem toca. Qualquer frente chama Sfx.Play("id"); o AudioDirector escuta.
    public static class Sfx
    {
        public static event Action<string> Requested;
        public static void Play(string id) => Requested?.Invoke(id);
    }

    /// Preferencias de efeito visual (acessibilidade), guardadas entre sessoes.
    public static class FxPrefs
    {
        const string ShakeKey = "fx.shake";
        static int shake = -1;

        /// Tremor de camera em golpes e explosoes. Desligado, a camera fica parada.
        public static bool ScreenShake
        {
            get { if (shake < 0) shake = PlayerPrefs.GetInt(ShakeKey, 1); return shake == 1; }
            set { shake = value ? 1 : 0; PlayerPrefs.SetInt(ShakeKey, shake); }
        }
    }

    /// Volumes escolhidos pelo jogador (0..1), guardados entre sessoes.
    public static class AudioPrefs
    {
        const string MusicKey = "audio.music", SfxKey = "audio.sfx";
        public static event Action Changed;
        static float music = -1f, sfx = -1f;

        public static float Music
        {
            get { if (music < 0f) music = PlayerPrefs.GetFloat(MusicKey, 0.6f); return music; }
            set { music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, music); Changed?.Invoke(); }
        }

        public static float Effects
        {
            get { if (sfx < 0f) sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f); return sfx; }
            set { sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfx); Changed?.Invoke(); }
        }

        /// Passo de volume em decimos, de 0 a 10 (o que a tela de pausa mostra).
        public static int Step(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 10f);
    }
}
