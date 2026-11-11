using System;
using UnityEngine;

namespace Quintessence.Core
{
    public struct ReactionEvent
    {
        /// Os dois elementos do par, em qualquer ordem (reacoes sao simetricas).
        public ElementType Mark;
        public ElementType Trigger;
        public Vector3 Point;
        public GameObject Target;
        public GameObject Source;
    }

    /// Barramento de eventos entre as frentes (combate, inimigos, run, UI, visual).
    /// Regra: uma frente nunca referencia o codigo da outra, so o Core. Quem causa chama Raise*, quem reage assina.
    public static class GameEvents
    {
        public static event Action<Health, DamageInfo, float> DamageDealt;
        public static event Action<Health, DamageInfo> Died;
        public static event Action<ElementType, Health> MarkApplied;
        public static event Action<ReactionEvent> ReactionTriggered;
        /// (anterior, novo)
        public static event Action<ElementType, ElementType> ElementSwitched;
        public static event Action<ElementType> ElementAwakened;
        /// 0..1
        public static event Action<float> QuintessenceMeterChanged;
        public static event Action<bool> QuintessenceActiveChanged;
        public static event Action RoomStarted;
        public static event Action RoomCleared;
        public static event Action RunStarted;
        /// true = vitoria
        public static event Action<bool> RunEnded;

        public static void RaiseDamageDealt(Health target, DamageInfo info, float applied) => DamageDealt?.Invoke(target, info, applied);
        public static void RaiseDied(Health target, DamageInfo info) => Died?.Invoke(target, info);
        public static void RaiseMarkApplied(ElementType element, Health target) => MarkApplied?.Invoke(element, target);
        public static void RaiseReaction(ReactionEvent e) => ReactionTriggered?.Invoke(e);
        public static void RaiseElementSwitched(ElementType from, ElementType to) => ElementSwitched?.Invoke(from, to);
        public static void RaiseElementAwakened(ElementType element) => ElementAwakened?.Invoke(element);
        public static void RaiseQuintessenceMeter(float normalized) => QuintessenceMeterChanged?.Invoke(normalized);
        public static void RaiseQuintessenceActive(bool active) => QuintessenceActiveChanged?.Invoke(active);
        public static void RaiseRoomStarted() => RoomStarted?.Invoke();
        public static void RaiseRoomCleared() => RoomCleared?.Invoke();
        public static void RaiseRunStarted() => RunStarted?.Invoke();
        public static void RaiseRunEnded(bool victory) => RunEnded?.Invoke(victory);

        /// Limpa assinantes ao entrar em Play (o projeto pode rodar sem domain reload) e ao trocar de run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            DamageDealt = null; Died = null; MarkApplied = null; ReactionTriggered = null;
            ElementSwitched = null; ElementAwakened = null;
            QuintessenceMeterChanged = null; QuintessenceActiveChanged = null;
            RoomStarted = null; RoomCleared = null; RunStarted = null; RunEnded = null;
        }
    }
}
