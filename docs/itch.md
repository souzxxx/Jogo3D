# Página do itch.io — texto pronto para colar

> Rascunho da página do jogo na jam https://itch.io/jam/projeto-jogo-3d-insper-20262 (entrega até 25/11/2026).
> A rubrica pede: como jogar, teclas diferentes, segredos, imagens e vídeo. Tudo abaixo existe no jogo de 17/11.
> Arquivos: `Builds/Quintessence-Windows.zip` (Windows, obrigatório) e `Builds/Quintessence-WebGL.zip` (jogar no navegador, marcar "This file will be played in the browser", tamanho 1280 × 720 e botão de tela cheia).

---

## Quintessence

**Os quatro elementos se separaram e estão corrompendo o mundo.** Escolha um dos quatro heróis, comece a descida com o elemento dele, desperte os outros três pelo caminho e combine as skills em reações até alcançar a Quintessência, o quinto estado.

Roguelite de ação em 3D com câmera de cima. Uma run leva de 10 a 15 minutos.

### Como jogar

| Ação | Teclado e mouse | Controle |
|---|---|---|
| Mover | W A S D | analógico esquerdo |
| Mirar | mouse (a seta e o anel no chão mostram para onde) | analógico direito |
| Golpe básico | clique esquerdo (segurar faz o combo) | RT ou X |
| Skills dos elementos | 1 Fogo · 2 Água · 3 Terra · 4 Ar | direcional |
| Desviar (dash) | Espaço | A |
| Quintessência (com os quatro elementos) | R | Y |
| Interagir (portas, baús, totens) | E | B |
| Inventário | I | Select |
| Mapa | Tab | — |
| Pausa e opções | Esc | Start |

**O básico em uma frase:** uma skill **marca** o inimigo com o elemento dela; a skill de **outro** elemento **detona** a reação.

- Fogo + Água = **Vapor** (quem está dentro erra metade dos ataques)
- Fogo + Terra = **Magma** (poça de lava)
- Fogo + Ar = **Tempestade de Fogo** (a maior explosão)
- Água + Terra = **Lama** (ninguém sai do lugar)
- Água + Ar = **Nevasca** (congela; o próximo golpe estilhaça)
- Terra + Ar = **Estilhaços** (área grande que empurra)

**Ler o inimigo:** esqueleto de arma erguida bate quando a arma desce; desvie com Espaço nessa hora. Bombas, explosões e golpes de chefe avisam um círculo vermelho no chão: saia dele. Atrás de pilar ou parede, herói e inimigos aparecem como silhueta.

### Os heróis

- **Rax** (Fogo): garras rápidas, +15% no combo básico
- **Liora** (Água): chicote longo, cura ao limpar a sala
- **Brann** (Terra): martelo pesado, +20 de vida máxima
- **Vey** (Ar): adagas velozes, 25% de esquiva e cura ao esquivar

### Segredos e coisas para descobrir

- **Fusões:** com três elementos, lance a skill do terceiro dentro da lava do Magma, da nuvem do Vapor ou do lamaçal da Lama.
- **Esquiva perfeita:** dar dash através de um golpe enche a Quintessência e deixa o próximo golpe crítico.
- **Escudeiro:** de frente o escudo segura quase tudo. Contorne.
- **Três chefes finais**, sorteados por run.
- **Ascensão:** depois da primeira vitória, o título libera 5 níveis de dificuldade a mais.
- **Refúgio:** os Fragmentos de cada run compram 8 melhorias permanentes.
- **10 Feitos** para cumprir (a lista está no título).
- **Maestria:** cada herói sobe até o nível 5 e começa as runs com vantagens próprias.
- **Treino:** no título, ensina tudo com o herói da última carta em foco.
- Achou difícil? **Opções → dificuldade Tranquila** (35% menos dano recebido).
- PC fraco? **Opções → Gráficos: leves**. A tabela completa de teclas e botões está em **Opções → Controles**.

A run é salva a cada sala: fechou o jogo, aparece **Continuar** no título.

### Equipe

Leonardo de Souza (combate e câmera) · Enzo Canavero (inimigos, salas e colisões) · Nicolas Volf (run, meta e interface) · Miguel Damasio (visual e som)

Projeto 3D de Jogos Digitais e Interação, Insper, 2026.2.

### Créditos

- Modelos e animações: KayKit Dungeon Remastered, Adventurers e Skeletons, de Kay Lousberg (CC0)
- Ícones: game-icons.net, de Lorc, Delapouite e outros (CC BY 3.0)
- Fontes: Lilita One, Titillium Web e Cinzel Decorative (Google Fonts, OFL)
- Peças dos heróis, chefes, inimigos, adereços e emblemas: modeladas pelo grupo no Blender sobre os corpos do KayKit
- Efeitos sonoros e música: sintetizados pelo grupo

---

## Imagens sugeridas (em `docs/img/`)

Ícone/miniatura do jogo: `icone.png` (o mesmo do .exe).

1. `s6_titulo_emblemas.png`: título com os quatro heróis (capa)
2. `run_noite_capa.jpg` ou um quadro de combate com reação
3. `s6_santuario.png`: Santuário com os emblemas
4. `sala_mestre.png` ou `loja.png`: salas de escolha
5. `chefe_rainha.png`: chefe final
6. `mapa.png`: mapa ramificado

Vídeo: `docs/img/run_noite.mp4` (run curta gravada pelo teste, visual de 16/11). Um vídeo jogado por gente, de 30 a 60 s, rende mais na página.
