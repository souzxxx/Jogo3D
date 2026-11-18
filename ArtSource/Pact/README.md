# Altar do Pacto

Peça estática feita no Blender (`_blender/pact.py`, usa o `lib.py` de `ArtSource/Biomes/_blender`; preview por `_blender/paprev.py`), mesmas opções de export dos biomas.
No Unity: `Assets/Art/Pact/pact_altar.fbx`, prefab `Assets/Game/Game/Prefabs/PactAltar.prefab` (escala 1,2, colisor e recorte na malha), feito pelo `GameSceneBuilder.PactAltarPrefab` e ligado no `RunDirector.BindPactAltar`. Sem ele, o Pacto volta para a coluna do Santuário.

| Peça | Arquivo | Tris | Tamanho (m) | Materiais |
|---|---|---|---|---|
| Altar: bloco escuro, pano vinho, bacia com líquido escuro, velas derretidas, corrente | `pact_altar.fbx` | 798 | 1.30 × 1.00 × 1.57 | StoneDark, Stone, StoneLight, Cloth, ClothDark, Trim, Glow, Wax, Flame, Iron |

Preview: `preview_front.png`.
