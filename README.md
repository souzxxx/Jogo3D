# Quintessence 3D

Action roguelite 3D de câmera top-down: você escolhe um dos quatro heróis (Rax, Liora, Brann ou Vey), começa a run com o elemento dele, desperta os outros três e combina as skills em reações até alcançar a Quintessência. Projeto 3D de Jogos Digitais e Interação (Insper).

**O que tem no jogo:** 4 heróis com golpe básico, passiva e maestria (5 níveis) próprios · 4 skills elementais, 6 reações e 6 fusões com o terceiro elemento · afinidade elemental e poças que marcam pelo elemento do trecho · run de 10 salas em mapa ramificado, por três trechos (Salões de Cinza, Cripta Afogada, Jardim de Pedra) até o Coração das Cinzas · 14 tipos de inimigo (entre eles o Escudeiro, que só cede pelas costas, e um elemental por trecho: Brasa Viva, Afogado e Sentinela), Elite com 7 traços (o último, Espinhosos, devolve o golpe básico), o mini-chefe Carrasco e três chefes finais sorteados por run (o Guardião, a Tecelã das Cinzas e a Rainha Afogada) · nível e 33 talentos (4 de assinatura, um por herói; Cadência premia alternar elementos), 29 bênçãos, Santuário que desperta ou aprofunda skills, Loja, Tesouro, Pacto, Provação (desafio que paga um talento extra) e Cerco (aguentar ondas sem fim até o tempo acabar) · 31 arranjos de sala (8 exclusivos de um trecho) com perigos (brasa, espinhos, poças, barril de pólvora) e cenário que reage aos elementos (velas, tochas e respiradouros apagam com Água e acendem com Fogo, o vento apaga velas, a Água molha a pólvora, a erupção marca Fogo, caixotes e cristais quebram) · Refúgio com 8 melhorias permanentes, Ascensão em 5 níveis e 10 Feitos · run salva a cada sala (Continuar no título) · Grimório, treino com o herói escolhido, dicas de primeira vez e opções (volume, tela cheia, tremor de tela, gráficos leves e dificuldade Tranquila) · joga do começo ao fim só com controle.

**Equipe:** Leonardo de Souza, Nicolas Volf, Enzo Canavero, Miguel Damasio
**Engine:** Unity `6000.6.4f1` (URP). Use exatamente essa versão.

## Documentos

- [`docs/ESCOPO.md`](docs/ESCOPO.md): escopo, direção visual, rubricas, divisão do time e kanban
- [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md): pastas, assemblies e contratos entre as frentes
- [`docs/progresso.html`](docs/progresso.html): o que já foi entregue, com vídeo (abra no navegador)
- [`docs/lookdev.html`](docs/lookdev.html): prints do alvo visual (abra no navegador)
- [`docs/notas_tecnicas_madrugada18.md`](docs/notas_tecnicas_madrugada18.md): cada mudança da madrugada de 18/11 com o arquivo, a função e o teste que a cobre
- [`docs/itch.md`](docs/itch.md): texto da página do itch.io; prints em `docs/itch_prints/`
- Kanban: https://github.com/users/souzxxx/projects/4

## Jogar

Abra `Assets/Scenes/Title.unity` no Unity e dê Play. A build WebGL também roda direto no navegador (ver abaixo).

WASD move · mouse mira · clique esquerdo ataca · 1 2 3 4 skills elementais · Espaço dash · R Quintessência · E interage · I inventário · Tab mapa · Esc pausa

Com controle: analógico esquerdo move, direito mira, X ataca, LB LT RT RB soltam as skills (Fogo, Água, Terra, Ar), A dash, Y Quintessência, B interage, Select inventário, Start pausa.

Testes: `unity test . --mode EditMode` e `unity test . --mode PlayMode` (os de PlayMode jogam as cenas com um roteiro e gravam quadros em `Logs/frames`).

## Builds (entrega até 25/11)

A entrega pede build **Windows** e o jogo na jam do itch.io: https://itch.io/jam/projeto-jogo-3d-insper-20262. **Página no ar:** https://souzxxx.itch.io/quintessence (publicada em 17/11 com as builds de 17h58: WebGL para jogar no navegador e zip do Windows para baixar). **Pendente (Leonardo):** subir de novo os dois zips de `Builds/` no itch (a build Windows de 17h58 tinha as armas magenta no perfil gráfico de PC, corrigido na madrugada de 18/11; zips finais de 18/11 às 07h46 em Builds/, com todas as novidades da madrugada) e inscrever o jogo na jam.

- **Windows:** menu **Quintessence > Build Windows** ou `unity build --target StandaloneWindows64 --execute-method BuildTools.BuildWindows -o Builds/Windows`. Para o itch.io, zipe a pasta `Builds/Windows` inteira, sem `Quintessence_BackUpThisFolder_ButDontShipItWithYourGame` (o `.exe` sozinho não roda). Precisa do módulo "Windows Build Support (Mono)" no Unity (`unity install-modules -e 6000.6.4f1 -m windows-mono`).
- **WebGL:** menu **Quintessence > Build WebGL (itch.io)** ou `unity build --target WebGL --execute-method BuildTools.BuildWebGL -o Builds/WebGL`. Para o itch.io, compacte o conteúdo de `Builds/WebGL` com o `index.html` na raiz.

A pasta `Builds/` não vai pro git.

## Look-dev

A cena `Assets/Scenes/LookDev/LookDev_B_Lit.unity` é gerada por `Assets/Editor/LookDevBuilder.cs` (menu **Quintessence > Look-dev > Build A + B**), que também salva os prints em `docs/img/`.

## Créditos

| Asset | Autor | Licença |
|---|---|---|
| [KayKit Dungeon Remastered](https://github.com/KayKit-Game-Assets/KayKit-Dungeon-Remastered-1.0) | Kay Lousberg (kaylousberg.com) | CC0 |
| [KayKit Adventurers](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Adventures-1.0) | Kay Lousberg | CC0 |
| [KayKit Skeletons](https://github.com/KayKit-Game-Assets/KayKit-Character-Pack-Skeletons-1.0) | Kay Lousberg | CC0 |
| [Lilita One](https://fonts.google.com/specimen/Lilita+One) | Juan Montoreano | OFL 1.1 |
| [Titillium Web](https://fonts.google.com/specimen/Titillium+Web) | Accademia di Belle Arti di Urbino | OFL 1.1 |
| [Cinzel Decorative](https://fonts.google.com/specimen/Cinzel+Decorative) | Natanael Gama | OFL 1.1 |
| Ícones de [game-icons.net](https://game-icons.net) (ícones em `Assets/Resources/Icons`) | Lorc, Delapouite, Carl Olsen, Skoll e outros | CC BY 3.0 |
| Cabeças, caudas e armas dos heróis, peças do Guardião, acessórios dos inimigos e adereços dos trechos (`Assets/Art`, fontes e scripts do Blender em `ArtSource/`) | Equipe, modelados sobre os corpos do KayKit | do projeto |

Som: todos os efeitos e as músicas são sintetizados pelo script `tools/gen_audio.py` (ffmpeg), sem arquivos de terceiros.

Ferramentas de IA: Claude Code (Anthropic) no planejamento, na montagem da cena de look-dev e no código.
