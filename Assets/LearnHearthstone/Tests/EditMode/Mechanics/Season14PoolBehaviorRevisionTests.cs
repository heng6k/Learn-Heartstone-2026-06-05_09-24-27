using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Season14PoolBehaviorRevisionTests
    {
        private const string UntilNextTurnDuration = "UNTIL_NEXT_TURN";

        [TestCase(GameVersionIds.LegacyCompositeSandbox, false, 1, 1)]
        [TestCase(GameVersionIds.LegacyCompositeSandbox, true, 2, 2)]
        [TestCase(GameVersionIds.Season14Preview, false, 2, 1)]
        [TestCase(GameVersionIds.Season14Preview, true, 4, 2)]
        public void AbyssalBrawler_TavernSpellGrowthUsesLockedVersion(
            string versionId,
            bool golden,
            int expectedAttack,
            int expectedHealth)
        {
            var service = CreateService(versionId);
            var source = CatalogMinion(service, "BG35_921", "brawler", golden);
            service.State.Player.Board.Add(source);
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "104436", CardKind.TavernSpell));

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(source.BaseAttack + expectedAttack, source.Attack);
            Assert.AreEqual(source.BaseHealth + expectedHealth, source.MaxHealth);
        }

        [TestCase(GameVersionIds.LegacyCompositeSandbox, false, 6, 2)]
        [TestCase(GameVersionIds.LegacyCompositeSandbox, true, 6, 4)]
        [TestCase(GameVersionIds.Season14Preview, false, 5, 2)]
        [TestCase(GameVersionIds.Season14Preview, true, 5, 4)]
        public void GunpowderCourier_GoldThresholdUsesLockedVersion(
            string versionId,
            bool golden,
            int threshold,
            int expectedAttack)
        {
            var service = CreateService(versionId);
            var source = CatalogMinion(service, "BG26_810", "courier", golden);
            var target = Minion("courier-target", Tribe.Pirate);
            service.State.Player.Board.AddRange(new[] { source, target });
            service.State.Player.Tavern.Gold = 20;

            SpendGoldOnRefreshes(service, threshold - 1);
            Assert.AreEqual(target.BaseAttack, target.Attack);

            SpendGoldOnRefreshes(service, 1);
            Assert.AreEqual(target.BaseAttack + expectedAttack, target.Attack);
        }

        [TestCase(GameVersionIds.LegacyCompositeSandbox, false, 3, 4)]
        [TestCase(GameVersionIds.LegacyCompositeSandbox, true, 6, 8)]
        [TestCase(GameVersionIds.Season14Preview, false, 4, 5)]
        [TestCase(GameVersionIds.Season14Preview, true, 8, 10)]
        public void DualWieldPirate_BuffUsesLockedVersion(
            string versionId,
            bool golden,
            int expectedAttack,
            int expectedHealth)
        {
            var service = CreateService(versionId);
            var source = CatalogMinion(service, "BG31_824", "dual-wield", golden);
            var firstTarget = Minion("dual-first", Tribe.Pirate);
            var secondTarget = Minion("dual-second", Tribe.Pirate);
            service.State.Player.Board.AddRange(new[] { source, firstTarget, secondTarget });
            service.State.Player.Tavern.Gold = 20;

            SpendGoldOnRefreshes(service, 5);

            Assert.AreEqual(source.BaseAttack + expectedAttack, source.Attack);
            Assert.AreEqual(source.BaseHealth + expectedHealth, source.MaxHealth);
            Assert.AreEqual(firstTarget.BaseAttack + expectedAttack, firstTarget.Attack);
            Assert.AreEqual(firstTarget.BaseHealth + expectedHealth, firstTarget.MaxHealth);
            Assert.AreEqual(secondTarget.BaseAttack, secondTarget.Attack);
            Assert.AreEqual(secondTarget.BaseHealth, secondTarget.MaxHealth);
        }

        [TestCase(GameVersionIds.LegacyCompositeSandbox, false, 0)]
        [TestCase(GameVersionIds.Season14Preview, false, 8)]
        [TestCase(GameVersionIds.Season14Preview, true, 16)]
        public void Goldrinn_PlayerBuffUsesLockedVersionAndExpiresAtNextTurn(
            string versionId,
            bool golden,
            int expectedBuff)
        {
            var service = CreateService(versionId);
            var goldrinn = CatalogMinion(service, "BGS_018", "goldrinn-player", golden);
            goldrinn.Attack = 0;
            goldrinn.Health = goldrinn.MaxHealth = 1;
            var beast = Minion("goldrinn-player-beast", Tribe.Beast);
            service.State.Player.Board.AddRange(new[] { goldrinn, beast });
            service.State.Opponent.Board.Add(Minion("goldrinn-player-killer", Tribe.None, BoardSide.Opponent, 20, 100));

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 1414, SafetyLimit = 1 }));

            Assert.AreEqual(beast.BaseAttack + expectedBuff, beast.Attack);
            Assert.AreEqual(beast.BaseHealth + expectedBuff, beast.MaxHealth);
            Assert.AreEqual(
                expectedBuff > 0 ? 1 : 0,
                beast.Enchantments.Count(enchantment => enchantment.Duration == UntilNextTurnDuration));

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(beast.BaseAttack, beast.Attack);
            Assert.AreEqual(beast.BaseHealth, beast.MaxHealth);
            Assert.IsFalse(beast.Enchantments.Any(enchantment => enchantment.Duration == UntilNextTurnDuration));
        }

        [Test]
        public void Goldrinn_OpponentBuffIsSideIsolatedAndExpiresAtNextTurn()
        {
            var service = CreateService(GameVersionIds.Season14Preview);
            var playerBeast = Minion("goldrinn-opponent-player-beast", Tribe.Beast, BoardSide.Player, 20, 100);
            service.State.Player.Board.Add(playerBeast);
            var goldrinn = CatalogMinion(service, "BGS_018", "goldrinn-opponent", false);
            goldrinn.Owner = BoardSide.Opponent;
            goldrinn.Attack = 0;
            goldrinn.Health = goldrinn.MaxHealth = 1;
            var opponentBeast = Minion("goldrinn-opponent-beast", Tribe.Beast, BoardSide.Opponent);
            service.State.Opponent.Board.AddRange(new[] { goldrinn, opponentBeast });

            service.Apply(new GameCommand(GameCommandType.RunCombatTest, new CombatTestOptions { Seed = 2424, SafetyLimit = 1 }));

            Assert.AreEqual(playerBeast.BaseAttack, playerBeast.Attack);
            Assert.AreEqual(opponentBeast.BaseAttack + 8, opponentBeast.Attack);
            Assert.AreEqual(opponentBeast.BaseHealth + 8, opponentBeast.MaxHealth);
            Assert.AreEqual(1, opponentBeast.Enchantments.Count(enchantment => enchantment.Duration == UntilNextTurnDuration));

            service.Apply(new GameCommand(GameCommandType.DebugSkipToNextTurn));

            Assert.AreEqual(opponentBeast.BaseAttack, opponentBeast.Attack);
            Assert.AreEqual(opponentBeast.BaseHealth, opponentBeast.MaxHealth);
        }

        [Test]
        public void PreviewContentSet_SelectsGoldrinnEffectRevisionOnlyForPreview()
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var preview = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var legacy = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.LegacyCompositeSandbox, snapshot);

            Assert.AreEqual(
                "minion.goldrinn@36.2-preview-v1",
                preview.EntityRevisions.Single(revision => revision.StableEntityId == "BGS_018").EffectRevision);
            Assert.IsFalse(legacy.EntityRevisions.Any(revision => revision.StableEntityId == "BGS_018"));
        }

        private static MatchService CreateService(string versionId)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var resolved = snapshot.VersionedContent.CreateResolver().Resolve(versionId, snapshot);
            var service = MatchService.CreateWithResolvedVersion(
                resolved,
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableTrinkets = false,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                });
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Shop.Clear();
            return service;
        }

        private static MinionInstance CatalogMinion(MatchService service, string cardId, string suffix, bool golden)
        {
            return MinionFactory.Create(
                service.Catalogs.Minions.GetByCardId(cardId),
                BoardSide.Player,
                suffix,
                golden,
                PoolSource.Copy,
                0);
        }

        private static void SpendGoldOnRefreshes(MatchService service, int count)
        {
            for (var index = 0; index < count; index += 1)
            {
                service.Apply(new GameCommand(GameCommandType.RerollShop));
            }
        }

        private static MinionInstance Minion(
            string id,
            Tribe tribe,
            BoardSide owner = BoardSide.Player,
            int attack = 1,
            int health = 1)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = id,
                DefinitionId = id,
                CardId = id,
                Name = id,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Owner = owner,
                TavernTier = 1,
                Tribes = new List<Tribe> { tribe },
                Keywords = new List<Keyword>(),
                OfficialKeywords = new List<Keyword>(),
                Enchantments = new List<Enchantment>(),
                Counters = new Dictionary<string, int>(),
                EffectIds = new List<string>(),
                Tags = new List<string>(),
                PoolSource = PoolSource.Copy
            };
        }
    }
}
