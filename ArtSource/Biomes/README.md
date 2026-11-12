# Adereços de cenário por bioma

Peças estáticas, um FBX de malha cada, origem no chão no centro da base, 1 unidade = 1 m. Mesmas opções de export das outras pastas:
`axis_forward='-Z'`, `axis_up='Y'`, `global_scale=1`, `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`, `bake_space_transform=True`, só malha, `mesh_smooth_type='EDGE'`, triangulado.
Materiais Principled só com cor base, roughness 0.8. **`Glow`** é o que deve brilhar de leve (emissão fica por conta do jogo).
bbox em metros no espaço do Blender (X largura, Y profundidade, Z altura). No Unity: x=−x, y=z (altura), z=−y. Tamanho = largura × profundidade × altura.
Algumas peças passam alguns centímetros abaixo de z=0 (raízes, pedrinhas, poças) de propósito, para não flutuar no piso.

## Cripta Afogada — `Crypt/`

| Peça | Arquivo | Tris | Tamanho (m) | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Sarcófago com tampa deslocada | `crypt_sarcophagus.fbx` | 512 | 2.26 × 1.62 × 1.10 | -1.005, -1.096, -0.007 | 1.256, 0.525, 1.095 | StoneDark, Stone, Shadow, StoneLight, Moss |
| 3 urnas, uma quebrada | `crypt_urns.fbx` | 696 | 1.93 × 1.65 × 1.49 | -1.017, -0.93, -0.03 | 0.911, 0.717, 1.463 | Urn, UrnBand, Shadow, Moss |
| Estátua quebrada no peito, cabeça encapuzada caída | `crypt_statue.fbx` | 716 | 1.85 × 1.46 × 2.03 | -0.814, -0.909, -0.03 | 1.039, 0.55, 2.002 | StoneDark, Stone, StoneLight, Shadow, Moss |
| Bacia rasa com água parada | `crypt_pool.fbx` | 608 | 2.30 × 2.15 × 0.28 | -1.101, -1.073, -0.004 | 1.204, 1.081, 0.277 | StoneLight, Stone, StoneDark, Water, Moss |
| Estalagmites com poças | `crypt_stalagmites.fbx` | 348 | 2.10 × 2.04 × 2.03 | -1.0, -1.141, -0.032 | 1.1, 0.9, 2.002 | StoneDark, Stone, Water |

Cores base: Stone #6c7c8a, StoneDark #485663, StoneLight #8c9aa5, Moss #3d5947, Water #2d5d67, Shadow #1b2127, Urn #566d78, UrnBand #39454f

Previews: `Crypt/preview_top.png` (de cima a 55°) e `Crypt/preview_front.png` (3/4), com o Mage do pack (~2,2 m) para escala.

## Jardim de Pedra — `Garden/`

| Peça | Arquivo | Tris | Tamanho (m) | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Árvore petrificada retorcida | `garden_tree.fbx` | 524 | 2.39 × 1.86 × 2.71 | -1.222, -0.938, -0.09 | 1.171, 0.923, 2.616 | Bark, BarkDark, Moss, MossDark |
| Cogumelos de pedra (Glow embaixo do chapéu) | `garden_mushrooms.fbx` | 990 | 2.33 × 1.86 × 1.85 | -1.061, -0.985, -0.015 | 1.27, 0.873, 1.84 | Stone, StoneDark, Glow, Moss |
| Raízes em arco | `garden_roots.fbx` | 416 | 2.27 × 1.87 × 1.45 | -1.186, -1.113, -0.178 | 1.088, 0.753, 1.269 | Bark, BarkDark, Moss, MossDark, Stone, StoneDark |
| Canteiro quebrado com samambaias | `garden_planter.fbx` | 552 | 2.30 × 1.10 × 0.96 | -0.95, -0.555, -0.036 | 1.354, 0.543, 0.92 | StoneDark, Stone, Soil, Moss, Fern |
| Cervo de pedra deitado com musgo | `garden_statue.fbx` | 910 | 2.24 × 1.10 × 2.17 | -1.243, -0.55, 0.0 | 1.0, 0.55, 2.169 | StoneDark, Stone, Moss, MossDark |

Cores base: Stone #a7a492, StoneDark #7b796b, Bark #8f8877, BarkDark #6c6658, Moss #5d7d44, MossDark #466238, Fern #5a9150, Glow #cde6ae, Soil #4d3f34

Previews: `Garden/preview_top.png` (de cima a 55°) e `Garden/preview_front.png` (3/4), com o Mage do pack (~2,2 m) para escala.

## Salões de Cinza — `Ash/`

| Peça | Arquivo | Tris | Tamanho (m) | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Braseiro de ferro com brasas | `ash_brazier.fbx` | 600 | 1.36 × 1.36 × 1.62 | -0.68, -0.68, -0.019 | 0.68, 0.68, 1.598 | Iron, IronLight, Charcoal, Glow |
| Pilha de vigas queimadas | `ash_beams.fbx` | 568 | 2.32 × 1.96 × 1.38 | -1.049, -0.848, -0.051 | 1.273, 1.111, 1.325 | Ash, Burnt, Glow, Charcoal |
| Bigorna sobre toco, com martelo | `ash_anvil.fbx` | 482 | 1.52 × 1.14 × 1.34 | -0.801, -0.65, -0.036 | 0.723, 0.492, 1.3 | Wood, WoodLight, Iron, IronLight |
| Gaiola tombada e vazia | `ash_cage.fbx` | 668 | 2.21 × 1.77 × 1.29 | -0.867, -1.04, -0.07 | 1.344, 0.733, 1.222 | IronLight, Iron |

Cores base: Iron #3e4046, IronLight #5c5f66, Charcoal #252120, Glow #d9632a, Ash #77736f, Wood #765240, WoodLight #9a7558, Burnt #332823

Previews: `Ash/preview_top.png` (de cima a 55°) e `Ash/preview_front.png` (3/4), com o Mage do pack (~2,2 m) para escala.

## Coração das Cinzas — `Heart/`

| Peça | Arquivo | Tris | Tamanho (m) | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Obelisco rachado com veio de brasa | `heart_spire.fbx` | 790 | 2.07 × 1.64 × 3.07 | -1.014, -0.88, -0.03 | 1.055, 0.762, 3.04 | StoneDark, Stone, Glow, Crystal |
| Cacho grande de cristais âmbar | `heart_crystals.fbx` | 300 | 2.31 × 2.09 × 2.38 | -1.143, -1.036, -0.085 | 1.17, 1.055, 2.291 | StoneDark, Stone, Crystal |

Cores base: Stone #57524f, StoneDark #3a3635, Glow #d9632a, Crystal #cf7c2b

Previews: `Heart/preview_top.png` (de cima a 55°) e `Heart/preview_front.png` (3/4), com o Mage do pack (~2,2 m) para escala.

## Regerar
Scripts em `_blender/`: `props.py` (peças) e `pprev.py` (previews); caminhos do PC remoto `~/quint_heroes`.
