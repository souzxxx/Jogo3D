# Quintessence 3D — GDD (rascunho em DDE)

> Rascunho para colar no Milanote. Segue o framework **DDE** (Design · Dynamics · Experience).
> Tudo que está aqui existe no jogo, salvo o que está marcado como **[planejado]**.
> Os trechos de lore marcados como **[proposta]** foram escritos para fechar o documento e precisam do ok do grupo.

**Grupo:** Leonardo de Souza (combate), Enzo Canavero (inimigos e salas), Nicolas Volf (run, meta e UI), Miguel Damasio (visual e som)
**Plataforma:** navegador (WebGL), teclado e mouse
**Gênero:** roguelite de ação com câmera de cima
**Sessão:** uma run em 10 a 15 minutos

---

## Resumo em uma frase

Você começa a run com um elemento, desperta os outros três pelo caminho e vence quando aprende a combiná-los.

## Pilares

1. **Os elementos são a build.** Trocar de elemento e detonar reações é a habilidade central; bênçãos, inimigos e chefe existem para alimentar isso.
2. **Toda run conta o arco da maestria.** Começa com um elemento e termina com quatro. A ordem em que você desperta muda a run.
3. **Combate curto e legível.** Todo golpe inimigo avisa antes; toda reação mostra o nome e o efeito.

---

# 1. DESIGN

## 1.1 Blueprint (mundo, narrativa, personagens)

### Mundo

Os quatro elementos viviam em equilíbrio. Quando se separaram, cada um passou a corromper a terra onde ficou mais forte. O jogo se passa no **Planalto das Cinzas**, a região tomada pelo Fogo: salas de pedra fria, rachaduras de brasa no chão e cristais âmbar que crescem onde a corrupção é mais antiga.

A corrupção é "bonita, mas errada": ela ilumina e aquece, e por isso ninguém a combateu a tempo.

### O herói

Um aprendiz que domina um único elemento, escolhido pelo jogador no início. Sozinho, um elemento só reforça o desequilíbrio. O caminho dele é despertar os outros três nos **Santuários** e aprender a usá-los juntos.

**[planejado]** trocar o mago do pack pelos quatro heróis do Quintessence 2D (Rax, Liora, Brann e Vey), um por elemento.

### Quintessência

O quinto estado: o que os elementos eram antes de se separarem. Quem carrega os quatro e os faz reagir o bastante entra nele por alguns segundos, e nesse tempo todo golpe dispara uma reação. É a única coisa que quebra o escudo do Guardião.

### O Guardião das Cinzas

**[proposta]** Era o protetor do planalto. Quando o Fogo se separou, ele tentou contê-lo sozinho, com um elemento só, e foi consumido. Hoje guarda a saída da região e chama os mortos do planalto para lutar por ele. Derrotá-lo exige o que ele não teve: os quatro elementos juntos.

### Os Mestres e o Mercador

**[proposta]** Os Mestres são os que restaram de cada tradição elemental. Não lutam mais, mas deixam bênçãos em baús para quem passa. Um Mestre só oferece a bênção do próprio elemento a quem já o despertou. O Mercador troca Essência, o que sobra dos corrompidos derrotados, por poções e bênçãos.

### Estrutura narrativa

Não há diálogo. A história é contada pela estrutura da run: um elemento → Santuários → quatro elementos → Quintessência → Guardião. O jogador repete o arco da maestria a cada tentativa.

## 1.2 Mechanics (regras)

### Controles

| Ação | Tecla |
|---|---|
| Mover | W A S D |
| Mirar | mouse |
| Atacar (combo de 3 golpes) | clique esquerdo |
| Skill do elemento | clique direito |
| Dash | Espaço |
| Trocar de elemento | Q / E |
| Quintessência | R |
| Interagir (portas, baús, loja) | F |
| Mapa | Tab (segurar) |
| Pausa | Esc |

### Os quatro elementos

| | Fogo | Água | Terra | Ar |
|---|---|---|---|---|
| Identidade | Agressivo, dano contínuo | Alcance e controle | Pesado, quebra armadura | Mobilidade e crítico |
| Skill | Labareda: cone, Queimadura | Maré: onda que empurra e Encharca | Pilar de Pedra: bloqueia e atordoa | Vendaval: radial, empurra |
| Dash | Deixa rastro de fogo | Limpa status negativos | Curto, bloqueia 1 golpe | 2 cargas |

### Marca e reação

O golpe de um elemento deixa a **marca** dele no inimigo. Acertar esse inimigo com **outro** elemento consome a marca e dispara a **reação** do par.

| Par | Reação | Efeito |
|---|---|---|
| Fogo + Ar | Tempestade de Fogo | Explosão em área, Queimadura |
| Fogo + Água | Vapor | Nuvem: inimigos dentro erram golpes |
| Fogo + Terra | Magma | Poça de lava que fere quem pisa |
| Água + Terra | Lama | Enraíza e depois Encharca |
| Água + Ar | Nevasca | Congela; o próximo golpe Estilhaça (+30%) |
| Terra + Ar | Estilhaços | Dano em área com empurrão forte |

**Golpe de Troca:** trocar de elemento logo depois de acertar dá bônus no golpe seguinte. Recompensa quem troca no ritmo da luta.

**Quintessência:** reações enchem o medidor. Com os quatro elementos despertos e o medidor cheio, R ativa 8 segundos em que cada golpe dispara uma reação e o herói leva metade do dano.

### A run

- Mapa ramificado de 8 camadas. Depois de cada sala, de uma a três portas mostram o tipo da próxima sala e a recompensa.
- **Combate:** 2 ondas. **Elite:** 3 ondas e um traço (Velozes, Resistentes ou Ferozes) anunciado na porta; paga o dobro de Essência e uma bênção.
- **Santuário:** desperta um elemento à escolha. **Mestre:** uma de três bênçãos. **Loja:** poção, elixir e bênçãos por Essência. **Descanso:** cura 30%.
- **Chefe:** o Guardião das Cinzas, em três fases. Na última ele levanta um escudo que só a Quintessência quebra.
- Seis arranjos de sala com cobertura e um respiradouro de brasa que fere quem pisa, herói ou inimigo.

### Inimigos

| Inimigo | Papel | Como lidar |
|---|---|---|
| Rastejante | Corpo a corpo, vem em grupo | Área e reações |
| Atirador | Projétil em linha, recua se você chega perto | Dash para fechar a distância |
| Bruto | Armadura: leva 40% do dano | O finalizador de Terra quebra a armadura |
| Explosivo | Corre, avisa um círculo e explode | Sair do círculo; a explosão também fere os outros inimigos |

Vida e dano sobem a cada camada (de 85% para 134% de vida, de 80% para 115% de dano). O tempo de aviso dos golpes nunca diminui.

### Progressão

- **Na run:** Essência (moeda da loja) e bênçãos (20 no catálogo; cada Mestre só oferece a do próprio elemento a quem já o despertou).
- **Entre runs:** Fragmentos, 3 por sala vencida e 15 por vencer o Guardião. No **Refúgio** compram cinco melhorias permanentes: Vigor (vida), Força (dano), Bolsa (Essência inicial), Foco (Quintessência) e Fôlego (cura por sala). O progresso é salvo no navegador.

### Estados e erros

- Morrer leva ao resultado com "Jogar de novo" e "Menu principal". Vencer o Guardião também.
- Esc pausa em qualquer sala. Perder o foco da janela pausa sozinho.
- Nenhuma sala prende o jogador: Santuário sem elemento para despertar e Mestre sem bênção sobrando abrem as portas; a Loja já entra com as portas abertas.
- Inimigo que sai da sala volta sozinho; luta parada por 25 segundos traz os inimigos para perto do herói.
- Save vazio ou corrompido vira progresso zerado, sem erro na tela.

## 1.3 Interface (o que o jogador vê e ouve)

### Visual

- **Low-poly fosco** com os packs KayKit (CC0). O que criamos por código segue o pack: cor contida, sem neon.
- Cena fria e escura; a cor vem dos elementos e das tochas. Câmera fixa de cima, inclinada, seguindo o herói.
- **Cores dos elementos:** Fogo laranja-avermelhado, Água azul, Terra ocre, Ar verde-água claro, Quintessência branco.

### HUD

- Barra de habilidades embaixo: os quatro elementos (cadeado nos que faltam), skill, dash e Quintessência, cada um com a tecla.
- Vida à esquerda, sala atual e Essência no topo, medalhas das bênçãos com o ícone do efeito.
- Barra do chefe com as fases. Mapa da run no Tab.

### Feedback de combate

- Inimigo pisca branco ao levar dano, número de dano sobe e empilha, pausa curta de impacto nos golpes fortes, tremor de câmera.
- Aviso no chão antes de todo golpe inimigo: círculo para área, linha para projétil.
- Reação mostra o nome por cima do inimigo, com anel e luz na cor do par.

### Som

34 efeitos e 2 músicas, todos sintetizados pelo grupo (`tools/gen_audio.py`). Volume de música e de efeitos na pausa.

### Menus

Título com escolha do elemento inicial, Refúgio e Treino. Pausa com volumes e lista de bênçãos. Resultado com o resumo da run e os Fragmentos ganhos.

---

# 2. DYNAMICS (o que acontece quando as regras encontram o jogador)

- **Marcar, trocar, detonar.** A regra da marca transforma a troca de elemento no ritmo da luta: bater, trocar, bater. Ficar num elemento só funciona, mas é mais lento e não enche a Quintessência.
- **A run começa estreita e abre.** Com um elemento não há reação; o primeiro Santuário muda o jogo. Isso faz o jogador querer o caminho do Santuário cedo, e a porta mostra onde ele está.
- **Escolher porta é escolher risco.** Elite paga o dobro e uma bênção, mas o traço está escrito na porta. Loja só vale com Essência no bolso. Descanso é a saída de quem está quase morto.
- **Os inimigos pedem elementos diferentes.** Bruto pede Terra; grupo de Rastejantes pede área (Tempestade de Fogo, Estilhaços); Atirador pede mobilidade (Ar). Quem despertou o elemento "errado" primeiro precisa se virar com reações.
- **Explosivo vira ferramenta.** A explosão fere os outros inimigos: atrair o Explosivo para o meio do grupo é uma jogada.
- **O chefe cobra o arco inteiro.** Sem os quatro elementos não há Quintessência, e sem ela o escudo da última fase não cai. O jogador aprende a guardar o medidor para o momento certo.
- **A derrota rende.** Toda run paga Fragmentos, então morrer na quarta sala já compra um nível no Refúgio. A próxima tentativa começa um pouco mais forte.

---

# 3. EXPERIENCE (o que queremos que o jogador sinta)

| Momento | O que o jogador faz | O que queremos que ele sinta |
|---|---|---|
| Primeiro minuto | Bate com um elemento, vê a marca | "Entendi o básico" |
| Primeiro Santuário | Desperta o segundo elemento e detona a primeira reação | Descoberta: "então é assim que funciona" |
| Meio da run | Troca de elemento de propósito para o inimigo da vez | Maestria: está escolhendo, não apertando botão |
| Primeira Quintessência | Aperta R com os quatro elementos | Poder: a tela inteira reage |
| Guardião, última fase | Guarda o medidor e quebra o escudo | Tensão e alívio |
| Tela de resultado | Vê os Fragmentos e volta ao Refúgio | "Mais uma" |

**Sentidos:** impacto nos golpes (flash, tremor, pausa), cor só onde importa, som seco e curto.
**Emoção:** tensão nas salas de Elite e no chefe, alívio no Descanso e na Loja.
**Razão:** ler o aviso no chão, escolher a porta, decidir qual elemento despertar e quando gastar a Quintessência.

---

## Créditos de assets

| Asset | Autor | Licença |
|---|---|---|
| Modelos e animações (Dungeon, Adventurers, Skeletons) | KayKit (Kay Lousberg) | CC0 |
| Ícones | game-icons.net (Lorc, Delapouite e outros) | CC BY 3.0 |
| Fontes Lilita One, Titillium Web, Cinzel Decorative | Google Fonts | OFL |
| Efeitos sonoros e música | sintetizados pelo grupo | próprios |

## O que falta

- **[planejado]** Chefe com modelo e identidade próprios (hoje é um esqueleto do pack ampliado).
- **[planejado]** Heróis do Quintessence 2D no lugar do mago do pack.
- **[planejado]** Passada única de animação.
- **[planejado]** Refúgio como sala 3D (hoje é um painel no título).
