# Chefe — Guardião das Cinzas (peças acopláveis sobre o Skeleton_Warrior do KayKit)

Mesmo contrato das peças dos heróis (`ArtSource/Heroes/README.md`): malhas estáticas no espaço de modelo do corpo em
repouso, origem nos pés, 1 unidade = 1 m na escala nativa do FBX (o jogo amplia 2,3x tudo junto). Instanciar na raiz
com identidade e reparentar ao osso mantendo a posição de mundo, com o rig em bind pose.
Export: `axis_forward='-Z'`, `axis_up='Y'`, `global_scale=1`, `apply_unit_scale=True`, `apply_scale_options='FBX_SCALE_ALL'`,
`bake_space_transform=True`, só malha, `mesh_smooth_type='EDGE'`, triangulado.

- Corpo: `Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Warrior.fbx`
- Textura: `boss_body_texture.png` (substitui `skeleton_texture`; mesmo atlas 1024², 8×4): osso cinza-carvão, armadura cinza-pedra,
  couro escuro, manchas e rachaduras quentes pintadas nas células de osso e armadura.
- **Esconder do pack**: `Skeleton_Warrior_Helmet` (o elmo novo substitui) e `Skeleton_Warrior_Cloak` (a peça das costas substitui).
- **Manter**: `Skeleton_Warrior_Head`, `Skeleton_Warrior_Jaw`, `Skeleton_Warrior_Eyes` (o crânio fica por baixo do elmo) e o resto do corpo.

bbox em metros no espaço do Blender (X lateral, Y trás(+)/frente(−), Z altura). Unity: x=−x, y=z, z=−y.

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Elmo-coroa quebrado + chifres de cristal | `boss_head.fbx` | `head` | 746 | -0.627, -0.6, 1.29 | 0.897, 0.662, 2.797 | Metal, Stone, StoneDark, Crystal, **Magic** (rachadura no domo) |
| Ombreiras assimétricas | `boss_shoulders.fbx` | `chest` | 680 | -1.172, -0.413, 0.959 | 1.012, 0.538, 2.171 | Stone, StoneDark, Crystal, **Magic** (rachadura na ombreira de pedra) |
| Capa rasgada + leque de cristais | `boss_back.fbx` | `chest` | 396 | -0.544, 0.14, 0.327 | 0.622, 0.954, 2.204 | Cloth, StoneDark, Crystal |
| Espadão de pedra (2 mãos) | `boss_weapon.fbx` | `handslot.r` | 788 | -1.333, -2.46, 0.908 | -0.433, 0.64, 1.199 | Stone, StoneDark, Metal, Leather, Crystal, **Magic** (veio central) |
| Ombreira esquerda de pedra (substitui a metade + de `boss_shoulders`) | `boss_shoulder_l.fbx` | `upperarm.l` | 344 | 0.167, -0.39, 0.957 | 0.905, 0.39, 1.699 | Stone, StoneDark, **Magic** (rachadura) |
| Ombreira direita, cacho de cristais | `boss_shoulder_r.fbx` | `upperarm.r` | 336 | -1.175, -0.413, 0.978 | -0.221, 0.537, 2.038 | StoneDark, Stone, Crystal |
| Escudo-torre partido (opcional) | `boss_shield.fbx` | `handslot.l` | 588 | 0.348, -0.978, 1.09 | 1.513, 0.835, 1.396 | Stone, StoneDark, Metal, Leather, Crystal, **Magic** (rachadura) |
| Núcleo de brasa | `boss_core.fbx` | `chest` | 110 | -0.144, -0.492, 0.8 | 0.294, 0.073, 1.351 | **Magic** |

Cores base: Stone #77736e, StoneDark #4b4846, Metal #3d3f45, Cloth #3e2622, Leather #4a3128, Crystal #cf7c2b (âmbar fosco), Magic #d9632a. Roughness 0.8.

Notas:
- Lado corrompido: esquerdo do personagem na cabeça (chifres de cristal) e direito no ombro (cacho de cristais); a ombreira esquerda é a de pedra.
- O espadão segue a orientação das armas 2H do pack (lâmina ao longo do slot, plana em XY); o escudo fica deitado acima da mão esquerda como os escudos do pack.
- Os olhos (`Skeleton_Warrior_Eyes`) usam material próprio do pack; a célula de olhos do atlas foi pintada de âmbar, mas confira no jogo se o material dos olhos usa a textura.

Previews: `preview_front.png`, `preview_back.png`, `preview_top.png` (escala nativa, pose `Idle`), `scale.png` e `scale_top.png` (Brann ao lado do chefe a 2,3x).
Regerar: `_blender/boss.py` (peças + textura) e `_blender/preview.py -- boss` / `-- scale` (caminhos do PC remoto `~/quint_heroes`).

## Rodada 7 — ombreiras por braço
- `boss_shoulder_l.fbx` → `upperarm.l` e `boss_shoulder_r.fbx` → `upperarm.r` substituem `boss_shoulders.fbx` (que continua na pasta, presa ao `chest`; não usar as duas juntas).
- O rig não tem osso de ombro/clavícula: a cadeia é `chest > upperarm.l/.r > lowerarm > wrist > hand > handslot`. A articulação do ombro fica em (±0.212, 0, 1.107).
- Toda a geometria fica a |x| ≥ 0.17, por fora da articulação, para girar junto com o braço sem entrar no peito. Os cristais da direita apontam para fora e para trás, nunca para dentro.
- Verificação: `preview_shoulders_idle.png` (Idle), `preview_shoulders_attack.png` e `_attack_b.png` (pico do `1H_Melee_Attack_Jump_Chop`, braço alto), `preview_shoulders_cheer.png` (Cheer, sem arma e escudo, teste de estresse).
