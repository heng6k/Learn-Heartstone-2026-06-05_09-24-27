using LearnHearthstone.Domain.Engine;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class TavernRulesTests
    {
        [Test]
        public void GetMaxGoldForRound_StartsAtThreeAndCapsAtTen()
        {
            Assert.AreEqual(3, TavernRules.GetMaxGoldForRound(1));
            Assert.AreEqual(10, TavernRules.GetMaxGoldForRound(20));
        }
    }
}
