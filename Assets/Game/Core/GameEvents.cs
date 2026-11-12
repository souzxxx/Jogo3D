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
        public string Name;
        public float Radius;
    }

    public enum AttackKind { Basic, Finisher, Skill, Dash }

    /// Um golpe saiu (acertando ou nao). Serve para VFX, som e camera.
    public struct AttackEvent
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public ElementType Element;
        public float Range;
        public float Arc;
        public AttackKind Kind;
        public GameObject Source;
    }

    /// Aviso no chao de um ataque que vai sair (circulo ou linha).
    public struct TelegraphEvent
    {
        public Vector3 Point;
        public Vector3 Direction;
        /// Circulo: raio. Linha: meia largura.
        public float Radius;
        /// So para linha.
        public float Length;
        public float Duration;
        public bool IsLine;
        /// Algo e arremessado de Source ate Point e cai no fim do aviso (bomba do Arremessador).
        public bool Lobbed;
        /// Nao e ataque no heroi (cura do Xama): desenha em verde.
        public bool Friendly;
        public GameObject Source;
    }

    /// Area de efeito que fica no chao (rastro de fogo, poca de lava...).
    public struct ZoneEvent
    {
        public Vector3 Point;
        public float Radius;
        public float Duration;
        public ElementType Element;
    }

    /// Barramento de eventos entre as frentes (combate, inimigos, run, UI, visual).
    /// Regra: uma frente nunca referencia o codigo da outra, so o Core. Quem causa chama Raise*, quem reage assina.
    public static class GameEvents
    {
        public static event Action<Health, DamageInfo, float> DamageDealt;
        public static event Action<Health, DamageInfo> Died;
        /// Um golpe foi anulado por bloqueio (armadura da dash de Terra).
        public static event Action<Health, DamageInfo> HitBlocked;
        /// Alvo Congelado levou um golpe e estilhacou.
        public static event Action<Health> Shattered;
        public static event Action<ElementType, Health> MarkApplied;
        public static event Action<Health> MarkCleared;
        public static event Action<ReactionEvent> ReactionTriggered;
        /// (alvo, status, duracao)
        public static event Action<Health, StatusType, float> StatusApplied;
        public static event Action<Health, StatusType> StatusEnded;
        public static event Action<AttackEvent> AttackPerformed;
        public static event Action<ZoneEvent> ZoneSpawned;
        public static event Action<TelegraphEvent> TelegraphStarted;
        /// O ataque avisado foi interrompido (inimigo morreu ou foi atordoado).
        public static event Action<GameObject> TelegraphCancelled;
        /// Um ataque errou por causa de Cego (Vapor).
        public static event Action<Health> AttackMissed;
        public static event Action<Health> EnemySpawned;
        /// (inimigo, tem armadura). A armadura cai com Armadura Quebrada.
        public static event Action<Health, bool> ArmorChanged;
        /// (vida do chefe, nome)
        public static event Action<Health, string> BossSpawned;
        /// (fase atual comecando em 1, total de fases)
        public static event Action<int, int> BossPhaseChanged;
        /// (escudo de pe, precisa de Quintessencia para quebrar)
        public static event Action<bool, bool> BossShieldChanged;
        /// Explosao de um inimigo suicida: (ponto, raio).
        public static event Action<Vector3, float> Exploded;
        /// (indice da onda comecando em 0, total de ondas)
        public static event Action<int, int> WaveStarted;
        /// Nasceu o Campeao de uma sala de Elite.
        public static event Action<Health> ChampionSpawned;
        /// Um inimigo curou outro: (quem recebeu, quanto).
        public static event Action<Health, float> EnemyHealed;
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
        public static void RaiseHitBlocked(Health target, DamageInfo info) => HitBlocked?.Invoke(target, info);
        public static void RaiseShattered(Health target) => Shattered?.Invoke(target);
        public static void RaiseMarkApplied(ElementType element, Health target) => MarkApplied?.Invoke(element, target);
        public static void RaiseMarkCleared(Health target) => MarkCleared?.Invoke(target);
        public static void RaiseReaction(ReactionEvent e) => ReactionTriggered?.Invoke(e);
        public static void RaiseStatusApplied(Health target, StatusType type, float duration) => StatusApplied?.Invoke(target, type, duration);
        public static void RaiseStatusEnded(Health target, StatusType type) => StatusEnded?.Invoke(target, type);
        public static void RaiseAttackPerformed(AttackEvent e) => AttackPerformed?.Invoke(e);
        public static void RaiseZoneSpawned(ZoneEvent e) => ZoneSpawned?.Invoke(e);
        public static void RaiseTelegraph(TelegraphEvent e) => TelegraphStarted?.Invoke(e);
        public static void RaiseTelegraphCancelled(GameObject source) => TelegraphCancelled?.Invoke(source);
        public static void RaiseAttackMissed(Health source) => AttackMissed?.Invoke(source);
        public static void RaiseEnemySpawned(Health enemy) => EnemySpawned?.Invoke(enemy);
        public static void RaiseBossSpawned(Health boss, string name) => BossSpawned?.Invoke(boss, name);
        public static void RaiseBossPhase(int phase, int total) => BossPhaseChanged?.Invoke(phase, total);
        public static void RaiseBossShield(bool up, bool needsQuintessence) => BossShieldChanged?.Invoke(up, needsQuintessence);
        public static void RaiseArmorChanged(Health enemy, bool armored) => ArmorChanged?.Invoke(enemy, armored);
        public static void RaiseExploded(Vector3 point, float radius) => Exploded?.Invoke(point, radius);
        public static void RaiseChampionSpawned(Health champion) => ChampionSpawned?.Invoke(champion);
        public static void RaiseEnemyHealed(Health target, float amount) => EnemyHealed?.Invoke(target, amount);
        public static void RaiseWaveStarted(int index, int total) => WaveStarted?.Invoke(index, total);
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
            DamageDealt = null; Died = null; HitBlocked = null; Shattered = null; MarkApplied = null; ReactionTriggered = null;
            MarkCleared = null; StatusApplied = null; StatusEnded = null; AttackPerformed = null; ZoneSpawned = null;
            TelegraphStarted = null; TelegraphCancelled = null; AttackMissed = null; EnemySpawned = null; WaveStarted = null; ArmorChanged = null; Exploded = null;
            EnemyHealed = null; ChampionSpawned = null;
            BossSpawned = null; BossPhaseChanged = null; BossShieldChanged = null;
            ElementSwitched = null; ElementAwakened = null;
            QuintessenceMeterChanged = null; QuintessenceActiveChanged = null;
            RoomStarted = null; RoomCleared = null; RunStarted = null; RunEnded = null;
        }
    }
}
