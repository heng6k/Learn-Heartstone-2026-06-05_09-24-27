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

            Assert.AreEqual(60, catalog.All.Count);
            Assert.AreEqual("\u5c16\u5229\u7bad\u77e2", spell.Name);
            Assert.AreEqual("Pointy Arrow", spell.EnglishName);
            Assert.AreEqual("100596", spell.CardNumber);
            Assert.AreEqual(1, spell.Cost);
            Assert.AreEqual(1, spell.TavernTier);
            Assert.AreEqual("\u4f7f\u4e00\u4e2a\u968f\u4ece\u83b7\u5f97+4\u653b\u51fb\u529b\u3002", spell.Text);
            Assert.AreEqual("CardImages/TavernSpells/EBG_Spell_014", spell.ImagePath);
            Assert.IsNotNull(Resources.Load<Texture2D>(spell.ImagePath));
        }
    }
}
