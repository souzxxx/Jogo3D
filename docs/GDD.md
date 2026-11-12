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

Você começa a run com uma skill elemental, desperta as outras três pelo caminho e vence quando aprende a combiná-las.

## Pilares

1. **Os elementos são a build.** Combinar as skills elementais para detonar reações é a habilidade central; bênçãos, inimigos e chefe existem para alimentar isso.
2. **Toda run conta o arco da maestria.** Começa com um elemento e termina com quatro. A ordem em que você desperta muda a run.
3. **Combate curto e legível.** Todo golpe inimigo avisa antes; toda reação mostra o nome e o efeito.

---

# 1. DESIGN

## 1.1 Blueprint (mundo, narrativa, personagens)

### Mundo

Os quatro elementos viviam em equilíbrio. Quando se separaram, cada um passou a corromper a terra onde ficou mais forte. O jogo se passa no **Planalto das Cinzas**, a região tomada pelo Fogo: salas de pedra fria, rachaduras de brasa no chão e cristais âmbar que crescem onde a corrupção é mais antiga.

A corrupção é "bonita, mas errada": ela ilumina e aquece, e por isso ninguém a combateu a tempo.

### O herói

Quatro aprendizes, cada um nascido de um elemento. O jogador escolhe um no início. Sozinho, um elemento só reforça o desequilíbrio: o caminho é despertar os outros três nos **Santuários** e aprender a usá-los juntos.

| Herói | Quem é | Elemento e primeira skill | Passiva |
|---|---|---|---|
| **Rax** | Felino caçador, garras | Fogo · Labareda | Garras do Caçador: combo básico +15% |
| **Liora** | Espírito cervo-raposa, orbe d'água | Água · Maré | Seiva da Fonte: cura 8% ao limpar a sala; a Maré cura mais 3% |
| **Brann** | Texugo veterano, martelo de pedra | Terra · Pilar de Pedra | Couro Velho: +20 de vida máxima |
| **Vey** | Espírito do vento encapuzado, sem pernas, duas adagas | Ar · Vendaval | Passo de Brisa: +10% de velocidade, dash 20% mais rápido, 20% de esquiva e +10 de vida; cada esquiva (da passiva ou perfeita) cura 3% |

São os heróis do Quintessence 2D. No 3D usam os corpos e as animações do pack com cabeça, cauda e arma próprias. A parte mágica de cada um (crina, orbe, rachaduras do martelo, olhos e debrum) assume a cor da última skill usada.

### Quintessência

**Fusões** (três elementos): Magma, Vapor e Lama deixam área no chão; a skill de um terceiro elemento sobre ela vira Obsidiana (Magma + Água), Incêndio (Magma + Ar), Ciclone (Vapor + Ar), Gêiser (Vapor + Terra), Barro Cozido (Lama + Fogo: quebra a armadura) ou Tempestade de Poeira (Lama + Ar: cega numa área grande).

**Esquiva perfeita:** atravessar um golpe com o dash (o herói fica invulnerável durante ele) enche 8% da Quintessência e deixa o próximo golpe básico crítico. Aparece "ESQUIVA PERFEITA" sobre o herói.

O quinto estado: o que os elementos eram antes de se separarem. Quem carrega os quatro e os faz reagir o bastante entra nele por alguns segundos, e nesse tempo todo golpe dispara uma reação. É a única coisa que quebra o escudo do Guardião. Ao entrar nele, cada herói solta o seu **despertar**: Rax, a Erupção do Caçador (queima tudo em volta); Liora, a Maré da Fonte (encharca e cura 15% da vida); Brann, o Rugido da Montanha (atordoa); Vey, o Olho do Vendaval (empurra longe e cega).

### O Guardião das Cinzas

**[proposta]** Era o protetor do planalto. Quando o Fogo se separou, ele tentou contê-lo sozinho, com um elemento só, e foi consumido. Hoje guarda a saída da região e chama os mortos do planalto para lutar por ele. Derrotá-lo exige o que ele não teve: os quatro elementos juntos.

### Os Mestres e o Mercador

**[proposta]** Os Mestres são os que restaram de cada tradição elemental. Não lutam mais, mas deixam bênçãos em baús para quem passa. Um Mestre só oferece a bênção do próprio elemento a quem já o despertou. O Mercador troca Essência, o que sobra dos corrompidos derrotados, por poções e bênçãos.

### Estrutura narrativa

Não há diálogo. A história é contada pela estrutura da run: um elemento → Santuários → quatro elementos → Quintessência → Guardião. O jogador repete o arco da maestria a cada tentativa.

## 1.2 Mechanics (regras)

### Controles

| Ação | Teclado e mouse | Controle |
|---|---|---|
| Mover | W A S D | analógico esquerdo |
| Mirar | mouse | analógico direito (sem ele, mira para onde anda) |
| Golpe básico (combo de 3) | clique esquerdo | RT ou X |
| Skill de Fogo / Água / Terra / Ar | 1 / 2 / 3 / 4 | direcional: cima / direita / baixo / esquerda |
| Primeira skill pronta | clique direito | LT |
| Dash | Espaço | A |
| Quintessência | R | Y |
| Interagir (portas, baús, loja) | E | B |
| Inventário | I | Select |
| Mapa | Tab (segurar) | clique do analógico direito |
| Pausa | Esc | Start |

### Golpe básico e skills

O **golpe básico não tem elemento**: dá dano, empurra e fecha o combo, mas não marca nem reage. As **skills elementais** são o que o herói desperta ao longo da run, uma por elemento, cada uma numa tecla e com a sua recarga. Além do dano, cada skill tem um papel:

| | Fogo (1) | Água (2) | Terra (3) | Ar (4) |
|---|---|---|---|---|
| Skill | Labareda | Maré | Pilar de Pedra | Vendaval |
| Forma | cone à frente | onda que empurra | pilar invocado | rajada em volta |
| No inimigo | Queimadura | Encharcado | Atordoa e quebra armadura | Empurra |
| No herói | — (é o ataque) | cura 8% da vida e limpa status | bloqueia o próximo golpe por 3 s | recarrega o dash (é a fuga) |

### Marca e reação

Uma skill deixa no inimigo a **marca** do elemento dela. Acertar esse inimigo com a skill de **outro** elemento consome a marca e dispara a **reação** do par. O golpe básico não mexe na marca, mas bate 15% mais forte em quem está marcado.

| Par | Reação | Efeito |
|---|---|---|
| Fogo + Ar | Tempestade de Fogo | Explosão em área, Queimadura |
| Fogo + Água | Vapor | Nuvem: inimigos dentro erram golpes |
| Fogo + Terra | Magma | Poça de lava que fere quem pisa |
| Água + Terra | Lama | Enraíza e depois Encharca |
| Água + Ar | Nevasca | Congela; o próximo golpe Estilhaça (+30%) |
| Terra + Ar | Estilhaços | Dano em área com empurrão forte |

**Quintessência:** reações enchem o medidor. Com os quatro elementos despertos e o medidor cheio, R ativa 8 segundos em que até o golpe básico dispara uma reação e o herói leva metade do dano.

**Fusões.** Magma e Vapor deixam área no chão. A skill de um terceiro elemento lançada sobre essa área vira uma fusão, que conta como reação: Obsidiana (Magma + Água, atordoa), Incêndio (Magma + Ar, espalha o fogo e queima), Ciclone (Vapor + Ar, puxa para o centro e cega) e Gêiser (Vapor + Terra, arremessa para fora e encharca).

### A run

- Mapa ramificado de 10 camadas. Depois de cada sala, de uma a três portas mostram o tipo da próxima sala e a recompensa.
- A descida atravessa três trechos, cada um com luz, cor da pedra e da corrupção, adereços e mistura de inimigos próprios: **Salões de Cinza** (forja: braseiros, bigornas, vigas queimadas), **Cripta Afogada** (sarcófagos, urnas, água parada; mais inimigos de longe) e **Jardim de Pedra** (árvores petrificadas, cogumelos, cervo de pedra; mais inimigos pesados). A última sala é o **Coração das Cinzas** (obelisco de brasa e cristais âmbar).
- **Combate:** 2 ondas (3 da sexta sala em diante). **Elite:** 3 ondas, um traço anunciado na porta (Velozes, Resistentes, Ferozes, Voláteis, que explodem ao morrer, Protegidos, que bloqueiam o primeiro golpe, ou Divisores, que ao cair viram dois Cinzentos) e um **Campeão** na última onda (da sexta sala em diante, no lugar dele vem um mini-chefe em três fases sem escudo, sorteado por sala e anunciado na porta: o **Carrasco**, a **Sacerdotisa Afogada**, uma Rainha menor com poças, ou o **Colosso de Pedra**, elemental de Terra de machado e escudo); paga o dobro de Essência e uma bênção.
- **Santuário:** desperta um elemento à escolha ou **aprofunda uma skill que já tem** (graus II e III: +30% de dano e recarga 10% menor cada). É a escolha entre espalhar (mais reações) e concentrar. **Mestre:** uma de três bênçãos. **Loja:** poção, elixir e bênçãos por Essência. **Descanso:** cura 30%.
- **Cerco:** as ondas de uma sala comum se repetem sem parar por 40 segundos; quando o tempo acaba, quem sobrou desmorona e a sala paga uma bênção e mais Essência. Aparece da quarta camada em diante.
- **Provação:** sala de luta comum com um desafio sorteado no letreiro: **Contra o relógio** (limpar em 60 s com duas ondas, 84 s com três), **Ileso** (tomar no máximo três golpes; golpes no mesmo instante contam um) ou **Pés no Chão** (limpar sem usar o dash). Cumprida, dá um talento extra e 15 de Essência; perdida, a sala segue normal e só o prêmio some. Aparece da terceira camada em diante, no máximo uma por camada.
- **Tesouro:** um baú com Essência e um gole de vida. **Pacto:** três pactos sorteados entre cinco, cada um com um ganho forte e um custo permanente: Sangue (+25% de dano, −20% de vida máxima), Vidro (skills 25% mais rápidas, recebe +25% de dano), Ganância (toda a Essência por um talento), Eco (reações +30%, −15% de vida máxima) e Pressa (mais velocidade e dash, recebe +15% de dano). Dá para recusar.
- **Chefe:** cada run sorteia um de três. A **Rainha Afogada**, senhora da Cripta, anda devagar até o herói: Erupções que deixam poças d'água (encharcam o herói, marcam os reforços para reações), leque de orbes, pancada e, da segunda fase, teletransporte; é elemental de Água (fraca a Terra) e chama Afogados. A **Tecelã das Cinzas**, a antiga mestra do santuário, luta de longe: leques de brasa, erupções, anéis e teletransporte, mantendo distância. O **Guardião das Cinzas**, em três fases, com cinco ataques avisados: Pancada, Leque de brasas, Erupção, Investida (da segunda fase) e Anel de Cinzas com faixa segura (da terceira). Na última fase, os dois levantam um escudo que só a Quintessência quebra.
- **Cristal de Ressonância:** um por sala de luta da segunda em diante (dois do meio da run). Um golpe estoura o cristal: ele fere pouco, aplica o efeito do trecho (queima, encharca ou enraíza) e marca com o elemento do trecho os inimigos em volta; uma skill de outro elemento logo depois detona reações no bando inteiro.
- A sala da run tem 28 x 20 m e a câmera acompanha o herói. Trinta e um arranjos de sala: oito são de um trecho só e aparecem nele com o triplo de chance (Forja e Arsenal nos Salões; Cisterna, Canais e Ossário na Cripta; Bosque de Pedra, Pedreira e Clareira no Jardim), os outros vinte e três valem em qualquer trecho, alguns com estrutura feita de barreiras baixas (corredores, fortim, sala em L, galeria) ou com a mobília do pack (o Refeitório, de mesas longas e barris), e quatro perigos que ferem herói e inimigo: respiradouro de brasa, armadilha de espinhos, poça de corrupção (queima; na Cripta, encharca; e marca os inimigos dentro com o elemento do trecho, Fogo nos Salões, Água na Cripta e Terra no Jardim, sem trocar a marca que o herói pôs: uma skill de outro elemento detona a reação, então até um herói de um elemento só reage atraindo o bando para a poça) e barril de pólvora (qualquer golpe acende; fere bem mais os inimigos e acende os barris vizinhos).

### Inimigos

| Inimigo | Papel | Como lidar |
|---|---|---|
| Rastejante | Corpo a corpo, vem em grupo | Área e reações |
| Atirador | Projétil em linha, recua se você chega perto | Dash para fechar a distância |
| Bruto | Armadura: leva metade do dano | O Pilar de Pedra quebra a armadura |
| Explosivo | Corre, avisa um círculo e explode | Sair do círculo; a explosão também fere os outros inimigos |
| Ceifador | Avisa uma linha larga e atravessa a sala por ela | Sair da linha e bater enquanto ele se recupera |
| Arremessador | Joga uma bomba onde o herói está | Sair do círculo avisado |
| Xamã | Cura os aliados feridos em volta (aviso verde); sozinho, atira | Derrubar primeiro |
| Espectro | Avisa um círculo nas costas do herói e aparece ali para golpear | Não ficar parado: sair do círculo e bater enquanto ele se recompõe |
| Porta-Estandarte | Fica atrás do bando e atira de longe; enquanto vive, quem está no círculo carmim dele bate 25% mais forte e anda 15% mais rápido | Derrubar primeiro: o bônus some na hora |
| Escudeiro | Escudo na frente: golpe direto de frente leva 15%. Gira devagar e não recua quando o golpe bate no escudo | Contornar (ou dash) e bater pelas costas; reações, queimadura e zonas atravessam; atordoado, congelado, enraizado ou com armadura quebrada, o escudo cai |
| Cinzentos | Bando de três ou quatro, pequenos e rápidos | Skills de área |
| Brasa Viva | Elemental de Fogo (Salões de Cinza): não queima, estoura ao morrer | Água, e sair do círculo depois |
| Sentinela de Pedra | Torre parada de Terra (Jardim de Pedra), não é empurrada | Ar, ou fechar a distância |
| Afogado | Elemental de Água (Cripta Afogada): joga um orbe que deixa uma poça; a poça encharca o herói e marca com Água os inimigos dentro | Terra; e usar a poça para reagir (Fogo vira Vapor, Ar vira Nevasca) |

**Afinidade elemental:** inimigo elemental leva metade do dano do próprio elemento e 1,6 vez do elemento que o vence (Água vence Fogo, Terra vence Água, Ar vence Terra, Fogo vence Ar).

Os tipos entram aos poucos: Ceifador na segunda sala, Cinzentos na terceira, Arremessador e Escudeiro na quarta, Xamã e Porta-Estandarte na quinta, Espectro na sexta. Vida e dano sobem a cada camada (9% de vida e 6,5% de dano por camada). As duas primeiras salas são aquecimento: o inimigo bate 75% e depois 87% do normal. O tempo de aviso dos golpes nunca diminui.

### Progressão

- **Na run:** Essência (moeda da loja) e bênçãos (29 no catálogo; cada Mestre só oferece a do próprio elemento a quem já o despertou).
- **Nível e talentos:** inimigo derrotado dá experiência. Cada nível oferece três talentos para escolher um, com a sala limpa. São 33: onze de corpo (combo, finalizador, vida, recarga por abate, Essência, Flanqueador para golpes pelas costas, Pés Firmes contra a lentidão do encharcado, Despertar Intenso, que só aparece com três elementos, Contágio, em que a marca de quem cai pula para o vizinho, Alquimia, que fortalece as Fusões, e Cadência, em que a skill de outro elemento até 3 s depois da anterior causa +30%, com o aviso "CADÊNCIA" sobre o herói), quatro de assinatura, um por herói e só para ele (Rax: Instinto do Caçador, +40% em quem tem menos de 30% de vida; Liora: Maré Crescente, cada reação cura; Brann: Pele de Pedra, +25 de vida e mais um bloqueio do Pilar; Vey: Vento de Cauda, dash mais rápido e +10% de velocidade), três por skill, que só aparecem com o elemento desperto, e seis de reação, que fortalecem uma reação quando os dois elementos dela estão despertos. Exemplos: Labareda deixa o chão em chamas; Maré congela; Pilar bloqueia mais um golpe e devolve dano; Vendaval dá pressa; +1 carga de dash. Bênçãos mudam números; talentos mudam o que as skills fazem.
- **Entre runs:** Fragmentos, 3 por sala vencida e 15 por vencer o Guardião. No **Refúgio** compram oito melhorias permanentes: Vigor (vida), Força (dano), Bolsa (Essência inicial), Foco (Quintessência), Fôlego (cura por sala), Sabedoria (experiência), Intuição (trocar as opções de talento) e Clarividência (4 opções em cada escolha de talento). O progresso é salvo no navegador.
- **Continuar:** a run é gravada ao entrar em cada sala. Fechar o jogo e voltar retoma do começo daquela sala. Começar uma run nova ou terminá-la apaga o save.
- **Ascensão:** cada vitória libera um nível de desafio (até cinco). Cada nível dá +12% de vida e +10% de dano aos inimigos e +25% de Fragmentos.
- **Sintonia:** concentrar talentos de um elemento (os da skill dele e os de reação que o envolvem) fortalece aquele elemento: com 3 pontos a skill causa +20%; com 5, também recarrega 15% mais rápido. O cartão do talento mostra quantos pontos ele soma; é o motivo para especializar o build.
- **Feitos:** dez metas permanentes, cada uma paga 10 Fragmentos uma vez: vencer uma run, vencer com os quatro heróis, maestria 5, vencer na Ascensão 3, despertar a Quintessência, cumprir uma Provação, fazer uma Fusão, 100 abates numa run, quebrar o escudo de um chefe e terminar com 6 talentos. Painel no título (FEITOS) e aviso no fim da run.
- **Maestria por herói:** cada run dá experiência ao herói usado (uma por sala vencida, uma a cada 10 abates, 10 pela vitória). São cinco níveis, e cada um vale só para aquele herói: 1) +10 de vida, 2) um talento à escolha no começo da run, 3) +8% de dano, 4) +1 troca de talentos, 5) começa com 30 de Essência. Aparece na carta do herói, no Refúgio, no Grimório e no fim da run.

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

- Barra de habilidades embaixo: as quatro skills nas teclas 1 a 4 (cadeado nas que faltam, recarga em cada uma), dash e Quintessência.
- Inventário no I: skills despertas com o papel de cada uma, atributos da run e bênçãos.
- Vida à esquerda, sala atual e Essência no topo, medalhas das bênçãos com o ícone do efeito.
- Barra do chefe com as fases. Mapa da run no Tab.

### Feedback de combate

- Inimigo pisca branco ao levar dano, número de dano sobe e empilha, pausa curta de impacto nos golpes fortes, tremor de câmera.
- Aviso no chão antes de todo golpe inimigo: círculo para área, linha para projétil.
- Reação mostra o nome por cima do inimigo, com anel e luz na cor do par.

### Som

34 efeitos e 2 músicas, todos sintetizados pelo grupo (`tools/gen_audio.py`). Volume de música e de efeitos na pausa.

### Menus

Título com os quatro heróis no altar (o herói da carta em foco comemora), Continuar run quando há uma salva, seletor de Ascensão depois da primeira vitória, Refúgio (com vitórias por herói), Grimório (heróis, inimigos e reações), Feitos, Treino (com o herói da última carta em foco), Opções (volumes, tela cheia, tremor de tela e dificuldade: Normal ou Tranquila, em que o herói recebe 35% menos dano de tudo; vale na hora, até pela pausa, e aparece no resultado) e Créditos. Na run: pausa com as opções, inventário (skills, reações, atributos, bênçãos e talentos), mapa com os trechos, escolha de talento a cada nível e dicas de primeira vez no rodapé. Resultado com herói, nível, abates, quem derrubou o herói e os Fragmentos ganhos.

A build WebGL tem página de carregamento própria (arte do título, barra dourada e os controles) e abre direto no título.

---

# 2. DYNAMICS (o que acontece quando as regras encontram o jogador)

- **Marcar e detonar.** Como só as skills marcam e cada uma tem recarga, a ordem importa: qual skill abre, qual fica guardada para detonar, e o golpe básico preenche o intervalo batendo mais forte em quem está marcado. Com uma skill só não há reação; o primeiro Santuário muda o jogo.
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
| Primeiro minuto | Bate, usa a primeira skill, vê a marca | "Entendi o básico" |
| Primeiro Santuário | Desperta o segundo elemento e detona a primeira reação | Descoberta: "então é assim que funciona" |
| Meio da run | Escolhe qual skill abrir e qual guardar para detonar | Maestria: está escolhendo, não apertando botão |
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

- **[planejado]** Passada única de animação.
- **[planejado]** Refúgio como sala 3D (hoje é um painel no título).
