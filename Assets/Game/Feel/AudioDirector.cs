using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quintessence.Feel
{
    /// Som do jogo inteiro. Cria a si mesmo ao iniciar e sobrevive a troca de cena.
    /// Escuta o GameEvents e o Sfx, toca os efeitos de Resources/Audio e cruza a musica de ambiente com a do chefe.
    public class AudioDirector : MonoBehaviour
    {
        /// Todos os ids de efeito usados no jogo (um .wav por id em Resources/Audio).
        public static readonly string[] Ids =
        {
            "cast_fire", "cast_water", "cast_earth", "cast_air", "skill_fire", "skill_water", "skill_earth", "skill_air",
            "hit", "hit_heavy", "hurt", "enemy_die", "dash", "switch", "reaction", "telegraph", "explode", "block", "shield",
            "shatter", "quint_on", "quint_off", "room_clear", "wave", "door", "pickup", "heal", "ui_click",
            "boss_spawn", "boss_phase", "victory", "defeat",
        };
        public const string AmbientMusic = "music_ambient", BossMusic = "music_boss";
        /// Ambiente de cada trecho, pelo indice do GameEvents.AmbientChanged.
        public static readonly string[] AmbientTracks = { AmbientMusic, "music_crypt", "music_garden" };
        readonly AudioClip[] ambient = new AudioClip[AmbientTracks.Length];
        int ambientNow, ambientWanted;
        /// Faixa de ambiente tocando agora (para os testes).
        public static string CurrentAmbient => instance != null && instance.musicA != null && instance.musicA.clip != null ? instance.musicA.clip.name : "";

        static AudioDirector instance;
        readonly Dictionary<string, AudioClip> clips = new();
        readonly Dictionary<string, float> lastPlayed = new();
        readonly List<AudioSource> pool = new();
        AudioSource musicA, musicB;
        bool bossMusic;
        int next;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (instance != null) return;
            var go = new GameObject("AudioDirector");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AudioDirector>();
        }

        void Awake()
        {
            foreach (var id in Ids) clips[id] = Resources.Load<AudioClip>("Audio/" + id);
            for (int i = 0; i < 12; i++)
            {
                var s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                pool.Add(s);
            }
            for (int i = 0; i < AmbientTracks.Length; i++) ambient[i] = Resources.Load<AudioClip>("Audio/" + AmbientTracks[i]);
            musicA = Music(AmbientMusic);
            musicB = Music(BossMusic);
            musicA.Play();
            musicB.Play();
        }

        AudioSource Music(string id)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.clip = Resources.Load<AudioClip>("Audio/" + id);
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnScene;
            Subscribe();
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnScene;
            Unsubscribe();
        }

        // a cada cena: volta para a musica de ambiente e refaz as assinaturas (alguem pode ter limpado o GameEvents)
        void OnScene(Scene scene, LoadSceneMode mode)
        {
            bossMusic = false;
            ambientWanted = 0;
            Unsubscribe();
            Subscribe();
        }

        void Subscribe()
        {
            Sfx.Requested += Play;
            GameEvents.AttackPerformed += OnAttack;
            GameEvents.DamageDealt += OnDamage;
            GameEvents.Died += OnDied;
            GameEvents.ReactionTriggered += OnReaction;
            GameEvents.ElementSwitched += OnSwitch;
            GameEvents.TelegraphStarted += OnTelegraph;
            GameEvents.Exploded += OnExploded;
            GameEvents.HitBlocked += OnBlocked;
            GameEvents.Shattered += OnShattered;
            GameEvents.QuintessenceActiveChanged += OnQuintessence;
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.RoomCleared += OnRoomClearedMusic;
            GameEvents.AmbientChanged += OnAmbient;
            GameEvents.WaveStarted += OnWave;
            GameEvents.BossSpawned += OnBoss;
            GameEvents.BossPhaseChanged += OnBossPhase;
            GameEvents.BossShieldChanged += OnBossShield;
            GameEvents.ElementAwakened += OnAwakened;
        }

        void Unsubscribe()
        {
            Sfx.Requested -= Play;
            GameEvents.AttackPerformed -= OnAttack;
            GameEvents.DamageDealt -= OnDamage;
            GameEvents.Died -= OnDied;
            GameEvents.ReactionTriggered -= OnReaction;
            GameEvents.ElementSwitched -= OnSwitch;
            GameEvents.TelegraphStarted -= OnTelegraph;
            GameEvents.Exploded -= OnExploded;
            GameEvents.HitBlocked -= OnBlocked;
            GameEvents.Shattered -= OnShattered;
            GameEvents.QuintessenceActiveChanged -= OnQuintessence;
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.RoomCleared -= OnRoomClearedMusic;
            GameEvents.AmbientChanged -= OnAmbient;
            GameEvents.WaveStarted -= OnWave;
            GameEvents.BossSpawned -= OnBoss;
            GameEvents.BossPhaseChanged -= OnBossPhase;
            GameEvents.BossShieldChanged -= OnBossShield;
            GameEvents.ElementAwakened -= OnAwakened;
        }

        static string Suffix(ElementType e) => e switch
        {
            ElementType.Water => "water", ElementType.Earth => "earth", ElementType.Air => "air", _ => "fire",
        };

        void OnAttack(AttackEvent e)
        {
            if (e.Kind == AttackKind.Dash) Play("dash", 0.9f, 0.08f);
            else if (e.Kind == AttackKind.Skill) Play("skill_" + Suffix(e.Element), 1f, 0.05f);
            else Play("cast_" + Suffix(e.Element), e.Kind == AttackKind.Finisher ? 1f : 0.75f, 0.1f, e.Kind == AttackKind.Finisher ? 0.85f : 1f);
        }

        void OnDamage(Health target, DamageInfo info, float applied)
        {
            if (target.Team == Team.Player) Play("hurt", 1f, 0.06f);
            else if (applied > 3f) Play(applied >= 10f ? "hit_heavy" : "hit", 0.8f, 0.12f);
        }

        void OnDied(Health target, DamageInfo info)
        {
            if (target.Team == Team.Enemy) Play("enemy_die", 0.8f, 0.1f);
        }

        void OnReaction(ReactionEvent e) => Play("reaction", 1f, 0.06f);
        void OnSwitch(ElementType from, ElementType to) { } // a skill ja tem o proprio som
        void OnTelegraph(TelegraphEvent e) => Play("telegraph", 0.6f, 0.05f);
        void OnExploded(Vector3 p, float r) => Play("explode", 0.9f, 0.08f);
        void OnBlocked(Health who, DamageInfo info) => Play("block", 0.7f, 0.06f);
        void OnShattered(Health who) => Play("shatter", 0.9f, 0.05f);
        void OnQuintessence(bool on) => Play(on ? "quint_on" : "quint_off", 1f, 0f);
        // sala limpa (inclusive a do mini-chefe): a musica de luta volta para a de ambiente
        void OnRoomClearedMusic() => bossMusic = false;
        void OnAmbient(int index) => ambientWanted = Mathf.Clamp(index, 0, AmbientTracks.Length - 1);

        void OnRoomCleared() => Play("room_clear", 0.9f, 0f);
        void OnWave(int index, int total) => Play("wave", 0.8f, 0f);
        void OnBoss(Health boss, string name) { bossMusic = true; Play("boss_spawn", 1f, 0f); }
        void OnBossPhase(int phase, int total) { if (phase > 1) Play("boss_phase", 1f, 0f); }
        void OnBossShield(bool up, bool needs) => Play(up ? "shield" : "shatter", 1f, 0f);
        void OnAwakened(ElementType e) { }

        void Play(string id) => Play(id, 1f, 0f);

        /// volume: 0..1 relativo. jitter: variacao aleatoria de tom, para o mesmo som nao soar identico em sequencia.
        void Play(string id, float volume, float jitter, float pitch = 1f)
        {
            if (!clips.TryGetValue(id, out var clip) || clip == null) return;
            // o mesmo som nao dispara duas vezes no mesmo instante (varios alvos no mesmo golpe)
            float now = Time.unscaledTime;
            if (lastPlayed.TryGetValue(id, out float last) && now - last < 0.05f) return;
            lastPlayed[id] = now;
            if (id == "victory" || id == "defeat") bossMusic = false;
            var s = pool[next];
            next = (next + 1) % pool.Count;
            s.pitch = pitch + Random.Range(-jitter, jitter);
            s.volume = volume * AudioPrefs.Effects;
            s.clip = clip;
            s.Play();
        }

        void Update()
        {
            float target = AudioPrefs.Music * 0.7f, dt = Time.unscaledDeltaTime;
            // troca de trecho: a faixa atual some, troca e volta
            bool swapping = ambientWanted != ambientNow && ambient[ambientWanted] != null;
            musicA.volume = Mathf.MoveTowards(musicA.volume, bossMusic || swapping ? 0f : target, dt * (swapping ? 0.8f : 0.5f));
            if (swapping && musicA.volume <= 0.01f)
            {
                ambientNow = ambientWanted;
                musicA.clip = ambient[ambientNow];
                musicA.Play();
            }
            musicB.volume = Mathf.MoveTowards(musicB.volume, bossMusic ? target : 0f, dt * 0.5f);
        }
    }
}
