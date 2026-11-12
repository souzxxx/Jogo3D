# Inimigos — acessórios para diferenciar de cima (esqueletos do KayKit)

Mesmo contrato de `ArtSource/Heroes` e `ArtSource/Boss`: malhas estáticas no espaço de modelo do corpo em repouso (origem nos pés,
1 unidade = 1 m na escala nativa), instanciar na raiz com identidade e reparentar ao osso mantendo a posição de mundo.
Export: `axis_forward='-Z'`, `axis_up='Y'`, `global_scale=1`, `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`,
`bake_space_transform=True`, só malha, `mesh_smooth_type='EDGE'`, triangulado. Materiais Principled só com cor base, roughness 0.8.
Os quatro esqueletos têm o mesmo rig e o mesmo crânio (x ±0.44, y −0.45..0.46, z 1.31..2.17), então as peças de cabeça servem em qualquer um.
bbox em metros no espaço do Blender (X lateral, Y trás(+)/frente(−), Z altura). Unity: x=−x, y=z, z=−y.

## Ceifador — `Skeleton_Rogue.fbx`, tom (0.62, 0.68, 0.82)
Esconder: `Skeleton_Rogue_Hood` (o capuz novo substitui) e `Skeleton_Rogue_Cape` (os trapos substituem).

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Capuz rasgado com ponta longa para trás | `ceifador_head.fbx` | `head` | 428 | -0.552, -0.59, 0.829 | 0.552, 1.505, 2.349 | Cloth, ClothDark |
| Foice de duas mãos | `ceifador_weapon.fbx` | `handslot.r` | 232 | -0.948, -1.819, 0.979 | 0.401, 0.81, 1.414 | Wood, Metal, MetalDark, Cloth |
| Trapos para trás (opcional) | `ceifador_back.fbx` | `chest` | 156 | -0.447, 0.227, 0.859 | 0.483, 1.583, 1.323 | Cloth, ClothDark |

A lâmina da foice fica quase deitada (plano XY do T-pose, levemente inclinada) para aparecer de cima nas poses `2H_Melee_*`.

## Arremessador — `Skeleton_Minion.fbx`, tom (0.78, 0.8, 0.56)
Esconder: nada obrigatório (o Minion não tem chapéu). `Skeleton_Minion_Cloak` pode ficar.

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Chapéu de aba + óculos de proteção | `arremessador_head.fbx` | `head` | 484 | -0.68, -0.66, 1.789 | 0.68, 0.7, 2.45 | Leather, LeatherDark, Cloth, Metal, Lens |
| Cesto com 3 bombas | `arremessador_back.fbx` | `chest` | 560 | -0.4, -0.347, 0.78 | 0.4, 1.1, 1.869 | Wicker, WickerDark, Bomb, Metal, Fuse, Ember, LeatherDark |
| Bomba na mão | `arremessador_weapon.fbx` | `handslot.r` | 140 | -1.092, -0.26, 0.929 | -0.674, 0.18, 1.566 | Bomb, Metal, Fuse, Ember |

## Xamã — `Skeleton_Mage.fbx`, tom (0.56, 0.82, 0.7)
Esconder: `Skeleton_Mage_Hat` (as galhadas atravessariam o chapéu). O crânio do pack (`Skeleton_Mage_Skull`) fica; a máscara cobre o rosto.

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Máscara de crânio de cervo + galhadas + penas | `xama_head.fbx` | `head` | 740 | -1.132, -1.016, 1.272 | 1.132, 1.117, 2.955 | Bone, Socket, Antler, Cord, Feather, FeatherTip |
| Cajado-totem com pedra | `xama_weapon.fbx` | `handslot.r` | 388 | -1.226, -2.068, 0.601 | -0.54, 0.85, 1.357 | Wood, WoodDark, Bone, BoneDark, Socket, Cord, Feather, FeatherTip, **Magic** (pedra) |
| Colar de ossos e penas (opcional) | `xama_back.fbx` | `chest` | 260 | -0.933, -0.168, 1.168 | 0.933, 0.845, 1.355 | Bone, Feather, FeatherTip, Cord |

O cajado-totem substitui o `Skeleton_Staff` na mão direita.

## Campeão — qualquer esqueleto (ampliado 1,3x pelo jogo)
| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Halo de pontas de cristal | `campeao_crown.fbx` | `head` | 324 | -0.496, -0.571, 2.822 | 0.496, 0.531, 3.24 | Crystal (#cf7c2b), Metal |

É um halo flutuante com o aro em z = 2,90 (escala nativa), acima do ponto mais alto dos quatro corpos
(chapéu do Mage 2,63; elmo do Warrior 2,59; capuz do Rogue 2,31; crânio do Minion 2,17). Serve nos quatro sem variação e sem esconder nada.
Se quiser o halo mais colado à cabeça, desloque no eixo vertical do osso `head`: Minion até −0,55; Rogue até −0,40; Warrior e Mage até −0,10.
Para Ceifador e Arremessador o halo também passa por cima (topo 2,35 e 2,45); no Xamã as galhadas (2,95) atravessam o aro — suba +0,25.

## Previews
`preview_<inimigo>_front.png`, `preview_<inimigo>_top.png` (corpo tingido no tom indicado), `lineup_top.png` e `lineup_front.png`
(Minion, Mage, Warrior, Rogue puros + Ceifador, Arremessador, Xamã), `campeao.png` e `campeao_top.png` (halo nos quatro corpos a 1,3x).
Scripts em `_blender/` (`enemies.py`, `eprev.py`, `lib.py`).
