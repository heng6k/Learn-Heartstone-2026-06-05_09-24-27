using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class PlayerDirectedAdvancedMechanicSelectionTests
    {
        [Test]
        public void PlayerDirectedQuestChoice_SelectsQuestAndRewardPair()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));

            var pair = service.GetPlayerSelectableQuestPairs()
                .First(option => option.IsSelectable);

            service.Apply(new GameCommand(
                GameCommandType.ChoosePlayerDirectedQuestPair,
                pair.CardId,
                pair.SecondaryCardId,
                CardKind.Quest,
                0));

            var quest = service.State.Player.Tavern.AdvancedMechanics.Quests.MainQuest;
            Assert.IsNotNull(quest);
            Assert.AreEqual(pair.CardId, quest.QuestCardId);
            Assert.AreEqual(pair.SecondaryCardId, quest.RewardCardId);
            Assert.IsNull(service.GetActiveMechanicChoice());
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("已定向选择任务：")));
        }

        [Test]
        public void PlayerDirectedTrinketChoice_RespectsActiveTribesAndEquipsThroughServicePath()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions { ActiveTribes = new List<Tribe> { Tribe.Beast } });
            service.State.Player.Tavern.Gold = 99;
            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));

            var options = service.GetPlayerSelectableTrinkets(TrinketSlotKind.Lesser);
            Assert.Greater(options.Count(option => option.IsSelectable), 0);
            Assert.IsTrue(options
                .Where(option => option.IsSelectable)
                .Select(option => service.TrinketCatalog.GetByCardId(option.CardId))
                .All(definition =>
                    definition.SlotKind == TrinketSlotKind.Lesser &&
                    definition.ImplementationStatus == TrinketImplementationStatus.Implemented &&
                    definition.OfferPoolStatus == TrinketOfferPoolStatus.Offerable &&
                    TribeAvailabilityRules.IsTrinketAvailable(definition, service.State.ActiveTribes)));

            var selected = options.First(option => option.IsSelectable);
            service.Apply(new GameCommand(
                GameCommandType.ChoosePlayerDirectedTrinket,
                selected.CardId,
                CardKind.Trinket,
                0));

            var trinkets = service.State.Player.Tavern.AdvancedMechanics.Trinkets;
            Assert.AreEqual(selected.CardId, trinkets.LesserTrinketId);
            Assert.IsTrue(trinkets.Equipped.Any(equipped => equipped.TrinketId == selected.CardId));
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("已定向选择小型饰品：")));
        }

        [Test]
        public void PlayerDirectedTrinketChoice_RejectsMismatchedPendingSlot()
        {
            var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
            service.State.Player.Tavern.Gold = 99;
            service.Apply(new GameCommand(GameCommandType.DebugOfferLesserTrinkets));

            var greater = service.GetPlayerSelectableTrinkets(TrinketSlotKind.Greater)
                .First(option => option.IsSelectable);

            Assert.Throws<System.InvalidOperationException>(() => service.Apply(new GameCommand(
                GameCommandType.ChoosePlayerDirectedTrinket,
                greater.CardId,
                CardKind.Trinket,
                1)));
            Assert.IsNull(service.State.Player.Tavern.AdvancedMechanics.Trinkets.GreaterTrinketId);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("定向选择未成功")));
            Assert.IsFalse(service.State.Player.Tavern.RecruitLog.Any(entry => entry.Message.Contains(greater.CardId)));
        }

        [Test]
        public void PlayerDirectedSecondHeroPower_ExcludesOwnedAndCurrentPowers()
        {
            var service = MatchService.CreateWithDefaultCatalog(
                12345,
                new InMemoryTestScenarioRepository(),
                new MatchSetupOptions
                {
                    EnableAnomalies = true,
                    RandomizeAnomaly = false,
                    SelectedAnomalyCardId = "BG31_Anomaly_123",
                    AnomalyPoolVersion = AnomalyPoolVersion.CurrentHsReplay
                });
            var primaryHeroPower = service.State.Player.HeroPowerCardId;

            var options = service.GetPlayerSelectableSecondHeroPowers();
            Assert.Greater(options.Count(option => option.IsSelectable), 0);
            Assert.IsFalse(options.Any(option => option.CardId == primaryHeroPower && option.IsSelectable));

            var selected = options.First(option => option.IsSelectable);
            service.Apply(new GameCommand(GameCommandType.ChoosePlayerDirectedSecondHeroPower, selected.CardId, CardKind.HeroPower));

            CollectionAssert.Contains(service.State.Player.ExtraHeroPowerCardIds, selected.CardId);
            Assert.IsNull(service.State.Player.Tavern.Discover);
            Assert.IsFalse(service.GetPlayerSelectableSecondHeroPowers().First(option => option.CardId == selected.CardId).IsSelectable);
            Assert.IsTrue(service.State.Player.Tavern.RecruitLog.Any(entry =>
                entry.Message.Contains("已定向选择第二英雄技能：")));
        }

        [Test]
        public void PlayerDirectedChoiceModal_ShowsButtonOnSupportedChoiceScreens()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var service = MatchService.CreateWithDefaultCatalog(12345, new InMemoryTestScenarioRepository());
                service.Apply(new GameCommand(GameCommandType.DebugOfferQuests));

                new UnityTavernTrainerView(rootObject.transform, service, new LocalAdvisorService(), () => { }).Build();

                var button = FindChild(rootObject.transform, "UnityPlayerDirectedChoiceButton-Quest");
                Assert.IsNotNull(button);
                button.GetComponent<Button>().onClick.Invoke();

                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceOverlay"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceFilters"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceFilterStatusSelectable"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceFilterCostFree"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceFilterTagAll"));
                Assert.IsNotNull(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceSelectButton"));
                Assert.AreEqual("关闭", FindChild(rootObject.transform, "UnityPlayerDirectedChoiceCloseButton").GetComponentInChildren<Text>(true).text);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceCloseButton").GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceCloseButton").GetComponentInChildren<Text>(true).fontSize, 14);

                var search = FindChild(rootObject.transform, "UnityPlayerDirectedChoiceSearchInput").GetComponent<InputField>();
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceSearchInput").GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.AreEqual(Vector2.zero, search.textComponent.rectTransform.anchorMin);
                Assert.AreEqual(Vector2.one, search.textComponent.rectTransform.anchorMax);
                Assert.GreaterOrEqual(search.textComponent.fontSize, 14);
                Assert.GreaterOrEqual(((Text)search.placeholder).fontSize, 14);

                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceFilterStatusSelectable").GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceFilterStatusSelectable").GetComponentInChildren<Text>(true).fontSize, 14);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceName").GetComponent<Text>().fontSize, 14);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceMeta").GetComponent<Text>().fontSize, 14);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceText").GetComponent<Text>().fontSize, 14);
                Assert.GreaterOrEqual(FindChild(rootObject.transform, "UnityPlayerDirectedChoiceSelectButton").GetComponent<LayoutElement>().preferredHeight, 44f);
                CollectionAssert.Contains(
                    new[] { "选择", "不可选择" },
                    FindChild(rootObject.transform, "UnityPlayerDirectedChoiceSelectButton").GetComponentInChildren<Text>(true).text);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        private static Transform FindChild(Transform parent, string name)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == name)
            {
                return parent;
            }

            for (var index = 0; index < parent.childCount; index += 1)
            {
                var found = FindChild(parent.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
