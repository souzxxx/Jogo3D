using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Feel
{
    /// Som de ambiente preso a uma peca (o estalo da fogueira do Descanso): toca em loop enquanto a peca existe,
    /// no volume de efeitos escolhido nas opcoes, e cala com o jogo pausado.
    [RequireComponent(typeof(AudioSource))]
    public class AmbientLoop : MonoBehaviour
    {
        [SerializeField] float volume = 0.35f;
        AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f; // a camera fica a 24 m: em 3D o som sumiria
            Apply();
        }

        void OnEnable() { AudioPrefs.Changed += Apply; if (source != null && source.clip != null) source.Play(); }
        void OnDisable() { AudioPrefs.Changed -= Apply; }

        void Apply() { if (source != null) source.volume = volume * AudioPrefs.Effects; }

        void Update()
        {
            if (GameClock.Paused == source.isPlaying) { if (GameClock.Paused) source.Pause(); else source.UnPause(); }
        }
    }
}
