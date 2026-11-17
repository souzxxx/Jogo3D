# Mercador da loja — peças acopláveis

Mesmas convenções de `ArtSource/Heroes` (espaço do corpo `Mage.fbx` em repouso, origem nos pés, export igual).
Script: `_blender/merchant.py` (usa o `lib.py` de `ArtSource/Biomes/_blender`); previews por `_blender/mprev.py`.
No Unity ficam em `Assets/Art/Merchant/` e o `GameSceneBuilder.ShopDecor` prende cada uma ao osso com `HeroBuilder.Attach`,
escondendo `Mage_Hat` e `Mage_Cape` do pack. O corpo usa a textura `rogue_texture` (túnica verde).

| Peça | Arquivo | Osso | Tris | bbox min | bbox max | Materiais |
|---|---|---|---|---|---|---|
| Chapéu de aba larga com fita e pena | `merchant_hat.fbx` | `head` | 308 | -0.86, -0.681, 1.86 | 0.86, 0.928, 2.644 | Felt, FeltDark, Band, Feather, FeatherLight |
| Mochila de mascate (cobertor enrolado, panela, pergaminhos, alças) | `merchant_pack.fbx` | `chest` | 588 | -0.47, -0.405, 0.619 | 0.55, 0.897, 1.623 | Canvas, CanvasDark, Leather, Blanket, BlanketLight, Metal, Wood, Paper |
| Bolsa de moedas na cintura | `merchant_pouch.fbx` | `hips` | 220 | 0.186, -0.46, 0.49 | 0.414, -0.26, 0.81 | Pouch, Tie, Coin |

Previews: `preview_front.png`, `preview_back.png`, `preview_top.png`.
