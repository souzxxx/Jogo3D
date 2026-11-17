# Quintessence 3D — Documento de Escopo

> Projeto 3D · Jogos Digitais e Interação · Insper
> Equipe: **Leonardo** (souzxxx), **Nicolas** (NicolasVolf), **Enzo** (canavero), **Miguel** (EuMiguelll)
> Entrega: **até 25/11/2026** (rígida, -2 pontos por dia de atraso). Build para **Windows** (obrigatória) e WebGL na jam do itch.io (https://itch.io/jam/projeto-jogo-3d-insper-20262), código no GitHub com créditos no README, GDD atualizado, confirmação no Blackboard
> Status: jogo completo; reta final de acabamento guiada pela rubrica final (seção 6).

Itens marcados com **[DECIDIR]** dependem do time ou do professor.

---

## 1. Pitch

**Os quatro elementos se separaram e estão corrompendo o mundo.** Você é um aprendiz que começa a run dominando **um** elemento. Sala por sala, você desperta os outros três em santuários, troca de elemento no meio do combo e encadeia **reações elementais** (Fogo + Ar = Tempestade de Fogo, Água + Ar = Nevasca...). Quem domina os quatro entra em **Quintessência**, o quinto estado, em que cada golpe é todos os elementos ao mesmo tempo, e só assim se quebra o escudo do Guardião corrompido.

É a versão 3D do nosso projeto pessoal *Quintessence* (2D, Godot): mesmo mundo e mesmo sistema de elementos, refeito do zero em Unity 3D. Mundo, nomes e personagens são **originais** (inspiração em jogos de roguelite cooperativo de salas e em fantasia elemental, sem usar IP de ninguém).

**Gênero:** action roguelite 3D, câmera top-down 3/4 fixa
**Plataforma:** Windows (obrigatória) e WebGL, os dois no itch.io · **Engine:** Unity 6000.6.4f1, URP · **Idioma:** PT-BR
**Sessão:** uma run completa em 10–15 min; 1–2 h até a primeira vitória

### Pilares
1. **Os elementos são a build.** Combinar as skills elementais e detonar reações é a habilidade central. Todo sistema (bênçãos, inimigos, chefe) alimenta isso.
2. **Toda run conta o arco da maestria.** Começa com um elemento, termina com quatro. A ordem em que você desperta muda a run.
3. **Combate curto, legível e gostoso.** Telegrafia clara, animações curtas, impacto forte. Se não for bom com cápsulas cinzas, arte não salva.

---

## 2. O que é sucesso

- **Nota A**: conceito A nas 4 frentes da rubrica final (seção 6). A nota é a média das quatro, então nenhuma pode ficar para trás.
- Em 1 minuto o jogador entende marca → reação. Em 5 minutos ele troca de elemento de propósito.
- Cada pessoa entrega a sua frente sem travar ninguém desde o primeiro dia.

### Objetivos
- Loop completo: hub → escolher elemento inicial → run (salas, portas com recompensa, santuários, chefe) → hub com upgrades permanentes.
- 4 elementos com identidade própria (ataque, skill, dash, marca) e **6 reações**.
- Quintessência como ultimate e como chave do chefe.
- 1 região completa (**Planalto das Cinzas**) com ~8 salas por run em mapa ramificado, 4 inimigos + 1 elite + 1 chefe.
- Visual 3D coeso, com os elementos carregando a cor e a luz.

### Não-objetivos
- Co-op local: **single-player agora, co-op só se sobrar tempo.** O código não assume um jogador só (lista de jogadores, câmera por alvo), mas nenhuma tarefa de co-op entra no kanban antes do jogo solo estar fechado. Online nunca.
- Mais de uma região, mais de um herói jogável (variação vem do elemento inicial e das bênçãos).
- Mundo aberto, inventário, crafting, diálogo dublado.
- Modelagem 3D própria: usamos packs CC0 e gastamos o tempo em shader, luz e VFX.

### Anti-objetivos
- Combate que funciona melhor ficando em um elemento só.
- Reação que o jogador não percebe que causou.
- Dead end: sala que não fecha, porta que não abre, run sem saída.
- Alguém do time parado esperando outra frente.
- Qualquer coisa reconhecível como IP de terceiros (nomes, símbolos, personagens).

---

## 3. Restrições

| Restrição | Consequência |
|---|---|
| Build **Windows** (obrigatória) e **WebGL** | URP com orçamento apertado pensando no WebGL: poucas luzes em tempo real, sombras só da luz principal, sem compute shader, texturas comprimidas, build < ~80 MB. Save em `PlayerPrefs`. Sem botão "Sair". |
| Todos na mesma versão do Unity (`6000.6.4f1`) | Versão diferente quebra o projeto. |
| Uma sessão do Claude, uma pessoa por vez | Cada parte é fechada e commitada com o nome de quem assumiu. Frentes em pastas/assemblies separados pra não dar conflito de cena. |
| Créditos de assets no README | Crédito entra no mesmo commit do asset. |
| Plágio | Código de terceiros com mais de 2–3 linhas leva link no código e no README. |
| GDD no Milanote em DDE | `docs/GDD.md` é o rascunho. |

---

## 4. Direção visual (proposta)

**Low-poly, crepúsculo escuro, elementos emissivos.** A cena base é dessaturada e escura (pedra azul-cinza, crepúsculo); tudo que é elemento **brilha** (emissivo + bloom) e é o que dá cor à tela. Corrupção é "bonita, mas errada": rachaduras de brasa no chão, cristais âmbar.

| Item | Proposta |
|---|---|
| Câmera | Perspectiva com FOV baixo (~30°), inclinada ~50°, fixa, segue o herói. Sala inteira quase sempre visível. |
| Modelos | Packs **CC0** do mesmo autor pra manter coesão: KayKit (Dungeon Remastered, Adventurers, Skeletons) e Quaternius como reserva. Licenças conferidas na hora de baixar. |
| Animação | As que vêm nos packs + Mixamo se faltar. |
| Luz | 1 direcional de crepúsculo com sombra + luzes pontuais só em braseiros e magias. Ambiente frio, luz quente. |
| Shader | **URP Lit padrão, sem toon e sem contorno** (variação B, escolhida em 11/11 pelos prints). Flash branco no hit. |
| Pós | Bloom, vinheta leve, color grading. Sem motion blur, sem DoF pesado. |
| Linguagem dos elementos | **Fogo** laranja→vermelho, faíscas subindo, triângulos · **Água** ciano→azul, arcos e fitas · **Terra** marrom/oliva, blocos e detritos · **Ar** branco/verde-água, espirais e riscos · **Quintessência** branco prismático |
| UI | Limpa, ícones dos 4 elementos em roda, sem moldura pesada. |

**Alvo visual atual:** `docs/img/lookdev_v1.png` e `docs/img/lookdev_v1_close.png` (comparação A × B em `docs/lookdev.html`). A cena é gerada por `Assets/Editor/LookDevBuilder.cs` (menu Quintessence > Look-dev) e salva em `Assets/Scenes/LookDev/`.

---

## 5. Design resumido

**Controles (desde 11/11):** WASD mover · mouse mira · clique esq. golpe básico · 1 2 3 4 skills elementais · Espaço dash · R Quintessência · E interagir · I inventário · Tab mapa · Esc pausa. Controle suportado (tabela no GDD).

| | Fogo | Água | Terra | Ar |
|---|---|---|---|---|
| Identidade | Agressivo, dano contínuo | Alcance e controle | Pesado, quebra armadura | Mobilidade e crítico |
| Skill | Labareda (cone, Queimadura) | Maré (onda, empurra, Encharcado) | Pilar de Pedra (bloqueia, atordoa) | Vendaval (radial, empurrão) |
| Dash | Deixa rastro de fogo | Limpa status negativos | Curto, dá armadura de 1 hit | 2 cargas |

**Marcas e reações:** o primeiro hit de um elemento marca o inimigo; acertar com outro elemento consome a marca e dispara a reação.

| Par | Reação | Efeito |
|---|---|---|
| Fogo + Ar | Tempestade de Fogo | Explosão em área, Queimadura |
| Fogo + Água | Vapor | Nuvem, inimigos erram |
| Fogo + Terra | Magma | Poça de lava |
| Água + Terra | Lama | Enraíza e depois Encharca |
| Água + Ar | Nevasca | Congela; próximo hit Estilhaça |
| Terra + Ar | Estilhaços | Lascas voam e acertam outros |

**Mudança de 11/11 (pedido do Leonardo):** o golpe básico não tem mais elemento e não existe "elemento atual" nem Golpe de Troca. Só as skills marcam e reagem entre si; o golpe básico bate 15% mais forte em alvo marcado. Cada skill tem um papel: Fogo ataca, Água cura, Terra protege, Ar é a fuga. As tabelas acima de "Identidade" e "Dash" por elemento ficaram como registro do desenho anterior.
**Quintessência:** medidor enche com reações; por 8 s cada hit dispara uma reação. Só com os 4 elementos despertos. Obrigatória pra quebrar o escudo da última fase do chefe.

**Run:** mapa ramificado de ~8 salas. Cada porta mostra a recompensa.
Tipos de sala: Combate (2–3 ondas) · Santuário (desperta elemento) · Mestre (escolhe 1 de 3 bênçãos) · Loja · Descanso · Elite · Chefe (**Guardião das Cinzas**, 3 fases).
**Inimigos:** Rastejante (corpo a corpo), Atirador (projétil), Bruto (armadura, pede Terra), Explosivo (suicida) + variação Elite + Chefe.
**Meta:** Fragmentos ganhos na run (3 por sala vencida, +15 ao vencer o Guardião) compram upgrades permanentes no hub, o **Refúgio** (painel no título; "Santuário" ficou sendo a sala que desperta elemento). Cinco melhorias: Vigor, Força, Bolsa, Foco e Fôlego. Save em `PlayerPrefs`.

---

## 6. Rubricas (rubrica final do professor, recebida em 16/11)

A tabela de 7 rubricas com pesos que propusemos antes ficou obsoleta. A rubrica oficial do Projeto 3D tem **4 frentes com o mesmo peso** e a nota final é a média delas (A = 8,5 a 10 · B = 6 a 8,5 · C = 4 a 6 · D = 0 a 4). O enunciado pede um jogo "simples, porém completo e bem-acabado": daqui até a entrega, acabamento vale mais que sistema novo.

| Frente | O que o professor olha | Dono | Onde estamos |
|---|---|---|---|
| 1. Controles, câmera e sensação 3D | Controles responsivos e claros; câmera que acompanha sem desorientar; entender profundidade, direção e distância | Leonardo | WASD + mouse e controle; câmera 3/4 fixa que segue o herói, mostra a silhueta do herói e dos inimigos atrás de pilares e paredes (o cenário não some) e não mostra o vazio fora da sala; avisos de ataque no chão. Leonardo jogou 3 runs em 16/11: nada travou e a câmera não prendeu |
| 2. Desempenho, fluidez e colisões | Sem travamentos nem quedas de FPS; não prender em quina, não atravessar, não tremer | Enzo | cerca de 360 FPS no WebGL em combate (Chrome, M3 Pro, 99% dos quadros em até 5 ms); no editor (pior caso), Cerco fundo, Elite Divisores e chefe final ficam em 115-119 FPS com 99% dos quadros em até 14 ms (`PerfProbe`); NavMesh nos inimigos; peças em L com um colisor por braço; teste de colisão em todos os arranjos (1101 aproximações, nada atravessa, prende ou treme). Falta medir no Windows |
| 3. Modelos 3D, materiais e texturas | Assets bem importados, escala e orientação certas, materiais coerentes, visual polido | Miguel | Pack KayKit (CC0) e peças próprias modeladas no Blender sobre os corpos do pack. 16/11: anéis, avisos, rachaduras e bloom foscos (estavam com cara de neon) |
| 4. Gerenciamento de estados e erros | Começar, perder, vencer e reiniciar sem ficar preso; recuperação justa | Leonardo | Título, run salva a cada sala (Continuar), pausa com recomeçar (pede confirmação) e voltar ao título, telas de vitória e derrota com "de novo" e "título", save inválido vira run nova; teste de idas e voltas cobre morrer durante a troca de sala, Continuar, recomeçar e treino |

**Interface em qualquer tela (16/11):** a interface se adapta à proporção da janela (`CanvasScaler` em Expand, referência 1920x1080) e os textos longos encolhem para caber na caixa (`UiKit.Fit`). Antes disso, na aba Game do editor, baixa e larga, os textos se sobrepunham.

---

## 7. Divisão do time

Quatro frentes verticais. Cada uma tem a sua pasta/assembly e conversa com as outras por eventos e ScriptableObjects, pra ninguém editar a mesma cena.

| Pessoa | Frente | Entrega |
|---|---|---|
| **Leonardo** | Combate e elementos | Herói, mira, ataque/combo, dash, 4 elementos, marcas, reações, Quintessência, morte/respawn, pausa |
| **Enzo** | Inimigos e salas | IA, 4 inimigos, elite, chefe, ondas, layout das salas, som |
| **Nicolas** | Run, meta e UI | Mapa ramificado, portas e recompensas, bênçãos, loja, hub e upgrades, save, HUD e menus, GDD, lore |
| **Miguel** | Visual e feedback | Look da cena, luz, shaders, pós, VFX de elementos e reações, feedback de hit, telegrafia, câmera/shake |

---

## 8. Kanban

Sprints sem data até confirmar a entrega. S0 é de todos e destrava o resto.

### S0 — Fundação (todos, feito nesta sessão)
- [x] Projeto Unity 6000.6.4f1 URP, .gitignore, estrutura de pastas e assemblies — **Leonardo**
- [x] Import dos packs CC0 + créditos no README — **Miguel**
- [x] Cena de look-dev aprovada pelo time — **Miguel**
- [x] Contratos entre frentes (eventos, `ElementType`, `DamageInfo`, `Health`) — **Leonardo**
- [x] Build WebGL gerada e testada no navegador (18,8 MB); falta subir no itch.io — **Nicolas**
- [x] Rubrica final recebida em 16/11: 4 frentes com o mesmo peso (seção 6)

### Leonardo — Combate e elementos
- [x] S1 · Movimento, mira no mouse, câmera seguindo
- [x] S1 · Ataque básico com combo, hitbox/hurtbox, `Health`, dano
- [x] S1 · Dash com i-frames
- [x] S2 · Fogo e Água completos (modificador, skill, dash)
- [x] S2 · Troca de elemento + Golpe de Troca
- [x] S2 · Marcas + resolvedor de reações (tabela em ScriptableObject)
- [x] S3 · Terra e Ar completos, as 6 reações e status (Queimadura, Encharcado, Congelado, Enraizado, Atordoado, Armadura Quebrada)
- [x] S3 · Quintessência (medidor + estado)
- [x] S4 · Morte, fim de run e pausa (feitos junto com a run do Nicolas)
- [x] S4 · Caça a dead ends e recuperação de erro (resgate de inimigo e herói fora da sala, luta travada, pausa ao perder o foco; um teste provoca cada caso). Falta olhar a build WebGL em si
- [x] S4 · Sala de treino/tutorial que ensina marca → reação (botão Treino no título, sete passos, opcional)

### Enzo — Inimigos e salas
- [x] S1 · Inimigo base: NavMesh, perseguir, telegrafar, atacar, morrer
- [x] S1 · Spawner de ondas + "sala limpa"
- [x] S2 · Rastejante e Atirador
- [x] S2 · 6 layouts de sala de combate com cobertura e perigo (respiradouro de brasa)
- [x] S3 · Bruto (armadura) e Explosivo
- [x] S3 · Elite com modificadores (Velozes, Resistentes, Ferozes; o traço aparece na porta). Os layouts ficaram em seis
- [x] S4 · Chefe Guardião das Cinzas, 3 fases, escudo que pede Quintessência
- [x] S4 · Curva de dificuldade por profundidade da sala (100%→149% de vida, 100%→135% de dano desde 11/11, depois de o Leonardo jogar e achar fácil com 85%→134%)
- [x] S4 · SFX e música (sintetizados por tools/gen_audio.py), volumes na pausa — ouvidos e aprovados pelo Leonardo em 11/11

### Nicolas — Run, meta e UI
- [x] S1 · Fluxo de cenas: título → hub → run → resultado (título → run → resultado; o hub fica no S4)
- [x] S1 · Gerador do mapa ramificado + tela de mapa
- [x] S2 · Portas com ícone de recompensa, transição entre salas (texto no lugar de ícone)
- [x] S2 · HUD: vida, roda de elementos, cooldowns, medidor de Quintessência
- [x] S3 · Santuário (despertar elemento) e Descanso
- [x] S3 · Mestre (3 bênçãos); Elite também paga bênção
- [x] S3 · Loja (poção, elixir e bênçãos por Essência)
- [x] S3 · ~20 bênçãos em ScriptableObject
- [x] S4 · Hub com upgrades permanentes + save em PlayerPrefs (Refúgio: painel no título, não uma sala 3D)
- [x] S4 · Menus: principal, pausa, opções (volume, tela cheia), créditos
- [x] Contínuo · GDD em DDE: rascunho em `docs/GDD.md` (falta colar no Milanote, aprovar a lore proposta e fazer a página do itch)

### Miguel — Visual e feedback
- [x] S1 · Pipeline URP pra WebGL, luz de crepúsculo, pós-processamento (variação B, build WebGL rodando)
- [x] S1 · Flash de hit e destaque de inimigo marcado (sem toon/outline)
- [x] S2 · VFX de Fogo e Água (ataque, skill, dash, marca): brasas que sobem, gotas em arco
- [x] S2 · Feedback de combate: hit stop, shake, números de dano, flash, anel de marca, barra de vida (primeira versão feita junto com o S2 do Leonardo)
- [x] S2 · Telegrafia no chão dos ataques inimigos
- [x] S3 · VFX de Terra e Ar + as 6 reações: lascas de pedra, riscos de vento; a reação solta os dois elementos do par
- [x] S3 · Kit de cenário do Planalto das Cinzas (chão, paredes, tochas, rachaduras de brasa, cristais, seis arranjos de sala)
- [x] S4 · VFX de Quintessência e do chefe (aros do escudo, cinzas, estilhaço, morte; aura dos quatro elementos)
- [x] S4 · Passada de performance WebGL: medidor no F3, 120 fps no Chrome em combate, build de 19,3 MB. Falta medir sala cheia, chefe e máquina fraca

---

### S6 — Rubrica final (16/11 a 25/11)
- [x] Interface que se adapta a qualquer proporção de tela; textos longos encolhem em vez de vazar; carta do título sem o filete por cima do texto; nome do chefe sem letreiro duplicado — **Leonardo**
- [x] Anéis, avisos de ataque, rachaduras e bloom foscos, no tom do pack — **Miguel**
- [x] Build Windows (`BuildTools.BuildWindows`) — **Enzo**
- [x] Teste de colisão em todos os arranjos (1101 aproximações: nada atravessa, prende ou treme) — **Enzo**
- [x] Câmera não mostra o vazio fora da sala; herói e inimigos atrás de uma peça aparecem como silhueta (no lugar de esconder a peça, como o professor sugeriu em 17/11) — **Leonardo**
- [x] Mira no chão (seta nos pés e anel no ponto mirado) e câmera que não mostra o chão de fora na porta sul — **Leonardo** e **Miguel**
- [x] Golpe corpo a corpo sem círculo no chão: animação sincronizada com o dano, corpo que carrega e solta, arma na mão do Rastejante e do Bruto — **Enzo**, **Miguel** e **Leonardo**
- [x] Grimório legível e painéis do título por cima da linha de teclas — **Miguel**
- [x] ESCOPO, GDD e página de progresso com a rubrica final e as plataformas — **Miguel**
- [x] Pausa com Recomeçar run (com confirmação) e resultado em blocos — **Leonardo**
- [x] Opções com Gráficos leves para PC fraco e tabela de Controles — **Enzo**
- [x] Ícone do jogo no .exe, na janela e no navegador — **Miguel**
- [x] Página do jogo no itch.io com as builds Windows e WebGL, texto, capa e capturas: https://souzxxx.itch.io/quintessence — **Leonardo**
- [ ] Inscrever o jogo na jam do itch.io, com como jogar, prints e vídeo — **Leonardo**
- [ ] Rodar a build Windows num PC com Windows e medir FPS numa sala cheia e no chefe — **Enzo**
- [ ] Colar o GDD no Milanote e aprovar a lore marcada como [proposta] — **Nicolas**

### Fila (depois do jogo fechado)

- Loja: "melhorou muito, mas dá pra deixar melhor depois" (Leonardo, 11/11). Pendências conhecidas: estantes somem no fundo, mercador não reage, modelo próprio para ele.
- Refúgio como sala 3D de verdade (hoje é um painel no título).
- [x] Portas, baús de bênção e santuários com modelos do pack (11/11). Falta: estandarte por tipo de sala visível na porta
- [x] Regra de estilo (pedido do Leonardo em 11/11): tudo que criarmos espelha os assets do pack — fosco, cor contida, nada de neon. Passada nos anéis, avisos, rachaduras e bloom em 16/11
- [ ] Personalizar o chefe (hoje é o esqueleto guerreiro do pack ampliado) — pedido do Leonardo em 11/11
- [x] HUD com ícones de verdade e barra de habilidades (11/11). Falta: botões e telas de pausa/resultado
- [x] Menu de título com identidade própria: altar dos cinco, aprendiz, emblema e pedestal que acende com o mouse (11/11)
- [x] Passada única de animação (11/11): combo, skill por elemento, dash direcional, reação a dano, morte; inimigos recuam; Guardião com golpe saltado e rugido. Só clipes do pack
- [x] Heróis do Quintessence 2D (Rax, Liora, Brann, Vey) no 3D (12/11): peças modeladas no Blender sobre os corpos do pack, um herói por elemento inicial, com passiva e golpes próprios. Falta: retoque na crina do Rax e no redemoinho do Vey
- [x] Leva da madrugada de 12/11: nível e 17 talentos; Ceifador, Arremessador, Xamã e Campeão de Elite; barril de pólvora e espinhos; 12 layouts; salas de Tesouro e Pacto; três biomas; run de 10 salas; dificuldade das salas comuns apertada com base numa sonda por roteiro (`BalanceProbe`). Falta: um humano jogar para calibrar

## 9. Riscos

| Risco | Mitigação |
|---|---|
| Performance 3D em WebGL | Build de teste no S0; orçamento de luz e partículas desde o começo |
| Escopo de 4 elementos × (ataque, skill, dash, 6 reações) | Fogo e Água primeiro; se apertar, Terra e Ar ficam sem dash próprio |
| Packs diferentes não combinam | Um autor só + shader e paleta nossos por cima |
| Chefe fica pra última hora | Protótipo do chefe com cápsula no S3 |
