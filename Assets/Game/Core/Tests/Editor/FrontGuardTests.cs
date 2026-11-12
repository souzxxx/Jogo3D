using NUnit.Framework;
using UnityEngine;

namespace Quintessence.Core.Tests
{
    public class FrontGuardTests
    {
        [Test] public void Covers_OnlyTheArcInFront_OnTheGroundPlane()
        {
            var f = Vector3.forward;
            Assert.IsTrue(FrontGuard.Covers(f, Vector3.forward, 130f));
            Assert.IsTrue(FrontGuard.Covers(f, new Vector3(1f, 0f, 1f), 130f), "45 graus esta dentro de 65");
            Assert.IsFalse(FrontGuard.Covers(f, Vector3.right, 130f), "de lado passa");
            Assert.IsFalse(FrontGuard.Covers(f, Vector3.back, 130f), "pelas costas passa");
            Assert.IsTrue(FrontGuard.Covers(f, new Vector3(0f, 5f, 1f), 130f), "a altura nao conta");
            Assert.IsFalse(FrontGuard.Covers(f, Vector3.zero, 130f), "sem direcao nao bloqueia");
            Assert.IsFalse(FrontGuard.Covers(f, Vector3.forward, 0f), "arco zero = sem escudo");
        }

        [Test] public void Guard_CutsDirectHitsFromTheFront_AndLetsTheRestThrough()
        {
            var go = new GameObject("Escudeiro");
            var attacker = new GameObject("Heroi");
            try
            {
                var guard = go.AddComponent<FrontGuard>();
                guard.Configure(130f, 0.15f);
                attacker.transform.position = new Vector3(0f, 0f, 3f); // na frente (o escudeiro olha para +z)
                var hit = new DamageInfo(20f, ElementType.None, Team.Player, attacker) { Direction = Vector3.back };
                Assert.AreEqual(0.15f, guard.Multiplier(hit), 1e-4f);
                Assert.IsTrue(guard.LastBlocked);
                Assert.AreEqual(1, guard.Blocks);

                var reaction = hit; reaction.IsReaction = true;
                Assert.AreEqual(1f, guard.Multiplier(reaction), "reacao atravessa o escudo");
                Assert.IsFalse(guard.LastBlocked);
                var zone = new DamageInfo(20f, ElementType.Fire, Team.Player, attacker);
                Assert.AreEqual(1f, guard.Multiplier(zone), "zona e queimadura chegam sem direcao e passam");

                var behind = hit; behind.Direction = Vector3.forward;
                Assert.AreEqual(1f, guard.Multiplier(behind), "pelas costas entra inteiro");
                Assert.AreEqual(1f, guard.Multiplier(behind), "mesmo com quem bateu na frente: vale a direcao do golpe (Pilar que nasce atras)");

                var holder = go.AddComponent<StatusHolder>();
                holder.State.Apply(StatusType.Rooted, 2f);
                Assert.IsFalse(guard.Up, "enraizado, o escudo cai");
                Assert.AreEqual(1f, guard.Multiplier(hit));
                Assert.IsTrue(FrontGuard.Drops(holder.State));
                var clean = new StatusState();
                Assert.IsFalse(FrontGuard.Drops(clean));
                foreach (var t in new[] { StatusType.Stun, StatusType.Frozen, StatusType.ArmorBroken })
                {
                    var s = new StatusState(); s.Apply(t, 1f);
                    Assert.IsTrue(FrontGuard.Drops(s), t + " derruba o escudo");
                }
                var wet = new StatusState(); wet.Apply(StatusType.Soaked, 1f);
                Assert.IsFalse(FrontGuard.Drops(wet), "encharcado nao derruba");
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(attacker);
            }
        }
    }
}
