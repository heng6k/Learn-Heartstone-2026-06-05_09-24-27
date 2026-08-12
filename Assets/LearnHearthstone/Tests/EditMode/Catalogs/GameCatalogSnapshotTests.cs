using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.Catalogs
{
    public sealed class GameCatalogSnapshotTests
    {
        [Test]
        public void EmbeddedSnapshot_KeepsStableChineseAndEnglishCatalogSets()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("test-client");

            Assert.AreEqual("embedded:test-client", snapshot.Info.ContentVersion);
            Assert.AreEqual("test-client", snapshot.Info.RequiredClientVersion);
            Assert.AreEqual(ContentSnapshotSource.Embedded, snapshot.Info.Source);
            Assert.AreSame(snapshot.Chinese, snapshot.ForLanguage(false));
            Assert.AreSame(snapshot.English, snapshot.ForLanguage(true));
            Assert.AreSame(snapshot.Chinese.Heroes, snapshot.English.Heroes);
            Assert.AreSame(snapshot.Chinese.TimewarpedTavern, snapshot.English.TimewarpedTavern);
            Assert.AreEqual(snapshot.Chinese.Minions.All.Count, snapshot.English.Minions.All.Count);
            Assert.AreEqual(snapshot.Chinese.Spells.All.Count, snapshot.English.Spells.All.Count);
        }

        [Test]
        public void MatchService_CreateWithCatalogs_RetainsInjectedSessionSet()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("test-client");

            var service = MatchService.CreateWithCatalogs(snapshot.English, setup: new MatchSetupOptions { UseEnglish = true });

            Assert.AreSame(snapshot.English, service.Catalogs);
            Assert.AreSame(snapshot.English.Heroes, service.HeroCatalog);
            Assert.IsTrue(service.UseEnglish);
        }

        [Test]
        public void MatchService_CreateWithDefaultCatalog_ReusesImmutableCatalogSetPerLanguage()
        {
            var chineseFirst = MatchService.CreateWithDefaultCatalog(101);
            var chineseSecond = MatchService.CreateWithDefaultCatalog(102);
            var englishFirst = MatchService.CreateWithDefaultCatalog(103, setup: new MatchSetupOptions { UseEnglish = true });
            var englishSecond = MatchService.CreateWithDefaultCatalog(104, setup: new MatchSetupOptions { UseEnglish = true });

            Assert.AreSame(chineseFirst.Catalogs, chineseSecond.Catalogs);
            Assert.AreSame(englishFirst.Catalogs, englishSecond.Catalogs);
            Assert.AreNotSame(chineseFirst.Catalogs, englishFirst.Catalogs);
        }
    }
}
