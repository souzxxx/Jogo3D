using NUnit.Framework;
using Quintessence.Core;

namespace Quintessence.Game.Tests
{
    public class ShrineHintTests
    {
        [Test]
        public void Pedestal_Names_The_Skill_And_The_Reaction_It_Opens()
        {
            var text = RunDirector.ShrineHint(ElementType.Water, new[] { ElementType.Fire });
            StringAssert.Contains("Maré", text);
            StringAssert.Contains("com Fogo: Vapor", text);
        }

        [Test]
        public void Every_Pair_Of_Elements_Has_A_Reaction_Hint()
        {
            foreach (var a in Elements.All)
                foreach (var b in Elements.All)
                    if (a != b) Assert.IsNotEmpty(RunDirector.ReactionHint(a, b), $"{a}+{b}");
        }

        [Test]
        public void Without_Another_Element_Only_The_Skill_Shows()
        {
            Assert.AreEqual("Skill: Vendaval", RunDirector.ShrineHint(ElementType.Air, new[] { ElementType.Air }));
        }
    }
}
