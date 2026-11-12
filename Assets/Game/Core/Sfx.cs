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
            set { shake = value ? 1 : 0; PlayerPrefs.SetInt(ShakeKey, shake); PlayerPrefs.Save(); }
        }
    }

    /// Dificuldade escolhida nas opcoes. Tranquila: o heroi recebe 35% menos dano de tudo (inimigos, chefes, perigos).
    public static class GamePrefs
    {
        const string RelaxedKey = "game.relaxed";
        public const float RelaxedDamageTaken = 0.65f;
        static int relaxed = -1;
        /// Os testes fixam a dificuldade aqui sem tocar na escolha guardada de quem joga.
        public static bool? Force;

        public static bool Relaxed
        {
            get { if (Force.HasValue) return Force.Value; if (relaxed < 0) relaxed = PlayerPrefs.GetInt(RelaxedKey, 0); return relaxed == 1; }
            set { relaxed = value ? 1 : 0; PlayerPrefs.SetInt(RelaxedKey, relaxed); PlayerPrefs.Save(); } // no navegador so grava com Save
        }

        public static float HeroDamageTaken => Relaxed ? RelaxedDamageTaken : 1f;
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
            set { music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, music); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        public static float Effects
        {
            get { if (sfx < 0f) sfx = PlayerPrefs.GetFloat(SfxKey, 0.8f); return sfx; }
            set { sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfx); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// Passo de volume em decimos, de 0 a 10 (o que a tela de pausa mostra).
        public static int Step(float value) => Mathf.RoundToInt(Mathf.Clamp01(value) * 10f);
    }
}
