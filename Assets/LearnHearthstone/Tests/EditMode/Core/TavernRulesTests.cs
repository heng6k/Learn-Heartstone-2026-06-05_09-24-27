using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
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

        [Test]
        public void GoldHelpers_ClampNormalMaxButAllowActualGoldToOverflow()
        {
            var tavern = new TavernState
            {
                Gold = 99,
                MaxGold = 98
            };

            TavernRules.IncreasePersistentMaxGold(tavern, 5);
            TavernRules.GainGold(tavern, 4);

            Assert.AreEqual(TavernRules.NormalGoldSoftCap, tavern.MaxGold);
            Assert.AreEqual(5, tavern.PersistentMaxGoldBonus);
            Assert.AreEqual(103, tavern.Gold);
            Assert.AreEqual(TavernRules.NormalGoldSoftCap, TavernRules.ClampMaxGold(500));
        }
    }
}
