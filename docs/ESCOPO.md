# Quintessence 3D — Documento de Escopo (RASCUNHO v0)

> Projeto 3D · Jogos Digitais e Interação · Insper
> Equipe: **Leonardo** (souzxxx), **Nicolas** (NicolasVolf), **Enzo** (canavero), **Miguel** (EuMiguelll)
> Entrega: **A CONFIRMAR** — assumindo o mesmo formato do 2D: build WebGL no itch.io + GitHub + GDD no Milanote + Blackboard
> Status: rascunho para aprovação. Nada de código antes de fechar este documento.

Itens marcados com **[DECIDIR]** dependem do time ou do professor.

---

## 1. Pitch

**Os quatro elementos se separaram e estão corrompendo o mundo.** Você é um aprendiz que começa a run dominando **um** elemento. Sala por sala, você desperta os outros três em santuários, troca de elemento no meio do combo e encadeia **reações elementais** (Fogo + Ar = Tempestade de Fogo, Água + Ar = Nevasca...). Quem domina os quatro entra em **Quintessência**, o quinto estado, em que cada golpe é todos os elementos ao mesmo tempo, e só assim se quebra o escudo do Guardião corrompido.

É a versão 3D do nosso projeto pessoal *Quintessence* (2D, Godot): mesmo mundo e mesmo sistema de elementos, refeito do zero em Unity 3D. Mundo, nomes e personagens são **originais** (inspiração em jogos de roguelite cooperativo de salas e em fantasia elemental, sem usar IP de ninguém).

**Gênero:** action roguelite 3D, câmera top-down 3/4 fixa
**Plataforma:** WebGL (itch.io) · **Engine:** Unity 6000.6.4f1, URP · **Idioma:** PT-BR
**Sessão:** uma run completa em 10–15 min; 1–2 h até a primeira vitória

### Pilares
1. **Os elementos são a build.** Trocar de elemento e detonar reações é a habilidade central. Todo sistema (bênçãos, inimigos, chefe) alimenta isso.
2. **Toda run conta o arco da maestria.** Começa com um elemento, termina com quatro. A ordem em que você desperta muda a run.
3. **Combate curto, legível e gostoso.** Telegrafia clara, animações curtas, impacto forte. Se não for bom com cápsulas cinzas, arte não salva.

---

## 2. O que é sucesso

- **Nota A+**: 7 rubricas A incluindo as de peso 2, no máximo 1 C, nenhum D.
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
| Build **WebGL** | URP com orçamento apertado: poucas luzes em tempo real, sombras só da luz principal, sem compute shader, texturas comprimidas, build < ~80 MB. Save em `PlayerPrefs`. Sem botão "Sair". |
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

**Controles:** WASD mover · mouse mira · clique esq. ataque · clique dir. skill · Espaço dash · Q/E troca elemento · R Quintessência · F interagir · Esc pausa.

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

**Golpe de Troca:** trocar de elemento logo depois de acertar dá +50% no próximo golpe. É o gancho de habilidade.
**Quintessência:** medidor enche com reações; por 8 s cada hit dispara uma reação. Só com os 4 elementos despertos. Obrigatória pra quebrar o escudo da última fase do chefe.

**Run:** mapa ramificado de ~8 salas. Cada porta mostra a recompensa.
Tipos de sala: Combate (2–3 ondas) · Santuário (desperta elemento) · Mestre (escolhe 1 de 3 bênçãos) · Loja · Descanso · Elite · Chefe (**Guardião das Cinzas**, 3 fases).
**Inimigos:** Rastejante (corpo a corpo), Atirador (projétil), Bruto (armadura, pede Terra), Explosivo (suicida) + variação Elite + Chefe.
**Meta:** Fragmentos de Espírito ganhos na run compram upgrades permanentes no hub (**Santuário**).

---

## 6. Rubricas — estratégia [DECIDIR com o professor]

| Rubrica | Peso proposto | Dono |
|---|---|---|
| Blueprint (Conceito e Narrativa) | 1 | Nicolas |
| Mecânicas | **2** | Leonardo |
| Feedback e Clareza de Interação | 1 | Miguel |
| UI/HUD e Menus | 1 | Nicolas |
| Engajamento e Imersão | 1 | Leonardo |
| Level design e Progressão | 1 | Enzo |
| Design de Som e Música | **descartada** | Enzo |
| Efeitos Visuais e Polimento | **2** | Miguel |
| Replayability | 1 | Nicolas |
| Balanceamento | **descartada** | Enzo |
| Estados e Erros (obrigatória) | 1 | Leonardo |
| GDD em DDE (obrigatória) | 1 | Nicolas |

**Pendente:** pesos e descartes só fecham na conversa com o professor; a tabela acima é a proposta que levamos.

Raciocínio: o jogo **é** mecânica de elementos + efeito visual de elementos, então dobramos as duas. Som e Balanceamento são onde o A é mais caro pra um action 3D em pouco tempo; mesmo descartadas, ficam em nível B (ninguém entrega jogo mudo).

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
- [ ] Build WebGL vazia rodando no itch (prova o pipeline cedo) — **Nicolas**
- [ ] Validar pesos da rubrica com o professor — **Nicolas**

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
- [ ] S4 · Caça a dead ends e recuperação de erro (rubrica Estados e Erros)
- [ ] S4 · Sala de treino/tutorial que ensina marca → reação

### Enzo — Inimigos e salas
- [x] S1 · Inimigo base: NavMesh, perseguir, telegrafar, atacar, morrer
- [x] S1 · Spawner de ondas + "sala limpa"
- [x] S2 · Rastejante e Atirador
- [ ] S2 · 4 layouts de sala de combate com cobertura e perigos
- [x] S3 · Bruto (armadura) e Explosivo
- [ ] S3 · Elite (modificadores) + mais 4 layouts
- [x] S4 · Chefe Guardião das Cinzas, 3 fases, escudo que pede Quintessência
- [ ] S4 · Curva de dificuldade por profundidade da sala
- [ ] S4 · SFX e música (biblioteca livre), mixer com volumes

### Nicolas — Run, meta e UI
- [x] S1 · Fluxo de cenas: título → hub → run → resultado (título → run → resultado; o hub fica no S4)
- [x] S1 · Gerador do mapa ramificado + tela de mapa
- [x] S2 · Portas com ícone de recompensa, transição entre salas (texto no lugar de ícone)
- [x] S2 · HUD: vida, roda de elementos, cooldowns, medidor de Quintessência
- [x] S3 · Santuário (despertar elemento) e Descanso
- [x] S3 · Mestre (3 bênçãos); Elite também paga bênção
- [ ] S3 · Loja (dar uso à Essência)
- [x] S3 · ~20 bênçãos em ScriptableObject
- [ ] S4 · Hub com upgrades permanentes + save em PlayerPrefs
- [ ] S4 · Menus: principal, pausa, opções (volume, tela cheia), créditos
- [ ] Contínuo · GDD em DDE no Milanote, lore do mundo e do Guardião, página do itch

### Miguel — Visual e feedback
- [ ] S1 · Pipeline URP pra WebGL, luz de crepúsculo, pós-processamento
- [ ] S1 · Flash de hit e destaque de inimigo marcado (sem toon/outline)
- [ ] S2 · VFX de Fogo e Água (ataque, skill, dash, marca)
- [x] S2 · Feedback de combate: hit stop, shake, números de dano, flash, anel de marca, barra de vida (primeira versão feita junto com o S2 do Leonardo)
- [x] S2 · Telegrafia no chão dos ataques inimigos
- [ ] S3 · VFX de Terra e Ar + as 6 reações
- [ ] S3 · Kit de cenário do Planalto das Cinzas (chão, paredes, braseiros, rachaduras de brasa, cristais)
- [ ] S4 · VFX de Quintessência e do chefe
- [ ] S4 · Passada de performance WebGL (draw calls, tamanho da build)

---

### Fila (depois do jogo fechado)
- [x] Portas, baús de bênção e santuários com modelos do pack (11/11). Falta: estandarte por tipo de sala visível na porta
- [ ] Regra de estilo (pedido do Leonardo em 11/11): tudo que criarmos espelha os assets do pack — fosco, cor contida, nada de neon
- [ ] Menu de título com identidade própria (hoje está genérico) — pedido do Leonardo em 11/11
- [ ] Passada única de animação: trocar as animações genéricas do pack (ataques, skills, dash, inimigos) — combinado em 11/11 deixar pro fim
- [ ] Trazer os heróis do Quintessence 2D (Rax, Liora, Brann, Vey) para o 3D no lugar do mago do pack — pedido do Leonardo em 11/11

## 9. Riscos

| Risco | Mitigação |
|---|---|
| Performance 3D em WebGL | Build de teste no S0; orçamento de luz e partículas desde o começo |
| Escopo de 4 elementos × (ataque, skill, dash, 6 reações) | Fogo e Água primeiro; se apertar, Terra e Ar ficam sem dash próprio |
| Packs diferentes não combinam | Um autor só + shader e paleta nossos por cima |
| Chefe fica pra última hora | Protótipo do chefe com cápsula no S3 |
