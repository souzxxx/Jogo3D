using NUnit.Framework;
using Quintessence.Core;
using Quintessence.Run;

namespace Quintessence.Game.Tests
{
    public class PoolTests
    {
        [Test] public void Pool_MarksWithTheElementOfItsStretch()
        {
            Assert.AreEqual(ElementType.Fire, CorruptionPool.ElementFor(Biome.Ashes), "Salões de Cinza: Fogo");
            Assert.AreEqual(ElementType.Water, CorruptionPool.ElementFor(Biome.Crypt), "Cripta Afogada: Água");
            Assert.AreEqual(ElementType.Earth, CorruptionPool.ElementFor(Biome.Garden), "Jardim de Pedra: Terra");
            Assert.AreEqual(ElementType.Fire, CorruptionPool.ElementFor(Biome.Heart));
            Assert.IsNotEmpty(Tips.Text("terrain"));
        }
    }
}
