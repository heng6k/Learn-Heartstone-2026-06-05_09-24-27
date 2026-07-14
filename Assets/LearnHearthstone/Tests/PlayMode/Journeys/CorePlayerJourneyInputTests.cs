using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Persistence;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.PlayMode
{
    public sealed class CorePlayerJourneyInputTests
    {
        [UnityTest]
        public IEnumerator PlayMode_PJ01_MainHubToTavernCompletesThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                MatchService service = null;
                Action openSetup = null;
                openSetup = () =>
                {
                    ClearChildren(scene.Root);
                    new UnityTavernTribeSelectionView(
                        scene.Root,
                        setup =>
                        {
                            ClearChildren(scene.Root);
                            service = MatchService.CreateWithDefaultCatalog(24680, new InMemoryTestScenarioRepository(), setup);
                            new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), openSetup).Build();
                        },
                        () => { },
                        UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();
                };

                new MainHubView(
                    scene.Root,
                    () => { },
                    () => { },
                    openSetup,
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                yield return WaitForChild(scene.Root, "酒馆训练器Button");
                Click(scene, FindChild(scene.Root, "酒馆训练器Button"));
                yield return WaitForChild(scene.Root, "UnityTribeSelectionAllButton");

                Assert.AreEqual("选择本局种族", FindChild(scene.Root, "UnityTribeSelectionTitle").GetComponent<Text>().text);
                Click(scene, FindChild(scene.Root, "UnityTribeSelectionAllButton"));
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsStartButton");

                Assert.IsNotNull(FindChild(scene.Root, "UnityAdvancedMechanicsSetupOverlay"));
                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsStartButton"));
                yield return WaitForChild(scene.Root, "UnityQuickRefreshButton");

                Assert.IsNotNull(service);
                Assert.IsNotNull(FindChild(scene.Root, "UnityPlayerBoardZone"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityHandZone"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityShopZone"));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_SetupFilters_CustomFiveResetAndStartCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                MatchSetupOptions startedWith = null;
                var selectedTribes = TribeAvailabilityRules.PlayableTribes.Take(5).ToList();
                var excludedTribe = TribeAvailabilityRules.PlayableTribes.Skip(5).First();
                new UnityTavernTribeSelectionView(
                    scene.Root,
                    setup => startedWith = setup,
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();
                yield return WaitForChild(scene.Root, "UnityTribeSelectionEnterButton");

                foreach (var tribe in selectedTribes)
                {
                    var buttonName = "UnityTribeSelection" + tribe + "Button";
                    Click(scene, FindChild(scene.Root, buttonName));
                    yield return WaitForChild(scene.Root, buttonName);
                }

                StringAssert.Contains("5/5", FindChild(scene.Root, "UnityTribeSelectionSummary").GetComponent<Text>().text);
                StringAssert.Contains("\u5df2\u9009", FindChild(scene.Root, "UnityTribeSelection" + selectedTribes[0] + "Button").GetComponentInChildren<Text>(true).text);
                var excludedButton = FindChild(scene.Root, "UnityTribeSelection" + excludedTribe + "Button");
                StringAssert.Contains("\u6392\u9664", excludedButton.GetComponentInChildren<Text>(true).text);
                Assert.IsFalse(excludedButton.GetComponent<Button>().interactable);
                StringAssert.Contains("\u672c\u5c40\u6392\u9664", FindChild(scene.Root, "UnityTribeSelectionExclusionSummary").GetComponent<Text>().text);

                Click(scene, FindChild(scene.Root, "UnityTribeSelectionEnterButton"));
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDebugOnly");

                var disabledToggle = FindChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDisabled").GetComponent<Toggle>();
                Assert.IsFalse(disabledToggle.interactable);
                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDebugOnly"));
                yield return WaitForState(
                    () => FindChildOrNull(scene.Root, "UnityAdvancedMechanicsToggle-ShowDisabled")?.GetComponent<Toggle>().interactable == true,
                    "Disabled Pool dependency enable");
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDisabled");

                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDisabled"));
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsSetupSummary");
                var activeSummary = FindChild(scene.Root, "UnityAdvancedMechanicsSetupSummary").GetComponent<Text>().text;
                StringAssert.Contains("\u8c03\u8bd5\u6c60", activeSummary);
                StringAssert.Contains("\u542b\u7981\u7528\u9879", activeSummary);

                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsResetFiltersButton"));
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDebugOnly");
                Assert.IsFalse(FindChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDebugOnly").GetComponent<Toggle>().isOn);
                Assert.IsFalse(FindChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowDisabled").GetComponent<Toggle>().isOn);
                Assert.IsTrue(FindChild(scene.Root, "UnityAdvancedMechanicsToggle-ShowProxySafe").GetComponent<Toggle>().isOn);
                Assert.IsTrue(FindChild(scene.Root, "UnityAdvancedMechanicsToggle-EnablePlayerDirectedChoices").GetComponent<Toggle>().isOn);

                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsBackButton"));
                yield return WaitForChild(scene.Root, "UnityTribeSelectionSummary");
                StringAssert.Contains("5/5", FindChild(scene.Root, "UnityTribeSelectionSummary").GetComponent<Text>().text);
                foreach (var tribe in selectedTribes)
                {
                    StringAssert.Contains("\u5df2\u9009", FindChild(scene.Root, "UnityTribeSelection" + tribe + "Button").GetComponentInChildren<Text>(true).text);
                }

                Click(scene, FindChild(scene.Root, "UnityTribeSelectionEnterButton"));
                yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsStartButton");
                Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsStartButton"));
                yield return WaitForState(() => startedWith != null, "custom setup start");

                CollectionAssert.AreEquivalent(selectedTribes, startedWith.ActiveTribes);
                Assert.IsTrue(startedWith.ShowProxySafe);
                Assert.IsFalse(startedWith.ShowDebugOnly);
                Assert.IsFalse(startedWith.ShowHiddenEffectOnly);
                Assert.IsFalse(startedWith.ShowDisabled);
                Assert.IsTrue(startedWith.EnablePlayerDirectedChoices);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_CardLibrary_SearchDetailClearAndAddCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuickToolsButton");

                Click(scene, FindChild(scene.Root, "UnityQuickToolsButton"));
                yield return WaitForChild(scene.Root, "UnityToolsOpenCardLibraryButton");
                Click(scene, FindChild(scene.Root, "UnityToolsOpenCardLibraryButton"));
                yield return WaitForChild(scene.Root, "UnityCardLibrarySearchInput");

                var targetCard = CardLibraryCards(scene.Root)
                    .Where(component =>
                        component.Card != null &&
                        component.Card.TavernTier >= 1 &&
                        component.Card.TavernTier <= 7 &&
                        component.Card.Tribes != null &&
                        component.Card.Tribes.Any(tribe => tribe != Tribe.None && tribe != Tribe.All) &&
                        !string.IsNullOrWhiteSpace(component.Card.Name))
                    .Select(component => component.Card)
                    .FirstOrDefault();
                Assert.IsNotNull(targetCard, "The visible library did not contain a tribal minion.");
                var targetCardId = targetCard.CardId;
                var targetCardName = targetCard.Name;
                var targetTierButton = "UnityCardLibraryTier" + targetCard.TavernTier + "Button";
                var targetTribe = targetCard.Tribes.First(tribe => tribe != Tribe.None && tribe != Tribe.All);
                var targetTribeButton = "UnityCardLibraryTribe" + targetTribe + "Button";

                Click(scene, FindChild(scene.Root, targetTierButton));
                yield return WaitForChild(scene.Root, "UnityCardLibrarySearchInput");
                Click(scene, FindChild(scene.Root, targetTribeButton));
                yield return WaitForChild(scene.Root, "UnityCardLibrarySearchInput");
                Assert.IsNotNull(FindCardLibraryCard(scene.Root, targetCardId));

                yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityCardLibrarySearchInput"), targetCardName);
                yield return WaitForState(
                    () =>
                    {
                        var filtered = CardLibraryCards(scene.Root).Where(component => component.Card != null).ToList();
                        return FindCardLibraryCard(scene.Root, targetCardId) != null &&
                               filtered.Count > 0 &&
                               filtered.Select(component => component.Card.Name).Distinct().SequenceEqual(new[] { targetCardName });
                    },
                    "card-library filtered result");
                Assert.AreEqual(targetCardName, FindChild(scene.Root, "UnityCardLibrarySearchInput").GetComponent<InputField>().text);
                Assert.IsTrue(FindChild(scene.Root, "UnityCardLibraryClearSearchButton").GetComponent<Button>().interactable);

                Click(scene, FindChild(scene.Root, "UnityCardLibraryDetailButton"));
                yield return WaitForChild(scene.Root, "UnityCardLibraryDetailOverlay");
                Assert.AreEqual(targetCardName, FindChild(scene.Root, "UnityCardDetailTitle").GetComponent<Text>().text);
                Assert.IsFalse(FindChild(scene.Root, "UnityCardDetailInfo").GetComponentsInChildren<Text>(true).Any(label => label.text.Contains(targetCardId)));
                Click(scene, FindChild(scene.Root, "UnityCardDetailCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityCardLibraryDetailOverlay");
                yield return WaitForChild(scene.Root, "UnityCardLibrarySearchInput");

                Assert.AreEqual(targetCardName, FindChild(scene.Root, "UnityCardLibrarySearchInput").GetComponent<InputField>().text);
                Assert.IsTrue(FindChild(scene.Root, targetTierButton).GetComponent<Outline>().enabled);
                Assert.IsTrue(FindChild(scene.Root, targetTribeButton).GetComponent<Outline>().enabled);

                Click(scene, FindChild(scene.Root, "UnityCardLibraryAddButton"));
                yield return WaitForState(() => service.State.Player.Tavern.Hand.Any(card => card.CardId == targetCardId), "card-library add");
                yield return WaitForChild(scene.Root, "UnityCardLibraryClearSearchButton");
                Click(scene, FindChild(scene.Root, "UnityCardLibraryClearSearchButton"));
                yield return WaitForChild(scene.Root, "UnityCardLibrarySearchInput");

                Assert.AreEqual(string.Empty, FindChild(scene.Root, "UnityCardLibrarySearchInput").GetComponent<InputField>().text);
                Assert.IsFalse(FindChild(scene.Root, "UnityCardLibraryClearSearchButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(scene.Root, targetTierButton).GetComponent<Outline>().enabled);
                Assert.IsTrue(FindChild(scene.Root, targetTribeButton).GetComponent<Outline>().enabled);

                Click(scene, FindChild(scene.Root, "UnityCardLibraryCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityCardLibraryOverlay");
                Assert.IsTrue(service.State.Player.Tavern.Hand.Any(card => card.CardId == targetCardId));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_Tools_CommonAndAdvancedDisclosureCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuickToolsButton");

                Click(scene, FindChild(scene.Root, "UnityQuickToolsButton"));
                yield return WaitForChild(scene.Root, "UnityToolsOpenAdvancedButton");
                Assert.IsNotNull(FindChild(scene.Root, "UnityToolsEconomySection"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityToolsOpenCardLibraryButton"));
                Assert.IsNull(FindChildOrNull(scene.Root, "UnityToolsTrinketDebugSection"));
                Assert.GreaterOrEqual(FindChild(scene.Root, "UnityToolsEconomySectionGrid").GetComponent<GridLayoutGroup>().cellSize.y, 44f);
                Assert.GreaterOrEqual(FindChild(scene.Root, "UnityToolsEconomySectionTitle").GetComponent<Text>().fontSize, 14);
                Assert.AreEqual("先选己方随从", FindChild(scene.Root, "UnityToolsReturnSelectedButtonText").GetComponent<Text>().text);
                Assert.AreEqual("对手战场为空", FindChild(scene.Root, "UnityToolsClearOpponentButtonText").GetComponent<Text>().text);
                Assert.AreEqual("己方战场为空", FindChild(scene.Root, "UnityToolsCopyOpponentButtonText").GetComponent<Text>().text);
                Assert.GreaterOrEqual(FindChild(scene.Root, "UnityToolsClearOpponentButtonText").GetComponent<Text>().fontSize, 14);

                yield return ScrollToAndClick(scene, "UnityTrainerToolsScroll", "UnityToolsOpenAdvancedButton");
                yield return WaitForChild(scene.Root, "UnityToolsBackToCommonButton");
                Assert.IsNull(FindChildOrNull(scene.Root, "UnityToolsEconomySection"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityToolsTrinketDebugSection"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityToolsPlayerModifierSection"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityToolsCombatSection"));
                Assert.AreEqual("暂无战斗快照", FindChild(scene.Root, "UnityToolsResetCombatSnapshotButtonText").GetComponent<Text>().text);
                Assert.AreEqual("当前已为 0", FindChild(scene.Root, "UnityToolsPlayerSpellsCastThisGameMinusButtonText").GetComponent<Text>().text);

                Click(scene, FindChild(scene.Root, "UnityToolsBackToCommonButton"));
                yield return WaitForChild(scene.Root, "UnityToolsOpenCardLibraryButton");
                Assert.IsNull(FindChildOrNull(scene.Root, "UnityToolsCombatSection"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityToolsEconomySection"));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_HeroEffectRack_FocusAndClickDetailsCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                var trinket = service.GetDebugSelectableTrinkets(TrinketSlotKind.Lesser).First();
                service.Apply(new GameCommand(GameCommandType.DebugReplaceTrinket, trinket.CardId, CardKind.Trinket, 0));

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityHeroEffectTrinket-Lesser");

                var effect = FindChild(scene.Root, "UnityHeroEffectTrinket-Lesser");
                Assert.GreaterOrEqual(effect.GetComponent<RectTransform>().rect.height, 44f);
                Assert.GreaterOrEqual(FindChild(scene.Root, "UnityHeroEffectType-Trinket-Lesser").GetComponent<Text>().fontSize, 14);

                scene.EventSystem.SetSelectedGameObject(effect.gameObject);
                yield return WaitForChild(scene.Root, "UnityHeroEffectTooltip");
                Assert.IsTrue(new[]
                {
                    "UnityHeroEffectTooltipKind",
                    "UnityHeroEffectTooltipTitle",
                    "UnityHeroEffectTooltipDescription",
                    "UnityHeroEffectTooltipSource",
                    "UnityHeroEffectTooltipStatus"
                }.All(name => FindChild(scene.Root, name).GetComponent<Text>().fontSize >= 14));

                scene.EventSystem.SetSelectedGameObject(null);
                yield return WaitForMissing(scene.Root, "UnityHeroEffectTooltip");
                Click(scene, effect);
                yield return WaitForChild(scene.Root, "UnityHeroEffectTooltip");
                Assert.AreEqual(trinket.Name, FindChild(scene.Root, "UnityHeroEffectTooltipTitle").GetComponent<Text>().text);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PlayerDirectedChoice_SearchCloseAndSelectCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
                var target = service.GetPlayerSelectableQuestPairs().First(option => option.IsSelectable);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityPlayerDirectedChoiceButton-Quest");
                Click(scene, FindChild(scene.Root, "UnityPlayerDirectedChoiceButton-Quest"));
                yield return WaitForChild(scene.Root, "UnityPlayerDirectedChoiceSearchInput");

                var searchTransform = FindChild(scene.Root, "UnityPlayerDirectedChoiceSearchInput");
                yield return WaitForState(
                    () => scene.EventSystem.currentSelectedGameObject == searchTransform.gameObject,
                    "player-directed initial search focus");
                Assert.GreaterOrEqual(searchTransform.GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.GreaterOrEqual(searchTransform.GetComponent<InputField>().textComponent.fontSize, 14);

                yield return EnterTextAndCommit(scene, searchTransform, target.CardId);
                yield return WaitForChild(scene.Root, "UnityPlayerDirectedChoiceSelectButton");
                Assert.AreEqual("选择", FindChild(scene.Root, "UnityPlayerDirectedChoiceSelectButton").GetComponentInChildren<Text>(true).text);
                Assert.GreaterOrEqual(FindChild(scene.Root, "UnityPlayerDirectedChoiceSelectButton").GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.GreaterOrEqual(FindChild(scene.Root, "UnityPlayerDirectedChoiceText").GetComponent<Text>().fontSize, 14);

                Click(scene, FindChild(scene.Root, "UnityPlayerDirectedChoiceCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityPlayerDirectedChoiceOverlay");
                Assert.IsNotNull(service.State.Player.Tavern.AdvancedMechanics.PendingChoice);

                Click(scene, FindChild(scene.Root, "UnityPlayerDirectedChoiceButton-Quest"));
                yield return WaitForChild(scene.Root, "UnityPlayerDirectedChoiceSearchInput");
                Assert.AreEqual(string.Empty, FindChild(scene.Root, "UnityPlayerDirectedChoiceSearchInput").GetComponent<InputField>().text);
                yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityPlayerDirectedChoiceSearchInput"), target.CardId);
                yield return WaitForChild(scene.Root, "UnityPlayerDirectedChoiceSelectButton");
                Click(scene, FindChild(scene.Root, "UnityPlayerDirectedChoiceSelectButton"));
                yield return WaitForState(
                    () => service.State.Player.Tavern.AdvancedMechanics.PendingChoice == null &&
                          service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest?.QuestCardId == target.CardId,
                    "player-directed quest pair selection");
                Assert.IsNull(FindChildOrNull(scene.Root, "UnityPlayerDirectedChoiceOverlay"));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_MechanicLibraries_SearchDetailAndSelectCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));
                service.Apply(new GameCommand(GameCommandType.ChooseMechanicOption, 0));

                var opponentRewardIds = new HashSet<string>(service.GetOpponentSelectableQuestRewards().Select(reward => reward.CardId));
                var rewards = service.GetDebugSelectableQuestRewards()
                    .Where(reward => opponentRewardIds.Contains(reward.CardId))
                    .OrderBy(reward => reward.PowerLevel + " / " + reward.Trigger + " / " + reward.OfferPoolStatus)
                    .ThenBy(reward => reward.Name)
                    .ToList();
                Assert.Greater(rewards.Count, 1);
                var targetReward = rewards.First();
                var oldReward = rewards.Last();
                service.Apply(new GameCommand(GameCommandType.DebugReplaceQuestReward, oldReward.CardId, CardKind.QuestReward, false, 0));

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuestReplaceRewardButton-Main");

                Click(scene, FindChild(scene.Root, "UnityQuestReplaceRewardButton-Main"));
                yield return WaitForChild(scene.Root, "UnityAdvancedCardLibrarySearchInput");
                yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityAdvancedCardLibrarySearchInput"), targetReward.CardId);
                yield return WaitForChild(scene.Root, "UnityAdvancedCardLibraryDetailButton");
                Assert.AreEqual(1, scene.Root.GetComponentsInChildren<Button>(true).Count(button => button.gameObject.name.StartsWith("UnityAdvancedCardLibrarySelectButton", StringComparison.Ordinal)));

                Click(scene, FindChild(scene.Root, "UnityAdvancedCardLibraryDetailButton"));
                yield return WaitForChild(scene.Root, "UnityMechanicLibraryDetailOverlay");
                Assert.AreEqual(targetReward.Name, FindChild(scene.Root, "UnityMechanicLibraryDetailTitle").GetComponent<Text>().text);
                Assert.AreEqual(targetReward.PowerLevel + " / " + targetReward.Trigger + " / " + targetReward.OfferPoolStatus, FindChild(scene.Root, "UnityMechanicLibraryDetailMeta").GetComponent<Text>().text);
                Assert.AreEqual(targetReward.CardId, FindChild(scene.Root, "UnityMechanicLibraryDetailCardId").GetComponent<Text>().text);
                Assert.IsTrue(new[] { "UnityMechanicLibraryDetailMeta", "UnityMechanicLibraryDetailText", "UnityMechanicLibraryDetailNotes", "UnityMechanicLibraryDetailCardId" }
                    .All(name => FindChild(scene.Root, name).GetComponent<Text>().fontSize >= 14));

                Click(scene, FindChild(scene.Root, "UnityMechanicLibraryDetailCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityMechanicLibraryDetailOverlay");
                yield return WaitForChild(scene.Root, "UnityAdvancedCardLibrarySearchInput");
                Assert.AreEqual(targetReward.CardId, FindChild(scene.Root, "UnityAdvancedCardLibrarySearchInput").GetComponent<InputField>().text);

                Click(scene, FindChild(scene.Root, "UnityAdvancedCardLibraryClearSearchButton"));
                yield return WaitForChild(scene.Root, "UnityAdvancedCardLibrarySearchInput");
                Assert.AreEqual(string.Empty, FindChild(scene.Root, "UnityAdvancedCardLibrarySearchInput").GetComponent<InputField>().text);
                yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityAdvancedCardLibrarySearchInput"), targetReward.CardId);
                yield return WaitForChild(scene.Root, "UnityAdvancedCardLibrarySelectButton");
                Click(scene, FindChild(scene.Root, "UnityAdvancedCardLibrarySelectButton"));
                yield return WaitForState(
                    () => service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest.RewardCardId == targetReward.CardId,
                    "player quest reward replacement");

                yield return WaitForChild(scene.Root, "UnityOpponentEntryButton");
                Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
                yield return WaitForChild(scene.Root, "UnityOpponentQuestRewardSelectButton");
                Click(scene, FindChild(scene.Root, "UnityOpponentQuestRewardSelectButton"));
                yield return WaitForChild(scene.Root, "UnityOpponentMechanicLibrarySearchInput");
                yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityOpponentMechanicLibrarySearchInput"), targetReward.CardId);
                yield return WaitForChild(scene.Root, "UnityOpponentMechanicLibraryDetailButton");

                Click(scene, FindChild(scene.Root, "UnityOpponentMechanicLibraryDetailButton"));
                yield return WaitForChild(scene.Root, "UnityMechanicLibraryDetailOverlay");
                Assert.AreEqual(targetReward.CardId, FindChild(scene.Root, "UnityMechanicLibraryDetailCardId").GetComponent<Text>().text);
                Click(scene, FindChild(scene.Root, "UnityMechanicLibraryDetailCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityMechanicLibraryDetailOverlay");
                yield return WaitForChild(scene.Root, "UnityOpponentMechanicLibrarySearchInput");
                Assert.AreEqual(targetReward.CardId, FindChild(scene.Root, "UnityOpponentMechanicLibrarySearchInput").GetComponent<InputField>().text);

                Click(scene, FindChild(scene.Root, "UnityOpponentMechanicLibrarySelectButton"));
                yield return WaitForState(
                    () => service.State.Opponent.AdvancedMechanics.Quests.MainQuest.RewardId == targetReward.Id,
                    "opponent quest reward selection");
                Assert.IsNull(FindChildOrNull(scene.Root, "UnityOpponentMechanicLibraryOverlay"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityOpponentPanelOverlay"));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_CardPoolEditor_EmptyResetExcludeAndStartCompleteThroughRaycast()
        {
            var directory = Path.Combine(Path.GetTempPath(), "learn-hearthstone-card-pool-playmode-" + Guid.NewGuid().ToString("N"));
            try
            {
                using (var scene = new JourneyScene())
                {
                    MatchSetupOptions startedWith = null;
                    var repository = new JsonCardPoolVersionRepository(directory, "versions.json");
                    var minionCatalog = MinionCatalogLoader.LoadFromResources();
                    var targetMinion = minionCatalog.All
                        .Where(card =>
                            card.InPool &&
                            !card.CardId.StartsWith("BGDUO", StringComparison.OrdinalIgnoreCase) &&
                            card.Tribes != null &&
                            card.Tribes.Any(tribe => tribe != Tribe.None && tribe != Tribe.All))
                        .OrderBy(card => card.TavernTier)
                        .ThenBy(card => card.Name)
                        .First();
                    var targetTribe = targetMinion.Tribes.First(tribe => tribe != Tribe.None && tribe != Tribe.All);
                    new UnityTavernTribeSelectionView(
                        scene.Root,
                        setup => startedWith = setup,
                        () => { },
                        UnityTavernLayoutContext.ForSize(1366f, 768f),
                        repository,
                        minionCatalog,
                        SpellCatalogLoader.LoadFromResources()).Build();
                    yield return WaitForChild(scene.Root, "UnityCardPoolVersionOpenButton");

                    Click(scene, FindChild(scene.Root, "UnityCardPoolVersionOpenButton"));
                    yield return WaitForChild(scene.Root, "UnityCardPoolVersionCopyButton");
                    Click(scene, FindChild(scene.Root, "UnityCardPoolVersionCopyButton"));
                    yield return WaitForChild(scene.Root, "UnityCardPoolVersionSearchInput");
                    Assert.IsTrue(FindChild(scene.Root, "UnityCardPoolVersionDeleteButton").GetComponent<Button>().interactable);

                    yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityCardPoolVersionSearchInput"), "__missing_card__");
                    yield return WaitForState(
                        () => FindChildOrNull(scene.Root, "UnityCardPoolVersionLoadState")?.GetComponent<Text>().text.Contains("\u5f53\u524d\u7b5b\u9009\u65e0\u5361\u724c") == true,
                        "card-pool empty search result");
                    Assert.IsTrue(FindChild(scene.Root, "UnityCardPoolVersionResetFiltersButton").GetComponent<Button>().interactable);

                    Click(scene, FindChild(scene.Root, "UnityCardPoolVersionResetFiltersButton"));
                    yield return WaitForChild(scene.Root, "UnityCardPoolVersionSearchInput");
                    Assert.AreEqual(string.Empty, FindChild(scene.Root, "UnityCardPoolVersionSearchInput").GetComponent<InputField>().text);
                    Assert.IsFalse(FindChild(scene.Root, "UnityCardPoolVersionResetFiltersButton").GetComponent<Button>().interactable);

                    var tierButton = "UnityCardPoolVersionTier" + targetMinion.TavernTier + "Button";
                    var tribeButton = "UnityCardPoolVersionTribe" + targetTribe + "Button";
                    Click(scene, FindChild(scene.Root, tierButton));
                    yield return WaitForChild(scene.Root, "UnityCardPoolVersionSearchInput");
                    Click(scene, FindChild(scene.Root, tribeButton));
                    yield return WaitForChild(scene.Root, "UnityCardPoolVersionSearchInput");
                    yield return EnterTextAndCommit(scene, FindChild(scene.Root, "UnityCardPoolVersionSearchInput"), targetMinion.CardId);
                    yield return WaitForChild(scene.Root, "UnityCardPoolMinionToggle-" + targetMinion.CardId);

                    var filterSummary = FindChild(scene.Root, "UnityCardPoolVersionFilterCount").GetComponent<Text>().text;
                    StringAssert.Contains(targetMinion.TavernTier + "\u672c", filterSummary);
                    StringAssert.Contains(FindChild(scene.Root, tribeButton).GetComponentInChildren<Text>(true).text, filterSummary);
                    Click(scene, FindChild(scene.Root, "UnityCardPoolMinionToggle-" + targetMinion.CardId));
                    yield return WaitForState(
                        () => FindChildOrNull(scene.Root, "UnityCardPoolMinionToggle-" + targetMinion.CardId)?.GetComponent<Toggle>().isOn == false,
                        "card-pool minion exclusion");

                    Click(scene, FindChild(scene.Root, "UnityCardPoolVersionCloseButton"));
                    yield return WaitForMissing(scene.Root, "UnityCardPoolVersionOverlay");
                    foreach (var tribe in TribeAvailabilityRules.PlayableTribes.Take(5))
                    {
                        var buttonName = "UnityTribeSelection" + tribe + "Button";
                        Click(scene, FindChild(scene.Root, buttonName));
                        yield return WaitForChild(scene.Root, buttonName);
                    }

                    Click(scene, FindChild(scene.Root, "UnityTribeSelectionEnterButton"));
                    yield return WaitForChild(scene.Root, "UnityAdvancedMechanicsStartButton");
                    Click(scene, FindChild(scene.Root, "UnityAdvancedMechanicsStartButton"));
                    yield return WaitForState(() => startedWith != null, "custom card-pool start");

                    Assert.IsFalse(startedWith.IsDefaultCardPoolVersion);
                    Assert.IsFalse(string.IsNullOrEmpty(startedWith.CardPoolVersionId));
                    CollectionAssert.DoesNotContain(startedWith.EnabledMinionCardIds, targetMinion.CardId);
                    CollectionAssert.AreEquivalent(TribeAvailabilityRules.PlayableTribes.Take(5), startedWith.ActiveTribes);
                    var saved = repository.Load();
                    Assert.AreEqual(1, saved.Versions.Count);
                    CollectionAssert.DoesNotContain(saved.Versions[0].EnabledMinionCardIds, targetMinion.CardId);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ02_BasicRecruitActionsCompleteThroughRealInput()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(13579, new InMemoryTestScenarioRepository());
                service.State.Player.Tavern.Gold = 20;
                service.State.Player.Tavern.MaxGold = 20;
                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuickRefreshButton");

                Click(scene, FindChild(scene.Root, "UnityQuickFreezeButton"));
                yield return WaitForState(() => service.State.Player.Tavern.Frozen, "freeze");
                yield return WaitForChild(scene.Root, "UnityQuickFreezeButton");
                Click(scene, FindChild(scene.Root, "UnityQuickFreezeButton"));
                yield return WaitForState(() => !service.State.Player.Tavern.Frozen, "unfreeze");

                var shopBefore = service.State.Player.Tavern.Shop.Select(card => card.InstanceId).ToArray();
                Click(scene, FindChild(scene.Root, "UnityQuickRefreshButton"));
                yield return WaitForState(
                    () => !shopBefore.SequenceEqual(service.State.Player.Tavern.Shop.Select(card => card.InstanceId)),
                    "shop refresh");

                var firstShopCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                Click(scene, FindChild(scene.Root, "UnityCardAction-" + firstShopCard.InstanceId));
                yield return WaitForState(() => service.State.Player.Tavern.Hand.Any(card => card.InstanceId == firstShopCard.InstanceId), "first purchase");
                yield return WaitForChild(scene.Root, "UnityCard-" + firstShopCard.InstanceId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + firstShopCard.InstanceId), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 0));
                yield return WaitForState(() => service.State.Player.Board.Any(card => card.InstanceId == firstShopCard.InstanceId), "first play");

                var secondShopCard = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                yield return WaitForChild(scene.Root, "UnityCardAction-" + secondShopCard.InstanceId);
                Click(scene, FindChild(scene.Root, "UnityCardAction-" + secondShopCard.InstanceId));
                yield return WaitForState(() => service.State.Player.Tavern.Hand.Any(card => card.InstanceId == secondShopCard.InstanceId), "second purchase");
                yield return WaitForChild(scene.Root, "UnityCard-" + secondShopCard.InstanceId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + secondShopCard.InstanceId), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 1));
                yield return WaitForState(() => service.State.Player.Board.Count >= 2, "second play");

                var movedId = service.State.Player.Board[1].InstanceId;
                yield return WaitForChild(scene.Root, "UnityCard-" + movedId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + movedId), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 0));
                yield return WaitForState(() => service.State.Player.Board[0].InstanceId == movedId, "board reorder");

                var soldId = service.State.Player.Board[0].InstanceId;
                yield return WaitForChild(scene.Root, "UnityCard-" + soldId);
                yield return Drag(scene, FindChild(scene.Root, "UnityCard-" + soldId), DropTarget(scene.Root, UnityTavernDropTarget.SellZone, -1));
                yield return WaitForState(() => service.State.Player.Board.All(card => card.InstanceId != soldId), "sell");

                var tierBefore = service.State.Player.Tavern.Tier;
                yield return WaitForChild(scene.Root, "UnityQuickUpgradeButton");
                Click(scene, FindChild(scene.Root, "UnityQuickUpgradeButton"));
                yield return WaitForState(() => service.State.Player.Tavern.Tier == tierBefore + 1, "upgrade");

                Assert.IsTrue(FindChild(scene.Root, "UnityQuickUpgradeButton").GetComponent<Button>().interactable);
                Assert.IsTrue(FindChild(scene.Root, "UnityFeedbackToast").GetComponentsInChildren<Text>(true).Any(text => text.text == "酒馆已升级"));
                var recruitMessages = service.State.Player.Tavern.RecruitLog.Select(entry => entry.Message).ToList();
                Assert.IsTrue(recruitMessages.Any(message => message == "刷新酒馆"));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("购买 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("打出 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("调整站位 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("出售 ", StringComparison.Ordinal)));
                Assert.IsTrue(recruitMessages.Any(message => message.StartsWith("升级到酒馆等级 ", StringComparison.Ordinal)));
                Assert.IsFalse(recruitMessages.Any(message => message.Contains("璐") || message.Contains("鎵") || message.Contains("鍑") || message.Contains("閰")));
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ06_PJ07_PJ08_ToolsReplayAndReturnCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var returned = false;
                var service = MatchService.CreateWithDefaultCatalog(97531, new InMemoryTestScenarioRepository());
                var visibleCards = service.State.Player.Tavern.Shop.Where(card => card != null && card.CardKind == CardKind.Minion).Take(2).ToList();
                Assert.AreEqual(2, visibleCards.Count);
                var player = visibleCards[0].Clone();
                player.InstanceId = "pj07-player";
                player.Owner = BoardSide.Player;
                var opponent = visibleCards[1].Clone();
                opponent.InstanceId = "pj07-opponent";
                opponent.Owner = BoardSide.Opponent;
                service.State.Player.Board.Add(player);
                service.State.Opponent.Board.Add(opponent);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => returned = true).Build();
                yield return WaitForChild(scene.Root, "UnityOpponentEntryButton");

                Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
                yield return WaitForChild(scene.Root, "UnityOpponentPanelCloseButton");
                Assert.IsNotNull(FindChild(scene.Root, "UnityOpponentPanel"));
                Click(scene, FindChild(scene.Root, "UnityOpponentPanelCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityOpponentPanel");

                Click(scene, FindChild(scene.Root, "UnityQuickToolsButton"));
                yield return WaitForChild(scene.Root, "UnityTrainerToolsCloseButton");
                Assert.IsNotNull(FindChild(scene.Root, "UnityTrainerToolsOverlay"));
                Click(scene, FindChild(scene.Root, "UnityTrainerToolsCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityTrainerToolsOverlay");

                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatBattlefieldRoot");
                Assert.IsNotNull(service.State.LastReplay);
                Assert.Greater(service.State.LastReplay.Frames.Count, 1);

                Click(scene, FindChild(scene.Root, "UnityReplayNextButton"));
                yield return WaitForChild(scene.Root, "UnityReplaySpeedButton");
                Click(scene, FindChild(scene.Root, "UnityReplaySpeedButton"));
                yield return WaitForChild(scene.Root, "UnityReplayPlayPauseButton");
                Click(scene, FindChild(scene.Root, "UnityReplayPlayPauseButton"));
                yield return null;
                yield return WaitForChild(scene.Root, "UnityReplayLastButton");
                Click(scene, FindChild(scene.Root, "UnityReplayLastButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCloseButton");
                Click(scene, FindChild(scene.Root, "UnityCombatCloseButton"));
                yield return WaitForChild(scene.Root, "UnityBackButton");
                Assert.IsNotNull(service.State.LastReplay);

                Click(scene, FindChild(scene.Root, "UnityBackButton"));
                yield return WaitForChild(scene.Root, "UnityReturnConfirmNoButton");
                Click(scene, FindChild(scene.Root, "UnityReturnConfirmNoButton"));
                yield return WaitForMissing(scene.Root, "UnityReturnConfirmOverlay");
                Assert.IsFalse(returned);

                Click(scene, FindChild(scene.Root, "UnityBackButton"));
                yield return WaitForChild(scene.Root, "UnityReturnConfirmYesButton");
                Click(scene, FindChild(scene.Root, "UnityReturnConfirmYesButton"));
                yield return WaitForState(() => returned, "confirmed return");
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ03_PJ05_GeorgeTargetingShieldAndReplayCompleteThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    86420,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions { SelectedHeroCardId = "TB_BaconShop_HERO_15" });
                service.State.Player.Tavern.Gold = 5;
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                var source = service.State.Player.Tavern.Shop.First(card => card != null && card.CardKind == CardKind.Minion);
                var target = source.Clone();
                target.InstanceId = "pj03-george-target";
                target.Owner = BoardSide.Player;
                target.Attack = target.BaseAttack = 1;
                target.Health = target.MaxHealth = target.BaseHealth = 100;
                target.Keywords.Remove(Keyword.DivineShield);
                var enemy = source.Clone();
                enemy.InstanceId = "pj03-george-enemy";
                enemy.Owner = BoardSide.Opponent;
                enemy.Attack = enemy.BaseAttack = 1;
                enemy.Health = enemy.MaxHealth = enemy.BaseHealth = 100;
                enemy.Keywords.Clear();
                service.State.Player.Board.Add(target);
                service.State.Opponent.Board.Add(enemy);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityQuickHeroPowerButton");

                var goldBefore = service.State.Player.Tavern.Gold;
                Click(scene, FindChild(scene.Root, "UnityQuickHeroPowerButton"));
                yield return WaitForChild(scene.Root, "UnityTargetingCancelButton");
                Assert.AreEqual("可选", FindChild(FindChild(scene.Root, "UnityCard-" + target.InstanceId), "UnityTargetingLabelText").GetComponent<Text>().text);
                Click(scene, FindChild(scene.Root, "UnityTargetingCancelButton"));
                yield return WaitForMissing(scene.Root, "UnityTargetingCancelButton");
                Assert.AreEqual(goldBefore, service.State.Player.Tavern.Gold);

                Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
                yield return WaitForChild(scene.Root, "UnityCard-" + enemy.InstanceId);
                var opponentDropTarget = DropTarget(scene.Root, UnityTavernDropTarget.OpponentBoard, 0);
                yield return ScrollUntilReachable(scene, "UnityOpponentPanelScroll", opponentDropTarget.transform);
                yield return Drag(scene, FindChild(scene.Root, "UnityQuickHeroPowerButton"), opponentDropTarget);
                yield return WaitForChild(scene.Root, "UnityErrorToast");
                Assert.AreEqual(goldBefore, service.State.Player.Tavern.Gold);
                Assert.IsFalse(enemy.Keywords.Contains(Keyword.DivineShield));

                Click(scene, FindChild(scene.Root, "UnityOpponentPanelCloseButton"));
                yield return WaitForChild(scene.Root, "UnityQuickHeroPowerButton");
                yield return Drag(scene, FindChild(scene.Root, "UnityQuickHeroPowerButton"), DropTarget(scene.Root, UnityTavernDropTarget.PlayerBoard, 0));
                yield return WaitForState(() => target.Keywords.Contains(Keyword.DivineShield), "George Divine Shield");
                Assert.AreEqual(goldBefore - 1, service.State.Player.Tavern.Gold);

                yield return WaitForChild(scene.Root, "UnityQuickNextTurnButton");
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");
                var shieldFrame = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.DivineShieldBroken);
                Assert.GreaterOrEqual(shieldFrame, 0);
                for (var index = 0; index < shieldFrame; index += 1)
                {
                    yield return WaitForChild(scene.Root, "UnityReplayNextButton");
                    Click(scene, FindChild(scene.Root, "UnityReplayNextButton"));
                    yield return null;
                }

                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");
                StringAssert.Contains("圣盾破裂", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        private sealed class JourneyScene : IDisposable
        {
            private readonly GameObject canvasObject;
            private readonly GameObject eventSystemObject;

            public JourneyScene()
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                Canvas = canvasObject.GetComponent<Canvas>();
                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);

                var rootObject = new GameObject("Root", typeof(RectTransform));
                rootObject.transform.SetParent(canvasObject.transform, false);
                Root = rootObject.transform;
                UnityTavernUiStyle.Stretch(rootObject.GetComponent<RectTransform>());
            }

            public Transform Root { get; }
            public Canvas Canvas { get; }
            public EventSystem EventSystem => eventSystemObject.GetComponent<EventSystem>();
            public GraphicRaycaster Raycaster => canvasObject.GetComponent<GraphicRaycaster>();

            public void Dispose()
            {
                UnityEngine.Object.Destroy(canvasObject);
                UnityEngine.Object.Destroy(eventSystemObject);
            }
        }

        private static void Click(JourneyScene scene, Transform target)
        {
            var pointer = PointerAt(scene, target.GetComponent<RectTransform>());
            var hit = Raycast(scene, pointer, target);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static IEnumerator ScrollToAndClick(JourneyScene scene, string scrollName, string targetName)
        {
            var scroll = FindChild(scene.Root, scrollName).GetComponent<ScrollRect>();
            var target = FindChild(scene.Root, targetName);
            for (var attempt = 0; attempt < 40; attempt += 1)
            {
                Canvas.ForceUpdateCanvases();
                if (TryClick(scene, target))
                {
                    yield break;
                }

                var pointer = new PointerEventData(scene.EventSystem)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center)),
                    scrollDelta = new Vector2(0f, -6f)
                };
                ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.scrollHandler);
                yield return null;
            }

            Assert.Fail("Could not scroll " + targetName + " into view.");
        }

        private static IEnumerator ScrollUntilReachable(JourneyScene scene, string scrollName, Transform target)
        {
            var scroll = FindChild(scene.Root, scrollName).GetComponent<ScrollRect>();
            for (var attempt = 0; attempt < 40; attempt += 1)
            {
                Canvas.ForceUpdateCanvases();
                var pointer = PointerAt(scene, target.GetComponent<RectTransform>());
                var hits = new List<RaycastResult>();
                scene.Raycaster.Raycast(pointer, hits);
                if (hits.Any(result => result.gameObject == target.gameObject || result.gameObject.transform.IsChildOf(target)))
                {
                    yield break;
                }

                pointer.position = RectTransformUtility.WorldToScreenPoint(null, scroll.viewport.TransformPoint(scroll.viewport.rect.center));
                pointer.scrollDelta = new Vector2(0f, -6f);
                ExecuteEvents.Execute(scroll.gameObject, pointer, ExecuteEvents.scrollHandler);
                yield return null;
            }

            Assert.Fail("Could not scroll " + target.name + " into raycast reach.");
        }

        private static bool TryClick(JourneyScene scene, Transform target)
        {
            var pointer = PointerAt(scene, target.GetComponent<RectTransform>());
            var hits = new List<RaycastResult>();
            scene.Raycaster.Raycast(pointer, hits);
            var hit = hits.FirstOrDefault(result => result.gameObject == target.gameObject || result.gameObject.transform.IsChildOf(target));
            if (hit.gameObject == null)
            {
                return false;
            }

            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            return true;
        }

        private static IEnumerator EnterTextAndCommit(JourneyScene scene, Transform target, string value)
        {
            Click(scene, target);
            yield return null;

            var input = target.GetComponent<InputField>();
            Assert.IsNotNull(input, target.name + " is not an InputField.");
            scene.EventSystem.SetSelectedGameObject(target.gameObject);
            input.ActivateInputField();
            yield return null;
            Assert.IsTrue(input.isFocused, target.name + " did not enter focused input state.");

            input.text = string.Empty;
            foreach (var character in value ?? string.Empty)
            {
                input.text += character;
                yield return null;
            }

            Assert.AreEqual(value ?? string.Empty, input.text);
            input.DeactivateInputField();
            scene.EventSystem.SetSelectedGameObject(null);
            yield return null;
        }

        private static void ClickAtNormalized(JourneyScene scene, Transform target, Vector2 normalized)
        {
            var rect = target.GetComponent<RectTransform>();
            var local = new Vector3(
                Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, normalized.x),
                Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, normalized.y));
            var pointer = new PointerEventData(scene.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(local))
            };
            var hit = Raycast(scene, pointer, target);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit, pointer, ExecuteEvents.pointerClickHandler);
        }

        private static IEnumerator Drag(JourneyScene scene, Transform source, UnityTavernDropTargetBehaviour target)
        {
            var sourcePointer = PointerAt(scene, source.GetComponent<RectTransform>());
            var sourceHit = Raycast(scene, sourcePointer, source);
            ExecuteEvents.ExecuteHierarchy(sourceHit, sourcePointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(sourceHit, sourcePointer, ExecuteEvents.beginDragHandler);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.IsTrue(target.gameObject.activeInHierarchy, target.name + " did not become visible after drag began.");

            var targetPointer = PointerAt(scene, target.GetComponent<RectTransform>());
            var targetHit = Raycast(scene, targetPointer, target.transform);
            ExecuteEvents.ExecuteHierarchy(sourceHit, targetPointer, ExecuteEvents.dragHandler);
            ExecuteEvents.ExecuteHierarchy(targetHit, targetPointer, ExecuteEvents.pointerEnterHandler);
            yield return null;
            ExecuteEvents.ExecuteHierarchy(targetHit, targetPointer, ExecuteEvents.dropHandler);
            ExecuteEvents.ExecuteHierarchy(sourceHit, targetPointer, ExecuteEvents.endDragHandler);
            ExecuteEvents.ExecuteHierarchy(sourceHit, targetPointer, ExecuteEvents.pointerUpHandler);
        }

        private static PointerEventData PointerAt(JourneyScene scene, RectTransform target)
        {
            return new PointerEventData(scene.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, target.TransformPoint(target.rect.center))
            };
        }

        private static GameObject Raycast(JourneyScene scene, PointerEventData pointer, Transform expected)
        {
            var hits = new List<RaycastResult>();
            scene.Raycaster.Raycast(pointer, hits);
            var hit = hits.FirstOrDefault(result => result.gameObject == expected.gameObject || result.gameObject.transform.IsChildOf(expected));
            Assert.IsNotNull(hit.gameObject, expected.name + " was not reachable through GraphicRaycaster.");
            return hit.gameObject;
        }

        private static UnityTavernDropTargetBehaviour DropTarget(Transform root, UnityTavernDropTarget target, int index)
        {
            var result = root.GetComponentsInChildren<UnityTavernDropTargetBehaviour>(true)
                .FirstOrDefault(candidate => candidate.Target == target && candidate.TargetIndex == index);
            Assert.IsNotNull(result, "Missing drop target " + target + " at " + index + ".");
            return result;
        }

        private static IEnumerable<UnityTavernCardComponent> CardLibraryCards(Transform root)
        {
            return root.GetComponentsInChildren<Button>(true)
                .Where(button =>
                    button.gameObject.activeInHierarchy &&
                    button.gameObject.name.StartsWith("UnityCardLibraryAddButton", StringComparison.Ordinal))
                .Select(button => button.transform.parent == null || button.transform.parent.parent == null
                    ? null
                    : button.transform.parent.parent.GetComponentInChildren<UnityTavernCardComponent>(true))
                .Where(component => component != null);
        }

        private static UnityTavernCardComponent FindCardLibraryCard(Transform root, string cardId)
        {
            return CardLibraryCards(root).FirstOrDefault(component => component.Card != null && component.Card.CardId == cardId);
        }

        private static Transform FindChild(Transform root, string name)
        {
            var child = FindChildOrNull(root, name);
            Assert.IsNotNull(child, "Missing child: " + name);
            return child;
        }

        private static Transform FindChildOrNull(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (var index = 0; index < root.childCount; index += 1)
            {
                var child = FindChildOrNull(root.GetChild(index), name);
                if (child != null) return child;
            }

            return null;
        }

        private static IEnumerator WaitForChild(Transform root, string name)
        {
            Transform stableChild = null;
            var stableFrames = 0;
            for (var frame = 0; frame < 90; frame += 1)
            {
                Canvas.ForceUpdateCanvases();
                var child = FindChildOrNull(root, name);
                if (child != null && child == stableChild)
                {
                    if (++stableFrames >= 2) yield break;
                }
                else
                {
                    stableChild = child;
                    stableFrames = child == null ? 0 : 1;
                }

                yield return null;
            }

            Assert.Fail("Timed out waiting for child: " + name);
        }

        private static IEnumerator WaitForState(Func<bool> condition, string operation)
        {
            for (var frame = 0; frame < 90; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (condition()) yield break;
            }

            Assert.Fail("Timed out waiting for " + operation + ".");
        }

        private static IEnumerator WaitForMissing(Transform root, string name)
        {
            for (var frame = 0; frame < 90; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (FindChildOrNull(root, name) == null) yield break;
            }

            Assert.Fail("Timed out waiting for removal: " + name);
        }

        private static void ClearChildren(Transform root)
        {
            for (var index = root.childCount - 1; index >= 0; index -= 1)
            {
                UnityEngine.Object.Destroy(root.GetChild(index).gameObject);
            }
        }
    }
}
