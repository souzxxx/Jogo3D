using System.Collections;
using System.Collections.Generic;
using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Enemies;
using Quintessence.Run;
using Quintessence.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quintessence.Game
{
    /// Conduz a run dentro da cena: monta cada sala do mapa, abre as portas quando ela e resolvida,
    /// troca de sala, e encerra em vitoria (chefe) ou derrota (heroi caiu).
    public class RunDirector : MonoBehaviour
    {
        [SerializeField] GameObject meleePrefab;
        [SerializeField] GameObject rangedPrefab;
        [SerializeField] Material flatMaterial;
        [SerializeField] Vector3 entryPoint = new(0, 0.05f, -5.5f);
        [SerializeField] float doorLine = 5.6f;
        [SerializeField] float interactRange = 2.4f;

        public RunState Run { get; private set; }
        public bool Finished { get; private set; }
        public bool Victory { get; private set; }
        public bool Busy { get; private set; }
        public IReadOnlyList<Interactable> Interactables => interactables;

        PlayerController player;
        Health playerHealth;
        WaveSpawner spawner;
        Hud hud;
        readonly List<Interactable> interactables = new();
        float startTime;
        bool mapByTab;

        public class Interactable
        {
            public GameObject Object;
            public string Prompt;
            public System.Action Use;
            public Vector3 Position => Object.transform.position;
        }

        public void Bind(GameObject melee, GameObject ranged, Material flat) { meleePrefab = melee; rangedPrefab = ranged; flatMaterial = flat; }

        void Awake()
        {
            GameClock.SetPaused(false);
            hud = FindFirstObjectByType<Hud>();
            if (hud == null) hud = new GameObject("Hud").AddComponent<Hud>();
        }

        void OnEnable()
        {
            GameEvents.RoomCleared += OnRoomCleared;
            GameEvents.Died += OnDied;
        }

        void OnDisable()
        {
            GameEvents.RoomCleared -= OnRoomCleared;
            GameEvents.Died -= OnDied;
        }

        void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            playerHealth = player.GetComponent<Health>();
            spawner = FindFirstObjectByType<WaveSpawner>();
            Run = new RunState(RunMapGenerator.Generate(GameFlow.Seed, GameFlow.Layers, GameFlow.Shrines));
            player.ResetLoadout(GameFlow.StartElement);
            startTime = Time.time;
            hud.SetEssence(0);
            StartCoroutine(EnterRoom(Run.Current, true));
        }

        void Update()
        {
            if (Finished) return;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.escapeKey.wasPressedThisFrame) SetPaused(!GameClock.Paused);
                // Tab segurado mostra o mapa; so fecha o que o proprio Tab abriu
                if (!GameClock.Paused && kb.tabKey.isPressed != mapByTab)
                {
                    mapByTab = kb.tabKey.isPressed;
                    hud.ShowMap(Run, mapByTab);
                }
            }
            if (GameClock.Paused || Busy) { hud.HidePrompt(); return; }

            Interactable near = null;
            float best = interactRange;
            foreach (var i in interactables)
            {
                float d = Vector3.Distance(i.Position, player.transform.position);
                if (d < best) { best = d; near = i; }
            }
            bool pressed = player.Input != null && player.Input.ConsumeInteract();
            if (near == null) { hud.HidePrompt(); return; }
            hud.ShowPrompt("F — " + near.Prompt);
            if (pressed) near.Use();
        }

        public void SetPaused(bool paused)
        {
            GameClock.SetPaused(paused);
            hud.ShowPause(paused, () => SetPaused(false), GameFlow.ToTitle);
            if (paused) hud.ShowMap(Run, false);
        }

        // ---------- salas ----------

        IEnumerator EnterRoom(MapNode node, bool first = false)
        {
            Busy = true;
            hud.HidePrompt();
            if (!first)
            {
                yield return Fade(0f, 1f, 0.3f);
                Run.Advance(node.Id);
            }
            Cleanup();
            player.Teleport(entryPoint, Vector3.forward);
            hud.SetRoom($"Sala {Run.Depth + 1}/{Run.Map.LayerCount} · {RoomInfo.Name(node.Type)}");
            yield return null;

            switch (node.Type)
            {
                case RoomType.Shrine:
                    SetupShrine();
                    break;
                case RoomType.Rest:
                    playerHealth.Heal(playerHealth.Max * 0.3f);
                    Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.5f, "DESCANSO: +30% DE VIDA", new Color(0.5f, 1f, 0.55f), 1.1f, 1.8f);
                    Run.RoomCleared();
                    OpenDoors();
                    break;
                default:
                    spawner.Run(BuildWaves(EncounterPlanner.Plan(node.Type, Run.Depth)));
                    break;
            }
            yield return Fade(1f, 0f, 0.35f);
            Busy = false;
        }

        IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                hud.SetFade(Mathf.Lerp(from, to, t / seconds));
                yield return null;
            }
            hud.SetFade(to);
        }

        List<Wave> BuildWaves(List<EncounterPlanner.WavePlan> plan)
        {
            var waves = new List<Wave>();
            foreach (var p in plan)
            {
                var w = new Wave();
                if (p.Melee > 0) w.entries.Add(new WaveEntry { prefab = meleePrefab, count = p.Melee });
                if (p.Ranged > 0) w.entries.Add(new WaveEntry { prefab = rangedPrefab, count = p.Ranged });
                waves.Add(w);
            }
            return waves;
        }

        void Cleanup()
        {
            foreach (var i in interactables) if (i.Object != null) Destroy(i.Object);
            interactables.Clear();
            foreach (var z in FindObjectsByType<DamageZone>(FindObjectsSortMode.None)) Destroy(z.gameObject);
            foreach (var p in FindObjectsByType<Projectile>(FindObjectsSortMode.None)) Destroy(p.gameObject);
            foreach (var t in FindObjectsByType<TimedLife>(FindObjectsSortMode.None)) Destroy(t.gameObject);
            foreach (var e in FindObjectsByType<EnemyBrain>(FindObjectsSortMode.None)) Destroy(e.gameObject);
        }

        void OnRoomCleared()
        {
            if (Finished) return;
            var node = Run.Current;
            Run.RoomCleared();
            int essence = EncounterPlanner.EssenceReward(node.Type, Run.Depth);
            if (node.Reward == RewardType.Health)
            {
                playerHealth.Heal(playerHealth.Max * 0.25f);
                Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.2f, "+25% DE VIDA", new Color(0.5f, 1f, 0.55f), 1f, 1.4f);
                essence /= 2;
            }
            Run.AddEssence(essence);
            hud.SetEssence(Run.Essence);
            if (node.Type == RoomType.Boss) { StartCoroutine(Finish(true)); return; }
            OpenDoors();
        }

        void OnDied(Health target, DamageInfo info)
        {
            if (target == playerHealth && !Finished) StartCoroutine(Finish(false));
        }

        IEnumerator Finish(bool victory)
        {
            Finished = true;
            Victory = victory;
            hud.HidePrompt();
            hud.ShowMap(Run, false);
            yield return new WaitForSecondsRealtime(victory ? 1.6f : 1.2f);
            int seconds = Mathf.RoundToInt(Time.time - startTime);
            string stats = $"Salas vencidas: {Run.RoomsCleared}   ·   Essência: {Run.Essence}   ·   Elementos: {player.Loadout.Awakened.Count}/4   ·   Tempo: {seconds / 60}:{seconds % 60:00}";
            hud.ShowResult(victory, stats, GameFlow.RestartRun, GameFlow.ToTitle);
        }

        // ---------- portas e santuario ----------

        void OpenDoors()
        {
            var choices = Run.Choices;
            for (int i = 0; i < choices.Count; i++)
            {
                var node = choices[i];
                float x = (i - (choices.Count - 1) / 2f) * 6f;
                var c = RoomInfo.Color(node.Type);
                string reward = RoomInfo.Name(node.Reward);
                string label = RoomInfo.Name(node.Type) + (reward.Length > 0 ? "\n" + reward : "");
                var go = BuildDoor(new Vector3(x, 0, doorLine), c, label);
                interactables.Add(new Interactable
                {
                    Object = go, Prompt = "entrar: " + RoomInfo.Name(node.Type),
                    Use = () => { if (!Busy) StartCoroutine(EnterRoom(node)); },
                });
            }
        }

        GameObject BuildDoor(Vector3 pos, Color color, string label)
        {
            var root = new GameObject("Door");
            root.transform.position = pos;
            Block(root.transform, new Vector3(-1.1f, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), color);
            Block(root.transform, new Vector3(1.1f, 1.5f, 0), new Vector3(0.3f, 3f, 0.3f), color);
            Block(root.transform, new Vector3(0, 3.1f, 0), new Vector3(2.5f, 0.3f, 0.3f), color);
            Block(root.transform, new Vector3(0, 1.5f, 0.1f), new Vector3(1.9f, 2.9f, 0.05f), new Color(color.r, color.g, color.b, 0.28f));
            Label(root.transform, new Vector3(0, 4.3f, 0), label, color);
            Glow(root.transform, new Vector3(0, 1.6f, -0.8f), color);
            return root;
        }

        void SetupShrine()
        {
            var missing = new List<ElementType>();
            foreach (var e in Elements.All)
            {
                bool has = false;
                foreach (var a in player.Loadout.Awakened) if (a == e) has = true;
                if (!has) missing.Add(e);
            }
            if (missing.Count == 0)
            {
                Run.AddEssence(20);
                hud.SetEssence(Run.Essence);
                Run.RoomCleared();
                OpenDoors();
                return;
            }
            string[] names = { "", "Fogo", "Água", "Terra", "Ar" };
            for (int i = 0; i < missing.Count; i++)
            {
                var e = missing[i];
                var c = ElementColors.Of(e);
                var root = new GameObject("Shrine_" + e);
                root.transform.position = new Vector3((i - (missing.Count - 1) / 2f) * 4.5f, 0, 1.5f);
                Block(root.transform, new Vector3(0, 0.5f, 0), new Vector3(1.1f, 1f, 1.1f), new Color(0.3f, 0.3f, 0.38f));
                var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(orb.GetComponent<Collider>());
                orb.transform.SetParent(root.transform, false);
                orb.transform.localPosition = new Vector3(0, 1.7f, 0);
                orb.transform.localScale = Vector3.one * 0.8f;
                orb.GetComponent<Renderer>().material = Tinted(c);
                Label(root.transform, new Vector3(0, 3.2f, 0), names[(int)e], c);
                Glow(root.transform, new Vector3(0, 1.8f, -0.6f), c);
                interactables.Add(new Interactable
                {
                    Object = root, Prompt = "despertar " + names[(int)e],
                    Use = () =>
                    {
                        if (!player.Awaken(e)) return;
                        Feel.DamageNumber.Spawn(player.transform.position + Vector3.up * 3.6f, names[(int)e].ToUpperInvariant() + " DESPERTO!", c, 1.3f, 1.8f);
                        foreach (var it in interactables) if (it.Object != null) Destroy(it.Object);
                        interactables.Clear();
                        Run.RoomCleared();
                        OpenDoors();
                    },
                });
            }
        }

        Material Tinted(Color c) => new(flatMaterial) { color = c };

        void Block(Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material = Tinted(color);
        }

        void Glow(Transform parent, Vector3 localPos, Color color)
        {
            var l = new GameObject("Glow").AddComponent<Light>();
            l.transform.SetParent(parent, false);
            l.transform.localPosition = localPos;
            l.type = LightType.Point; l.color = color; l.intensity = 3f; l.range = 6f;
        }

        void Label(Transform parent, Vector3 localPos, string text, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<TextMesh>();
            t.font = UiKit.Font;
            t.text = text;
            t.fontSize = 64;
            t.characterSize = 0.09f;
            t.fontStyle = FontStyle.Bold;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = Color.Lerp(color, Color.white, 0.55f);
            go.GetComponent<MeshRenderer>().sharedMaterial = UiKit.Font.material;
            go.AddComponent<FaceCamera>();
        }
    }

    public class FaceCamera : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
