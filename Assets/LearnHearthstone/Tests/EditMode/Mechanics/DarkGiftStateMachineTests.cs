using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class DarkGiftStateMachineTests
    {
        [Test]
        public void Acquire_ResolvesByEffectRevisionBindsMinionAndWritesOrderedEvents()
        {
            var state = CreateState(out var target);
            var definition = Definition("gift-a", "gift-effect@1");
            var registry = new DarkGiftResolverRegistry();
            registry.Register("gift-effect@1", context =>
            {
                Assert.AreEqual(DarkGiftResolutionPhase.Acquire, context.Phase);
                Assert.AreEqual(target.InstanceId, context.Target.InstanceId);
                Assert.AreNotSame(target, context.Target);
                Assert.AreEqual(definition.RevisionId, context.Instance.DefinitionRevisionId);
                return DarkGiftResolution.Success(
                    "buffed",
                    (live, resolvedTarget) => MechanicEngine.ApplyToMinion(resolvedTarget, new MechanicAction
                    {
                        Type = MechanicActionType.BuffStats,
                        Scope = BuffScope.Instance,
                        Attack = 2,
                        Health = 3,
                        SourceId = definition.RevisionId
                    }));
            });

            var result = DarkGiftStateMachine.Acquire(
                state,
                target,
                definition,
                "normal-button",
                "request-a",
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, state.PlayerDarkGifts.AcquiredGiftInstances.Count);
            Assert.AreEqual(target.InstanceId, state.PlayerDarkGifts.AcquiredGiftInstances[0].InstanceId);
            Assert.AreEqual(definition.RevisionId, state.PlayerDarkGifts.AcquiredGiftInstances[0].DefinitionRevisionId);
            Assert.AreEqual(1, state.PlayerDarkGifts.AcquiredGiftInstances[0].StackCount);
            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(5, target.Health);
            CollectionAssert.AreEqual(
                new[] { "dark-gift.acquired", "dark-gift.applied" },
                state.MechanicEvents.Select(item => item.Type));
            CollectionAssert.AreEqual(
                state.MechanicEvents.Select(item => item.Sequence),
                state.PlayerDarkGifts.TriggerHistory.Events.Select(item => item.Sequence));
        }

        [Test]
        public void Acquire_MissingResolverRejectsWithoutChangingGiftOrTarget()
        {
            var state = CreateState(out var target);
            var attackBefore = target.Attack;
            var healthBefore = target.Health;

            var result = DarkGiftStateMachine.Acquire(
                state,
                target,
                Definition("gift-missing", "missing-effect@1"),
                "test",
                "request-missing",
                new DarkGiftResolverRegistry());

            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("dark-gift.resolver.not-found", result.Code);
            Assert.IsEmpty(state.PlayerDarkGifts.AcquiredGiftInstances);
            Assert.AreEqual(attackBefore, target.Attack);
            Assert.AreEqual(healthBefore, target.Health);
            Assert.AreEqual("dark-gift.rejected", state.MechanicEvents.Single().Type);
            Assert.AreEqual(state.MechanicEvents.Single().Sequence, state.PlayerDarkGifts.TriggerHistory.Events.Single().Sequence);
        }

        [Test]
        public void Season14Defaults_ApplyImmediateKeywordStatAndTribeGifts()
        {
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);

            var talons = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.HarpysTalonsRevision);
            CollectionAssert.Contains(talons.Keywords, Keyword.DivineShield);
            CollectionAssert.Contains(talons.Keywords, Keyword.Windfury);

            var fortitude = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.FortitudeRevision);
            Assert.AreEqual(6, fortitude.Attack);
            Assert.AreEqual(6, fortitude.Health);
            Assert.AreEqual(6, fortitude.MaxHealth);

            var furtiveness = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.FurtivenessRevision);
            CollectionAssert.Contains(furtiveness.Keywords, Keyword.Stealth);

            var amalgamation = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.AmalgamationRevision);
            CollectionAssert.AreEqual(new[] { Tribe.All }, amalgamation.Tribes);

            var toxicity = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.ToxicityRevision);
            CollectionAssert.Contains(toxicity.Keywords, Keyword.Venomous);

            var titanic = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.TitanicStrengthRevision);
            Assert.AreEqual(1002, titanic.Attack);
            Assert.IsTrue(new[] { talons, fortitude, furtiveness, amalgamation, toxicity, titanic }
                .All(target => target.Tags.Count == 1 && target.Tags[0].StartsWith("dark-gift.dg-r")));
        }

        [Test]
        public void Season14Defaults_DoNotReplaceExplicitlyInjectedResolver()
        {
            var registry = new DarkGiftResolverRegistry();
            registry.Register(
                Season14DarkGiftResolvers.HarpysTalonsRevision,
                context => DarkGiftResolution.Success("custom-resolver"));
            Season14DarkGiftResolvers.RegisterDefaults(registry);

            var target = AcquireSeason14Gift(registry, Season14DarkGiftResolvers.HarpysTalonsRevision);

            CollectionAssert.DoesNotContain(target.Keywords, Keyword.DivineShield);
            CollectionAssert.DoesNotContain(target.Keywords, Keyword.Windfury);
        }

        [TestCase(Season14DarkGiftResolvers.SharpenedSwordRevision, 2, 0)]
        [TestCase(Season14DarkGiftResolvers.ToughenedShieldRevision, 0, 2)]
        [TestCase(Season14DarkGiftResolvers.DexterityLowRevision, 2, 2)]
        [TestCase(Season14DarkGiftResolvers.DexterityHighRevision, 4, 4)]
        public void Season14Defaults_CardPlayedBuffsResolveExactlyOnce(
            string effectRevision,
            int attackBonus,
            int healthBonus)
        {
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            var state = CreateState(out var target);
            var definition = Definition("card-play-gift-" + effectRevision, effectRevision);
            definition.TriggerSpec = MechanicEventType.CardPlayed.ToString();
            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, definition, "test", "acquire-card-play", registry).Succeeded);

            var result = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.CardPlayed, "played-card-1"),
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(2 + attackBonus, target.Attack);
            Assert.AreEqual(2 + healthBonus, target.Health);
            Assert.AreEqual(2 + healthBonus, target.MaxHealth);
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [TestCase(Season14DarkGiftResolvers.BattleScarsLowRevision, 4)]
        [TestCase(Season14DarkGiftResolvers.DeathsEmbraceLowRevision, 3)]
        [TestCase(Season14DarkGiftResolvers.SpellSiphonLowRevision, 8)]
        [TestCase(Season14DarkGiftResolvers.BattleScarsHighRevision, 6)]
        [TestCase(Season14DarkGiftResolvers.DeathsEmbraceHighRevision, 6)]
        [TestCase(Season14DarkGiftResolvers.SpellSiphonHighRevision, 12)]
        public void Season14Defaults_HistoricalBuffsUseTriggeredAndCastCounts(
            string effectRevision,
            int expectedBonus)
        {
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            var state = CreateState(out var target);
            state.Player.Tavern.BattlecriesTriggeredThisGame = 2;
            state.Player.Tavern.DeathrattlesTriggeredThisGame = 3;
            state.Player.Tavern.TavernSpellsCastThisGame = 4;
            var definition = Definition("history-gift-" + effectRevision, effectRevision);

            var result = DarkGiftStateMachine.Acquire(
                state,
                target,
                definition,
                "test",
                "acquire-history-" + effectRevision,
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(2 + expectedBonus, target.Attack);
            Assert.AreEqual(2 + expectedBonus, target.Health);
            Assert.AreEqual(2 + expectedBonus, target.MaxHealth);
        }

        [Test]
        public void MatchService_PlayingBattlecryRecordsEachResolvedTrigger()
        {
            var service = MatchService.CreateWithDefaultCatalog(4567, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            var battlecry = TestMinion("battlecry-history", "BATTLECRY_HISTORY");
            battlecry.Keywords.Add(Keyword.Battlecry);
            service.State.Player.Tavern.Hand.Add(battlecry);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(1, service.State.Player.Tavern.BattlecriesTriggeredThisGame);
        }

        [Test]
        public void MatchService_SunkenPersistenceKeepsSpellcraftStatsAfterTurnEnd()
        {
            var definition = Definition("sunken-persistence", Season14DarkGiftResolvers.SunkenPersistenceRevision);
            var service = MatchService.CreateWithDefaultCatalog(
                4568,
                new InMemoryTestScenarioRepository(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("sunken-persistence-card", "SUNKEN_PERSISTENCE_CARD");
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-sunken-persistence",
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.AddCardToHand, "REEF_RIFFER_SPELL", CardKind.Spell));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0, 0));
            var buffedAttack = gifted.Attack;
            var buffedHealth = gifted.MaxHealth;
            Assert.Greater(buffedAttack, 2);
            Assert.Greater(buffedHealth, 2);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(buffedAttack, gifted.Attack);
            Assert.AreEqual(buffedHealth, gifted.MaxHealth);
            Assert.IsFalse(gifted.Enchantments.Any(enchantment => enchantment.SourceId == "Temporary Spellcraft"));
        }

        [Test]
        public void Season14Defaults_AffinityUsesEnabledSameTypePool()
        {
            var minions = new MinionCatalog(new[]
            {
                new MinionDefinition
                {
                    Id = "affinity-reward",
                    CardId = "AFFINITY_REWARD",
                    Name = "Affinity Reward",
                    BaseAttack = 1,
                    BaseHealth = 1,
                    TavernTier = 1,
                    InPool = true,
                    Tribes = new List<Tribe> { Tribe.Beast }
                },
                new MinionDefinition
                {
                    Id = "inactive-beast",
                    CardId = "INACTIVE_BEAST",
                    Name = "Inactive Beast",
                    BaseAttack = 1,
                    BaseHealth = 1,
                    TavernTier = 1,
                    InPool = true,
                    Tribes = new List<Tribe> { Tribe.Beast }
                }
            });
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry, minions);
            var state = CreateState(out var target);
            target.Tribes = new List<Tribe> { Tribe.Beast };
            state.EnabledMinionCardIds = new List<string> { "AFFINITY_REWARD" };
            state.Player.Tavern.Tier = 1;
            state.Player.Tavern.Hand.Clear();
            state.Player.Board.Add(target);
            var definition = Definition("affinity", Season14DarkGiftResolvers.AffinityRevision);
            definition.TriggerSpec = MechanicEventType.TurnEnded.ToString();
            definition.TriggerDelayRounds = 1;
            definition.CooldownRounds = 2;
            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, definition, "test", "acquire-affinity", registry).Succeeded);

            state.Round = 4;
            var result = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.TurnEnded, "trigger-affinity"),
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(1, state.Player.Tavern.Hand.Count);
            Assert.AreEqual("AFFINITY_REWARD", state.Player.Tavern.Hand.Single().CardId);
            Assert.AreEqual(PoolSource.Copy, state.Player.Tavern.Hand.Single().PoolSource);
            CollectionAssert.Contains(state.Player.Tavern.Hand.Single().Tags, "generated_copy");
        }

        [Test]
        public void MatchService_ReplicationTriggersEveryTwoTurnEndsWithPlainCopies()
        {
            var definition = Definition("replication", Season14DarkGiftResolvers.ReplicationRevision);
            definition.TriggerSpec = MechanicEventType.TurnEnded.ToString();
            definition.TriggerDelayRounds = 1;
            definition.CooldownRounds = 2;
            var service = MatchService.CreateWithDefaultCatalog(
                4569,
                new InMemoryTestScenarioRepository(),
                setup: new MatchSetupOptions
                {
                    EnableTrinkets = false,
                    EnableQuests = false,
                    EnableQuestRewards = false,
                    EnableTimewarpedTavern = false
                },
                darkGiftDefinitions: new[] { definition });
            service.State.Round = 3;
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("replication-card", "REPLICATION_CARD");
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-replication",
                registry).Succeeded);
            gifted.Golden = true;
            gifted.Attack = 9;
            gifted.Health = 9;
            gifted.MaxHealth = 9;

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(0, service.State.Player.Tavern.Hand.Count);
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            var first = service.State.Player.Tavern.Hand.Single();
            Assert.IsFalse(first.Golden);
            Assert.AreEqual(2, first.Attack);
            Assert.AreEqual(2, first.MaxHealth);
            CollectionAssert.Contains(first.Tags, "plain_copy");
            CollectionAssert.DoesNotContain(first.Tags, "dark-gift.dg-r13");

            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            service.Apply(new GameCommand(GameCommandType.NextTurn));
            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count);
            Assert.AreEqual(2, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
            Assert.AreEqual(0, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.trigger-rejected"));
        }

        [Test]
        public void MatchService_ConsanguinityRewardsOnlyItsOwnRallyRepeats()
        {
            var definition = Definition("consanguinity", Season14DarkGiftResolvers.ConsanguinityRevision);
            definition.TriggerSpec = MechanicEventType.RallyResolved.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                4570,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("consanguinity-card", "CONSANGUINITY_CARD");
            gifted.Attack = 1;
            gifted.Health = 10;
            gifted.MaxHealth = 10;
            service.State.Player.Board.Add(gifted);
            service.State.Player.Board.Add(TestMinion("rally-filler", "RALLY_FILLER"));
            var enemy = TestMinion("rally-enemy", "RALLY_ENEMY");
            enemy.Owner = BoardSide.Opponent;
            enemy.Attack = 0;
            enemy.Health = 30;
            enemy.MaxHealth = 30;
            service.State.Opponent.Board.Add(enemy);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-consanguinity",
                registry).Succeeded);
            CollectionAssert.Contains(gifted.Keywords, Keyword.Rally);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 91, SafetyLimit = 1 }));

            Assert.AreEqual(2, service.State.Player.Tavern.Hand.Count(card => card.CardId == "BLOOD_GEM"));
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void MatchService_DeathrattleGiftsRewardOnlyBoundSource()
        {
            var refresh = Definition("fresh-perspective", Season14DarkGiftResolvers.FreshPerspectiveRevision);
            refresh.TriggerSpec = MechanicEventType.DeathrattleResolved.ToString();
            var spell = Definition("mystic-essence", Season14DarkGiftResolvers.MysticEssenceRevision);
            spell.TriggerSpec = MechanicEventType.DeathrattleResolved.ToString();
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);

            var refreshService = CreateDeathrattleGiftService(refresh, 4571, out var refreshTarget);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(refreshService.State, refreshTarget, refresh, "test", "acquire-refresh", registry).Succeeded);
            refreshService.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 92, SafetyLimit = 1 }));
            Assert.AreEqual(2, refreshService.State.Player.Tavern.FreeRefreshes);
            Assert.AreEqual(1, refreshService.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));

            var spellService = CreateDeathrattleGiftService(spell, 4572, out var spellTarget);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(spellService.State, spellTarget, spell, "test", "acquire-spell", registry).Succeeded);
            spellService.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 93, SafetyLimit = 1 }));
            Assert.AreEqual(1, spellService.State.Player.Tavern.Hand.Count(card => card.CardKind == CardKind.TavernSpell));
            Assert.AreEqual(1, spellService.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [TestCase(Season14DarkGiftResolvers.OffensiveSacrificeRevision, 7, 2)]
        [TestCase(Season14DarkGiftResolvers.DefensiveSacrificeRevision, 2, 9)]
        public void MatchService_SacrificeGiftsBuffAnotherCombatMinion(
            string effectRevision,
            int expectedAttack,
            int expectedHealth)
        {
            var definition = Definition("sacrifice", effectRevision);
            definition.TriggerSpec = MechanicEventType.DeathrattleResolved.ToString();
            var service = CreateDeathrattleGiftService(definition, 4573, out var gifted);
            gifted.Attack = 5;
            gifted.Health = 1;
            gifted.MaxHealth = 7;
            var survivor = TestMinion("sacrifice-survivor", "SACRIFICE_SURVIVOR");
            service.State.Player.Board.Add(survivor);
            var thirdEnemy = TestMinion("sacrifice-enemy-extra", "SACRIFICE_ENEMY");
            thirdEnemy.Owner = BoardSide.Opponent;
            thirdEnemy.Attack = 1;
            thirdEnemy.Health = 10;
            thirdEnemy.MaxHealth = 10;
            service.State.Opponent.Board.Add(thirdEnemy);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-sacrifice", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 94, SafetyLimit = 1 }));

            var final = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == survivor.InstanceId);
            Assert.AreEqual(expectedAttack, final.Attack);
            Assert.AreEqual(expectedHealth, final.MaxHealth);
        }

        [Test]
        public void MatchService_GolemancySummonsSourceStats()
        {
            var definition = Definition("golemancy", Season14DarkGiftResolvers.GolemancyRevision);
            definition.TriggerSpec = MechanicEventType.DeathrattleResolved.ToString();
            var service = CreateDeathrattleGiftService(definition, 4574, out var gifted);
            gifted.Attack = 5;
            gifted.Health = 1;
            gifted.MaxHealth = 7;
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-golemancy", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 95, SafetyLimit = 1 }));

            var golem = service.State.LastResult.FinalPlayerBoard.Single(card => card.Name == "Golem");
            Assert.AreEqual(5, golem.Attack);
            Assert.AreEqual(7, golem.MaxHealth);
        }

        [Test]
        public void MatchService_CharismaUsesMostCommonFriendlyType()
        {
            var definition = Definition("charisma", Season14DarkGiftResolvers.CharismaRevision);
            definition.TriggerSpec = MechanicEventType.RallyResolved.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                4575,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("charisma-card", "CHARISMA_CARD");
            gifted.Attack = 1;
            gifted.Health = 10;
            gifted.MaxHealth = 10;
            service.State.Player.Board.Add(gifted);
            service.State.Player.Board.Add(TestMinion("charisma-beast-1", "CHARISMA_BEAST_1"));
            service.State.Player.Board.Add(TestMinion("charisma-beast-2", "CHARISMA_BEAST_2"));
            var enemy = TestMinion("charisma-enemy", "CHARISMA_ENEMY");
            enemy.Owner = BoardSide.Opponent;
            enemy.Attack = 0;
            enemy.Health = 30;
            enemy.MaxHealth = 30;
            service.State.Opponent.Board.Add(enemy);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-charisma", registry).Succeeded);
            CollectionAssert.Contains(gifted.Keywords, Keyword.Rally);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 96, SafetyLimit = 1 }));

            Assert.AreEqual(1, service.State.Player.Tavern.Hand.Count);
            Assert.IsTrue(BoardTribeAnalyzer.HasTribe(service.State.Player.Tavern.Hand.Single(), Tribe.Beast));
            Assert.AreEqual(PoolSource.Copy, service.State.Player.Tavern.Hand.Single().PoolSource);
        }

        [Test]
        public void MatchService_TimeTurningReplaysOnlyGiftedEndOfTurnEffectAtTurnStart()
        {
            var definition = Definition("time-turning", Season14DarkGiftResolvers.TimeTurningRevision);
            definition.TriggerSpec = MechanicEventType.TurnStarted.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                4576,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Round = 3;
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("time-turning-frontdrake", "BG26_529");
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-time-turning",
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(2, gifted.Counters["upbeat-frontdrake-turns"]);
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void MatchService_DemonologyAddsFodderToNextThreeRefreshesFromNaturalRally()
        {
            var definition = Definition("demonology", Season14DarkGiftResolvers.DemonologyRevision);
            definition.TriggerSpec = MechanicEventType.RallyResolved.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                4577,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("demonology-card", "DEMONOLOGY_CARD");
            gifted.Tribes = new List<Tribe> { Tribe.Demon };
            gifted.Attack = 1;
            gifted.Health = 10;
            gifted.MaxHealth = 10;
            service.State.Player.Board.Add(gifted);
            service.State.Player.Board.Add(TestMinion("demonology-filler", "DEMONOLOGY_FILLER"));
            var enemy = TestMinion("demonology-enemy", "DEMONOLOGY_ENEMY");
            enemy.Owner = BoardSide.Opponent;
            enemy.Attack = 0;
            enemy.Health = 30;
            enemy.MaxHealth = 30;
            service.State.Opponent.Board.Add(enemy);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-demonology",
                registry).Succeeded);

            CollectionAssert.Contains(gifted.Keywords, Keyword.Rally);
            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 97, SafetyLimit = 1 }));

            Assert.AreEqual(3, service.State.Player.Tavern.DemonFodderRefreshes);
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void MatchService_PolarizationMagnetizesGeneratedMechAtTurnEnd()
        {
            var definition = Definition("polarization", Season14DarkGiftResolvers.PolarizationRevision);
            definition.TriggerSpec = MechanicEventType.TurnEnded.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                4578,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.Tier = 6;
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("polarization-card", "POLARIZATION_CARD");
            gifted.Tribes = new List<Tribe> { Tribe.Mech };
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-polarization",
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.Greater(gifted.Attack + gifted.MaxHealth, 4);
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void MatchService_EchoingVoiceTriggersGiftedBattlecryAtTurnEnd()
        {
            var definition = Definition("echoing-voice", Season14DarkGiftResolvers.EchoingVoiceRevision);
            definition.TriggerSpec = MechanicEventType.TurnEnded.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                4579,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.BattlecriesTriggeredThisGame = 0;
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("echoing-voice-card", "ECHOING_VOICE_CARD");
            gifted.Keywords.Add(Keyword.Battlecry);
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-echoing-voice",
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(1, service.State.Player.Tavern.BattlecriesTriggeredThisGame);
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [TestCase(Season14DarkGiftResolvers.TranscendenceRevision, 6, 6)]
        [TestCase(Season14DarkGiftResolvers.ResistanceRevision, 2, 4)]
        [TestCase(Season14DarkGiftResolvers.HostilityRevision, 4, 2)]
        public void MatchService_StartOfCombatStatGiftsAffectOnlyCombatCopy(
            string effectRevision,
            int expectedAttack,
            int expectedHealth)
        {
            var definition = Definition("start-combat-stat", effectRevision);
            definition.TriggerSpec = MechanicEventType.CombatStarted.ToString();
            var service = CreateStartCombatGiftService(definition, 4576, out var gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-start-stat", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 97, SafetyLimit = 1 }));

            var combat = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == gifted.InstanceId);
            Assert.AreEqual(expectedAttack, combat.Attack);
            Assert.AreEqual(expectedHealth, combat.MaxHealth);
            Assert.AreEqual(2, gifted.Attack);
            Assert.AreEqual(2, gifted.MaxHealth);
        }

        [Test]
        public void MatchService_AdmirationGainsLeftMinionStatsAtCombatStart()
        {
            var definition = Definition("admiration", Season14DarkGiftResolvers.AdmirationRevision);
            definition.TriggerSpec = MechanicEventType.CombatStarted.ToString();
            var service = CreateStartCombatGiftService(definition, 4577, out var gifted);
            var left = TestMinion("admiration-left", "ADMIRATION_LEFT");
            left.Attack = 3;
            left.Health = 4;
            left.MaxHealth = 4;
            service.State.Player.Board.Insert(0, left);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-admiration", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 98, SafetyLimit = 1 }));

            var combat = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == gifted.InstanceId);
            Assert.AreEqual(5, combat.Attack);
            Assert.AreEqual(6, combat.MaxHealth);
        }

        [Test]
        public void MatchService_JawsOfDeathTriggersDeathrattleWithoutKillingSource()
        {
            var definition = Definition("jaws-of-death", Season14DarkGiftResolvers.JawsOfDeathRevision);
            definition.TriggerSpec = MechanicEventType.CombatStarted.ToString();
            var service = CreateStartCombatGiftService(definition, 4578, out var gifted);
            gifted.Keywords.Add(Keyword.Deathrattle);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-jaws", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 99, SafetyLimit = 1 }));

            Assert.IsTrue(service.State.LastResult.PlayerRewards.Any(reward =>
                reward.Type == CombatRewardType.FriendlyDeathrattleTriggered &&
                reward.SourceInstanceId == gifted.InstanceId));
            Assert.IsTrue(service.State.LastResult.FinalPlayerBoard.Any(card => card.InstanceId == gifted.InstanceId));
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void MatchService_TorethsBlessingShieldBreaksOnThirdHit()
        {
            var definition = Definition("toreths-blessing", Season14DarkGiftResolvers.TorethsBlessingRevision);
            var service = CreateStartCombatGiftService(definition, 4579, out var gifted);
            gifted.Attack = 1;
            gifted.Health = 100;
            gifted.MaxHealth = 100;
            gifted.Keywords.Add(Keyword.DivineShield);
            service.State.Opponent.Board.Clear();
            for (var index = 0; index < 3; index += 1)
            {
                var enemy = TestMinion("shield-enemy-" + index, "SHIELD_ENEMY");
                enemy.Owner = BoardSide.Opponent;
                enemy.Attack = 1;
                enemy.Health = 100;
                enemy.MaxHealth = 100;
                service.State.Opponent.Board.Add(enemy);
            }

            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-shield", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 100, SafetyLimit = 3 }));

            var final = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == gifted.InstanceId);
            Assert.IsFalse(final.Keywords.Contains(Keyword.DivineShield));
            Assert.AreEqual(100, final.Health);
            Assert.AreEqual(1, service.State.LastResult.Replay.Frames.Count(frame => frame.EventType == CombatEventType.DivineShieldBroken));
        }

        [Test]
        public void MatchService_TarecgosasBlessingPersistsPositiveCombatDelta()
        {
            var definition = Definition("tarecgosas-blessing", Season14DarkGiftResolvers.TarecgosasBlessingRevision);
            var service = CreateStartCombatGiftService(definition, 4580, out var gifted);
            service.State.Player.Tavern.NextCombatBoardAttack = 3;
            service.State.Player.Tavern.NextCombatBoardHealth = 4;
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-persistence", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 101, SafetyLimit = 1 }));

            Assert.AreEqual(5, gifted.Attack);
            Assert.AreEqual(6, gifted.MaxHealth);
        }

        [Test]
        public void MatchService_PersistingHorrorRebornKeepsFullStatsAndBonusKeywords()
        {
            var definition = Definition("persisting-horror", Season14DarkGiftResolvers.PersistingHorrorRevision);
            var service = CreateDeathrattleGiftService(definition, 4581, out var gifted);
            gifted.Keywords.Remove(Keyword.Deathrattle);
            gifted.Keywords.Add(Keyword.Windfury);
            gifted.Attack = 3;
            gifted.Health = 1;
            gifted.MaxHealth = 4;
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-horror", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 102, SafetyLimit = 1 }));

            var reborn = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId.StartsWith(gifted.InstanceId + "-reborn"));
            Assert.AreEqual(3, reborn.Attack);
            Assert.AreEqual(4, reborn.Health);
            Assert.AreEqual(4, reborn.MaxHealth);
            Assert.IsTrue(reborn.Keywords.Contains(Keyword.Windfury));
            Assert.IsFalse(reborn.Keywords.Contains(Keyword.Reborn));
        }

        [Test]
        public void MatchService_InvulnerabilityIgnoresDefenderDamageWhileAttacking()
        {
            var definition = Definition("invulnerability", Season14DarkGiftResolvers.InvulnerabilityRevision);
            var service = CreateStartCombatGiftService(definition, 4582, out var gifted);
            gifted.Attack = 5;
            gifted.Health = 1;
            gifted.MaxHealth = 1;
            var enemy = service.State.Opponent.Board.Single();
            enemy.Attack = 100;
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, gifted, definition, "test", "acquire-invulnerability", registry).Succeeded);

            service.Apply(new GameCommand(
                GameCommandType.RunCombatTest,
                new CombatTestOptions { Seed = 103, SafetyLimit = 1 }));

            var final = service.State.LastResult.FinalPlayerBoard.Single(card => card.InstanceId == gifted.InstanceId);
            Assert.AreEqual(1, final.Health);
            Assert.IsTrue(service.State.LastResult.Log.Any(entry => entry.Title == "ImmuneWhileAttackingResolved"));
        }

        [Test]
        public void Season14Defaults_DoubleVisionAddsUnboundGeneratedCopy()
        {
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            var state = CreateState(out var target);
            var definition = Definition("double-vision", Season14DarkGiftResolvers.DoubleVisionRevision);

            var result = DarkGiftStateMachine.Acquire(
                state,
                target,
                definition,
                "test",
                "acquire-double-vision",
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(2, state.Player.Tavern.Hand.Count);
            var copy = state.Player.Tavern.Hand.Single(card => card.InstanceId != target.InstanceId);
            Assert.AreEqual(target.CardId, copy.CardId);
            Assert.AreEqual(PoolSource.Copy, copy.PoolSource);
            CollectionAssert.Contains(copy.Tags, "generated_copy");
            CollectionAssert.DoesNotContain(copy.Tags, "dark-gift.dg-r18");
            CollectionAssert.Contains(target.Tags, "dark-gift.dg-r18");
            Assert.AreEqual(target.InstanceId, state.PlayerDarkGifts.AcquiredGiftInstances.Single().InstanceId);
        }

        [Test]
        public void Season14Defaults_GildingUsesGoldenTextAndSuppressesTripleReward()
        {
            var minions = new MinionCatalog(new[]
            {
                new MinionDefinition
                {
                    Id = "gifted-minion",
                    CardId = "GIFTED_MINION",
                    Name = "Gifted Minion",
                    BaseAttack = 2,
                    BaseHealth = 2,
                    Text = "Normal text",
                    Golden = new GoldenMinionDefinition
                    {
                        BaseAttack = 4,
                        BaseHealth = 4,
                        Text = "Golden text"
                    }
                }
            });
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry, minions);
            var state = CreateState(out var target);
            target.Text = "Normal text";
            var definition = Definition("gilding", Season14DarkGiftResolvers.GildingRevision);

            var result = DarkGiftStateMachine.Acquire(
                state,
                target,
                definition,
                "test",
                "acquire-gilding",
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.IsTrue(target.Golden);
            Assert.AreEqual(4, target.Attack);
            Assert.AreEqual(4, target.Health);
            Assert.AreEqual("Golden text", target.Text);
            Assert.AreEqual(1, target.Counters["triple-reward-granted"]);
            CollectionAssert.Contains(target.Tags, "dark-gift.dg-r17");
        }

        [TestCase(3, 1, 2)]
        [TestCase(4, 2, 2)]
        [TestCase(5, 3, 3)]
        [TestCase(6, 4, 4)]
        public void Season14Defaults_SteadyGrowthUsesAcquiredRound(
            int acquiredRound,
            int attackBonus,
            int healthBonus)
        {
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            var state = CreateState(out var target);
            state.Round = acquiredRound;
            var definition = Definition("steady-growth", Season14DarkGiftResolvers.SteadyGrowthRevision);
            definition.TriggerSpec = MechanicEventType.TurnEnded.ToString();
            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, definition, "test", "acquire-steady-growth", registry).Succeeded);

            var result = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.TurnEnded, "turn-ended-1"),
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            Assert.AreEqual(2 + attackBonus, target.Attack);
            Assert.AreEqual(2 + healthBonus, target.Health);
            Assert.AreEqual(2 + healthBonus, target.MaxHealth);
        }

        [Test]
        public void MatchService_NextTurnDispatchesSteadyGrowthAtTurnEnd()
        {
            var definition = Definition("steady-growth-integration", Season14DarkGiftResolvers.SteadyGrowthRevision);
            definition.TriggerSpec = MechanicEventType.TurnEnded.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                3456,
                new InMemoryTestScenarioRepository(),
                darkGiftDefinitions: new[] { definition });
            service.State.Round = 3;
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("steady-growth-card", "STEADY_GROWTH_CARD");
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-steady-growth-integration",
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(3, gifted.Attack);
            Assert.AreEqual(4, gifted.Health);
            Assert.AreEqual(4, gifted.MaxHealth);
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void MatchService_IncubationBuffsNowAndDoublesExactlyTwoTurnsLater()
        {
            var definition = Definition("incubation-integration", Season14DarkGiftResolvers.IncubationRevision);
            definition.TriggerSpec = MechanicEventType.TurnStarted.ToString();
            definition.TriggerDelayRounds = 2;
            definition.InitialUses = 1;
            definition.DurationPolicy = DarkGiftDurationPolicies.Uses;
            var service = MatchService.CreateWithDefaultCatalog(
                5678,
                new InMemoryTestScenarioRepository(),
                darkGiftDefinitions: new[] { definition });
            service.State.Round = 6;
            service.State.Phase = MatchPhase.Tavern;
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("incubation-card", "INCUBATION_CARD");
            service.State.Player.Board.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);

            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-incubation",
                registry).Succeeded);
            Assert.AreEqual(4, gifted.Attack);
            Assert.AreEqual(4, gifted.MaxHealth);
            Assert.AreEqual(8, service.State.PlayerDarkGifts.AcquiredGiftInstances.Single().NextTriggerRound);

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(7, service.State.Round);
            Assert.AreEqual(4, gifted.Attack);
            Assert.AreEqual(0, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
            Assert.AreEqual(0, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.rejected"));

            service.Apply(new GameCommand(GameCommandType.NextTurn));

            Assert.AreEqual(8, service.State.Round);
            Assert.AreEqual(8, gifted.Attack);
            Assert.AreEqual(8, gifted.Health);
            Assert.AreEqual(8, gifted.MaxHealth);
            Assert.AreEqual(1, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
            Assert.IsTrue(service.State.PlayerDarkGifts.AcquiredGiftInstances.Single().Expired);
        }

        [Test]
        public void MatchService_PlayedGiftedMinionKeepsBindingAndCardPlayedBuffsItself()
        {
            var definition = Definition("card-play-integration", Season14DarkGiftResolvers.SharpenedSwordRevision);
            definition.TriggerSpec = MechanicEventType.CardPlayed.ToString();
            var service = MatchService.CreateWithDefaultCatalog(
                2345,
                new InMemoryTestScenarioRepository(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            var gifted = TestMinion("gifted-card", "GIFTED_CARD");
            var originalInstanceId = gifted.InstanceId;
            service.State.Player.Tavern.Hand.Add(gifted);
            var registry = new DarkGiftResolverRegistry();
            Season14DarkGiftResolvers.RegisterDefaults(registry);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(
                service.State,
                gifted,
                definition,
                "test",
                "acquire-integration",
                registry).Succeeded);

            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreNotEqual(originalInstanceId, gifted.InstanceId);
            Assert.AreEqual(gifted.InstanceId, service.State.PlayerDarkGifts.AcquiredGiftInstances.Single().InstanceId);
            Assert.AreEqual(4, gifted.Attack);

            service.State.Player.Tavern.Hand.Add(TestMinion("second-card", "SECOND_CARD"));
            service.Apply(new GameCommand(GameCommandType.PlayMinion, 0));

            Assert.AreEqual(6, gifted.Attack);
            Assert.AreEqual(2, service.State.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
        }

        [Test]
        public void Acquire_SameRequestIdIsIdempotent()
        {
            var state = CreateState(out var target);
            var calls = 0;
            var definition = Definition("gift-idempotent", "idempotent-effect@1");
            var registry = new DarkGiftResolverRegistry();
            registry.Register("idempotent-effect@1", context =>
            {
                calls += 1;
                return DarkGiftResolution.Success("attached");
            });

            var first = DarkGiftStateMachine.Acquire(state, target, definition, "test", "same-request", registry);
            var second = DarkGiftStateMachine.Acquire(state, target, definition, "test", "same-request", registry);

            Assert.IsTrue(first.Succeeded);
            Assert.IsTrue(second.Succeeded);
            Assert.AreEqual("dark-gift.acquire.already-applied", second.Code);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(1, state.PlayerDarkGifts.AcquiredGiftInstances.Count);
            Assert.AreEqual(2, state.MechanicEvents.Count);
        }

        [Test]
        public void Trigger_EnforcesEventUsesCooldownAndExpiryAfterSuccessfulCommit()
        {
            var state = CreateState(out var target);
            state.Player.Tavern.Gold = 0;
            var definition = Definition("gift-trigger", "trigger-effect@1");
            definition.TriggerSpec = MechanicEventType.TurnStarted.ToString();
            definition.InitialUses = 2;
            definition.CooldownRounds = 1;
            definition.DurationPolicy = DarkGiftDurationPolicies.Uses;
            var registry = new DarkGiftResolverRegistry();
            registry.Register("trigger-effect@1", context =>
                context.Phase == DarkGiftResolutionPhase.Acquire
                    ? DarkGiftResolution.Success("attached")
                    : DarkGiftResolution.Success("gold+1", (live, resolvedTarget) => live.Player.Tavern.Gold += 1));
            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, definition, "test", "acquire-trigger", registry).Succeeded);

            var wrongEvent = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.CardBought, "wrong-event"),
                registry);
            var first = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.TurnStarted, "trigger-1"),
                registry);
            var cooldown = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.TurnStarted, "trigger-cooldown"),
                registry);
            state.Round = 4;
            DarkGiftStateMachine.AdvanceRound(state);
            var second = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.TurnStarted, "trigger-2"),
                registry);
            var expired = DarkGiftStateMachine.Trigger(
                state,
                definition,
                TriggerRequest(target, definition, MechanicEventType.TurnStarted, "trigger-expired"),
                registry);
            var instance = state.PlayerDarkGifts.AcquiredGiftInstances.Single();

            Assert.IsFalse(wrongEvent.Succeeded);
            Assert.AreEqual("dark-gift.trigger.not-matched", wrongEvent.Code);
            Assert.IsTrue(first.Succeeded, first.Message);
            Assert.IsFalse(cooldown.Succeeded);
            Assert.AreEqual("dark-gift.cooldown", cooldown.Code);
            Assert.IsTrue(second.Succeeded, second.Message);
            Assert.AreEqual(2, state.Player.Tavern.Gold);
            Assert.AreEqual(0, instance.RemainingUses);
            Assert.IsTrue(instance.Expired);
            Assert.IsFalse(instance.Active);
            Assert.IsFalse(expired.Succeeded);
            Assert.AreEqual("dark-gift.expired", expired.Code);
            Assert.AreEqual(state.MechanicEvents.Count, state.PlayerDarkGifts.TriggerHistory.Events.Count);
            Assert.AreEqual(2, state.MechanicEvents.Count(item => item.Type == "dark-gift.triggered"));
            Assert.AreEqual(2, state.MechanicEvents.Count(item => item.Type == "dark-gift.resolved"));
        }

        [Test]
        public void Acquire_DifferentDefinitionsCoexistWhileDuplicatePolicyStillApplies()
        {
            var state = CreateState(out var target);
            var first = Definition("coexist-first", "coexist-first-effect@1");
            var second = Definition("coexist-second", "coexist-second-effect@1");
            var registry = new DarkGiftResolverRegistry();
            registry.Register(first.EffectRevision, context => DarkGiftResolution.Success("first"));
            registry.Register(second.EffectRevision, context => DarkGiftResolution.Success("second"));

            var firstResult = DarkGiftStateMachine.Acquire(state, target, first, "normal-button", "coexist-1", registry);
            var secondResult = DarkGiftStateMachine.Acquire(state, target, second, "triple", "coexist-2", registry);
            var duplicate = DarkGiftStateMachine.Acquire(state, target, first, "normal-button", "coexist-3", registry);

            Assert.IsTrue(firstResult.Succeeded, firstResult.Message);
            Assert.IsTrue(secondResult.Succeeded, secondResult.Message);
            Assert.IsFalse(duplicate.Succeeded);
            Assert.AreEqual("dark-gift.duplicate", duplicate.Code);
            Assert.AreEqual(2, state.PlayerDarkGifts.AcquiredGiftInstances.Count(item => item.Active && !item.Expired));
            CollectionAssert.AreEquivalent(
                new[] { first.RevisionId, second.RevisionId },
                state.PlayerDarkGifts.AcquiredGiftInstances
                    .Where(item => item.Active && !item.Expired)
                    .Select(item => item.DefinitionRevisionId));
        }

        [Test]
        public void MatchService_TriplePreservesAndStacksEveryMaterialDarkGiftBinding()
        {
            var first = Definition("triple-first", "triple-first-effect@1");
            var second = Definition("triple-second", "triple-second-effect@1");
            var service = MatchService.CreateWithDefaultCatalog(
                4591,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { first, second });
            service.State.Player.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();

            var materials = Enumerable.Range(1, 3)
                .Select(index => TestMinion("triple-material-" + index, "TRIPLE_MATERIAL"))
                .ToList();
            foreach (var material in materials)
            {
                material.DefinitionId = "triple-material";
                service.State.Player.Tavern.Hand.Add(material);
            }

            var registry = new DarkGiftResolverRegistry();
            registry.Register(first.EffectRevision, context => DarkGiftResolution.Success("first"));
            registry.Register(second.EffectRevision, context => DarkGiftResolution.Success("second"));
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, materials[0], first, "normal-button", "triple-gift-1", registry).Succeeded);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, materials[1], first, "normal-button", "triple-gift-2", registry).Succeeded);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(service.State, materials[2], second, "normal-button", "triple-gift-3", registry).Succeeded);

            var resolveTriples = typeof(MatchService).GetMethod(
                "ResolvePlayerTriples",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(resolveTriples);
            resolveTriples.Invoke(service, null);

            var golden = service.State.Player.Tavern.Hand
                .Concat(service.State.Player.Board)
                .Single(card => card.Golden && card.DefinitionId == "triple-material");
            var activeBindings = service.State.PlayerDarkGifts.AcquiredGiftInstances
                .Where(item => item.Active && !item.Expired)
                .ToList();
            Assert.AreEqual(3, activeBindings.Count);
            Assert.IsTrue(activeBindings.All(item => item.InstanceId == golden.InstanceId));
            Assert.AreEqual(2, activeBindings.Count(item => item.DefinitionRevisionId == first.RevisionId));
            Assert.AreEqual(1, activeBindings.Count(item => item.DefinitionRevisionId == second.RevisionId));
            CollectionAssert.IsEmpty(
                activeBindings.Where(item => materials.Any(material => material.InstanceId == item.InstanceId)));

            var scenario = TestScenarioMapper.Clone(
                TestScenarioMapper.Capture(service.State, "triple-dark-gift-round-trip"));
            var restored = MatchService.CreateWithDefaultCatalog(
                4592,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { first, second });
            var restore = TestScenarioMapper.TryApplyTo(restored.State, scenario);
            Assert.AreEqual(TestScenarioRestoreStatus.Applied, restore.Status, restore.Message);
            var restoredGolden = restored.State.Player.Tavern.Hand
                .Concat(restored.State.Player.Board)
                .Single(card => card.Golden && card.DefinitionId == "triple-material");
            var restoredBindings = restored.State.PlayerDarkGifts.AcquiredGiftInstances
                .Where(item => item.Active && !item.Expired)
                .ToList();
            Assert.AreEqual(3, restoredBindings.Count);
            Assert.IsTrue(restoredBindings.All(item => item.InstanceId == restoredGolden.InstanceId));
            Assert.AreEqual(2, restoredBindings.Count(item => item.DefinitionRevisionId == first.RevisionId));
            Assert.AreEqual(1, restoredBindings.Count(item => item.DefinitionRevisionId == second.RevisionId));
        }

        [Test]
        public void DarkGiftChoice_ThirdCopyImmediatelyCreatesTripleAtFarRight()
        {
            var definition = Definition("choice-triple", Season14DarkGiftResolvers.SteadyGrowthRevision);
            var service = MatchService.CreateWithDefaultCatalog(
                4593,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            var tavern = service.State.Player.Tavern;
            tavern.Hand.Clear();
            service.State.Player.Board.Clear();
            var cardId = service.Catalogs.Minions.All.First(item => item.InPool).CardId;
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.Apply(new GameCommand(GameCommandType.AddCardToHand, cardId, CardKind.Minion));
            service.State.ChoiceQueue = new ChoiceQueueState
            {
                ActiveChoice = new ChoiceQueueItem
                {
                    RequestId = "dark-gift-choice-triple",
                    Kind = ChoiceRequestKind.DarkGift,
                    Source = "test",
                    Blocking = true,
                    RemainingPicks = 1,
                    Options = new List<MechanicChoiceOption>
                    {
                        new MechanicChoiceOption
                        {
                            OptionId = "dark-gift-choice-triple-option",
                            Kind = AdvancedMechanicKind.DarkGift,
                            SourceId = cardId,
                            RewardId = definition.RevisionId
                        }
                    }
                }
            };

            service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

            Assert.AreEqual(1, tavern.Hand.Count);
            Assert.IsTrue(tavern.Hand[0].Golden);
            Assert.AreEqual(cardId, tavern.Hand[0].CardId);
        }

        [Test]
        public void Acquire_StackThenReplacePreservesExpiredHistory()
        {
            var state = CreateState(out var target);
            var stack = Definition("stack-gift", "stack-effect@1");
            stack.StackPolicy = DarkGiftStackPolicies.Stack;
            stack.MaxStacks = 2;
            var replacement = Definition("replacement-gift", "replacement-effect@1");
            replacement.StackPolicy = DarkGiftStackPolicies.Replace;
            var registry = new DarkGiftResolverRegistry();
            registry.Register("stack-effect@1", context => DarkGiftResolution.Success("stacked"));
            registry.Register("replacement-effect@1", context => DarkGiftResolution.Success("replaced"));

            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, stack, "test", "stack-1", registry).Succeeded);
            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, stack, "test", "stack-2", registry).Succeeded);
            Assert.IsTrue(DarkGiftStateMachine.SetSuppressed(
                state,
                target.InstanceId,
                stack.RevisionId,
                true,
                "test",
                "suppress-stack"));
            var overLimit = DarkGiftStateMachine.Acquire(state, target, stack, "test", "stack-3", registry);
            var replaced = DarkGiftStateMachine.Acquire(state, target, replacement, "test", "replace-1", registry);

            Assert.IsFalse(overLimit.Succeeded);
            Assert.AreEqual("dark-gift.stack-limit", overLimit.Code);
            Assert.IsTrue(replaced.Succeeded, replaced.Message);
            Assert.AreEqual(2, state.PlayerDarkGifts.AcquiredGiftInstances.Count);
            var oldInstance = state.PlayerDarkGifts.AcquiredGiftInstances.Single(item => item.DefinitionRevisionId == stack.RevisionId);
            var newInstance = state.PlayerDarkGifts.AcquiredGiftInstances.Single(item => item.DefinitionRevisionId == replacement.RevisionId);
            Assert.AreEqual(2, oldInstance.StackCount);
            Assert.IsTrue(oldInstance.Suppressed);
            Assert.IsTrue(oldInstance.Expired);
            Assert.IsFalse(oldInstance.Active);
            Assert.IsTrue(newInstance.Active);
            Assert.IsTrue(state.MechanicEvents.Any(item => item.Type == "dark-gift.replaced"));
        }

        [Test]
        public void AdvanceRound_ExpiresRoundDurationAndDoesNotRepeatExpiryEvent()
        {
            var state = CreateState(out var target);
            var definition = Definition("round-gift", "round-effect@1");
            definition.DurationPolicy = DarkGiftDurationPolicies.Rounds;
            definition.DurationRounds = 2;
            var registry = new DarkGiftResolverRegistry();
            registry.Register("round-effect@1", context => DarkGiftResolution.Success("attached"));
            Assert.IsTrue(DarkGiftStateMachine.Acquire(state, target, definition, "test", "round-acquire", registry).Succeeded);

            state.Round = 4;
            DarkGiftStateMachine.AdvanceRound(state, new[] { definition });
            Assert.IsTrue(state.PlayerDarkGifts.AcquiredGiftInstances.Single().Active);
            state.Round = 5;
            DarkGiftStateMachine.AdvanceRound(state, new[] { definition });
            DarkGiftStateMachine.AdvanceRound(state, new[] { definition });

            Assert.IsTrue(state.PlayerDarkGifts.AcquiredGiftInstances.Single().Expired);
            Assert.IsFalse(state.PlayerDarkGifts.AcquiredGiftInstances.Single().Active);
            Assert.AreEqual(1, state.MechanicEvents.Count(item => item.Type == "dark-gift.expired"));
        }

        [Test]
        public void AutomaticChoice_DefaultRequiresPlayerAndBatchPolicyCanSelectFirst()
        {
            var offer = new DarkGiftOfferResult
            {
                Succeeded = true,
                Options = new List<DarkGiftOfferOption>
                {
                    new DarkGiftOfferOption { OptionId = "first" },
                    new DarkGiftOfferOption { OptionId = "second" }
                }
            };
            var profile = new DarkGiftProfile();

            Assert.IsNull(DarkGiftOfferService.SelectAutomaticOption(profile, offer));
            profile.AutoChoicePolicy = DarkGiftAutoChoicePolicy.FirstOption;
            var selected = DarkGiftOfferService.SelectAutomaticOption(profile, offer);

            Assert.AreEqual("first", selected.OptionId);
            Assert.AreNotSame(offer.Options[0], selected);
        }

        private static MatchState CreateState(out MinionInstance target)
        {
            var state = MatchService.CreateWithDefaultCatalog(1234, new InMemoryTestScenarioRepository()).State;
            state.Round = 3;
            state.Phase = MatchPhase.Tavern;
            state.MechanicEvents.Clear();
            state.PlayerDarkGifts = new PlayerDarkGiftState();
            target = new MinionInstance
            {
                InstanceId = "gifted-minion-1",
                DefinitionId = "gifted-minion",
                CardId = "GIFTED_MINION",
                Name = "Gifted Minion",
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 2,
                Owner = BoardSide.Player
            };
            state.Player.Tavern.Hand.Clear();
            state.Player.Tavern.Hand.Add(target);
            return state;
        }

        private static MinionInstance AcquireSeason14Gift(
            DarkGiftResolverRegistry registry,
            string effectRevision)
        {
            var state = CreateState(out var target);
            var definition = Definition("season14-" + effectRevision, effectRevision);

            var result = DarkGiftStateMachine.Acquire(
                state,
                target,
                definition,
                "season14-test",
                "request-" + effectRevision,
                registry);

            Assert.IsTrue(result.Succeeded, result.Message);
            return target;
        }

        private static MinionInstance TestMinion(string instanceId, string cardId)
        {
            return new MinionInstance
            {
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = cardId,
                Name = instanceId,
                BaseAttack = 2,
                BaseHealth = 2,
                Attack = 2,
                Health = 2,
                MaxHealth = 2,
                TavernTier = 1,
                Owner = BoardSide.Player,
                Tribes = new List<Tribe> { Tribe.Beast }
            };
        }

        private static MatchSetupOptions DarkGiftOnlySetup()
        {
            return new MatchSetupOptions
            {
                EnableTrinkets = false,
                EnableQuests = false,
                EnableQuestRewards = false,
                EnableTimewarpedTavern = false
            };
        }

        private static MatchService CreateDeathrattleGiftService(
            DarkGiftDefinition definition,
            int seed,
            out MinionInstance gifted)
        {
            var service = MatchService.CreateWithDefaultCatalog(
                seed,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.Player.Tavern.FreeRefreshes = 0;
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            gifted = TestMinion("deathrattle-gift-card", "DEATHRATTLE_GIFT_CARD");
            gifted.Attack = 0;
            gifted.Health = 1;
            gifted.MaxHealth = 1;
            service.State.Player.Board.Add(gifted);
            for (var index = 0; index < 2; index += 1)
            {
                var enemy = TestMinion("deathrattle-enemy-" + index, "DEATHRATTLE_ENEMY");
                enemy.Owner = BoardSide.Opponent;
                enemy.Attack = 1;
                enemy.Health = 10;
                enemy.MaxHealth = 10;
                service.State.Opponent.Board.Add(enemy);
            }

            return service;
        }

        private static MatchService CreateStartCombatGiftService(
            DarkGiftDefinition definition,
            int seed,
            out MinionInstance gifted)
        {
            var service = MatchService.CreateWithDefaultCatalog(
                seed,
                new InMemoryTestScenarioRepository(),
                setup: DarkGiftOnlySetup(),
                darkGiftDefinitions: new[] { definition });
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            service.State.Player.Tavern.Hand.Clear();
            service.State.PlayerDarkGifts = new PlayerDarkGiftState();
            gifted = TestMinion("start-combat-gifted", "START_COMBAT_GIFTED");
            service.State.Player.Board.Add(gifted);
            var enemy = TestMinion("start-combat-enemy", "START_COMBAT_ENEMY");
            enemy.Owner = BoardSide.Opponent;
            enemy.Attack = 0;
            enemy.Health = 100;
            enemy.MaxHealth = 100;
            service.State.Opponent.Board.Add(enemy);
            return service;
        }

        private static DarkGiftDefinition Definition(string id, string effectRevision)
        {
            return new DarkGiftDefinition
            {
                Id = id,
                RevisionId = id + "@1",
                EffectRevision = effectRevision,
                DisplayName = id,
                StackPolicy = DarkGiftStackPolicies.Reject,
                DurationPolicy = DarkGiftDurationPolicies.Persistent,
                ImplementationStatus = DarkGiftImplementationStatus.FrameworkOnly
            };
        }

        private static DarkGiftTriggerRequest TriggerRequest(
            MinionInstance target,
            DarkGiftDefinition definition,
            MechanicEventType eventType,
            string requestId)
        {
            return new DarkGiftTriggerRequest
            {
                TargetInstanceId = target.InstanceId,
                DefinitionRevisionId = definition.RevisionId,
                EventType = eventType,
                RequestId = requestId
            };
        }
    }
}
