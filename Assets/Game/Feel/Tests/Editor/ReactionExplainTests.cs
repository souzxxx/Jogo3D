using NUnit.Framework;
using Quintessence.Feel;

namespace Quintessence.Feel.Tests
{
    public class ReactionExplainTests
    {
        const string Key = "quintessence.reactions.edittest";
        [SetUp] public void SetUp() { CombatFeedback.ReactionsSeen.UseKey(Key); CombatFeedback.ReactionsSeen.ResetAll(); }
        [TearDown] public void TearDown() { CombatFeedback.ReactionsSeen.ResetAll(); CombatFeedback.ReactionsSeen.UseKey(CombatFeedback.ReactionsSeen.DefaultKey); }

        [Test]
        public void Every_Reaction_In_The_Table_Has_An_Effect_Line()
        {
            foreach (var name in new[] { "Vapor", "Magma", "Tempestade de Fogo", "Lama", "Nevasca", "Estilhacos" })
                Assert.IsNotEmpty(CombatFeedback.ReactionEffect(name), name);
        }

        [Test]
        public void The_Effect_Line_Shows_Only_The_First_Time()
        {
            Assert.IsTrue(CombatFeedback.ReactionsSeen.Add("Vapor"), "primeira vez");
            Assert.IsFalse(CombatFeedback.ReactionsSeen.Add("Vapor"), "depois nao");
            CombatFeedback.ReactionsSeen.UseKey(Key); // relendo do disco continua visto
            Assert.IsTrue(CombatFeedback.ReactionsSeen.Has("Vapor"));
        }
    }
}
