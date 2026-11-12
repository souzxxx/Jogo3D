# Arquitetura

## Regra de ouro

Cada frente tem a sua pasta e o seu assembly em `Assets/Game/`, e **só referencia o `Quintessence.Core`**, nunca outra frente. As frentes conversam por `GameEvents` e pelos tipos do Core.

| Pasta | Assembly | Dono | Conteúdo |
|---|---|---|---|
| `Assets/Game/Core` | `Quintessence.Core` | Leonardo | Contratos: `ElementType`, `Team`, `DamageInfo`, `IDamageable`, `Health`, `GameEvents` |
| `Assets/Game/Combat` | `Quintessence.Combat` | Leonardo | Herói, ataques, elementos, marcas, reações, Quintessência |
| `Assets/Game/Enemies` | `Quintessence.Enemies` | Enzo | IA, inimigos, ondas, chefe, layouts de sala |
| `Assets/Game/Run` | `Quintessence.Run` | Nicolas | Regras da run sem Unity: mapa, estado, plano de ondas, recompensas, loja e progresso permanente (`MetaProgress`; quem grava é `MetaStore`, em `Game`) |
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
- **Bênção/upgrade:** escreve em `IHeroStatus.Modifiers` (`HeroModifiers`, no Core); o combate lê de lá. Bênção nova = uma linha no asset `Boons.asset`; atributo novo = um campo em `HeroModifiers` + um caso em `BoonEffects`.
- **Um componente por arquivo**, com o mesmo nome do arquivo. Classe `MonoBehaviour`/`ScriptableObject` em arquivo de outro nome perde a referência na cena (já aconteceu com `ReactionTable` e `TitleController`).
- **Layout de sala:** um asset `RoomLayout` em `Assets/Game/Game/Layouts` (lista de peças com posição). Peça de cobertura precisa de colisor e `NavMeshObstacle` com carving; `LayoutAssetTests` confere isso e que nada bloqueia entrada ou portas. Arranjo de um trecho só: preencha `biomes` (pesa o triplo nele e não aparece nos outros; `LayoutPicker.PickWeighted`).
- **Defesa do alvo:** `Health.TakeDamage` multiplica por `ElementalAffinity` (resiste ao próprio elemento, fraco ao que o vence) e por `FrontGuard` (escudo frontal do Escudeiro: só golpe direto com `Direction` vindo da frente; reação, zona e queimadura chegam sem direção e passam).
- **Poças que marcam:** `CorruptionPool` e `WaterPuddle` (em `Game`) marcam inimigos com um elemento sem trocar a marca do herói. Poça nova de inimigo: o inimigo chama `GameEvents.RaisePuddleLanded(ponto, raio, elemento, segundos)` e o `RunDirector` cria a poça a partir do prefab montado no editor.
- **Chefe:** `BossDefinition` (números, `pattern` 0/1/2 = Guardião/Tecela/Rainha em `BossRules`, `element` dos golpes, `eruptionPuddleSeconds`) + `BossBrain`. Ataque novo = um `BossAttack` e um ciclo em `BossRules`. A última fase sempre levanta o `BossShield` (só cai com a Quintessência ou por tempo, se o herói não tem os quatro elementos).
- **Progresso permanente:** tudo em `MetaProgress` (`Run/Meta.cs`), serializado numa linha com campos opcionais no fim (vitórias por herói, maestria, Feitos): campo novo vai sempre no fim e `Parse` aceita save sem ele. Feito novo = um valor em `Feat` + uma regra em `Feats.Earned` + um contador no `RunRecord` que o `RunDirector` preenche.
- **Provação:** regras puras em `Run/Trials.cs` (`TrialKind`, `Holding`, `Status`); o `RunDirector` conta tempo, golpes (`DamageDealt` no herói) e dashes (`AttackPerformed` com `Kind = Dash`).
- **Dificuldade:** `DifficultyCurve` (profundidade, `EarlyDamage` das duas primeiras salas, traços de Elite em `EliteModifier`), `Ascension` e a opção Tranquila (`GamePrefs.Relaxed` → `HealthPool.AssistMultiplier` no herói). A calibração usa a sonda `BalanceProbe` (teste Explicit) com `ProbeDodge` 0,65: um jogador razoável.
- **Talento:** um `Add(...)` em `TalentCatalog` (`Run/Progression.cs`) com um `TalentEffect` novo **no fim do enum**, um caso em `TalentCatalog.Apply` que escreve num campo de `HeroModifiers` (zerado em `Reset`), e o filtro em `Offer` se ele só faz sentido com N elementos (Contágio e Cadência pedem 2, Alquimia e Despertar Intenso pedem 3). O ícone sai de `Hud.TalentIcon`. O save guarda talentos por id, então renomear o id quebra runs salvas.
- **Combinar skills:** a Cadência fica em `PlayerController.DoSkillHit` (elemento e hora da última skill; janela `HeroModifiers.CadenceWindow`) e avisa por `GameEvents.Cadence`. Contadores da run para a linha Build do resultado: `Fusions`, `PerfectDodges`, `Cadences` (lidos em `RunDirector.BuildLine`).
- **Traço de Elite:** valor novo no fim de `EliteModifier`, caso em `DifficultyCurve.For`, nome e descrição (a porta mostra os dois) e o `% N` de `PickElite`; o número chega ao inimigo por `EnemyScaling` (o `RunDirector` copia de `Difficulty`). Espinhosos: `EnemyBrain.OnDamaged` devolve parte do golpe sem elemento com `DamageInfo.Reflected = true`, que o herói não conta como golpe de criatura (`PlayerController.FromCreature`: sem cura de esquiva, sem esquiva perfeita).
- **Peça de layout com forma:** `GameSceneBuilder.Obstacle` cobre a caixa inteira do modelo; peça em L (cantoneira) usa `CornerObstacle`, com um colisor e um recorte por braço nos filhos (`LayoutAssetTests` procura `NavMeshObstacle` nos filhos). Na rotação 0 a quina fica a sudoeste; cada +90 gira no sentido horário.
- **Roteiro de teste que anda:** use `SteerTo(alvo)` (NavMesh) em vez de andar em linha reta; arranjo novo com móveis prende quem anda reto.
- Evento novo entra no `GameEvents` no mesmo commit de quem precisa dele.

## Dados

Números de design (dano, vida, cooldown, tabela de reações, bênçãos) ficam em ScriptableObjects, não no código.

## Testes

Regra pura (sem Unity) vai em classe C# simples e ganha teste EditMode em `Tests/Editor` da frente. Rodar: `unity test . --mode EditMode`.

## Cenas

Ninguém edita a cena de outra frente. Cada um trabalha em prefabs e numa cena de teste própria (`Assets/Scenes/Dev/<Nome>_*.unity`); a cena do jogo só instancia prefabs.
