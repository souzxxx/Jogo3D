using System;
using System.Collections.Generic;
using Quintessence.Core;
using UnityEngine;
using UnityEngine.AI;

namespace Quintessence.Enemies
{
    [Serializable]
    public class WaveEntry
    {
        public GameObject prefab;
        public int count = 1;
    }

    [Serializable]
    public class Wave
    {
        public List<WaveEntry> entries = new();
        public int Size { get { int n = 0; foreach (var e in entries) n += Mathf.Max(0, e.count); return n; } }
    }

    /// Solta as ondas da sala. Avisa RoomStarted, WaveStarted e, quando a ultima onda morre, RoomCleared.
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] List<Wave> waves = new();
        [SerializeField] bool autoStart = true;
        [SerializeField] float startDelay = 0.8f;
        [SerializeField] float delayBetweenWaves = 1.2f;
        [SerializeField] float spawnRadius = 7f;
        [Tooltip("Ninguem nasce mais perto do heroi que isso.")]
        [SerializeField] float minDistanceFromPlayer = 5f;

        public WaveState State { get; private set; }
        public List<Wave> Waves => waves;
        /// Vale para quem nascer daqui em diante. A run define antes de cada sala.
        public EnemyScaling Scaling { get; set; } = EnemyScaling.None;
        bool announcedClear;
        [Tooltip("Segundos de luta sem ninguem levar dano ate os inimigos serem trazidos para perto do heroi.")]
        [SerializeField] float stallSeconds = 25f;
        StallTimer stall;
        public int StallRescues => stall != null ? stall.Fired : 0;
        public void SetStallSeconds(float seconds) { stallSeconds = seconds; stall = new StallTimer(seconds); }

        void OnEnable() => GameEvents.DamageDealt += OnAnyDamage;
        void OnDisable() => GameEvents.DamageDealt -= OnAnyDamage;
        void OnAnyDamage(Health target, DamageInfo info, float applied) => stall?.NoteProgress();

        Transform player;

        void Start()
        {
            var sizes = new int[waves.Count];
            for (int i = 0; i < sizes.Length; i++) sizes[i] = waves[i].Size;
            State = new WaveState(sizes, delayBetweenWaves);
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            if (autoStart) Invoke(nameof(Begin), startDelay);
        }

        /// Troca as ondas e comeca a sala (usado pela run a cada sala de combate).
        public void Run(List<Wave> newWaves)
        {
            CancelInvoke();
            foreach (Transform child in transform) Destroy(child.gameObject);
            waves = newWaves;
            var sizes = new int[waves.Count];
            for (int i = 0; i < sizes.Length; i++) sizes[i] = waves[i].Size;
            State = new WaveState(sizes, delayBetweenWaves);
            announcedClear = false;
            Invoke(nameof(Begin), startDelay);
        }

        public void SetAutoStart(bool value) => autoStart = value;

        public void Begin()
        {
            State.Start();
            GameEvents.RaiseRoomStarted();
        }

        void Update()
        {
            if (State == null || waves.Count == 0) return;
            int wave = State.Tick(Time.deltaTime);
            if (wave >= 0) SpawnWave(wave);
            // luta parada: alguem ficou fora de alcance. Traz os vivos para perto do heroi em vez de prender o jogador na sala
            stall ??= new StallTimer(stallSeconds);
            if (stall.Tick(Time.deltaTime, State.Alive > 0 && !GameClock.Paused) && player != null)
            {
                int k = 0;
                foreach (var brain in GetComponentsInChildren<EnemyBrain>())
                    brain.Rescue(player.position + Quaternion.Euler(0, 70f * k++, 0) * Vector3.forward * 5f);
            }
            if (State.Cleared && !announcedClear)
            {
                announcedClear = true;
                GameEvents.RaiseRoomCleared();
            }
        }

        void SpawnWave(int index)
        {
            GameEvents.RaiseWaveStarted(index, waves.Count);
            int total = waves[index].Size, k = 0;
            float offset = UnityEngine.Random.Range(0f, 360f);
            foreach (var entry in waves[index].entries)
                for (int i = 0; i < entry.count; i++)
                {
                    var pos = PickPoint(offset + 360f * k++ / Mathf.Max(1, total));
                    var go = Instantiate(entry.prefab, pos, Quaternion.identity, transform);
                    if (player != null)
                    {
                        var look = player.position - pos; look.y = 0f;
                        if (look.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(look);
                    }
                    go.GetComponent<Health>().Died += _ => State.NotifyDeath();
                }
        }

        Vector3 PickPoint(float angle)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var dir = Quaternion.Euler(0, angle + attempt * 31f, 0) * Vector3.forward;
                var p = transform.position + dir * (spawnRadius * UnityEngine.Random.Range(0.7f, 1f));
                if (!NavMesh.SamplePosition(p, out var hit, 2.5f, NavMesh.AllAreas)) continue;
                if (player != null && Vector3.Distance(hit.position, player.position) < minDistanceFromPlayer) continue;
                return hit.position;
            }
            return NavMesh.SamplePosition(transform.position, out var center, 5f, NavMesh.AllAreas) ? center.position : transform.position;
        }
    }
}
