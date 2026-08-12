using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class CardAcquisitionTierBoundaryTests
    {
        private static readonly Type[] SupplySelectorParameterTypes =
        {
            typeof(Tribe),
            typeof(int),
            typeof(int),
            typeof(int),
            typeof(bool),
            typeof(Func<MinionDefinition, bool>),
            typeof(bool)
        };

        [Test]
        public void OrdinaryMinionPools_UseCurrentTavernTier()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;

            var candidates = SelectSupplyMinions(service, Tribe.Beast);

            Assert.IsNotEmpty(candidates);
            Assert.That(candidates.Select(minion => minion.TavernTier), Is.All.EqualTo(1));

            Invoke(service, "StartTribeDiscover", new[] { typeof(Tribe), typeof(string) }, Tribe.Beast, "tier-boundary-test");
            Assert.IsNotNull(tavern.Discover);
            Assert.IsNotEmpty(tavern.Discover.Options);
            Assert.That(tavern.Discover.Options.Select(option => option.TavernTier), Is.All.EqualTo(1));

            tavern.Discover = null;
            tavern.Hand.Clear();
            Invoke(service, "AddRandomTribeMinionToHand", new[] { typeof(Tribe), typeof(int), typeof(string) }, Tribe.Beast, 5, "tier-boundary-test");
            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));

            tavern.Hand.Clear();
            Invoke(service, "AddRandomDistinctMagneticMechsToHand", new[] { typeof(int), typeof(string), typeof(bool) }, 5, "tier-boundary-test", false);
            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));
        }

        [Test]
        public void ExplicitMinionTierAndAnyTierRequests_RemainAvailable()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;

            var fixedTier = SelectSupplyMinions(service, exactTier: 6);
            Assert.IsNotEmpty(fixedTier);
            Assert.That(fixedTier.Select(minion => minion.TavernTier), Is.All.EqualTo(6));

            var anyTier = SelectSupplyMinions(service, allowAboveCurrentTavernTier: true);
            Assert.That(anyTier.Max(minion => minion.TavernTier), Is.EqualTo(6));

            var triple = Invoke<DiscoverState>(service, "CreateTripleDiscover", Type.EmptyTypes);
            Assert.AreEqual(2, triple.RewardTier);
            Assert.AreEqual(3, triple.Options.Count);
            Assert.That(triple.Options.Select(option => option.TavernTier), Is.All.EqualTo(2));

            tavern.Hand.Clear();
            Invoke(service, "AddRandomDistinctMagneticMechsToHand", new[] { typeof(int), typeof(string), typeof(bool) }, 5, "any-tier-test", true);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Does.Contain(4));
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Does.Contain(6));
        }

        [Test]
        public void OrdinaryTavernSpellPools_UseCurrentTavernTierAlongsideOtherFilters()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;

            var available = Invoke<IEnumerable<TavernSpellDefinition>>(
                    service,
                    "AvailableTavernSpells",
                    new[] { typeof(int) },
                    0)
                .ToList();
            Assert.IsNotEmpty(available);
            Assert.That(available.Select(spell => spell.TavernTier), Is.All.EqualTo(1));

            tavern.Hand.Clear();
            Invoke(service, "AddRandomTavernSpellToHandByCost", new[] { typeof(int), typeof(int), typeof(string) }, 2, 4, "cost-filter-test");
            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));
            Assert.That(tavern.Hand.Select(card => card.Cost), Is.All.EqualTo(2));

            tavern.Hand.Clear();
            Invoke(service, "AddRandomTavernSpellToHandMinCost", new[] { typeof(int), typeof(int), typeof(string) }, 2, 4, "min-cost-filter-test");
            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));
            Assert.That(tavern.Hand.Select(card => card.Cost), Is.All.GreaterThanOrEqualTo(2));

            tavern.Hand.Clear();
            Invoke(service, "AddRandomStatTavernSpellsToHand", new[] { typeof(int), typeof(string) }, 4, "stat-filter-test");
            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));
        }

        [TestCase("BG31_178", 4, "HandleTurnEndedForTierFourMinions")]
        [TestCase("BG28_595", 6, "HandleTurnEndedForTierSixSevenMinions")]
        public void OrdinaryEndOfTurnTavernSpellRewards_UseCurrentTavernTier(string cardId, int cardTier, string handlerName)
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            service.State.Player.Board.Add(MinionFactory.Create(
                CreateMinion(cardId, cardTier, Tribe.None),
                BoardSide.Player,
                "tier-boundary-end-turn"));

            Invoke(service, handlerName, new[] { typeof(string) }, (string)null);

            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.CardKind), Is.All.EqualTo(CardKind.TavernSpell));
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));
        }

        [Test]
        public void GeneratedSpellcraft_DefaultPoolUsesCurrentTavernTier()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            tavern.Hand.Clear();

            Invoke(service, "AddRandomSpellcraftSpellsToHand", new[] { typeof(int), typeof(string) }, 6, "generated-spellcraft-filter-test");

            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.CardId), Is.All.EqualTo("SURF_N_SURF_SPELL"));
        }

        [Test]
        public void ExplicitTavernSpellTierRequest_CanExceedCurrentTier()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            tavern.Hand.Clear();

            var added = Invoke<int>(
                service,
                "AddRandomTavernSpellToHandExactTier",
                new[] { typeof(int), typeof(int), typeof(string) },
                6,
                1,
                "explicit-tier-test");

            Assert.AreEqual(1, added);
            Assert.AreEqual(6, tavern.Hand.Single().TavernTier);
        }

        [TestCase("BGS_Treasures_100")]
        [TestCase("BGS_Treasures_106")]
        public void TavernSpellEngineRandomRewards_UseCurrentTavernTier(string cardId)
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            tavern.Hand.Clear();

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = cardId, Name = cardId },
                service.State,
                MixedTierMinionCatalog(),
                MixedTierSpellCatalog(),
                new SeededRng(12345));

            Assert.IsNotEmpty(tavern.Hand);
            Assert.That(tavern.Hand.Select(card => card.TavernTier), Is.All.EqualTo(1));
        }

        [TestCase("105664")]
        [TestCase("110400")]
        [TestCase("DEEPWATER_SCHOOL_COPY")]
        public void TavernSpellEngineRandomMinionRewards_UseCurrentTavernTier(string cardId)
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            service.State.Player.Board.Add(MinionFactory.Create(
                CreateMinion("TEST_CHEF_TARGET", 1, Tribe.Beast),
                BoardSide.Player,
                "tier-boundary-target"));

            for (var seed = 1; seed <= 24; seed += 1)
            {
                tavern.Hand.Clear();
                TavernSpellEngine.Cast(
                    new MinionInstance { CardKind = CardKind.TavernSpell, CardId = cardId, Name = cardId },
                    service.State,
                    MixedTierMinionCatalog(),
                    MixedTierSpellCatalog(),
                    new SeededRng(seed),
                    targetIndex: cardId == "105664" ? 0 : -1);

                Assert.IsNotEmpty(tavern.Hand, "Expected a generated minion for seed " + seed + ".");
                Assert.That(
                    tavern.Hand.Select(card => card.TavernTier),
                    Is.All.EqualTo(1),
                    "Generated an over-tier minion for seed " + seed + ".");
            }
        }

        [Test]
        public void TavernSpellEngineTribeDiscover_UsesCurrentTavernTier()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            tavern.Discover = null;

            TavernSpellEngine.Cast(
                new MinionInstance { CardKind = CardKind.TavernSpell, CardId = "126957", Name = "126957" },
                service.State,
                MixedTierMinionCatalog(),
                MixedTierSpellCatalog(),
                new SeededRng(12345));

            Assert.IsNotNull(tavern.Discover);
            Assert.IsNotEmpty(tavern.Discover.Options);
            Assert.That(tavern.Discover.Options.Select(option => option.TavernTier), Is.All.EqualTo(1));
        }

        [Test]
        public void CookieTribeDiscoverFallback_UsesCurrentTavernTier()
        {
            var service = CreateMixedTierService();
            var tavern = service.State.Player.Tavern;
            tavern.Tier = 1;
            tavern.Shop.Clear();
            tavern.Shop.Add(MinionFactory.Create(
                CreateMinion("TEST_COOKIE_BEAST_TARGET", 1, Tribe.Beast),
                BoardSide.Player,
                "cookie-target"));
            tavern.HeroEffectCounters["hero:cookie:fed"] = 2;
            service.State.Player.HeroPowerCardId = "BG21_HERO_020p";

            HeroEffectEngine.Dispatch(new HeroEffectContext
            {
                EventType = HeroEffectEventType.HeroPowerUsed,
                State = service.State,
                Minions = MixedTierMinionCatalog(),
                Spells = MixedTierSpellCatalog(),
                Rng = new SeededRng(9876),
                TargetIndex = 0
            });

            Assert.IsNotNull(tavern.Discover);
            Assert.IsNotEmpty(tavern.Discover.Options);
            Assert.That(tavern.Discover.Options.Select(option => option.TavernTier), Is.All.LessThanOrEqualTo(1));
        }

        [Test]
        public void CurrentPoolDiscover_UsesCurrentTavernTier()
        {
            var service = CreateMixedTierService();
            service.State.Player.Tavern.Tier = 1;

            var options = Invoke<List<MinionInstance>>(
                service,
                "CreateCurrentPoolDiscoverOptions",
                new[]
                {
                    typeof(string),
                    typeof(SeededRng),
                    typeof(Func<MinionDefinition, bool>),
                    typeof(Func<HeroBuddyDefinition, bool>)
                },
                "current-pool-tier-boundary",
                new SeededRng(2468),
                null,
                null);

            Assert.IsNotEmpty(options);
            Assert.That(options.Select(option => option.TavernTier), Is.All.LessThanOrEqualTo(1));
        }

        private static MatchService CreateMixedTierService()
        {
            var constructor = typeof(MatchService).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[]
                {
                    typeof(MinionCatalog),
                    typeof(SpellCatalog),
                    typeof(HeroCatalog),
                    typeof(TrinketCatalog),
                    typeof(QuestCatalog),
                    typeof(TimewarpedTavernCatalog),
                    typeof(AnomalyCatalog),
                    typeof(DarkmoonPrizeCatalog),
                    typeof(int),
                    typeof(ITestScenarioRepository),
                    typeof(MatchSetupOptions),
                    typeof(RecruitActionResolverRegistry),
                    typeof(DelayedObjectResolverRegistry),
                    typeof(IEnumerable<DarkGiftDefinition>),
                    typeof(DarkGiftResolverRegistry)
                },
                null);
            Assert.IsNotNull(constructor);

            return (MatchService)constructor.Invoke(new object[]
            {
                MixedTierMinionCatalog(),
                MixedTierSpellCatalog(),
                HeroCatalogLoader.LoadFromResources(),
                TrinketCatalogLoader.LoadFromResources(),
                QuestCatalogLoader.LoadFromResources(),
                TimewarpedTavernCatalogLoader.LoadFromResources(),
                AnomalyCatalogLoader.LoadFromResources(),
                DarkmoonPrizeCatalogLoader.LoadFromResources(),
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableQuests = false,
                    EnableQuestRewards = false,
                    EnableTrinkets = false,
                    EnablePlayerDirectedChoices = false,
                    EnableTimewarpedTavern = false,
                    EnableAnomalies = false
                },
                new RecruitActionResolverRegistry(),
                new DelayedObjectResolverRegistry(),
                Array.Empty<DarkGiftDefinition>(),
                new DarkGiftResolverRegistry()
            });
        }

        private static MinionCatalog MixedTierMinionCatalog()
        {
            return new MinionCatalog(new[]
            {
                CreateMinion("TEST_BEAST_T1", 1, Tribe.Beast),
                CreateMinion("TEST_BEAST_T4", 4, Tribe.Beast),
                CreateMinion("TEST_BEAST_T6", 6, Tribe.Beast),
                CreateMinion("TEST_MAGNETIC_T1", 1, Tribe.Mech, Keyword.Magnetic),
                CreateMinion("TEST_MAGNETIC_T4", 4, Tribe.Mech, Keyword.Magnetic),
                CreateMinion("TEST_MAGNETIC_T6", 6, Tribe.Mech, Keyword.Magnetic),
                CreateMinion("TEST_MURLOC_T1", 1, Tribe.Murloc),
                CreateMinion("TEST_MURLOC_T4", 4, Tribe.Murloc),
                CreateMinion("TEST_MURLOC_T6", 6, Tribe.Murloc),
                CreateMinion("TEST_UNDEAD_T1", 1, Tribe.Undead),
                CreateMinion("TEST_UNDEAD_T4", 4, Tribe.Undead),
                CreateMinion("TEST_UNDEAD_T6", 6, Tribe.Undead),
                CreateMinion("TEST_TRIPLE_T2_A", 2, Tribe.None),
                CreateMinion("TEST_TRIPLE_T2_B", 2, Tribe.None),
                CreateMinion("TEST_TRIPLE_T2_C", 2, Tribe.None)
            });
        }

        private static SpellCatalog MixedTierSpellCatalog()
        {
            return new SpellCatalog(new[]
            {
                CreateTavernSpell("TEST_SPELL_T1_C1", 1001, 1, 1),
                CreateTavernSpell("TEST_SPELL_T1_C2", 1002, 1, 2),
                CreateTavernSpell("TEST_SPELL_T4_C1", 1003, 4, 1),
                CreateTavernSpell("TEST_SPELL_T4_C2", 1004, 4, 2),
                CreateTavernSpell("TEST_SPELL_T6_C1", 1005, 6, 1),
                CreateTavernSpell("TEST_SPELL_T6_C2", 1006, 6, 2)
            });
        }

        private static MinionDefinition CreateMinion(string cardId, int tier, Tribe tribe, params Keyword[] keywords)
        {
            return new MinionDefinition
            {
                Id = cardId,
                CardId = cardId,
                Name = cardId,
                TavernTier = tier,
                BaseAttack = tier,
                BaseHealth = tier,
                InPool = true,
                PoolCount = 20,
                Tribes = new List<Tribe> { tribe },
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                EffectIds = new List<string>(),
                Tags = new List<string>()
            };
        }

        private static TavernSpellDefinition CreateTavernSpell(string cardNumber, int sourceId, int tier, int cost)
        {
            return new TavernSpellDefinition
            {
                Id = cardNumber,
                SourceId = sourceId,
                CardNumber = cardNumber,
                Name = cardNumber,
                Category = "TavernSpell",
                Cost = cost,
                TavernTier = tier,
                InPool = true,
                Text = "Give a minion +1/+1.",
                AvailableModes = new List<string>(),
                Keywords = new List<string>(),
                EffectIds = new List<string>(),
                Tags = new List<string> { "buff_spell", "spellcraft" }
            };
        }

        private static List<MinionDefinition> SelectSupplyMinions(
            MatchService service,
            Tribe tribe = Tribe.None,
            int exactTier = 0,
            int minTier = 0,
            int maxTier = 0,
            bool excludeDuos = false,
            Func<MinionDefinition, bool> predicate = null,
            bool allowAboveCurrentTavernTier = false)
        {
            return Invoke<List<MinionDefinition>>(
                service,
                "SelectSupplyMinionDefinitions",
                SupplySelectorParameterTypes,
                tribe,
                exactTier,
                minTier,
                maxTier,
                excludeDuos,
                predicate,
                allowAboveCurrentTavernTier);
        }

        private static void Invoke(object target, string methodName, Type[] parameterTypes, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.IsNotNull(method, "Private method not found: " + methodName);
            method.Invoke(target, arguments);
        }

        private static T Invoke<T>(object target, string methodName, Type[] parameterTypes, params object[] arguments)
        {
            var method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                parameterTypes,
                null);
            Assert.IsNotNull(method, "Private method not found: " + methodName);
            return (T)method.Invoke(target, arguments);
        }
    }
}
