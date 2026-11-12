# Chefe 2 — Tecelã das Cinzas (peças sobre o Skeleton_Mage do KayKit)

Mesmo contrato do Guardião (`ArtSource/Boss/README.md`): malhas estáticas no espaço de modelo do corpo em repouso, origem nos pés,
1 unidade = 1 m na escala nativa (o jogo amplia 2,2x tudo junto). Instanciar na raiz com identidade e reparentar ao osso mantendo a posição de mundo.
Export: `axis_forward='-Z'`, `axis_up='Y'`, `global_scale=1`, `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`, `bake_space_transform=True`, só malha, `mesh_smooth_type='EDGE'`, triangulado.

- Corpo: `Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Mage.fbx`
- Textura: `weaver_body_texture.png` (substitui `skeleton_texture`; mesmo atlas): osso cinza-carvão com manchas e rachaduras quentes, robe roxo-escuro/vinho, marcas da testa em brasa.
- **Esconder do pack**: `Skeleton_Mage_Hat`. O `Skeleton_Mage.fbx` não traz cajado preso; se o jogo prende o `Skeleton_Staff` na mão direita, tirar e usar `weaver_staff.fbx`.
- **Manter**: `Skeleton_Mage_Skull`, `Skeleton_Mage_Jaw`, `Skeleton_Mage_Eyes`, corpo, braços e pernas.

bbox em metros no espaço do Blender (X lateral, Y trás(+)/frente(−), Z altura). Unity: x=−x, y=z, z=−y.

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Coroa alta de pontas de osso/pedra com cristais, halo de brasa atrás, véu rasgado nas costas | `weaver_head.fbx` | `head` | 941 | -0.888, -0.65, 1.071 | 0.888, 0.838, 3.065 | BoneDark, Bone, StoneDark, Crystal, Magic, ClothDark, Cloth |
| Gola alta (só atrás), lapelas abrindo a frente, capa curta rasgada nas costas | `weaver_mantle.fbx` | `chest` | 260 | -0.576, -0.309, 0.28 | 0.573, 0.673, 1.677 | ClothDark, Cloth |
| Drapeado + plaquinhas de pedra, ombro esquerdo | `weaver_shoulder_l.fbx` | `upperarm.l` | 208 | 0.208, -0.214, 1.184 | 0.52, 0.214, 1.506 | Cloth, Stone, StoneDark |
| Drapeado + plaquinhas de pedra, ombro direito | `weaver_shoulder_r.fbx` | `upperarm.r` | 208 | -0.52, -0.214, 1.184 | -0.208, 0.214, 1.506 | Cloth, Stone, StoneDark |
| Núcleo de brasa no peito + 3 fios até a cintura | `weaver_core.fbx` | `chest` | 290 | -0.228, -0.422, 0.547 | 0.213, -0.18, 1.24 | Magic, BoneDark |
| Cajado longo de osso, orbe de brasa preso por garras | `weaver_staff.fbx` | `handslot.r` | 562 | -1.142, -2.275, 0.833 | -0.672, 1.17, 1.332 | Bone, BoneDark, ClothDark, Crystal, StoneDark, Magic |
| 6 lascas de cristal flutuando em volta dos ombros/costas | `weaver_shards.fbx` | `chest` | 96 | -1.064, 0.033, 0.947 | 1.064, 1.01, 2.406 | Crystal |

Cores base: Bone #a59d90, BoneDark #6f6a64, StoneDark #4b4846, Stone #77736e, Cloth #4a2338, ClothDark #2e1824, Crystal #cf7c2b, Magic #d9632a. Roughness 0.8 em todos.

`Magic`: halo, núcleo, os 3 fios e o orbe do cajado. `Crystal`: cristais da coroa, das lascas flutuantes e do cajado.

Notas:
- As plaquinhas de pedra e o drapeado dos ombros saíram do manto para duas peças próprias nos ossos dos braços (`weaver_shoulder_l/r`), pela mesma razão das ombreiras do Guardião: presas ao `chest`, os braços erguidos atravessavam. A gola alta ficou só no arco de trás pelo mesmo motivo.
- O cajado segue a convenção dos cajados do pack (ao longo do slot, orbe na ponta −Y). Em `Idle` fica apontado para a frente/baixo; em `Spellcast_Raise`/`Spellcast_Summon` fica em pé, com o orbe acima da cabeça.
- O halo e as lascas são estáticos (sem animação); dá para girar o halo ou fazer as lascas flutuarem no jogo.

Previews: `preview_front.png`, `preview_back.png`, `preview_top.png` (Idle, escala nativa); `preview_attack.png` e `preview_attack_b.png` (pico do `Spellcast_Raise`), `preview_attack_summon.png` (pico do `Spellcast_Summon`);
`compare.png` e `compare_top.png`: Guardião a 2,3x, Liora a 1x e Tecelã a 2,2x.
Scripts em `_blender/`: `weaver.py` (peças + textura), `wprev.py` (renders), `wcheck.py` (teste de interseção por pose).
