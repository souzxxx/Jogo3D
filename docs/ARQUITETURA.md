# Arquitetura

## Regra de ouro

Cada frente tem a sua pasta e o seu assembly em `Assets/Game/`, e **só referencia o `Quintessence.Core`**, nunca outra frente. As frentes conversam por `GameEvents` e pelos tipos do Core.

| Pasta | Assembly | Dono | Conteúdo |
|---|---|---|---|
| `Assets/Game/Core` | `Quintessence.Core` | Leonardo | Contratos: `ElementType`, `Team`, `DamageInfo`, `IDamageable`, `Health`, `GameEvents` |
| `Assets/Game/Combat` | `Quintessence.Combat` | Leonardo | Herói, ataques, elementos, marcas, reações, Quintessência |
| `Assets/Game/Enemies` | `Quintessence.Enemies` | Enzo | IA, inimigos, ondas, chefe, layouts de sala |
| `Assets/Game/Run` | `Quintessence.Run` | Nicolas | Mapa, portas, recompensas, bênçãos, hub, save |
| `Assets/Game/UI` | `Quintessence.UI` | Nicolas | HUD e menus |
| `Assets/Game/Feel` | `Quintessence.Feel` | Miguel | VFX, luz, câmera, shake, feedback |

Só o Core existe por enquanto; cada pessoa cria a pasta e o `.asmdef` da sua frente no primeiro card (referenciando `Quintessence.Core`).

## Contratos

- **Causar dano:** monte um `DamageInfo` e chame `IDamageable.TakeDamage`. O `Health` ignora dano do mesmo `Team`, de quem já morreu e de invulnerável, e devolve o dano aplicado.
- **Reagir a algo de outra frente:** assine `GameEvents` (`DamageDealt`, `Died`, `MarkApplied`, `ReactionTriggered`, `ElementSwitched`, `ElementAwakened`, `QuintessenceMeterChanged`, `QuintessenceActiveChanged`, `RoomStarted`, `RoomCleared`, `RunStarted`, `RunEnded`). Desassine em `OnDisable`.
- **Avisar as outras frentes:** chame o `GameEvents.Raise*` correspondente. Ex.: o spawner do Enzo chama `RaiseRoomCleared()`, a porta do Nicolas escuta.
- Evento novo entra no `GameEvents` no mesmo commit de quem precisa dele.

## Dados

Números de design (dano, vida, cooldown, tabela de reações, bênçãos) ficam em ScriptableObjects, não no código.

## Testes

Regra pura (sem Unity) vai em classe C# simples e ganha teste EditMode em `Tests/Editor` da frente. Rodar: `unity test . --mode EditMode`.

## Cenas

Ninguém edita a cena de outra frente. Cada um trabalha em prefabs e numa cena de teste própria (`Assets/Scenes/Dev/<Nome>_*.unity`); a cena do jogo só instancia prefabs.
