# Quintessence 3D

Action roguelite 3D de câmera top-down: você começa a run com um elemento, desperta os outros três e encadeia reações elementais até alcançar a Quintessência. Projeto 3D de Jogos Digitais e Interação (Insper).

**Equipe:** Leonardo, Nicolas, Enzo, Miguel
**Engine:** Unity `6000.6.4f1` (URP). Use exatamente essa versão.

## Documentos

- [`docs/ESCOPO.md`](docs/ESCOPO.md): escopo, direção visual, rubricas, divisão do time e kanban
- [`docs/ARQUITETURA.md`](docs/ARQUITETURA.md): pastas, assemblies e contratos entre as frentes
- [`docs/progresso.html`](docs/progresso.html): o que já foi entregue, com vídeo (abra no navegador)
- [`docs/lookdev.html`](docs/lookdev.html): prints do alvo visual (abra no navegador)
- Kanban: https://github.com/users/souzxxx/projects/4

## Jogar

Abra `Assets/Scenes/Title.unity` no Unity e dê Play.

WASD move · mouse mira · clique esquerdo ataca · clique direito skill · Espaço dash · Q/E troca elemento · R Quintessência · F interage · Tab mapa · Esc pausa

Testes: `unity test . --mode EditMode` e `unity test . --mode PlayMode` (os de PlayMode jogam as cenas com um roteiro e gravam quadros em `Logs/frames`).

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
| Ícones de [game-icons.net](https://game-icons.net) (21 ícones em `Assets/Resources/Icons`) | Lorc, Carl Olsen, Skoll | CC BY 3.0 |

Som: todos os efeitos e as músicas são sintetizados pelo script `tools/gen_audio.py` (ffmpeg), sem arquivos de terceiros.

Ferramentas de IA: Claude Code (Anthropic) no planejamento, na montagem da cena de look-dev e no código.
