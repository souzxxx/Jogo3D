# Heróis — peças acopláveis (KayKit Adventurers → heróis não humanos)

Peças estáticas (sem armature) modeladas por script no Blender 5.2.2, no **espaço de modelo do corpo em bind pose**
(origem nos pés, Z-up/-Y frente no Blender, mesma escala do FBX do pack: 1 unidade = 1 m, personagem ~2,2 m).
No Unity: instanciar a peça na raiz do personagem com transform local identidade e então reparentar ao osso
mantendo a posição de mundo (`SetParent(bone, true)`), com o rig ainda em bind pose (antes do Animator avaliar).

Export FBX (Blender): `axis_forward='-Z'`, `axis_up='Y'`, `global_scale=1`, `apply_unit_scale=True`,
`apply_scale_options='FBX_SCALE_ALL'`, `bake_space_transform=True`, `object_types={'MESH'}`,
`mesh_smooth_type='EDGE'`, sem animação, malhas trianguladas, transforms aplicados.
No importador do Unity manter `Scale Factor 1` + `Use File Scale` (igual aos FBX do pack).

Materiais: Principled só com base color, roughness 0.8. O material **`Magic`** é o que o jogo recolore por elemento.
Texturas `*_body_texture.png`: mesmo layout UV do atlas do pack (1024², grade 8×4 de degradês) — basta trocar a textura do material do corpo.

Limites (bbox) em metros, espaço de modelo do Blender (X direita/esquerda, Y trás(+)/frente(−), Z altura).
No Unity: x_unity = −x, y_unity = z, z_unity = −y.

## Rax — pantera/lince caçador (Fogo)
- Corpo: `Rogue.fbx` · textura `Rax/rax_body_texture.png` (substitui `rogue_texture`)
- Esconder: `Rogue_Head`, `Rogue_Cape` (a capa cobre a cauda) e os props do pack (`Knife`, `Knife_Offhand`, `1H_Crossbow`, `2H_Crossbow`, `Throwable`)

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Cabeça + juba | `rax_head.fbx` | `head` | 1268 | -0.685, -0.577, 1.061 | 0.685, 0.654, 2.404 | Fur, FurLight, FurDark, Nose, Eye (oval preto), EarInner, **Magic** (crina em mechas) |
| Cauda | `rax_tail.fbx` | `hips` | 142 | -0.074, 0.207, 0.413 | 0.405, 1.336, 1.624 | Fur, **Magic** (ponta) |
| Garra esq. | `rax_weapon_l.fbx` | `handslot.l` | 172 | 0.8, -0.19, 0.945 | 1.403, 0.19, 1.265 | Leather, Metal, Claw |
| Garra dir. | `rax_weapon_r.fbx` | `handslot.r` | 172 | -1.403, -0.19, 0.945 | -0.8, 0.19, 1.265 | Leather, Metal, Claw |

## Liora — espírito cervo-raposa (Água)
- Corpo: `Mage.fbx` · textura `Liora/liora_body_texture.png` (substitui `mage_texture`)
- Esconder: `Mage_Head`, `Mage_Hat`, `Mage_Cape` e props (`1H_Wand`, `2H_Staff`, `Spellbook`, `Spellbook_open`)

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Cabeça + galhadas | `liora_head.fbx` | `head` | 1044 | -0.896, -0.798, 1.12 | 0.896, 0.434, 3.095 | Fur, FurLight, Nose, Eye, EarInner, Antler, Blossom, Leaf |
| Cauda | `liora_tail.fbx` | `hips` | 184 | -0.33, 0.218, 0.311 | 0.33, 1.398, 1.503 | Fur, FurLight |
| Orbe | `liora_weapon.fbx` | `handslot.r` | 120 | -1.178, -0.538, 1.09 | -0.63, -0.062, 1.51 | **Magic** |

## Brann — texugo veterano (Terra)
- Corpo: `Barbarian.fbx` · textura `Brann/brann_body_texture.png` (substitui `barbarian_texture`)
- Esconder: `Barbarian_Head`, `Barbarian_Hat`, `Barbarian_Cape` e props (`1H_Axe`, `1H_Axe_Offhand`, `2H_Axe`, `Barbarian_Round_Shield`, `Mug`)

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Cabeça + barba | `brann_head.fbx` | `head` | 1040 | -0.677, -0.79, 0.998 | 0.677, 0.461, 2.122 | FurLight, Fur, FurDark, Nose, Eye, Beard, Scar |
| Cauda | `brann_tail.fbx` | `hips` | 42 | -0.139, 0.228, 0.4 | 0.139, 0.927, 0.746 | Fur, FurDark |
| Martelo (2 mãos) | `brann_weapon.fbx` | `handslot.r` | 828 | -1.283, -1.356, 0.813 | -0.483, 0.57, 1.314 | Wood, Metal, Leather, Stone, StoneDark, Moss, **Magic** (veios) |

O martelo segue a mesma orientação do `2H_Axe` do pack (cabo ao longo do slot), então as animações `2H_Melee_*` servem.

## Vey — espírito do vento encapuzado, sem pernas (Ar)
- Corpo: `RogueHooded.fbx` · textura `Vey/vey_body_texture.png` (substitui `rogue_texture`; ela pinta o rosto de preto e o capuz de azul-ardósia)
- Esconder: `Rogue_LegLeft`, `Rogue_LegRight` e props (`Knife`, `Knife_Offhand`, `1H_Crossbow`, `2H_Crossbow`, `Throwable`)
- **Manter**: `Rogue_Head_Hooded` (capuz do próprio pack) e `Rogue_Cape`

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Rosto (vazio preto) + olhos + debrum | `vey_head.fbx` | `head` | 456 | -0.486, -0.63, 1.15 | 0.486, 0.44, 1.89 | Void (preto fosco), **Magic** (olhos e debrum do capuz) |
| Redemoinho (3 fitas espirais) | `vey_tail.fbx` | `hips` | 368 | -0.528, -0.409, -0.011 | 0.594, 0.316, 0.692 | **Magic** |
| Adaga dir. | `vey_weapon_r.fbx` | `handslot.r` | 172 | -1.062, -0.981, 0.973 | -0.763, 0.225, 1.125 | Metal, Grip, Guard |
| Adaga esq. | `vey_weapon_l.fbx` | `handslot.l` | 172 | 0.763, -0.981, 0.973 | 1.062, 0.225, 1.125 | Metal, Grip, Guard |

As adagas são a malha `Knife` do pack, levemente curvada e com materiais no lugar da textura.

## Ossos (iguais nos quatro FBX do pack)
`root > hips > spine > chest > head`; mãos: `hand.l/.r > handslot.l/.r` (slot de props).

## Previews
`<Heroi>/preview_front.png`, `preview_back.png`, `preview_top.png` (pose de idle do pack, peças presas aos ossos acima) e
`lineup.png` / `lineup_top.png`. As cores de `Magic` nos renders são só o elemento inicial dessaturado.

## Regerar
Scripts em `_blender/` (`lib.py`, `heroes.py`, `preview.py`); caminhos apontam para `~/quint_heroes` no PC remoto:
`blender -b -P heroes.py -- rax liora brann vey` e `blender -b -P preview.py -- pose Rax Liora Brann Vey` / `-- lineup`.

## Rodada 2 (retoques)
- Rax: cabeça esférica com bochechas e focinho curto, olhos ovais pretos (material `Pupil` removido), orelhas de lince com tufo, juba trocada por mechas chapadas em camadas da coroa até a nuca.
- Vey: orelhas removidas (materiais `Ear`/`EarDark` não existem mais); domo preto `Void` dentro do capuz cobre nariz e boca; debrum `Magic` segue a borda real da abertura do capuz do pack; redemoinho virou funil de três fitas espirais.
- Brann: veios do martelo viraram rachaduras em zigue-zague quase rentes à pedra.
