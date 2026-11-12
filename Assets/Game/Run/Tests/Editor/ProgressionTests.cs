using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Quintessence.Core;

namespace Quintessence.Run.Tests
{
    public class ProgressionTests
    {
        [Test] public void StartsAtLevelOne_WithNothingPending()
        {
            var p = new Progression();
            Assert.AreEqual(1, p.Level);
            Assert.AreEqual(0, p.PendingPicks);
            Assert.AreEqual(0f, p.Normalized);
        }

        [Test] public void EnoughXp_RaisesLevel_AndKeepsTheRest()
        {
            var p = new Progression();
            int need = p.XpToNext;
            Assert.AreEqual(1, p.AddXp(need + 3));
            Assert.AreEqual(2, p.Level);
            Assert.AreEqual(3, p.Xp);
            Assert.AreEqual(1, p.PendingPicks);
        }

        [Test] public void BigGain_CanRaiseSeveralLevels()
        {
            var p = new Progression();
            int gained = p.AddXp(Progression.XpFor(1) + Progression.XpFor(2) + Progression.XpFor(3));
            Assert.AreEqual(3, gained);
            Assert.AreEqual(4, p.Level);
            Assert.AreEqual(3, p.PendingPicks);
        }

        [Test] public void EachLevelCostsMoreThanTheLast()
        {
            for (int l = 1; l < Progression.MaxLevel; l++) Assert.Greater(Progression.XpFor(l + 1), Progression.XpFor(l));
        }

        [Test] public void StopsAtMaxLevel()
        {
            var p = new Progression();
            p.AddXp(100000);
            Assert.AreEqual(Progression.MaxLevel, p.Level);
            Assert.AreEqual(0, p.AddXp(500));
            Assert.AreEqual(1f, p.Normalized);
        }

        [Test] public void NegativeOrZeroXp_DoesNothing()
        {
            var p = new Progression();
            Assert.AreEqual(0, p.AddXp(0));
            Assert.AreEqual(0, p.AddXp(-10));
            Assert.AreEqual(0, p.Xp);
        }

        [Test] public void ConsumePick_OnlyWhenThereIsOne()
        {
            var p = new Progression();
            Assert.IsFalse(p.ConsumePick());
            p.AddXp(p.XpToNext);
            Assert.IsTrue(p.ConsumePick());
            Assert.IsFalse(p.ConsumePick());
        }

        [Test] public void BiggerEnemies_GiveMoreXp_BossGivesMost()
        {
            Assert.Greater(Progression.XpForEnemy(90), Progression.XpForEnemy(30));
            Assert.Greater(Progression.XpForEnemy(0, true), Progression.XpForEnemy(90));
            Assert.GreaterOrEqual(Progression.XpForEnemy(1), 2);
        }

        [Test] public void TalentIds_AreUnique_AndTextsFilled()
        {
            var ids = new HashSet<string>();
            foreach (var t in TalentCatalog.All)
            {
                Assert.IsTrue(ids.Add(t.Id), t.Id);
                Assert.IsNotEmpty(t.Name); Assert.IsNotEmpty(t.Description);
                Assert.GreaterOrEqual(t.MaxRank, 1);
            }
            foreach (var e in Elements.All) Assert.GreaterOrEqual(TalentCatalog.All.Count(t => t.Element == e), 3, e.ToString());
        }

        [Test] public void Offer_OnlyHasAwakenedElements_AndNoRepeats([NUnit.Framework.Range(1, 25)] int seed)
        {
            var offer = TalentCatalog.Offer(new TalentBook(), new[] { ElementType.Fire }, new System.Random(seed));
            Assert.AreEqual(3, offer.Count);
            Assert.AreEqual(3, offer.Select(t => t.Id).Distinct().Count());
            foreach (var t in offer) Assert.IsTrue((t.Element == ElementType.None || t.Element == ElementType.Fire) && t.Second == ElementType.None, t.Id);
        }

        [Test] public void Talent_StopsBeingOffered_AtMaxRank()
        {
            var book = new TalentBook();
            var mods = new HeroModifiers();
            var asas = TalentCatalog.Find("air_dash");
            Assert.IsTrue(book.Take(asas, mods));
            Assert.IsFalse(book.Take(asas, mods));
            Assert.AreEqual(1, mods.DashChargesAdd);
            for (int seed = 1; seed < 40; seed++)
                Assert.IsFalse(TalentCatalog.Offer(book, Elements.All, new System.Random(seed)).Any(t => t.Id == "air_dash"));
        }

        [Test] public void EveryTalent_ChangesTheModifiers()
        {
            foreach (var t in TalentCatalog.All)
            {
                var m = new HeroModifiers();
                string before = Dump(m);
                TalentCatalog.Apply(m, t);
                Assert.AreNotEqual(before, Dump(m), t.Id + " nao mudou nada");
            }
        }

        [Test] public void Reset_ClearsTalentEffects()
        {
            var m = new HeroModifiers();
            string clean = Dump(m);
            foreach (var t in TalentCatalog.All) TalentCatalog.Apply(m, t);
            m.Reset();
            Assert.AreEqual(clean, Dump(m));
        }

[Test] public void Deepen_RaisesTheSkill_UpToRankThree()
        {
            var run = new RunState(RunMapGenerator.Generate(1));
            var m = new HeroModifiers();
            Assert.AreEqual(1, run.SkillRank[(int)ElementType.Fire]);
            Assert.IsTrue(run.Deepen(ElementType.Fire, m));
            Assert.IsTrue(run.Deepen(ElementType.Fire, m));
            Assert.IsFalse(run.Deepen(ElementType.Fire, m), "grau maximo e III");
            Assert.AreEqual(3, run.SkillRank[(int)ElementType.Fire]);
            Assert.AreEqual(1.6f, m.Damage(ElementType.Fire), 0.001f);
            Assert.AreEqual(0.81f, m.SkillCooldown(ElementType.Fire), 0.001f);
            Assert.AreEqual(1f, m.Damage(ElementType.Water), 0.001f, "so a skill aprofundada muda");
            Assert.AreEqual("III", RunState.Roman(3));
            Assert.Greater(m.FireTrailSeconds, 0f, "Labareda III deixa o chao em chamas");
            var air = new HeroModifiers();
            run.Deepen(ElementType.Air, air);
            Assert.AreEqual(0, air.DashChargesAdd, "grau II e so numero");
            run.Deepen(ElementType.Air, air);
            Assert.AreEqual(1, air.DashChargesAdd, "Vendaval III da mais uma carga de dash");
            foreach (var e in Elements.All) Assert.IsNotEmpty(RunState.Signature(e));
        }

        [Test] public void ReactionTalents_NeedBothElements_AndBoostOnlyTheirPair()
        {
            var pairs = TalentCatalog.All.Where(t => t.Effect == TalentEffect.ReactionBoost).ToList();
            Assert.AreEqual(6, pairs.Count, "uma por reacao");
            for (int seed = 1; seed < 60; seed++)
            {
                foreach (var t in TalentCatalog.Offer(new TalentBook(), new[] { ElementType.Fire, ElementType.Water }, new System.Random(seed)))
                    if (t.Second != ElementType.None)
                        Assert.AreEqual("react_steam", t.Id, "com Fogo e Agua, so o Vapor");
            }
            var m = new HeroModifiers();
            TalentCatalog.Apply(m, TalentCatalog.Find("react_steam"));
            Assert.AreEqual(1.4f, m.ReactionBonus(ElementType.Water, ElementType.Fire), 0.001f, "simetrico");
            Assert.AreEqual(1f, m.ReactionBonus(ElementType.Fire, ElementType.Earth), 0.001f, "o Magma nao muda");
        }

                static string Dump(HeroModifiers m)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var f in typeof(HeroModifiers).GetFields()) sb.Append(f.Name).Append('=').Append(f.GetValue(m)).Append(';');
            foreach (var e in Elements.All) sb.Append(m.Damage(e)).Append('|').Append(m.SkillRange(e)).Append('|').Append(m.SkillCooldown(e)).Append(';');
            foreach (var a in Elements.All) foreach (var b in Elements.All) sb.Append(m.ReactionBonus(a, b)).Append(',');
            return sb.ToString();
        }

        [Test] public void AwakeningTalent_OnlyComesWithThreeElements()
        {
            var book = new TalentBook();
            for (int seed = 0; seed < 200; seed++)
            {
                var early = TalentCatalog.Offer(book, new[] { ElementType.Fire }, new System.Random(seed));
                Assert.IsFalse(early.Any(t => t.Effect == TalentEffect.BurstPower), "com um elemento nao oferece o despertar");
            }
            bool seen = false;
            for (int seed = 0; seed < 300 && !seen; seed++)
                seen = TalentCatalog.Offer(book, new[] { ElementType.Fire, ElementType.Water, ElementType.Earth }, new System.Random(seed)).Any(t => t.Effect == TalentEffect.BurstPower);
            Assert.IsTrue(seen, "com tres elementos ele aparece");
            var m = new HeroModifiers();
            TalentCatalog.Apply(m, TalentCatalog.All.First(t => t.Id == "body_footing"));
            Assert.IsTrue(m.SureFooted);
            TalentCatalog.Apply(m, TalentCatalog.All.First(t => t.Id == "body_flank"));
            Assert.AreEqual(0.25f, m.FlankBonus, 1e-4f);
        }

        [Test] public void ComboTalents_WaitForTheElementsTheyNeed()
        {
            var book = new TalentBook();
            for (int seed = 0; seed < 200; seed++)
            {
                var one = TalentCatalog.Offer(book, new[] { ElementType.Fire }, new System.Random(seed));
                Assert.IsFalse(one.Any(t => t.Effect == TalentEffect.Contagion), "Contagio pede dois elementos (reacao)");
                var two = TalentCatalog.Offer(book, new[] { ElementType.Fire, ElementType.Water }, new System.Random(seed));
                Assert.IsFalse(two.Any(t => t.Effect == TalentEffect.Alchemy), "Alquimia pede tres (Fusao)");
            }
            var m = new HeroModifiers();
            TalentCatalog.Apply(m, TalentCatalog.All.First(t => t.Id == "body_contagion"));
            Assert.AreEqual(5f, m.MarkSpread, 1e-4f);
            TalentCatalog.Apply(m, TalentCatalog.All.First(t => t.Id == "body_alchemy"));
            Assert.AreEqual(0.4f, m.FusionBonus, 1e-4f);
        }

        [Test] public void Synergy_ThreeAndFivePointsOfAnElement_StrengthenIt()
        {
            var book = new TalentBook();
            var m = new HeroModifiers();
            Talent T(string id) => TalentCatalog.All.First(t => t.Id == id);
            book.Take(T("fire_trail"), m);
            book.Take(T("fire_range"), m);
            Assert.AreEqual(0, book.LastSynergyTier);
            Assert.AreEqual(1f, m.Damage(ElementType.Fire), 1e-4f);
            Assert.AreEqual("Sintonia de Fogo 3/3!", Synergy.Hint(book, T("fire_burning")), "o cartao avisa que este fecha a Sintonia");
            book.Take(T("fire_burning"), m);
            Assert.AreEqual(ElementType.Fire, book.LastSynergy);
            Assert.AreEqual(1, book.LastSynergyTier);
            Assert.AreEqual(1f + Synergy.DamageBonus, m.Damage(ElementType.Fire), 1e-4f, "grau 1: +20% no Fogo");
            Assert.AreEqual(1f, m.Damage(ElementType.Water), 1e-4f, "os outros elementos nao mudam");
            // talento de reacao conta para os dois elementos do par
            var steam = TalentCatalog.All.First(t => t.Effect == TalentEffect.ReactionBoost && ((t.Element == ElementType.Fire && t.Second == ElementType.Water) || (t.Element == ElementType.Water && t.Second == ElementType.Fire)));
            StringAssert.StartsWith("Sintonia: ", Synergy.Hint(book, steam), "talento de reacao mostra os dois elementos");
            StringAssert.Contains("Água 1/3", Synergy.Hint(book, steam));
            book.Take(steam, m);
            Assert.AreEqual(4, Synergy.Points(book, ElementType.Fire));
            Assert.AreEqual(1, Synergy.Points(book, ElementType.Water));
            float cdBefore = m.SkillCooldown(ElementType.Fire);
            book.Take(T("fire_range"), m); // segundo grau do mesmo talento tambem conta
            Assert.AreEqual(2, book.LastSynergyTier, "cinco pontos: grau 2");
            Assert.AreEqual(cdBefore * Synergy.CooldownFactor, m.SkillCooldown(ElementType.Fire), 1e-4f);
            Assert.AreEqual(1f + Synergy.DamageBonus, m.Damage(ElementType.Fire), 1e-4f, "o bonus de dano nao dobra");
            // voltar de um save nao aplica de novo
            var restored = new TalentBook();
            var clean = new HeroModifiers();
            foreach (var t in book.Taken) restored.Restore(t);
            Assert.AreEqual(1f, clean.Damage(ElementType.Fire));
            Assert.AreEqual(2, Synergy.Tier(restored, ElementType.Fire));
            Assert.AreEqual("", Synergy.Hint(book, T("body_basic")), "talento de corpo nao entra na Sintonia");
        }

        [Test] public void Synergy_OneReactionTalent_CanRaiseBothElements()
        {
            var book = new TalentBook();
            var m = new HeroModifiers();
            Talent T(string id) => TalentCatalog.All.First(t => t.Id == id);
            book.Take(T("fire_trail"), m); book.Take(T("fire_range"), m);
            book.Take(T("water_heal"), m); book.Take(T("water_freeze"), m);
            var steam = TalentCatalog.All.First(t => t.Effect == TalentEffect.ReactionBoost && ((t.Element == ElementType.Fire && t.Second == ElementType.Water) || (t.Element == ElementType.Water && t.Second == ElementType.Fire)));
            book.Take(steam, m);
            Assert.AreEqual(2, book.LastSynergies.Count, "Fogo e Agua chegam a 3 juntos");
            Assert.AreEqual(1f + Synergy.DamageBonus, m.Damage(ElementType.Fire), 1e-4f);
            Assert.AreEqual(1f + Synergy.DamageBonus, m.Damage(ElementType.Water), 1e-4f);
        }
    }
}
