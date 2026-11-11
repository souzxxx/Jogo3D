using System.Collections.Generic;
using NUnit.Framework;
using Quintessence.Core;
using UnityEngine;

namespace Quintessence.Combat.Tests
{
    public class MarkStateTests
    {
        [Test] public void FirstElementMarks_SameRefreshes_OtherReacts()
        {
            var m = new MarkState();
            Assert.AreEqual(MarkOutcome.Applied, m.Hit(ElementType.Fire, out _));
            Assert.AreEqual(MarkOutcome.Refreshed, m.Hit(ElementType.Fire, out _));
            Assert.AreEqual(MarkOutcome.Reaction, m.Hit(ElementType.Water, out var consumed));
            Assert.AreEqual(ElementType.Fire, consumed);
            Assert.IsFalse(m.HasMark, "a reacao consome a marca");
        }

        [Test] public void NoElementNeverMarks()
        {
            var m = new MarkState();
            Assert.AreEqual(MarkOutcome.None, m.Hit(ElementType.None, out _));
            Assert.IsFalse(m.HasMark);
        }

        [Test] public void MarkExpiresAfterFourSeconds()
        {
            var m = new MarkState();
            m.Hit(ElementType.Air, out _);
            Assert.IsFalse(m.Tick(3.9f));
            Assert.IsTrue(m.Tick(0.2f));
            Assert.IsFalse(m.HasMark);
        }

        [Test] public void CannotReactTwiceWithinHalfSecond()
        {
            var m = new MarkState();
            m.Hit(ElementType.Fire, out _);
            m.Hit(ElementType.Water, out _);
            Assert.AreEqual(MarkOutcome.Applied, m.Hit(ElementType.Water, out _));
            Assert.AreEqual(MarkOutcome.None, m.Hit(ElementType.Fire, out _), "ainda na trava de reacao");
            m.Tick(0.5f);
            Assert.AreEqual(MarkOutcome.Reaction, m.Hit(ElementType.Fire, out _));
        }
    }

    public class LoadoutStateTests
    {
        [Test] public void CannotSwitchWithOneElement()
        {
            var l = new LoadoutState(ElementType.Fire);
            Assert.IsFalse(l.TrySwitch(1));
        }

        [Test] public void SwitchCyclesAndRespectsCooldown()
        {
            var l = new LoadoutState(ElementType.Fire);
            l.Awaken(ElementType.Water); l.Awaken(ElementType.Earth);
            Assert.IsTrue(l.TrySwitch(1)); Assert.AreEqual(ElementType.Water, l.Current);
            Assert.IsFalse(l.TrySwitch(1), "recarga de 0,4 s");
            l.Tick(0.41f);
            Assert.IsTrue(l.TrySwitch(-1)); Assert.AreEqual(ElementType.Fire, l.Current);
            l.Tick(0.41f);
            Assert.IsTrue(l.TrySwitch(-1)); Assert.AreEqual(ElementType.Earth, l.Current);
        }

        [Test] public void SwitchRightAfterHitGrantsStrikeOnce()
        {
            var l = new LoadoutState(ElementType.Fire);
            l.Awaken(ElementType.Water);
            l.NotifyHitLanded(); l.Tick(0.2f);
            l.TrySwitch(1);
            Assert.IsTrue(l.ConsumeStrike());
            Assert.IsFalse(l.ConsumeStrike());
        }

        [Test] public void LateSwitchGrantsNoStrike()
        {
            var l = new LoadoutState(ElementType.Fire);
            l.Awaken(ElementType.Water);
            l.NotifyHitLanded(); l.Tick(0.35f);
            l.TrySwitch(1);
            Assert.IsFalse(l.ConsumeStrike());
        }

        [Test] public void AwakenIgnoresDuplicates()
        {
            var l = new LoadoutState(ElementType.Fire);
            Assert.IsFalse(l.Awaken(ElementType.Fire));
            Assert.IsTrue(l.Awaken(ElementType.Air));
            Assert.IsFalse(l.HasAll);
        }
    }

    public class StatusStateTests
    {
        [Test] public void BurnTicksSixTimesInThreeSeconds()
        {
            var s = new StatusState();
            s.Apply(StatusType.Burn, 3f);
            int ticks = 0;
            var ended = new List<StatusType>();
            for (int i = 0; i < 40; i++) ticks += s.Tick(0.1f, ended);
            Assert.AreEqual(6, ticks);
            Assert.Contains(StatusType.Burn, ended);
            Assert.IsFalse(s.Has(StatusType.Burn));
        }

        [Test] public void ReapplyRefreshesButDoesNotStack()
        {
            var s = new StatusState();
            Assert.IsTrue(s.Apply(StatusType.Soaked, 3f));
            s.Tick(2f);
            Assert.IsFalse(s.Apply(StatusType.Soaked, 3f));
            Assert.AreEqual(3f, s.Remaining(StatusType.Soaked), 0.001f);
        }

        [Test] public void StatusEffectsOnMovementAndActing()
        {
            var s = new StatusState();
            Assert.AreEqual(1f, s.MoveMultiplier);
            s.Apply(StatusType.Soaked, 1f); Assert.AreEqual(0.7f, s.MoveMultiplier, 0.001f);
            s.Apply(StatusType.Rooted, 1f); Assert.AreEqual(0f, s.MoveMultiplier); Assert.IsTrue(s.CanAct);
            s.Apply(StatusType.Frozen, 1f); Assert.IsFalse(s.CanAct);
            s.Apply(StatusType.ArmorBroken, 1f); Assert.AreEqual(1.25f, s.DamageTakenMultiplier, 0.001f);
        }
    }

    public class ReactionDeliveryTests
    {
        Health target, bystander;
        ReactionTable table;

        [SetUp] public void SetUp()
        {
            GameEvents.Clear();
            target = Make("target", Vector3.zero);
            bystander = Make("bystander", new Vector3(1.5f, 0, 0));
            table = ScriptableObject.CreateInstance<ReactionTable>();
            table.reactions.Add(new Reaction { name = "Vapor", a = ElementType.Fire, b = ElementType.Water, damage = 8, radius = 3 });
            Physics.SyncTransforms();
        }

        [TearDown] public void TearDown()
        {
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(bystander.gameObject);
            Object.DestroyImmediate(table);
            GameEvents.Clear();
        }

        static Health Make(string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            go.AddComponent<SphereCollider>().radius = 0.5f;
            var h = go.AddComponent<Health>();
            h.Configure(100, Team.Enemy);
            return h;
        }

        [Test] public void TableIsSymmetric()
        {
            Assert.IsNotNull(table.Find(ElementType.Water, ElementType.Fire));
            Assert.IsNull(table.Find(ElementType.Fire, ElementType.Air));
        }

        [Test] public void SecondElementTriggersReactionThatHitsTheArea()
        {
            string reaction = null;
            GameEvents.ReactionTriggered += e => reaction = e.Name;
            ElementCombat.Deliver(target, new DamageInfo(5, ElementType.Fire, Team.Player), table);
            Assert.IsNull(reaction, "o primeiro golpe so marca");
            Assert.AreEqual(100, bystander.Current);
            ElementCombat.Deliver(target, new DamageInfo(5, ElementType.Water, Team.Player), table);
            Assert.AreEqual("Vapor", reaction);
            Assert.AreEqual(100 - 5 - 5 - 8, target.Current, 0.001f);
            Assert.AreEqual(100 - 8, bystander.Current, 0.001f, "a reacao acerta quem esta no raio");
        }

        [Test] public void ReactionDamageDoesNotChainReactions()
        {
            int reactions = 0;
            GameEvents.ReactionTriggered += _ => reactions++;
            ElementCombat.Deliver(bystander, new DamageInfo(1, ElementType.Fire, Team.Player), table);
            ElementCombat.Deliver(target, new DamageInfo(1, ElementType.Fire, Team.Player), table);
            ElementCombat.Deliver(target, new DamageInfo(1, ElementType.Water, Team.Player), table);
            Assert.AreEqual(1, reactions, "o dano de reacao no vizinho marcado nao dispara outra reacao");
        }
    }
}
