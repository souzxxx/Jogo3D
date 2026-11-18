# Pedestal do Santuário

Peça estática feita no Blender (`_blender/shrine.py`, usa o `lib.py` de `ArtSource/Biomes/_blender`; preview por `_blender/sprev.py`).
No Unity: `Assets/Art/Shrine/shrine_pedestal.fbx`, prefab `Assets/Game/Game/Prefabs/ShrinePedestal.prefab` pelo `GameSceneBuilder.StaticPropPrefab` (colisor e recorte na malha), ligado como `shrinePrefab` do `RunDirector` (Santuário e aprofundar skill). Altura 1,25 m: o emblema do elemento flutua no encaixe do topo.

| Peça | Arquivo | Tris | Tamanho (m) | Materiais |
|---|---|---|---|---|
| Pedestal octogonal em degraus, quatro runas entalhadas, encaixe raso no topo | `shrine_pedestal.fbx` | 440 | 1.22 × 1.24 × 1.25 | StoneDark, Stone, StoneLight, Rune, Socket, Moss |

Preview: `preview_front.png`.
