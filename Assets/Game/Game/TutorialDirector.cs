using Quintessence.Combat;
using Quintessence.Core;
using Quintessence.Run;
using Quintessence.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Quintessence.Game
{
    /// Sala de treino: bonecos que nao revidam e um passo de cada vez, ate marca -> troca -> reacao -> Quintessencia.
    public class TutorialDirector : MonoBehaviour
    {
        public TutorialState State { get; private set; } = new();
        PlayerController player;
        Hud hud;
        Vector3 lastPos;
        bool ended;

        public void Begin(PlayerController hero, Hud display, GameObject dummyPrefab)
        {
            player = hero; hud = display;
            player.ResetLoadout(GameFlow.StartElement);
            player.Teleport(new Vector3(0, 0, -4f), Vector3.forward);
            lastPos = player.transform.position;
            hud.SetRoom("Treino", RoomType.Rest);
            hud.SetEssence(0);
            if (dummyPrefab != null)
                foreach (var p in new[] { new Vector3(-4f, 0, 2f), new Vector3(0, 0, 3.5f), new Vector3(4f, 0, 2f) })
                    Instantiate(dummyPrefab, p, Quaternion.Euler(0, 180, 0), transform);
            player.DashStarted += OnDash;
            player.SkillStarted += OnSkill;
            GameEvents.DamageDealt += OnDamage;
            GameEvents.ElementSwitched += OnSwitch;
            GameEvents.ReactionTriggered += OnReaction;
            GameEvents.QuintessenceActiveChanged += OnQuintessence;
            Show();
        }

        void OnDestroy()
        {
            if (player != null) { player.DashStarted -= OnDash; player.SkillStarted -= OnSkill; }
            GameEvents.DamageDealt -= OnDamage;
            GameEvents.ElementSwitched -= OnSwitch;
            GameEvents.ReactionTriggered -= OnReaction;
            GameEvents.QuintessenceActiveChanged -= OnQuintessence;
        }

        void OnDash(Vector3 _) => Report(TutorialStep.Dash);
        void OnSkill(ElementType _) => Report(TutorialStep.Skill);
        void OnDamage(Health target, DamageInfo info, float applied) { if (info.SourceTeam == Team.Player) Report(TutorialStep.Attack); }
        void OnSwitch(ElementType from, ElementType to) { if (from != ElementType.None) Report(TutorialStep.Switch); }
        void OnReaction(ReactionEvent e) => Report(TutorialStep.React);
        void OnQuintessence(bool on) { if (on) Report(TutorialStep.Quintessence); }

        void Report(TutorialStep signal, float amount = 1f)
        {
            if (ended || !State.Report(signal, amount)) { if (!ended && signal == State.Step) Show(); return; }
            Sfx.Play("pickup");
            Prepare();
            Show();
        }

        /// Da ao jogador o que o passo novo precisa: o segundo elemento para trocar, tudo e a barra cheia para a Quintessencia.
        void Prepare()
        {
            switch (State.Step)
            {
                case TutorialStep.Switch:
                    foreach (var e in Elements.All)
                        if (e != GameFlow.StartElement) { player.Awaken(e); break; }
                    break;
                case TutorialStep.Quintessence:
                    foreach (var e in Elements.All) player.Awaken(e);
                    player.AddQuintessence(QuintessenceState.Max);
                    break;
                case TutorialStep.Done:
                    Invoke(nameof(End), 4f); // deixa a Quintessencia rolar um pouco antes da tela final
                    break;
            }
        }

        void End()
        {
            ended = true;
            hud.ShowTutorialEnd(() => GameFlow.StartRun(GameFlow.StartElement), GameFlow.ToTitle);
        }

        void Show() => hud.ShowTutorial(State.Number, TutorialState.StepCount, TutorialState.Title(State.Step), TutorialState.Text(State.Step), State.Progress01);

        void Update()
        {
            if (ended || player == null) return;
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                bool pause = !GameClock.Paused;
                GameClock.SetPaused(pause);
                hud.ShowPause(pause, () => { GameClock.SetPaused(false); hud.ShowPause(false); }, GameFlow.ToTitle);
            }
            if (GameClock.Paused) return;
            if (!Arena.Contains(player.transform.position)) player.Teleport(new Vector3(0, 0, -4f), Vector3.forward);
            var pos = player.transform.position;
            float moved = Vector3.Distance(pos, lastPos);
            lastPos = pos;
            if (State.Step == TutorialStep.Move && moved > 0f && moved < 2f && !player.IsDashing) Report(TutorialStep.Move, moved);
        }
    }
}
