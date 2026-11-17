# Sala de Descanso

Peças estáticas feitas no Blender (`_blender/campfire.py`, usa o `lib.py` de `ArtSource/Biomes/_blender`), mesmas opções de export dos biomas.
No Unity ficam em `Assets/Art/Biomes/Rest/` (o caminho com `/Biomes/` faz o material `Glow` acender de leve) e entram no prefab `RestDecor` pelo `GameSceneBuilder.Campfire`.

| Peça | Arquivo | Tris | Tamanho (m) | Materiais |
|---|---|---|---|---|
| Fogueira: anel de pedras, lenha em pé, brasa | `rest_campfire.fbx` | 588 | 1.96 × 1.88 × 0.90 | Stone, StoneDark, Wood, WoodLight, Burnt, Glow, Ash |
| Tronco para sentar, com musgo | `rest_log.fbx` | 104 | 1.63 × 0.44 × 0.48 | Bark, WoodLight, Moss |

Cores base: Stone #8a8378, StoneDark #5f5a52, Wood #8a5a3c, WoodLight #b48a5e, Burnt #3a2e28, Glow #f08a3c, Ash #4a4440, Bark #5e4030, Moss #5d7d44.
A chama é de partículas (no jogo), com luz quente que oscila (`Feel/FireFlicker`).

Preview: `preview_top.png`.
