using System.Collections.Generic;
using System.IO;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class UnityCombatReplayPanelAcceptanceTests
    {
        private const string CaptureDirectory = ".planning/tavern-ui-screenshot-requirements/captures";

        [Test]
        public void CombatReplayPanel_WhiteBoxStatsAndMaxStepsRenderExactHudValues()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var options = AcceptanceOptions(400, "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%", "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400");
                options.TimelineOpen = true;
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(CreateAcceptanceReplay(), 1, options);

                Assert.AreEqual(
                    "\u6700\u5927\u8f6e\u6b21 400",
                    FindChild(panelObject.transform, "UnityCombatMaxStepsLabel").GetComponent<Text>().text);
                Assert.AreEqual(
                    "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%",
                    FindChild(panelObject.transform, "UnityCombatStatsText").GetComponent<Text>().text);
                Assert.AreEqual(
                    "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400",
                    FindChild(panelObject.transform, "UnityCombatStatsMetaText").GetComponent<Text>().text);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineDrawer"));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_BoardSlotsRenderCardFacesAndChineseTerms()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = CreateAcceptanceReplay();
                replay.Frames[1].EventType = CombatEventType.TrinketTriggered;
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    replay,
                    1,
                    AcceptanceOptions(400, "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%", "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400"));

                var playerTile = FindChild(panelObject.transform, "UnityReplayMinion-accept-player-1");
                Assert.IsNotNull(playerTile);
                Assert.IsNotNull(FindChild(playerTile, "UnityCombatCardFace-accept-player-1"));
                var playerArt = FindChild(playerTile, "UnityCombatCardArt-accept-player-1").GetComponent<Image>();
                var playerArtViewport = FindChild(playerTile, "UnityCombatCardArtViewport-accept-player-1");
                Assert.IsNotNull(playerArt);
                Assert.IsNotNull(playerArtViewport.GetComponent<RectMask2D>());
                Assert.IsNotNull(playerArtViewport.GetComponent<Mask>());
                Assert.AreSame(playerArtViewport, playerArt.transform.parent);
                Assert.AreEqual(new Vector2(-0.20f, -1.22f), playerArt.rectTransform.anchorMin);
                Assert.AreEqual(new Vector2(1.20f, 1.05f), playerArt.rectTransform.anchorMax);
                Assert.IsFalse(playerArt.preserveAspect);
                Assert.AreEqual("4本 野兽", FindChild(playerTile, "UnityCombatCardHeader-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("复活的骑兵", FindChild(playerTile, "UnityCombatCardName-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("圣盾 嘲讽", FindChild(playerTile, "UnityCombatCardKeywords-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("6", FindChild(playerTile, "UnityCombatCardAttackText-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("7", FindChild(playerTile, "UnityCombatCardHealthText-accept-player-1").GetComponent<Text>().text);
                var playerKeywordRoot = FindChild(playerTile, "UnityKeywordVisualRoot");
                Assert.IsNotNull(playerKeywordRoot);
                Assert.IsFalse(playerKeywordRoot.GetComponent<CanvasGroup>().blocksRaycasts);
                Assert.IsNotNull(FindChild(playerKeywordRoot, "UnityKeywordBadge-DivineShield"));
                Assert.IsNotNull(FindChild(playerKeywordRoot, "UnityKeywordBadge-Taunt"));
                Assert.IsNotNull(FindChild(playerKeywordRoot, "UnityKeywordEffect-DivineShield"));
                Assert.IsNotNull(FindChild(playerKeywordRoot, "UnityKeywordEffect-Taunt"));

                var opponentTile = FindChild(panelObject.transform, "UnityReplayMinion-accept-opponent-1");
                var opponentArt = FindChild(opponentTile, "UnityCombatCardArt-accept-opponent-1").GetComponent<Image>();
                Assert.IsNotNull(FindChild(opponentTile, "UnityCombatCardArtViewport-accept-opponent-1").GetComponent<RectMask2D>());
                Assert.IsNotNull(FindChild(opponentTile, "UnityCombatCardArtViewport-accept-opponent-1").GetComponent<Mask>());
                Assert.AreEqual(new Vector2(-0.20f, -1.22f), opponentArt.rectTransform.anchorMin);
                Assert.AreEqual(new Vector2(1.20f, 1.05f), opponentArt.rectTransform.anchorMax);
                Assert.IsFalse(opponentArt.preserveAspect);
                Assert.AreEqual("烈毒", FindChild(opponentTile, "UnityCombatCardKeywords-accept-opponent-1").GetComponent<Text>().text);
                var opponentKeywordRoot = FindChild(opponentTile, "UnityKeywordVisualRoot");
                Assert.IsNotNull(opponentKeywordRoot);
                Assert.IsNotNull(FindChild(opponentKeywordRoot, "UnityKeywordBadge-Venomous"));
                Assert.AreEqual("饰品触发", FindChild(panelObject.transform, "UnityReplayEventChipText-Event").GetComponent<Text>().text);
                Assert.IsFalse(ContainsText(panelObject.transform, "DivineShield"));
                Assert.IsFalse(ContainsText(panelObject.transform, "Venomous"));
                Assert.IsFalse(ContainsText(panelObject.transform, "Beast"));
                Assert.IsFalse(ContainsText(panelObject.transform, "player attack resolves"));
                Assert.IsFalse(ContainsText(panelObject.transform, "Shield Captain"));
                Assert.IsFalse(ContainsText(panelObject.transform, "Venom Guard"));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_LoadsNestedSnapshotMinionArt()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = CreateAcceptanceReplay();
                var minion = replay.Frames[1].PlayerBoardSnapshot.Minions[0];
                minion.CardId = "BG36_514";
                minion.CardKind = CardKind.Minion;
                minion.ImagePath = "CardImages/Minions/Season14/BG36_514";

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    replay,
                    1,
                    AcceptanceOptions(400, "nested art", "nested art"));

                Assert.IsNotNull(
                    FindChild(panelObject.transform, "UnityCombatCardArt-accept-player-1").GetComponent<Image>().sprite);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_ShowsHeroAdjacentTrinketsWithAccessibleDetails()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var options = AcceptanceOptions(400, "trinkets", "trinkets");
                options.PlayerTrinkets = new List<UnityCombatTrinketDisplay>
                {
                    new UnityCombatTrinketDisplay
                    {
                        SlotKind = TrinketSlotKind.Lesser,
                        Name = "Player Lesser",
                        Description = "Player lesser trinket effect.",
                        Status = "Active",
                        CardId = "BG36_MagicItem_200",
                        ImagePath = "CardImages/Trinkets/Season14/BG36_MagicItem_200",
                        Active = true
                    }
                };
                options.OpponentTrinkets = new List<UnityCombatTrinketDisplay>
                {
                    new UnityCombatTrinketDisplay
                    {
                        SlotKind = TrinketSlotKind.Greater,
                        Name = "Opponent Greater",
                        Description = "Opponent greater trinket effect.",
                        Status = "Scheduled for round 9",
                        CardId = "BG36_MagicItem_211",
                        ImagePath = "CardImages/Trinkets/Season14/BG36_MagicItem_211",
                        Active = false
                    }
                };

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(CreateAcceptanceReplay(), 1, options);

                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatPlayerTrinketRack"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatOpponentTrinketRack"));

                var playerIcon = FindChild(panelObject.transform, "UnityCombatPlayerTrinket-Lesser");
                var opponentIcon = FindChild(panelObject.transform, "UnityCombatOpponentTrinket-Greater");
                Assert.IsNotNull(playerIcon.GetComponent<Image>().sprite);
                Assert.IsNotNull(opponentIcon.GetComponent<Image>().sprite);
                Assert.GreaterOrEqual(playerIcon.GetComponent<LayoutElement>().preferredWidth, 44f);
                Assert.GreaterOrEqual(playerIcon.GetComponent<LayoutElement>().preferredHeight, 44f);
                Assert.Less(opponentIcon.GetComponent<Image>().color.a, playerIcon.GetComponent<Image>().color.a);
                StringAssert.Contains("9", FindChild(opponentIcon, "UnityCombatOpponentTrinketLockBadge-Greater").GetComponent<Text>().text);

                var trigger = playerIcon.GetComponent<EventTrigger>();
                Assert.IsTrue(trigger.triggers.Exists(entry => entry.eventID == EventTriggerType.PointerEnter));
                Assert.IsTrue(trigger.triggers.Exists(entry => entry.eventID == EventTriggerType.PointerExit));
                Assert.IsTrue(trigger.triggers.Exists(entry => entry.eventID == EventTriggerType.Select));
                Assert.IsTrue(trigger.triggers.Exists(entry => entry.eventID == EventTriggerType.Deselect));

                playerIcon.GetComponent<Button>().onClick.Invoke();
                var tooltip = FindChild(panelObject.transform, "UnityCombatTrinketTooltip");
                Assert.IsNotNull(tooltip);
                Assert.IsTrue(ContainsText(tooltip, "Player Lesser"));
                Assert.IsTrue(ContainsText(tooltip, "Player lesser trinket effect."));
                Assert.IsTrue(ContainsText(tooltip, "Active"));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_RendersRewardReceiptAndTriggerChain()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var options = AcceptanceOptions(400, "\u80dc 45%", "\u6837\u672c 20");
                options.TimelineOpen = true;
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(CreateAcceptanceReplay(), 2, options);

                var rewardPanel = FindChild(panelObject.transform, "UnityCombatRewardReceiptPanel");
                var triggerPanel = FindChild(panelObject.transform, "UnityCombatTriggerChainPanel");
                Assert.IsNotNull(rewardPanel);
                Assert.IsNotNull(triggerPanel);
                Assert.IsFalse(rewardPanel.gameObject.activeSelf);
                Assert.IsFalse(triggerPanel.gameObject.activeSelf);

                FindChild(panelObject.transform, "UnityCombatRewardToggleButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsTrue(rewardPanel.gameObject.activeSelf);
                Assert.IsFalse(triggerPanel.gameObject.activeSelf);

                FindChild(panelObject.transform, "UnityCombatTriggerToggleButton").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(rewardPanel.gameObject.activeSelf);
                Assert.IsTrue(triggerPanel.gameObject.activeSelf);
                Assert.IsTrue(ContainsText(panelObject.transform, "奖励结算收据"));
                Assert.IsTrue(ContainsText(panelObject.transform, "亡灵攻击 +1 -> 已写入全局"));
                Assert.IsTrue(ContainsText(panelObject.transform, "Rylak -> Nerubian Deathswarmer -> 亡灵攻击提升 -> 亡灵攻击 +1"));
                Assert.AreEqual("跳过", FindChild(panelObject.transform, "UnityReplayLastButtonText").GetComponent<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_DrawerUsesCompactProgressiveHierarchy()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var options = AcceptanceOptions(400, "drawer stats", "drawer meta");
                options.TimelineOpen = true;
                options.MechanicEvents = MechanicEvents();
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(CreateAcceptanceReplay(), 1, options);

                Assert.LessOrEqual(
                    FindChild(panelObject.transform, "UnityCombatAnalysisSummary").GetComponent<LayoutElement>().preferredHeight,
                    92f);
                var filters = FindChild(panelObject.transform, "UnityCombatTimelineFilters");
                Assert.LessOrEqual(filters.GetComponent<LayoutElement>().preferredHeight, 50f);
                Assert.IsNotNull(filters.GetComponent<HorizontalLayoutGroup>());
                Assert.IsFalse(FindChild(panelObject.transform, "UnityCombatRewardReceiptPanel").gameObject.activeSelf);
                Assert.IsFalse(FindChild(panelObject.transform, "UnityCombatTriggerChainPanel").gameObject.activeSelf);
                Assert.Greater(
                    FindChild(panelObject.transform, "UnityCombatTimeline").GetComponent<LayoutElement>().flexibleHeight,
                    0f);

                var tools = FindChild(panelObject.transform, "UnityCombatAnalysisTools");
                Assert.IsNotNull(tools);
                Assert.IsTrue(IsDescendant(tools, FindChild(panelObject.transform, "UnityCombatMaxStepsLabel")));
                Assert.IsTrue(IsDescendant(tools, FindChild(panelObject.transform, "UnityCombatStatsButton")));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelinePauseBadge"));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_ComposesBattlefieldWithHeroAnchorsAndOverlayAnalysisDrawer()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var options = AcceptanceOptions(400, "胜 45%", "样本 20");
                options.ViewportWidth = 1920f;
                options.ViewportHeight = 1080f;
                options.PlayerHeroName = "我方英雄";
                options.PlayerHealth = 34;
                options.PlayerArmor = 5;
                options.OpponentHeroName = "敌方英雄";
                options.OpponentHealth = 28;
                options.OpponentArmor = 3;

                var component = panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>();
                component.Build(CreateAcceptanceReplay(), 1, options);

                var backdrop = FindChild(panelObject.transform, "UnityCombatBattlefieldBackdrop").GetComponent<Image>();
                if (backdrop.sprite == null)
                {
                    Assert.Fail("processed battlefield backdrop sprite is missing");
                }
                Assert.IsFalse(backdrop.raycastTarget);
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatBattlefield").GetComponent<Outline>());
                if (FindChild(panelObject.transform, "UnityCombatOpponentHeroAnchor") == null)
                {
                    Assert.Fail("opponent hero anchor is missing");
                }

                if (FindChild(panelObject.transform, "UnityCombatPlayerHeroAnchor") == null)
                {
                    Assert.Fail("player hero anchor is missing");
                }
                Assert.IsTrue(ContainsText(panelObject.transform, "敌方英雄"));
                Assert.IsTrue(ContainsText(panelObject.transform, "生命 28"));
                Assert.IsTrue(ContainsText(panelObject.transform, "护甲 3"));
                Assert.IsTrue(ContainsText(panelObject.transform, "我方英雄"));
                Assert.IsTrue(ContainsText(panelObject.transform, "生命 34"));
                Assert.IsTrue(ContainsText(panelObject.transform, "护甲 5"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatPlayerHeroPortrait").GetComponent<Image>().sprite);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatOpponentHeroPortrait").GetComponent<Image>().sprite);
                var emptySocket = FindChild(panelObject.transform, "UnityCombatSlot-Opponent-3");
                Assert.IsNotNull(emptySocket);
                Assert.Less(emptySocket.GetComponent<Image>().color.a, 0.25f);
                Assert.AreEqual(1, emptySocket.GetComponentsInChildren<Image>(true).Length);

                if (FindChild(panelObject.transform, "UnityCombatAnalysisPeek") == null)
                {
                    Assert.Fail("live analysis peek is missing");
                }
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatTimelineDrawer"));
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatRewardDiagnosticsPanel"));

                var closedBattlefield = FindChild(panelObject.transform, "UnityCombatBattlefield").GetComponent<RectTransform>();
                var closedAnchorMin = closedBattlefield.anchorMin;
                var closedAnchorMax = closedBattlefield.anchorMax;
                var closedOffsetMin = closedBattlefield.offsetMin;
                var closedOffsetMax = closedBattlefield.offsetMax;

                options.TimelineOpen = true;
                component.Build(CreateAcceptanceReplay(), 1, options);

                var openBattlefield = FindChild(panelObject.transform, "UnityCombatBattlefield").GetComponent<RectTransform>();
                Assert.AreEqual(closedAnchorMin, openBattlefield.anchorMin);
                Assert.AreEqual(closedAnchorMax, openBattlefield.anchorMax);
                Assert.AreEqual(closedOffsetMin, openBattlefield.offsetMin);
                Assert.AreEqual(closedOffsetMax, openBattlefield.offsetMax);
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatAnalysisPeek"));

                var drawer = FindChild(panelObject.transform, "UnityCombatTimelineDrawer");
                if (drawer == null)
                {
                    Assert.Fail("expanded analysis drawer is missing");
                }
                Assert.IsTrue(drawer.GetComponent<LayoutElement>().ignoreLayout);
                Assert.IsTrue(IsDescendant(drawer, FindChild(panelObject.transform, "UnityCombatRewardDiagnosticsPanel")));
                Assert.IsTrue(IsDescendant(drawer, FindChild(panelObject.transform, "UnityCombatStatsText")));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_MissingArtUsesReadableFallbackLabels()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = CreateAcceptanceReplay();
                replay.Frames[1].PlayerBoardSnapshot.Minions[1].CardId = "MISSING_COMBAT_ART";
                var options = AcceptanceOptions(400, "fallback", "fallback");
                options.PlayerHeroCardId = "MISSING_PLAYER_HERO";
                options.PlayerHeroImagePath = null;
                options.OpponentHeroCardId = "MISSING_OPPONENT_HERO";
                options.OpponentHeroImagePath = null;

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(replay, 1, options);

                var playerHeroFallback = FindChild(panelObject.transform, "UnityCombatPlayerHeroPortraitFallback").GetComponent<Text>();
                var opponentHeroFallback = FindChild(panelObject.transform, "UnityCombatOpponentHeroPortraitFallback").GetComponent<Text>();
                Assert.AreEqual("我方", playerHeroFallback.text);
                Assert.AreEqual("敌方", opponentHeroFallback.text);
                Assert.IsNotNull(playerHeroFallback.GetComponent<Outline>());
                Assert.IsNotNull(opponentHeroFallback.GetComponent<Outline>());

                var minionArt = FindChild(panelObject.transform, "UnityCombatCardArt-accept-player-2").GetComponent<Image>();
                var minionFallback = FindChild(panelObject.transform, "UnityCombatCardArtFallbackText-accept-player-2").GetComponent<Text>();
                Assert.IsNull(minionArt.sprite);
                Assert.AreEqual("RY", minionFallback.text);
                Assert.GreaterOrEqual(minionFallback.fontSize, 20);
                Assert.IsNotNull(minionFallback.GetComponent<Outline>());
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void ReplayTileAnimator_StrikeLungesAndReducedMotionKeepsTheTokenStable()
        {
            var tile = new GameObject("Tile", typeof(RectTransform), typeof(Image), typeof(UnityTavernReplayTileAnimator));
            var previousReduceMotion = UnityUiMotionSettings.ReduceMotion;
            try
            {
                var animator = tile.GetComponent<UnityTavernReplayTileAnimator>();
                animator.Configure(UnityTavernReplayTileMotion.Strike, Color.gray, 1f, new Vector2(18f, 72f));
                animator.ApplyPreview(0.5f);
                Assert.Greater(tile.GetComponent<RectTransform>().anchoredPosition.y, 40f);

                UnityUiMotionSettings.ReduceMotion = true;
                tile.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                animator.Configure(UnityTavernReplayTileMotion.Strike, Color.gray, 1f, new Vector2(18f, 72f));
                animator.ApplyPreview(1f);
                Assert.AreEqual(Vector2.zero, tile.GetComponent<RectTransform>().anchoredPosition);
            }
            finally
            {
                UnityUiMotionSettings.ReduceMotion = previousReduceMotion;
                Object.DestroyImmediate(tile);
            }
        }

        [Test]
        public void CombatReplayPanel_ShowsLockedVersionAndFilterableMechanicTimeline()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var selectedFilter = UnityReplayTimelineFilter.All;
                var options = AcceptanceOptions(400, "胜 45%", "样本 20");
                options.TimelineOpen = true;
                options.GameVersionId = "36.2-preview";
                options.ContentSnapshotId = "snapshot-ui5";
                options.TimelineFilter = UnityReplayTimelineFilter.All;
                options.SetTimelineFilter = filter => selectedFilter = filter;
                options.MechanicEvents = MechanicEvents();
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(CreateAcceptanceReplay(), 1, options);

                var versionBadge = FindChild(panelObject.transform, "UnityCombatReplayVersionBadge").GetComponent<Text>().text;
                StringAssert.Contains("只读", versionBadge);
                StringAssert.Contains("36.2-preview", versionBadge);
                StringAssert.Contains("snapshot-ui5", versionBadge);
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineFilter-All"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineFilter-Choice"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineFilter-DarkGift"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineFilter-RecruitAction"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineFilter-DelayedObject"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTimelineFilter-Combat"));
                Assert.IsTrue(ContainsText(panelObject.transform, "赛季按钮 → choice.completed → DarkGift"));
                Assert.IsTrue(ContainsText(panelObject.transform, "英雄技能 → dark-gift.resolved → +2/+2"));
                Assert.IsTrue(ContainsText(panelObject.transform, "诱饵猎手 → recruit-action.resolved → fishbait.reward"));
                Assert.IsTrue(ContainsText(panelObject.transform, "越狱行动 → delayed-object.opened → lockbox.reward"));

                FindChild(panelObject.transform, "UnityCombatTimelineFilter-RecruitAction").GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(UnityReplayTimelineFilter.RecruitAction, selectedFilter);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [TestCase(UnityReplayTimelineFilter.Choice, "choice.completed", "dark-gift.resolved")]
        [TestCase(UnityReplayTimelineFilter.DarkGift, "dark-gift.resolved", "recruit-action.resolved")]
        [TestCase(UnityReplayTimelineFilter.RecruitAction, "recruit-action.resolved", "delayed-object.opened")]
        [TestCase(UnityReplayTimelineFilter.DelayedObject, "delayed-object.opened", "choice.completed")]
        [TestCase(UnityReplayTimelineFilter.Combat, "攻击", "choice.completed")]
        public void CombatReplayPanel_TimelineFilterShowsOnlyRequestedCategory(
            UnityReplayTimelineFilter filter,
            string expected,
            string excluded)
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var options = AcceptanceOptions(400, "胜 45%", "样本 20");
                options.TimelineOpen = true;
                options.TimelineFilter = filter;
                options.SetTimelineFilter = _ => { };
                options.MechanicEvents = MechanicEvents();
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(CreateAcceptanceReplay(), 1, options);

                var timeline = FindChild(panelObject.transform, "UnityCombatTimeline");
                Assert.IsNotNull(timeline);
                Assert.IsTrue(ContainsText(timeline, expected), filter.ToString());
                Assert.IsFalse(ContainsText(timeline, excluded), filter.ToString());
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayPanel_CapturesFullscreenAcceptanceAtTargetResolutions()
        {
            var replay = CreateAcceptanceReplay();
            replay.Frames[1].PlayerBoardSnapshot.Minions[1].CardId = "BG26_801";
            replay.Frames[1].PlayerBoardSnapshot.Minions[2].CardId = "BG25_011";
            CaptureAndAssert(replay, 1920, 1080, "step5-combat-fullscreen-1920x1080.png");
            CaptureAndAssert(replay, 2560, 1080, "step5-combat-fullscreen-2560x1080.png");
            CaptureAndAssert(replay, 1366, 768, "step5-combat-fullscreen-1366x768.png");
            CaptureAndAssert(replay, 1280, 720, "step5-combat-fullscreen-1280x720.png");
            CaptureAndAssert(replay, 1000, 600, "step5-combat-fullscreen-1000x600.png");
            CaptureAndAssert(replay, 994, 384, "step5-combat-fullscreen-994x384.png");
            CaptureAndAssert(replay, 1920, 1080, "step5-combat-fullscreen-drawer-1920x1080.png", true);
        }

        private static void CaptureAndAssert(CombatReplay replay, int width, int height, string fileName, bool timelineOpen = false)
        {
            Directory.CreateDirectory(CaptureDirectory);
            var path = Path.Combine(CaptureDirectory, fileName);
            var nonBackgroundSamples = CaptureCombatPanel(replay, width, height, path, timelineOpen);

            Assert.IsTrue(File.Exists(path), path);
            Assert.Greater(new FileInfo(path).Length, 0, path);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Greater(nonBackgroundSamples, 20, path);
            }
        }

        private static int CaptureCombatPanel(CombatReplay replay, int width, int height, string path, bool timelineOpen)
        {
            var cameraObject = new GameObject("CombatCaptureCamera", typeof(Camera));
            var canvasObject = new GameObject("CombatCaptureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;
            try
            {
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;

                var canvas = canvasObject.GetComponent<Canvas>();
                LearnHearthstoneBootstrap.ConfigureCanvas(canvas, UnityTavernLayoutContext.ForSize(width, height));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;

                var panelObject = new GameObject("UnityCombatReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
                panelObject.transform.SetParent(canvasObject.transform, false);
                var options = AcceptanceOptions(400, "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%", "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400");
                options.ViewportWidth = width;
                options.ViewportHeight = height;
                options.TimelineOpen = timelineOpen;
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(replay, 1, options);
                FindChild(panelObject.transform, "UnityCombatTimelineDrawer")
                    ?.GetComponent<UnityCombatDrawerAnimator>()
                    ?.ApplyPreview(1f);

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasObject.GetComponent<RectTransform>());
                AssertPlaybackControlsHaveRoom(panelObject.transform, width, height);
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                return CountNonBackgroundSamples(texture);
            }
            finally
            {
                RenderTexture.active = previousActive;
                var camera = cameraObject.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.targetTexture = null;
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    Object.DestroyImmediate(renderTexture);
                }

                if (texture != null)
                {
                    Object.DestroyImmediate(texture);
                }

                Object.DestroyImmediate(canvasObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        private static int CountNonBackgroundSamples(Texture2D texture)
        {
            var count = 0;
            var pixels = texture.GetPixels32();
            var stride = Mathf.Max(1, pixels.Length / 512);
            for (var index = 0; index < pixels.Length; index += stride)
            {
                var pixel = pixels[index];
                if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8)
                {
                    count += 1;
                }
            }

            return count;
        }

        private static void AssertPlaybackControlsHaveRoom(Transform root, int width, int height)
        {
            var controls = FindChild(root, "UnityCombatReplayControls").GetComponent<RectTransform>();
            Assert.GreaterOrEqual(controls.rect.width, 430f, width + "x" + height);
        }

        private static UnityCombatReplayPanelOptions AcceptanceOptions(int maxSteps, string statsText, string statsMetaText)
        {
            return new UnityCombatReplayPanelOptions
            {
                ReplayPlaying = false,
                SpeedLabel = "1x",
                MaxSteps = maxSteps,
                StatsText = statsText,
                StatsMetaText = statsMetaText,
                ViewportWidth = 1920f,
                ViewportHeight = 1080f,
                PlayerHeroName = "我方英雄",
                PlayerHeroCardId = "TB_BaconShop_HERO_49",
                PlayerHeroImagePath = "HeroBuddyImages/heroes/TB_BaconShop_HERO_49",
                PlayerHealth = 34,
                PlayerArmor = 5,
                OpponentHeroName = "敌方英雄",
                OpponentHeroCardId = "TB_BaconShop_HERO_15",
                OpponentHeroImagePath = "HeroBuddyImages/heroes/TB_BaconShop_HERO_15",
                OpponentHealth = 28,
                OpponentArmor = 3,
                PlayerTrinkets = new List<UnityCombatTrinketDisplay>
                {
                    new UnityCombatTrinketDisplay
                    {
                        SlotKind = TrinketSlotKind.Lesser,
                        Name = "小饰品",
                        Description = "我方小饰品效果。",
                        CardId = "BG36_MagicItem_200",
                        ImagePath = "CardImages/Trinkets/Season14/BG36_MagicItem_200",
                        Status = "持续生效",
                        Active = true
                    },
                    new UnityCombatTrinketDisplay
                    {
                        SlotKind = TrinketSlotKind.Greater,
                        Name = "大饰品",
                        Description = "我方大饰品效果。",
                        CardId = "BG36_MagicItem_211",
                        ImagePath = "CardImages/Trinkets/Season14/BG36_MagicItem_211",
                        Status = "持续生效",
                        Active = true
                    }
                },
                OpponentTrinkets = new List<UnityCombatTrinketDisplay>
                {
                    new UnityCombatTrinketDisplay
                    {
                        SlotKind = TrinketSlotKind.Lesser,
                        Name = "敌方小饰品",
                        Description = "敌方小饰品效果。",
                        CardId = "BG36_MagicItem_212",
                        ImagePath = "CardImages/Trinkets/Season14/BG36_MagicItem_212",
                        Status = "持续生效",
                        Active = true
                    },
                    new UnityCombatTrinketDisplay
                    {
                        SlotKind = TrinketSlotKind.Greater,
                        Name = "敌方大饰品",
                        Description = "敌方大饰品效果。",
                        CardId = "BG36_MagicItem_213",
                        ImagePath = "CardImages/Trinkets/Season14/BG36_MagicItem_213",
                        Status = "第 9 回合生效",
                        Active = false
                    }
                },
                SetFrame = _ => { },
                TogglePlayback = () => { },
                CycleSpeed = () => { },
                ToggleTimeline = () => { },
                DecreaseMaxSteps = () => { },
                IncreaseMaxSteps = () => { },
                RunStatistics = () => { },
                Close = () => { }
            };
        }

        private static IReadOnlyList<MechanicEventRecord> MechanicEvents()
        {
            return new List<MechanicEventRecord>
            {
                MechanicEvent(1, "choice.completed", "赛季按钮", "DarkGift"),
                MechanicEvent(2, "dark-gift.resolved", "英雄技能", "+2/+2"),
                MechanicEvent(3, "recruit-action.resolved", "诱饵猎手", "fishbait.reward"),
                MechanicEvent(4, "delayed-object.opened", "越狱行动", "lockbox.reward")
            };
        }

        private static MechanicEventRecord MechanicEvent(int sequence, string type, string source, string result)
        {
            return new MechanicEventRecord
            {
                Sequence = sequence,
                Round = sequence + 2,
                Phase = MatchPhase.Tavern,
                Type = type,
                Source = source,
                Targets = new List<string> { "target-" + sequence },
                Result = result
            };
        }

        private static CombatReplay CreateAcceptanceReplay()
        {
            var replay = new CombatReplay
            {
                Seed = 4242,
                Result = CombatWinner.Player
            };

            replay.Frames.Add(new CombatFrame
            {
                Index = 0,
                EventType = CombatEventType.CombatStarted,
                PlayerBoardSnapshot = BoardSnapshot(
                    BoardSide.Player,
                    MinionSnapshot("accept-player-1", "Shield Captain", 0, 6, 7, "BG25_001", Keyword.DivineShield, Keyword.Taunt),
                    MinionSnapshot("accept-player-2", "Rylak", 1, 4, 9, "TEST_RYLAK", Keyword.Reborn),
                    MinionSnapshot("accept-player-3", "Nerubian Deathswarmer", 2, 3, 3, "TEST_NERUBIAN")),
                OpponentBoardSnapshot = BoardSnapshot(
                    BoardSide.Opponent,
                    MinionSnapshot("accept-opponent-1", "Venom Guard", 0, 5, 5, "BG21_005", Keyword.Venomous),
                    MinionSnapshot("accept-opponent-2", "Wind Rider", 1, 7, 4, "BG23_004", Keyword.Windfury),
                    MinionSnapshot("accept-opponent-3", "Death Caller", 2, 2, 8, "BG25_009", Keyword.Deathrattle)),
                LogText = "acceptance start"
            });

            replay.Frames.Add(new CombatFrame
            {
                Index = 1,
                EventType = CombatEventType.AttackDeclared,
                ActorSide = BoardSide.Player,
                ActorId = "accept-player-1",
                TargetSide = BoardSide.Opponent,
                TargetId = "accept-opponent-1",
                DamagedEntityIds = new List<string> { "accept-opponent-1" },
                TriggerSourceIds = new List<string> { "accept-player-2" },
                PlayerBoardSnapshot = BoardSnapshot(
                    BoardSide.Player,
                    MinionSnapshot("accept-player-1", "Shield Captain", 0, 6, 7, "BG25_001", Keyword.DivineShield, Keyword.Taunt),
                    MinionSnapshot("accept-player-2", "Rylak", 1, 4, 9, "TEST_RYLAK", Keyword.Reborn),
                    MinionSnapshot("accept-player-3", "Nerubian Deathswarmer", 2, 3, 3, "TEST_NERUBIAN")),
                OpponentBoardSnapshot = BoardSnapshot(
                    BoardSide.Opponent,
                    MinionSnapshot("accept-opponent-1", "Venom Guard", 0, 5, 1, "BG21_005", Keyword.Venomous),
                    MinionSnapshot("accept-opponent-2", "Wind Rider", 1, 7, 4, "BG23_004", Keyword.Windfury),
                    MinionSnapshot("accept-opponent-3", "Death Caller", 2, 2, 8, "BG25_009", Keyword.Deathrattle)),
                LogText = "player attack resolves with trigger context"
            });

            replay.Frames.Add(new CombatFrame
            {
                Index = 2,
                EventType = CombatEventType.CombatRewardQueued,
                ActorSide = BoardSide.Player,
                ActorId = "accept-player-2",
                TargetSide = BoardSide.Player,
                TargetId = "accept-player-3",
                RelatedEntityIds = new List<string> { "accept-player-2", "accept-player-3", "TEST_NERUBIAN" },
                TriggerSourceIds = new List<string> { "accept-player-2" },
                PlayerBoardSnapshot = BoardSnapshot(
                    BoardSide.Player,
                    MinionSnapshot("accept-player-1", "Shield Captain", 0, 6, 7, "BG25_001", Keyword.DivineShield, Keyword.Taunt),
                    MinionSnapshot("accept-player-2", "Rylak", 1, 4, 9, "TEST_RYLAK", Keyword.Reborn),
                    MinionSnapshot("accept-player-3", "Nerubian Deathswarmer", 2, 3, 3, "TEST_NERUBIAN")),
                OpponentBoardSnapshot = BoardSnapshot(
                    BoardSide.Opponent,
                    MinionSnapshot("accept-opponent-1", "Venom Guard", 0, 5, 1, "BG21_005", Keyword.Venomous),
                    MinionSnapshot("accept-opponent-2", "Wind Rider", 1, 7, 4, "BG23_004", Keyword.Windfury),
                    MinionSnapshot("accept-opponent-3", "Death Caller", 2, 2, 8, "BG25_009", Keyword.Deathrattle)),
                LogText = "Rylak triggered Nerubian Deathswarmer"
            });

            replay.Frames.Add(new CombatFrame
            {
                Index = 3,
                EventType = CombatEventType.CombatEnded,
                PlayerBoardSnapshot = BoardSnapshot(
                    BoardSide.Player,
                    MinionSnapshot("accept-player-1", "Shield Captain", 0, 6, 7, "BG25_001", Keyword.DivineShield, Keyword.Taunt),
                    MinionSnapshot("accept-player-2", "Rylak", 1, 4, 9, "TEST_RYLAK", Keyword.Reborn)),
                OpponentBoardSnapshot = BoardSnapshot(BoardSide.Opponent),
                DeadEntityIds = new List<string> { "accept-opponent-1", "accept-opponent-2", "accept-opponent-3" },
                LogText = "acceptance combat ended"
            });

            replay.PlayerRewards.Add(new CombatReward
            {
                Type = CombatRewardType.ImproveUndeadAttack,
                Side = BoardSide.Player,
                SourceCardId = "TEST_NERUBIAN",
                SourceInstanceId = "accept-player-3",
                Amount = 1
            });

            return replay;
        }

        private static CombatBoardSnapshot BoardSnapshot(BoardSide side, params CombatMinionSnapshot[] minions)
        {
            var snapshot = new CombatBoardSnapshot { Side = side };
            snapshot.Minions.AddRange(minions);
            return snapshot;
        }

        private static CombatMinionSnapshot MinionSnapshot(string instanceId, string name, int position, int attack, int health, params Keyword[] keywords)
        {
            return MinionSnapshot(instanceId, name, position, attack, health, instanceId + "-card", keywords);
        }

        private static CombatMinionSnapshot MinionSnapshot(string instanceId, string name, int position, int attack, int health, string cardId, params Keyword[] keywords)
        {
            return new CombatMinionSnapshot
            {
                InstanceId = instanceId,
                CardId = cardId,
                Name = name,
                Position = position,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 4,
                CanAttack = true,
                Keywords = new List<Keyword>(keywords),
                Tribes = new List<Tribe> { Tribe.Beast }
            };
        }

        private static bool ContainsText(Transform root, string value)
        {
            foreach (var label in root.GetComponentsInChildren<Text>(true))
            {
                if ((label.text ?? string.Empty).Contains(value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsDescendant(Transform ancestor, Transform candidate)
        {
            if (ancestor == null || candidate == null)
            {
                return false;
            }

            for (var current = candidate; current != null; current = current.parent)
            {
                if (current == ancestor)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = FindChild(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
