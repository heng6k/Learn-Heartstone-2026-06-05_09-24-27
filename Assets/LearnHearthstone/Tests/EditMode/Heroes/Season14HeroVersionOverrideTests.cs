using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14HeroVersionOverrideTests
    {
        private const string EdwinHeroId = "TB_BaconShop_HERO_01";
        private const string RakanishuHeroId = "TB_BaconShop_HERO_75";
        private const string CarielHeroId = "BG21_HERO_000";
        private const string RagnarosHeroId = "TB_BaconShop_HERO_11";
        private const string SaurfangHeroId = "BG20_HERO_102";
        private const string EnhanceOMechanoHeroId = "BG24_HERO_204";
        private const string XaviusHeroId = "BG36_HERO_105";
        private const string TrastathHeroId = "BG36_HERO_101";

        private static readonly Dictionary<string, string> PreviewRevisionIds = new Dictionary<string, string>
        {
            { EdwinHeroId, "hero.edwin-vancleef@36.2-preview-community-p1a" },
            { RakanishuHeroId, "hero.rakanishu@36.2-preview-community-p1a" },
            { CarielHeroId, "hero.cariel-roame@36.2-preview-community-p1a" },
            { RagnarosHeroId, "hero.ragnaros@36.2-preview-community-p1a" },
            { SaurfangHeroId, "hero.overlord-saurfang@36.2-preview-community-p1a" },
            { EnhanceOMechanoHeroId, "hero.enhance-o-mechano@36.2-preview-community-p1a" }
        };

        [Test]
        public void PreviewCatalog_AppliesSixHeroRevisionsWithoutMutatingLegacy()
        {
            var preview = Resolve(GameVersionIds.Season14Preview);
            var legacy = Resolve(GameVersionIds.LegacyCompositeSandbox);

            Assert.AreEqual(8, preview.ContentSet.HeroRevisionIds.Count);
            foreach (var expected in PreviewRevisionIds)
            {
                var revised = preview.Snapshot.English.Heroes.GetHeroByCardId(expected.Key);
                var historical = legacy.Snapshot.English.Heroes.GetHeroByCardId(expected.Key);
                Assert.AreEqual(expected.Value, revised.RevisionId, expected.Key);
                Assert.IsNotEmpty(revised.EffectRevision, expected.Key);
                Assert.IsTrue(string.IsNullOrEmpty(historical.RevisionId), expected.Key);
                Assert.IsTrue(string.IsNullOrEmpty(historical.EffectRevision), expected.Key);
            }

            Assert.AreEqual(0, preview.Snapshot.English.Heroes.GetHeroByCardId(CarielHeroId).HeroPower.Cost);
            Assert.AreEqual(1, legacy.Snapshot.English.Heroes.GetHeroByCardId(CarielHeroId).HeroPower.Cost);
            StringAssert.Contains("4 cards", preview.Snapshot.English.Heroes.GetHeroByCardId(EdwinHeroId).HeroPower.Text);
            StringAssert.Contains("5 cards", legacy.Snapshot.English.Heroes.GetHeroByCardId(EdwinHeroId).HeroPower.Text);
            StringAssert.Contains("Tavern Spells", preview.Snapshot.English.Heroes.GetHeroByCardId(RakanishuHeroId).HeroPower.Text);
            StringAssert.Contains("Lantern Light", legacy.Snapshot.English.Heroes.GetHeroByCardId(RakanishuHeroId).HeroPower.Text);
        }

        [Test]
        public void PreviewNewHeroes_UseProductionIdentityAndOnlyLocalHeroArt()
        {
            var preview = Resolve(GameVersionIds.Season14Preview).Snapshot.English.Heroes;

            foreach (var heroId in new[] { XaviusHeroId, TrastathHeroId })
            {
                var hero = preview.GetHeroByCardId(heroId);
                Assert.AreEqual(heroId == XaviusHeroId ? 132608 : 132578, hero.HeroDbfId);
                Assert.AreEqual(heroId == XaviusHeroId ? 134010 : 132581, hero.HeroPower.DbfId);
                Assert.AreEqual(heroId == XaviusHeroId ? "BG36_HERO_105p" : "BG36_HERO_101p", hero.HeroPower.CardId);
                StringAssert.StartsWith("BG36_HERO_", hero.HeroCardId);
                StringAssert.StartsWith("CardImages/Heroes/Season14/", hero.ImagePath);
                Assert.IsFalse(hero.ImagePath.StartsWith("http", StringComparison.OrdinalIgnoreCase));
                Assert.IsNotNull(Resources.Load<Texture2D>(hero.ImagePath));
                Assert.AreEqual(
                    heroId == XaviusHeroId
                        ? "3e4d5d3713dfca689b6f4751f508af5e8d31c3178d8629a17ead4a318c3dec7f"
                        : "59d0cb04e2b1f000d550a2b620728874d12e7b4c653d00015bb67a7d9fafa194",
                    hero.ImageSha256);
                Assert.IsTrue(string.IsNullOrEmpty(hero.HeroPower.ImagePath));
            }

            var texture = new Texture2D(2, 2);
            var fallback = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            try
            {
                var xaviusPower = preview.GetHeroByCardId(XaviusHeroId).HeroPower;
                Assert.AreSame(
                    fallback,
                    CardImageProvider.LoadSprite(
                        xaviusPower.ImagePath,
                        xaviusPower.CardId,
                        CardKind.HeroPower,
                        fallback));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(fallback);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void EdwinAndSaurfang_UsePreviewThresholdsAndKeepLegacyThresholds()
        {
            var previewEdwin = CreateService(GameVersionIds.Season14Preview, EdwinHeroId);
            var legacyEdwin = CreateService(GameVersionIds.LegacyCompositeSandbox, EdwinHeroId);
            previewEdwin.State.Player.Board.Add(TestMinion("preview-edwin-target"));
            legacyEdwin.State.Player.Board.Add(TestMinion("legacy-edwin-target"));
            previewEdwin.State.Player.Tavern.Gold = 10;
            legacyEdwin.State.Player.Tavern.Gold = 10;
            for (var count = 0; count < 4; count += 1)
            {
                Dispatch(previewEdwin, HeroEffectEventType.CardBought, TestMinion("preview-buy-" + count));
                Dispatch(legacyEdwin, HeroEffectEventType.CardBought, TestMinion("legacy-buy-" + count));
            }

            Dispatch(previewEdwin, HeroEffectEventType.HeroPowerUsed, targetIndex: 0);
            Dispatch(legacyEdwin, HeroEffectEventType.HeroPowerUsed, targetIndex: 0);
            Assert.AreEqual(3, previewEdwin.State.Player.Board[0].Attack);
            Assert.AreEqual(2, legacyEdwin.State.Player.Board[0].Attack);

            var previewSaurfang = CreateService(GameVersionIds.Season14Preview, SaurfangHeroId);
            var legacySaurfang = CreateService(GameVersionIds.LegacyCompositeSandbox, SaurfangHeroId);
            previewSaurfang.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("preview-shop") };
            legacySaurfang.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("legacy-shop") };
            for (var count = 0; count < 3; count += 1)
            {
                Dispatch(previewSaurfang, HeroEffectEventType.CardBought, TestMinion("preview-minion-" + count));
                Dispatch(legacySaurfang, HeroEffectEventType.CardBought, TestMinion("legacy-minion-" + count));
            }

            Assert.AreEqual(3, previewSaurfang.State.Player.Tavern.Shop[0].MaxHealth);
            Assert.AreEqual(1, legacySaurfang.State.Player.Tavern.Shop[0].MaxHealth);
            previewSaurfang.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("preview-refreshed-shop") };
            Dispatch(previewSaurfang, HeroEffectEventType.ShopRefreshed);
            Assert.AreEqual(3, previewSaurfang.State.Player.Tavern.Shop[0].MaxHealth);
        }

        [Test]
        public void RagnarosAndEnhanceOMechano_UsePreviewTriggerCountsOnly()
        {
            var previewRagnaros = CreateService(GameVersionIds.Season14Preview, RagnarosHeroId);
            var legacyRagnaros = CreateService(GameVersionIds.LegacyCompositeSandbox, RagnarosHeroId);
            previewRagnaros.State.Player.Board.Add(TestMinion("preview-rag-left"));
            previewRagnaros.State.Player.Board.Add(TestMinion("preview-rag-right"));
            legacyRagnaros.State.Player.Board.Add(TestMinion("legacy-rag-left"));
            legacyRagnaros.State.Player.Board.Add(TestMinion("legacy-rag-right"));
            HeroEffectResult unlockResult = null;
            for (var count = 0; count < 12; count += 1)
            {
                unlockResult = Dispatch(previewRagnaros, HeroEffectEventType.CardBought, TestMinion("preview-rag-buy-" + count));
                Dispatch(legacyRagnaros, HeroEffectEventType.CardBought, TestMinion("legacy-rag-buy-" + count));
            }

            Assert.AreEqual(1, unlockResult.Messages.Count(message => message.Contains("Sulfuras unlocked")));
            var afterUnlock = Dispatch(previewRagnaros, HeroEffectEventType.CardBought, TestMinion("preview-rag-buy-after-unlock"));
            Assert.IsFalse(afterUnlock.Messages.Any(message => message.Contains("Sulfuras unlocked")));

            Dispatch(previewRagnaros, HeroEffectEventType.TurnEnded);
            Dispatch(legacyRagnaros, HeroEffectEventType.TurnEnded);
            Assert.AreEqual(4, previewRagnaros.State.Player.Board[0].Attack);
            Assert.AreEqual(1, legacyRagnaros.State.Player.Board[0].Attack);

            var previewEnhance = CreateService(GameVersionIds.Season14Preview, EnhanceOMechanoHeroId);
            var repeatedPreviewEnhance = CreateService(GameVersionIds.Season14Preview, EnhanceOMechanoHeroId);
            var legacyEnhance = CreateService(GameVersionIds.LegacyCompositeSandbox, EnhanceOMechanoHeroId);
            previewEnhance.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("preview-enhance") };
            repeatedPreviewEnhance.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("preview-enhance-repeat") };
            legacyEnhance.State.Player.Tavern.Shop = new List<MinionInstance> { TestMinion("legacy-enhance") };
            Dispatch(previewEnhance, HeroEffectEventType.ShopRefreshed);
            Dispatch(repeatedPreviewEnhance, HeroEffectEventType.ShopRefreshed);
            Dispatch(legacyEnhance, HeroEffectEventType.ShopRefreshed);
            Assert.AreEqual(2, previewEnhance.State.Player.Tavern.Shop[0].Keywords.Count);
            Assert.AreEqual(1, legacyEnhance.State.Player.Tavern.Shop[0].Keywords.Count);
            CollectionAssert.AreEqual(
                previewEnhance.State.Player.Tavern.Shop[0].Keywords,
                repeatedPreviewEnhance.State.Player.Tavern.Shop[0].Keywords);
        }

        [Test]
        public void Cariel_PreviewPowerCostsZeroWhileLegacyStillCostsOne()
        {
            var preview = CreateService(GameVersionIds.Season14Preview, CarielHeroId);
            var legacy = CreateService(GameVersionIds.LegacyCompositeSandbox, CarielHeroId);
            preview.State.Player.Board.Add(TestMinion("preview-cariel"));
            legacy.State.Player.Board.Add(TestMinion("legacy-cariel"));
            preview.State.Player.Tavern.Gold = 5;
            legacy.State.Player.Tavern.Gold = 5;

            preview.Apply(new GameCommand(GameCommandType.UseHeroPower));
            legacy.Apply(new GameCommand(GameCommandType.UseHeroPower));

            Assert.AreEqual(5, preview.State.Player.Tavern.Gold);
            Assert.AreEqual(4, legacy.State.Player.Tavern.Gold);
        }

        [Test]
        public void Rakanishu_PreviewIsPassiveAndItsTavernSpellBonusImprovesEveryThirdTurn()
        {
            var preview = CreateService(GameVersionIds.Season14Preview, RakanishuHeroId);
            var legacy = CreateService(GameVersionIds.LegacyCompositeSandbox, RakanishuHeroId);
            Assert.IsFalse(preview.CanUseHeroPower());
            Assert.IsTrue(legacy.CanUseHeroPower());

            preview.State.Player.Board.Add(TestMinion("rakanishu-first"));
            preview.Apply(new GameCommand(GameCommandType.DebugCastCard, "100596", CardKind.TavernSpell, 0));
            Assert.AreEqual(6, preview.State.Player.Board[0].Attack);
            Assert.AreEqual(2, preview.State.Player.Board[0].MaxHealth);

            preview.State.Round = 3;
            Dispatch(preview, HeroEffectEventType.TurnStarted);
            preview.State.Player.Board.Add(TestMinion("rakanishu-third-turn"));
            preview.Apply(new GameCommand(GameCommandType.DebugCastCard, "100596", CardKind.TavernSpell, 1));
            Assert.AreEqual(7, preview.State.Player.Board[1].Attack);
            Assert.AreEqual(3, preview.State.Player.Board[1].MaxHealth);
        }

        [Test]
        public void PreviewSelectionAndScenarioLock_KeepTheResolvedHeroRevision()
        {
            var source = CreateService(GameVersionIds.Season14Preview, EdwinHeroId);
            var selected = source.HeroCatalog.GetHeroByCardId(source.State.Player.HeroId);
            var scenario = TestScenarioMapper.Clone(TestScenarioMapper.Capture(source.State, "p1g-hero-lock"));
            var restored = CreateService(GameVersionIds.Season14Preview, EdwinHeroId);

            Assert.AreEqual(PreviewRevisionIds[EdwinHeroId], selected.RevisionId);
            Assert.AreEqual(source.State.ContentFingerprint, scenario.ContentFingerprint);
            Assert.AreEqual(TestScenarioRestoreStatus.Applied, TestScenarioMapper.TryApplyTo(restored.State, scenario).Status);
            Assert.DoesNotThrow(() => restored.ValidateRestoredVersionLock(restored.State));

            var legacy = CreateService(GameVersionIds.LegacyCompositeSandbox, EdwinHeroId);
            Assert.Throws<InvalidDataException>(() => legacy.ValidateRestoredVersionLock(restored.State));
        }

        private static ResolvedGameVersion Resolve(string versionId)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            return snapshot.VersionedContent.CreateResolver().Resolve(versionId, snapshot);
        }

        private static MatchService CreateService(string versionId, string heroCardId)
        {
            return MatchService.CreateWithResolvedVersion(
                Resolve(versionId),
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    SelectedHeroCardId = heroCardId,
                    EnableQuests = false,
                    EnableTrinkets = false,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
        }

        private static HeroEffectResult Dispatch(
            MatchService service,
            HeroEffectEventType eventType,
            MinionInstance card = null,
            int targetIndex = -1)
        {
            return HeroEffectEngine.Dispatch(new HeroEffectContext
            {
                EventType = eventType,
                State = service.State,
                Heroes = service.HeroCatalog,
                Minions = service.Catalogs.Minions,
                Spells = service.Catalogs.Spells,
                Card = card,
                TargetIndex = targetIndex,
                TargetZone = targetIndex >= 0 ? TargetZone.FriendlyBoard : TargetZone.Unspecified,
                Rng = new SeededRng(1701)
            });
        }

        private static MinionInstance TestMinion(string instanceId)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId,
                Name = instanceId,
                CardKind = CardKind.Minion,
                TavernTier = 1,
                Attack = 1,
                Health = 1,
                MaxHealth = 1,
                Tribes = new List<Tribe> { Tribe.Beast },
                Keywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Owner = BoardSide.Player,
                PoolSource = PoolSource.Copy,
                OriginPoolSource = PoolSource.Copy
            };
        }
    }
}
