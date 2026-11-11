# Arquitetura

## Regra de ouro

Cada frente tem a sua pasta e o seu assembly em `Assets/Game/`, e **só referencia o `Quintessence.Core`**, nunca outra frente. As frentes conversam por `GameEvents` e pelos tipos do Core.

| Pasta | Assembly | Dono | Conteúdo |
|---|---|---|---|
| `Assets/Game/Core` | `Quintessence.Core` | Leonardo | Contratos: `ElementType`, `Team`, `DamageInfo`, `IDamageable`, `Health`, `GameEvents` |
| `Assets/Game/Combat` | `Quintessence.Combat` | Leonardo | Herói, ataques, elementos, marcas, reações, Quintessência |
| `Assets/Game/Enemies` | `Quintessence.Enemies` | Enzo | IA, inimigos, ondas, chefe, layouts de sala |
| `Assets/Game/Run` | `Quintessence.Run` | Nicolas | Regras da run sem Unity: mapa, estado, plano de ondas, recompensas |
| `Assets/Game/UI` | `Quintessence.UI` | Nicolas | HUD, mapa, pausa, resultado, título. Pode referenciar `Run` além do Core |
| `Assets/Game/Game` | `Quintessence.Game` | todos | **Composição:** o único assembly que referencia todas as frentes. `GameFlow` (cenas) e `RunDirector` (monta cada sala, portas, santuário, fim de run) |
| `Assets/Game/Feel` | `Quintessence.Feel` | Miguel | VFX, luz, câmera, shake, feedback |

O HUD lê o herói pela interface `IHeroStatus` do Core, sem conhecer o combate. Pausa passa por `GameClock.SetPaused`.

Cenas do jogo: `Assets/Scenes/Title.unity` e `Assets/Scenes/Run.unity`, geradas por `GameSceneBuilder` (menu Quintessence > Build cenas do jogo). A run inteira acontece na cena Run; trocar de sala não recarrega a cena.

## Contratos

- **Causar dano:** monte um `DamageInfo` e chame `IDamageable.TakeDamage`. O `Health` ignora dano do mesmo `Team`, de quem já morreu e de invulnerável, e devolve o dano aplicado.
- **Reagir a algo de outra frente:** assine `GameEvents` (`DamageDealt`, `Died`, `MarkApplied`, `ReactionTriggered`, `ElementSwitched`, `ElementAwakened`, `QuintessenceMeterChanged`, `QuintessenceActiveChanged`, `RoomStarted`, `RoomCleared`, `RunStarted`, `RunEnded`). Desassine em `OnDisable`.
- **Avisar as outras frentes:** chame o `GameEvents.Raise*` correspondente. Ex.: o spawner do Enzo chama `RaiseRoomCleared()`, a porta do Nicolas escuta.
- **Golpe elemental:** use `ElementCombat.Deliver(alvo, info, tabela)` em vez de `TakeDamage` direto; ele aplica a marca e dispara a reação. Dano de reação e de Queimadura vai com `IsReaction = true` e não marca.
- **Status:** `StatusHolder.GetOrAdd(health).Apply(tipo, duração, time)`. Inimigos leem `State.MoveMultiplier`, `State.CanAct` e `State.MissChance`.
- **Feedback:** a frente Feel só escuta `GameEvents` (`AttackPerformed`, `DamageDealt`, `MarkApplied`, `ReactionTriggered`, `StatusApplied`, `ZoneSpawned`...). Quem causa o efeito não chama VFX direto.
- **Inimigo novo:** um `EnemyDefinition` (números) + um prefab com `Health`, `NavMeshAgent`, colisor e `EnemyBrain`. Ataque sempre avisado com `GameEvents.RaiseTelegraph` (mínimo 0,4 s).
- **Sala:** `WaveSpawner` levanta `RoomStarted`, `WaveStarted` e `RoomCleared`. A sala precisa de NavMesh (ver `EnemyDevSceneBuilder`).
- Evento novo entra no `GameEvents` no mesmo commit de quem precisa dele.

## Dados

Números de design (dano, vida, cooldown, tabela de reações, bênçãos) ficam em ScriptableObjects, não no código.

## Testes

Regra pura (sem Unity) vai em classe C# simples e ganha teste EditMode em `Tests/Editor` da frente. Rodar: `unity test . --mode EditMode`.

## Cenas

Ninguém edita a cena de outra frente. Cada um trabalha em prefabs e numa cena de teste própria (`Assets/Scenes/Dev/<Nome>_*.unity`); a cena do jogo só instancia prefabs.
