using NUnit.Framework;

namespace Quintessence.Run.Tests
{
    public class TutorialTests
    {
        [Test]
        public void Starts_On_Move_And_Needs_Distance()
        {
            var t = new TutorialState();
            Assert.AreEqual(TutorialStep.Move, t.Step);
            Assert.IsFalse(t.Report(TutorialStep.Move, TutorialState.MoveDistance * 0.5f));
            Assert.AreEqual(0.5f, t.Progress01, 0.01f);
            Assert.IsTrue(t.Report(TutorialStep.Move, TutorialState.MoveDistance * 0.5f));
            Assert.AreEqual(TutorialStep.Dash, t.Step);
        }

        [Test]
        public void Wrong_Signal_Does_Not_Skip_A_Lesson()
        {
            var t = new TutorialState();
            Assert.IsFalse(t.Report(TutorialStep.React));
            Assert.IsFalse(t.Report(TutorialStep.Dash));
            Assert.IsFalse(t.Report(TutorialStep.Quintessence));
            Assert.AreEqual(TutorialStep.Move, t.Step);
            Assert.IsFalse(t.Report(TutorialStep.Move, -5f));
            Assert.AreEqual(0f, t.Progress01);
        }

        [Test]
        public void Attack_Needs_Three_Hits()
        {
            var t = new TutorialState();
            t.Report(TutorialStep.Move, 99f);
            t.Report(TutorialStep.Dash);
            Assert.AreEqual(TutorialStep.Attack, t.Step);
            Assert.IsFalse(t.Report(TutorialStep.Attack));
            Assert.IsFalse(t.Report(TutorialStep.Attack));
            Assert.IsTrue(t.Report(TutorialStep.Attack));
            Assert.AreEqual(TutorialStep.Skill, t.Step);
        }

        [Test]
        public void Full_Sequence_Ends_And_Stays_Done()
        {
            var t = new TutorialState();
            t.Report(TutorialStep.Move, 99f);
            t.Report(TutorialStep.Dash);
            t.Report(TutorialStep.Attack, 3f);
            t.Report(TutorialStep.Skill);
            t.Report(TutorialStep.React);
            Assert.AreEqual(TutorialStep.Quintessence, t.Step);
            Assert.AreEqual(TutorialState.StepCount, t.Number);
            Assert.IsTrue(t.Report(TutorialStep.Quintessence));
            Assert.IsTrue(t.Finished);
            Assert.IsFalse(t.Report(TutorialStep.Done));
            Assert.IsFalse(t.Report(TutorialStep.Move, 99f));
            Assert.IsTrue(t.Finished);
        }

        [Test]
        public void Skip_Goes_Straight_To_Done()
        {
            var t = new TutorialState();
            t.Skip();
            Assert.IsTrue(t.Finished);
        }

        [Test]
        public void Every_Step_Has_Title_And_Text()
        {
            foreach (TutorialStep s in System.Enum.GetValues(typeof(TutorialStep)))
            {
                Assert.IsNotEmpty(TutorialState.Title(s));
                Assert.IsNotEmpty(TutorialState.Text(s));
            }
        }
    }
}
