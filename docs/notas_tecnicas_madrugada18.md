# Notas técnicas da madrugada 17→18/11 (rodadas 13 a 27 e revisão dos estados)

Para quem for mexer no código depois: o que entrou, onde está e qual teste cobre. O que mudou para o jogador está em `progresso.html`.

## Combate e inimigos

| O quê | Onde | Teste |
|---|---|---|
| Fila de ataque corpo a corpo: no máximo 3 inimigos avisando/batendo ao mesmo tempo; quem espera rodeia o herói | `Enemies/MeleeTokens.cs` (`Max`, `TryTake`, `Release`), `EnemyBrain.Update` (pega a vez antes do `fsm.Tick`; devolve no `Strike`, `Cancel`, `OnDied`, `OnDisable`, `OnDestroy`), `EnemyBrain.WaitingTurn` (rodeio), `EnemyFsm.CooldownReady` | `MeleeTokensTests` (EditMode), `A_Melee_Crowd_Takes_Turns_To_Attack` |
| Campeão da Elite não entra na fila | `EnemyBrain.UsesTurns`, `isChampion` (lido no `Start`) | — |
| Explosivo avisa 0,9 s; Rastejante 0,65 s | `Enemies/Data/Explosivo.asset`, `Rastejante.asset` (`telegraphTime`) | sonda |
| Lacaios do chefe só nascem no chão andável e dentro da sala (8 ângulos; sem ponto, não invoca) | `BossBrain.SummonPoint`, `SummonNow` (testes) | `Boss_In_A_Corner_Summons_Adds_On_The_NavMesh` |
| Resgate de inimigo fora do NavMesh posiciona antes de religar o agente | `EnemyBrain.Rescue` | `Nothing_Traps_The_Player` |
| "Sala limpa" desarma explosões de morte e tiros inimigos no ar | `RunDirector.DefuseLeftovers`, `Projectile.FromEnemy`, `VolatileBurst.OnDestroy` | `Cleared_Room_Is_Safe_And_Signs_Stay_Out_Of_The_Fight` |

## Cenário que reage

| O quê | Onde | Teste |
|---|---|---|
| Skill de Ar apaga velas (não tochas) | `TorchReact.OnAttack` → `TouchCandles(..., "cast_air")`, contador `CandlesBlown` | `Candles_Go_Out_With_Water_And_Camera_Leans_To_The_Boss` |
| Velas apagam com Água (troca a malha acesa/apagada do pack) e ganham uma chama pequena (cópia da `TorchFlame`) | `Game/TorchReact.cs` (`CandlePairs`, `RefreshCandles`, `AddCandleFlame`), pares ligados em `GameSceneBuilder` → `RunDirector.BindCandles` | `Candles_Go_Out_With_Water_And_Camera_Leans_To_The_Boss` |
| Barril de pólvora molhado: Água deixa molhado 6 s (azulado, sem a faixa do alcance); o golpe seguinte quebra sem explodir; seca e volta ao normal | `Enemies/ExplosiveBarrel.cs` (`Intercept`, `Soak`, `WetSeconds`), gancho `Core/IDamageIntercept` em `Health.TakeDamage`, evento `GameEvents.PowderDoused` → `CombatFeedback.OnPowderDoused` (gotas, "MOLHADO", vapor) e `AudioDirector.OnPowder` (som `fizz` de `tools/gen_audio.py`) | `Hazards_Treasure_Pact_And_Biomes` |
| Respiradouro de brasa: Água apaga 6 s (miolo cinza), Fogo adianta a erupção com o aviso inteiro | `Enemies/EmberVent.cs` (`Touch`, `Quenched`, `SetEmbers`) | `Ember_Vent_Goes_Out_With_Water_And_Erupts_With_Fire` |

## Câmera

| O quê | Onde |
|---|---|
| Não adianta além da parede do fundo | `FollowCamera.NorthMargin` (2,5 m) |
| Puxa 10% para o chefe (teto 1,2 m) | `FollowCamera.Lean`, chamado em `RunDirector.OnBossSpawned` |
| Sem adiantamento nas salas sem luta | `FollowCamera.Calm`, ligado por `RunDirector.SetCalmCamera` |

## Interface e textos

| O quê | Onde |
|---|---|
| Letreiro discreto com a luta seguindo | `RunDirector.Banner` (`QuietBanners`) |
| Placas pela altura real do texto | `RunDirector.Stack` (mede `MeshRenderer.bounds`) |
| Pedestais, baús, altares e mercadorias mais perto da entrada | `RunDirector.ShrineZ`, `BoonZ`, `PactZ`, `WareZ`; Tesouro em z 0 |
| Dica sobe acima do "E entrar" | `Hud.TipAbovePrompt`, `Hud.TickTip` |
| Pulso do nível na barra | `Hud.LevelPulses` |
| Chefe sem "ONDA 1/1"; Elite com o modificador no chip | `CombatFeedback.QuietWaves`; `roomBase` montado em `RunDirector.StartEncounter` |
| Textos do mundo somem no painel de talento; números de dano somem quando as portas abrem | `DamageNumber.ClearLive`, `DamageNumber.ClearNumbers` |
| Dano que arredonda para 0 não vira número | `CombatFeedback.OnDamage` (`ZeroNumbersSkipped`) |
| Inventário: reações maiores com "falta X"; ícone apagado em vez de cadeado | `Hud` (inventário) |
| Dica da Tranquila na derrota até a sala 4 (até 3 vezes) | `RunDirector.ShowRelaxedHint` (PlayerPrefs `quintessence.relaxedHint`) |
| Aviso de jogo lento (média < 30 fps por 4 s na luta, uma vez) | `RunDirector.TickSlowCheck` / `ReportFrameRate`, `Tips` id `slow` |
| Treino vestido como Salões; bonecos mais perto | `RunDirector.Start` (ramo do tutorial), `TutorialDirector` |
| Com run salva, a carta de herói pede um segundo clique antes de apagar a salva | `TitleMenu.PickCard` (`ArmedCard`); teste em `Title_Starts_A_Run_With_The_Picked_Element` |
| Só com o controle: botões do título desligados sob os painéis; A ignorado por 0,35 s quando um painel pede foco; foco volta ao último botão quando o controle mexe sem nada escolhido; Refúgio mantém o foco na compra | `UI/FocusGuard.cs` (`Hold`, `TryRecover`, posto no EventSystem por `UiKit.EnsureEventSystem`; `UiKit.Focus` lembra o botão mesmo sem controle), `TitleMenu.SyncOverlayBlock`, `keepFocus` em `BuildRefuge`; teste `Gamepad_Only_Title_Overlays_Lock_The_Buttons_Behind` |
| Esc na troca de sala fica guardado e pausa quando a sala abre (o cartão do trecho não cobre o painel) | `RunDirector.PressPause` (`pauseQueued`, `PausesQueued`); teste em `Short_Run_From_First_Room_To_Victory` |

## Som

`tools/gen_audio.py crack` gera o estalo da armadura; `AudioDirector` toca `crack` quando `ArmorChanged` é falso e `dash_perfect` (o dash com tom 1,5) na esquiva perfeita.

## Sonda de balanceamento

`BalanceProbe.Probe_Full_Runs` agora faz `Random.InitState` por run, mas ainda varia pelo tempo real (dicas e letreiros): compare mudanças com duas ou mais sondas. Última: 11 e 11 de 12. Se o jogo parecer fácil demais, o primeiro botão é `MeleeTokens.Max`.

## Testes intermitentes conhecidos

`Guardian_Three_Phases_Shield_Needs_Quintessence_Then_Dies` falhou de vez em quando na suíte inteira ("matar o chefe limpa a sala"); passa sozinho. Rodar de novo antes de investigar.
