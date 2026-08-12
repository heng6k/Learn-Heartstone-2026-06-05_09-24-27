using System.Linq;
using LearnHearthstone.Adapters.Data;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class SpellCatalogTests
    {
        [Test]
        public void LoadFromJson_LoadsYingdiBattlegroundsSpellPayload()
        {
            var catalog = SpellCatalogLoader.LoadFromResources();
            var spell = catalog.GetBySourceId(34597);
            var tierOneSpells = catalog.GetTavernSpellsForTier(1);

            Assert.AreEqual(86, catalog.All.Count);
            Assert.AreEqual(69, catalog.All.Count(candidate => candidate.InPool && candidate.Category == "TavernSpell" && !new[] { "119603", "122489", "123553", "127642" }.Contains(candidate.CardNumber)));
            Assert.AreEqual("\u5c16\u5229\u7bad\u77e2", spell.Name);
            Assert.AreEqual("Pointy Arrow", spell.EnglishName);
            Assert.AreEqual("100596", spell.CardNumber);
            Assert.AreEqual(1, spell.Cost);
            Assert.AreEqual(1, spell.TavernTier);
            Assert.IsTrue(spell.InPool);
            Assert.AreEqual("TavernSpell", spell.Category);
            Assert.AreEqual("\u4f7f\u4e00\u4e2a\u968f\u4ece\u83b7\u5f97+4\u653b\u51fb\u529b\u3002", spell.Text);
            Assert.AreEqual("CardImages/TavernSpells/EBG_Spell_014", spell.ImagePath);
            Assert.IsNotNull(Resources.Load<Texture2D>(spell.ImagePath));
            Assert.IsTrue(tierOneSpells.Contains(spell));
            Assert.AreEqual(8, tierOneSpells.Count);
            Assert.IsTrue(tierOneSpells.TrueForAll(candidate => candidate.InPool && candidate.Category == "TavernSpell" && candidate.TavernTier <= 1));
            Assert.IsTrue(catalog.All.All(candidate => ContainsChinese(candidate.Name) && ContainsChinese(candidate.Text)));
            Assert.Contains("targeted_spell", spell.Tags);
            Assert.Contains("targeted_attack_buff", spell.Tags);
            Assert.Contains("economy_spell", catalog.GetBySourceId(34609).Tags);
            Assert.Contains("discover_spell", catalog.GetBySourceId(48236).Tags);
            Assert.Contains("shop_steal", catalog.GetBySourceId(34619).Tags);
            Assert.Throws<System.InvalidOperationException>(() => catalog.GetBySourceId(38647));
            Assert.Throws<System.InvalidOperationException>(() => catalog.GetBySourceId(38648));
            Assert.Throws<System.InvalidOperationException>(() => catalog.GetBySourceId(38649));
            Assert.IsNull(Resources.Load<Texture2D>("CardImages/TavernSpells/BG31_242"));
            Assert.IsNull(Resources.Load<Texture2D>("CardImages/TavernSpells/BG31_243"));
            Assert.IsNull(Resources.Load<Texture2D>("CardImages/TavernSpells/BG31_244"));
        }

        [Test]
        public void LoadFromResources_EnglishCatalogHasCompleteNamesAndDescriptions()
        {
            var catalog = SpellCatalogLoader.LoadFromResources(true);
            var spell = catalog.GetBySourceId(34597);

            Assert.AreEqual(86, catalog.All.Count);
            Assert.AreEqual("Pointy Arrow", spell.Name);
            Assert.AreEqual("Pointy Arrow", spell.EnglishName);
            Assert.IsTrue(spell.Text.Contains("+4 Attack"));
            Assert.AreEqual(spell.EnglishText, spell.Text);
            Assert.IsTrue(catalog.All.All(candidate =>
                !string.IsNullOrWhiteSpace(candidate.Name) &&
                !string.IsNullOrWhiteSpace(candidate.Text) &&
                !candidate.Name.StartsWith("[Missing en-US:") &&
                !candidate.Text.StartsWith("[Missing en-US:") &&
                !ContainsChinese(candidate.Name) &&
                !ContainsChinese(candidate.Text)));
        }

        [TestCase("115910")]
        [TestCase("116596")]
        [TestCase("116221")]
        [TestCase("117567")]
        [TestCase("117584")]
        [TestCase("132903")]
        [TestCase("132995")]
        [TestCase("133369")]
        [TestCase("133371")]
        [TestCase("133711")]
        public void Season14Spell_LocalizedArtIsBundledAtCatalogPath(string cardNumber)
        {
            var spell = SpellCatalogLoader.LoadFromResources().All.Single(item => item.CardNumber == cardNumber);

            Assert.IsFalse(string.IsNullOrWhiteSpace(spell.ImagePath));
            Assert.IsNotNull(Resources.Load<Texture2D>(spell.ImagePath), spell.EnglishName + " art is missing.");
        }

        private static bool ContainsChinese(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Any(character => character >= '\u4e00' && character <= '\u9fff');
        }
    }
}
