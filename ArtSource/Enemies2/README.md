# Inimigos novos — Cinzento, Brasa Viva, Sentinela de Pedra

Mesmo contrato de `ArtSource/Enemies`: malhas estáticas no espaço do corpo em repouso (origem nos pés, 1 unidade = 1 m na escala nativa; o jogo aplica a escala do inimigo depois),
instanciar na raiz com identidade e reparentar ao osso mantendo a posição de mundo. Export: `axis_forward='-Z'`, `axis_up='Y'`, `global_scale=1`, `apply_unit_scale=True`,
`apply_scale_options='FBX_SCALE_ALL'`, `bake_space_transform=True`, só malha, `mesh_smooth_type='EDGE'`, triangulado. Materiais Principled só cor, roughness 0.8. `Magic` = o que acende.
bbox em metros no espaço do Blender (X lateral, Y trás(+)/frente(−), Z altura). Unity: x=−x, y=z, z=−y.

## Cinzento — `Skeleton_Minion.fbx`, escala 0,65x, tom cinza (preview: 0.62, 0.6, 0.6)
Esconder: nada.

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Capuz de trapo puído com orelhas redondas de rato | `cinzento_head.fbx` | `head` | 556 | -0.849, -0.629, 0.98 | 0.849, 0.663, 2.504 | Rag, RagDark, RagInner |
| Corcunda de trapos com ossinhos, corda e rabo de trapo até o chão | `cinzento_back.fbx` | `chest` | 526 | -0.455, -0.109, 0.015 | 0.455, 1.504, 1.778 | Rag, RagDark, Bone, Rope |
| Garra curta de 3 ossos (opcional) | `cinzento_weapon.fbx` | `handslot.r` | 122 | -0.974, -0.452, 0.979 | -0.792, 0.11, 1.171 | Rope, Bone |

Cores: Rag #6e6863, RagDark #47423f, RagInner #8a6f66, Bone #b5ab98, Rope #7a6248

## Brasa Viva — `Skeleton_Warrior.fbx`, escala 1,1x, tom laranja-carvão (preview: 0.9, 0.55, 0.4)
Esconder: `Skeleton_Warrior_Helmet` e `Skeleton_Warrior_Cloak` (o lenço vermelho do pack brigava com as placas do peito).

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Coroa de chamas em placas sobre um aro de carvão | `brasa_head.fbx` | `head` | 476 | -0.645, -0.718, 1.549 | 0.645, 0.737, 2.909 | Charcoal, Magic |
| Placas de carvão rachadas no peito e nas costas com fendas | `brasa_body.fbx` | `chest` | 420 | -0.31, -0.449, 0.802 | 0.31, 0.425, 1.268 | Charcoal, CharcoalLight, Magic |
| Ombreira de carvão com fenda e chama (peça extra) | `brasa_shoulder_l.fbx` | `upperarm.l` | 296 | 0.211, -0.235, 1.026 | 0.737, 0.235, 1.736 | Charcoal, CharcoalLight, Magic |
| Idem, lado direito (peça extra) | `brasa_shoulder_r.fbx` | `upperarm.r` | 296 | -0.737, -0.235, 1.026 | -0.211, 0.235, 1.736 | Charcoal, CharcoalLight, Magic |
| Maça de carvão incandescente | `brasa_weapon.fbx` | `handslot.r` | 252 | -1.17, -1.246, 0.823 | -0.403, 0.3, 1.524 | Charcoal, CharcoalLight, Magic |

As ombreiras saíram do `brasa_body` para os ossos dos braços pela mesma razão do Guardião e da Tecelã: presas ao `chest`, os braços erguidos atravessam.

Cores: Charcoal #2b2624, CharcoalLight #4a423d, Magic #d9632a

## Sentinela de Pedra — `Skeleton_Mage.fbx`, escala 1,25x, tom musgo (preview: 0.62, 0.72, 0.6)
Esconder: `Skeleton_Mage_Hat`. O `Skeleton_Mage.fbx` não traz cajado nem varinha presos; se o jogo prende o `Skeleton_Staff`, tirar e usar `sentinela_weapon`.
O elmo fechado cobre o crânio inteiro (`Skeleton_Mage_Skull`, `_Jaw`, `_Eyes` ficam por dentro; pode escondê-los para economizar, não é obrigatório).

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Elmo-totem de pedra fechado com fenda de olho | `sentinela_head.fbx` | `head` | 468 | -0.58, -0.57, 1.25 | 0.65, 0.54, 2.579 | Stone, StoneDark, Magic, Moss, MossDark |
| Casco de pedra grande com musgo | `sentinela_back.fbx` | `chest` | 448 | -0.778, 0.325, 0.43 | 0.778, 1.388, 1.844 | Stone, StoneDark, Moss, MossDark |
| Pedestal octogonal quebrado com raízes de pedra em volta dos pés | `sentinela_base.fbx` | `root` | 586 | -1.006, -0.887, -0.048 | 1.006, 1.067, 0.268 | StoneDark, Stone, Moss, MossDark |
| Cajado-obelisco curto com cristal | `sentinela_weapon.fbx` | `handslot.r` | 246 | -1.013, -1.34, 0.784 | -0.753, 0.51, 1.314 | StoneDark, MossDark, Stone, Magic, Moss |

`sentinela_base` vai no osso `root` (raiz do armature, na origem, entre os pés). A plataforma tem 0,2 m de altura e engole os pés de propósito, para ela parecer plantada no chão.
O cristal do cajado também é `Magic` (pinta junto com a fenda do olho).

Cores: Stone #7d8274, StoneDark #545a50, Moss #56713f, MossDark #3f5532, Magic #bfe3b0

## Previews
`preview_<inimigo>_front.png` e `preview_<inimigo>_top.png` (tom e escala indicados, pose Idle). `lineup_top.png` e `lineup_front.png`: Rastejante (Minion puro 1x), Cinzento, Brasa Viva, Sentinela, Ceifador (1,12x) e Bruto (Warrior puro 1,3x).
Scripts em `_blender/`.
