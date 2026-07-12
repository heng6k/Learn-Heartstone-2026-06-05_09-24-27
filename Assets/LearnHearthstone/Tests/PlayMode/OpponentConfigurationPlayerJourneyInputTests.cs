using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LearnHearthstone.Tests.PlayMode
{
    public sealed class OpponentConfigurationPlayerJourneyInputTests
    {
        private const string AlAkirHeroPowerCardId = "TB_BaconShop_HP_086";
        private const string EvilTwinRewardId = "BG24_Reward_111";
        private const string LesserValorousMedallionCardId = "BG30_MagicItem_970";
        private const string GreaterValorousMedallionCardId = "BG30_MagicItem_970t";

        [UnityTest]
        public IEnumerator PlayMode_PJ06_OpponentHeroPowerQuestAndTrinketsConfigureAndAffectCombatThroughRaycast()
        {
            using (var scene = new JourneyScene())
            {
                var service = MatchService.CreateWithDefaultCatalog(
                    6201,
                    new InMemoryTestScenarioRepository(),
                    new MatchSetupOptions
                    {
                        ActiveTribes = new List<Tribe>
                        {
                            Tribe.Dragon,
                            Tribe.Beast,
                            Tribe.Mech,
                            Tribe.Murloc,
                            Tribe.Elemental
                        }
                    });
                service.State.Round = 9;
                service.State.Player.Board.Clear();
                service.State.Opponent.Board.Clear();
                service.State.Player.Board.Add(Minion("pj06-player-control", "玩家控制随从", BoardSide.Player, 0, 100));
                service.State.Opponent.Board.Add(Minion("pj06-opponent-stacked", "对手叠加随从", BoardSide.Opponent, 2, 3));

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return WaitForChild(scene.Root, "UnityOpponentEntryButton");
                Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
                yield return WaitForChild(scene.Root, "UnityOpponentHeroPowerSelectButton");

                Click(scene, FindChild(scene.Root, "UnityOpponentHeroPowerSelectButton"));
                yield return SelectLibraryCard(scene, AlAkirHeroPowerCardId);
                Assert.AreEqual(AlAkirHeroPowerCardId, service.State.Opponent.HeroPowerCardId);
                AssertChinese(FindChild(scene.Root, "UnityOpponentHeroPowerName").GetComponent<Text>().text);
                AssertChinese(FindChild(scene.Root, "UnityOpponentHeroPowerText").GetComponent<Text>().text);

                Click(scene, FindChild(scene.Root, "UnityOpponentQuestRewardSelectButton"));
                yield return SelectLibraryCard(scene, EvilTwinRewardId);
                Assert.AreEqual(EvilTwinRewardId, service.State.Opponent.AdvancedMechanics.Quests.MainQuest.RewardCardId);
                AssertChinese(FindChild(scene.Root, "UnityOpponentQuestRewardName").GetComponent<Text>().text);
                AssertChinese(FindChild(scene.Root, "UnityOpponentQuestRewardText").GetComponent<Text>().text);

                Click(scene, FindChild(scene.Root, "UnityOpponentTrinketSelectButton-Lesser"));
                yield return SelectLibraryCard(scene, LesserValorousMedallionCardId);
                Assert.AreEqual(LesserValorousMedallionCardId, service.State.Opponent.AdvancedMechanics.Trinkets.LesserTrinketId);
                AssertChinese(FindChild(scene.Root, "UnityOpponentTrinketName-Lesser").GetComponent<Text>().text);

                Click(scene, FindChild(scene.Root, "UnityOpponentTrinketSelectButton-Greater"));
                yield return SelectLibraryCard(scene, GreaterValorousMedallionCardId);
                Assert.AreEqual(GreaterValorousMedallionCardId, service.State.Opponent.AdvancedMechanics.Trinkets.GreaterTrinketId);
                AssertChinese(FindChild(scene.Root, "UnityOpponentTrinketName-Greater").GetComponent<Text>().text);

                var preview = service.GetOpponentCombatTavernStatePreview();
                Assert.AreEqual(EvilTwinRewardId, preview.AdvancedMechanics.Quests.MainQuest.RewardCardId);
                Assert.AreEqual(LesserValorousMedallionCardId, preview.AdvancedMechanics.Trinkets.LesserTrinketId);
                Assert.AreEqual(GreaterValorousMedallionCardId, preview.AdvancedMechanics.Trinkets.GreaterTrinketId);

                Click(scene, FindChild(scene.Root, "UnityOpponentPanelCloseButton"));
                yield return WaitForMissing(scene.Root, "UnityOpponentPanel");
                Assert.IsNotNull(FindChild(scene.Root, "UnityPlayerBoardZone"));
                Assert.IsNotNull(FindChild(scene.Root, "UnityQuickNextTurnButton"));

                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");

                var opponentInitial = service.State.LastReplay.InitialSnapshot.Opponent.Minions
                    .Single(card => card.InstanceId == "pj06-opponent-stacked");
                Assert.IsTrue(opponentInitial.Keywords.Contains(Keyword.Windfury));
                Assert.IsTrue(opponentInitial.Keywords.Contains(Keyword.DivineShield));
                Assert.IsTrue(opponentInitial.Keywords.Contains(Keyword.Taunt));
                Assert.AreEqual(2, service.State.LastReplay.InitialSnapshot.Opponent.Minions.Count);
                Assert.AreEqual(2, service.State.LastReplay.InitialSnapshot.Opponent.Minions.Count(card => card.CardId == opponentInitial.CardId));
                var opponentFinal = service.State.LastResult.FinalOpponentBoard
                    .Single(card => card.InstanceId == "pj06-opponent-stacked");
                Assert.AreEqual(10, opponentFinal.Attack);
                Assert.AreEqual(11, opponentFinal.MaxHealth);
                var playerInitial = service.State.LastReplay.InitialSnapshot.Player.Minions
                    .Single(card => card.InstanceId == "pj06-player-control");
                Assert.AreEqual(0, playerInitial.Attack);
                Assert.AreEqual(100, playerInitial.MaxHealth);

                var windfuryFrame = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.WindfuryResolved);
                Assert.GreaterOrEqual(windfuryFrame, 0);
                yield return AdvanceReplayToFrame(scene, windfuryFrame);
                StringAssert.Contains("风怒", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        private static IEnumerator SelectLibraryCard(JourneyScene scene, string cardId)
        {
            yield return WaitForChild(scene.Root, "UnityOpponentMechanicLibraryScroll");
            var card = FindChildEndingWith(scene.Root, "-" + cardId);
            Assert.IsNotNull(card, "Opponent mechanic library did not contain " + cardId + ".");
            var button = FindChildStartingWith(card, "UnityOpponentMechanicLibrarySelectButton");
            Assert.IsNotNull(button, "Opponent mechanic library card had no selection button for " + cardId + ".");
            var scroll = FindChild(scene.Root, "UnityOpponentMechanicLibraryScroll").GetComponent<ScrollRect>();

            for (var attempt = 0; attempt < 40; attempt += 1)
            {
                Canvas.ForceUpdateCanvases();
                if (TryClick(scene, button))
                {
                    yield return WaitForMissing(scene.Root, "UnityOpponentMechanicLibraryOverlay");
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

            Assert.Fail("Could not scroll the opponent mechanic library selection button into view for " + cardId + ".");
        }

        private static IEnumerator AdvanceReplayToFrame(JourneyScene scene, int frameIndex)
        {
            for (var index = 0; index < frameIndex; index += 1)
            {
                yield return WaitForChild(scene.Root, "UnityReplayNextButton");
                Click(scene, FindChild(scene.Root, "UnityReplayNextButton"));
                yield return null;
            }
        }

        private static MinionInstance Minion(string instanceId, string name, BoardSide owner, int attack, int health)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId.ToUpperInvariant(),
                Name = name,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Owner = owner,
                OriginPoolSource = PoolSource.Debug,
                PoolSource = PoolSource.Debug
            };
        }

        private static void AssertChinese(string value)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(value));
            Assert.IsTrue(value.Any(character => character >= '\u4e00' && character <= '\u9fff'), "Expected Chinese player text but got: " + value);
        }

        private static void Click(JourneyScene scene, Transform target)
        {
            Assert.IsTrue(TryClick(scene, target), target.name + " was not reachable through GraphicRaycaster.");
        }

        private static bool TryClick(JourneyScene scene, Transform target)
        {
            var rect = target.GetComponent<RectTransform>();
            var pointer = new PointerEventData(scene.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            scene.Raycaster.Raycast(pointer, hits);
            var hit = hits.FirstOrDefault(result => result.gameObject == target.gameObject || result.gameObject.transform.IsChildOf(target));
            if (hit.gameObject == null) return false;
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            return true;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            var child = FindChildOrNull(parent, name);
            Assert.IsNotNull(child, "Missing child: " + name);
            return child;
        }

        private static Transform FindChildOrNull(Transform parent, string name)
        {
            if (parent == null) return null;
            if (parent.name == name) return parent;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = FindChildOrNull(parent.GetChild(index), name);
                if (child != null) return child;
            }
            return null;
        }

        private static Transform FindChildEndingWith(Transform parent, string suffix)
        {
            if (parent == null) return null;
            if (parent.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return parent;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = FindChildEndingWith(parent.GetChild(index), suffix);
                if (child != null) return child;
            }
            return null;
        }

        private static Transform FindChildStartingWith(Transform parent, string prefix)
        {
            if (parent == null) return null;
            if (parent.name.StartsWith(prefix, StringComparison.Ordinal)) return parent;
            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = FindChildStartingWith(parent.GetChild(index), prefix);
                if (child != null) return child;
            }
            return null;
        }

        private static IEnumerator WaitForChild(Transform root, string name)
        {
            Transform stable = null;
            var stableFrames = 0;
            for (var frame = 0; frame < 90; frame += 1)
            {
                Canvas.ForceUpdateCanvases();
                var child = FindChildOrNull(root, name);
                if (child != null && child == stable)
                {
                    stableFrames += 1;
                    if (stableFrames >= 2) yield break;
                }
                else
                {
                    stable = child;
                    stableFrames = child == null ? 0 : 1;
                }
                yield return null;
            }
            Assert.Fail("Timed out waiting for child: " + name);
        }

        private static IEnumerator WaitForMissing(Transform root, string name)
        {
            for (var frame = 0; frame < 90; frame += 1)
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                if (FindChildOrNull(root, name) == null) yield break;
            }
            Assert.Fail("Timed out waiting for child to disappear: " + name);
        }

        private sealed class JourneyScene : IDisposable
        {
            private readonly GameObject canvasObject;
            private readonly GameObject eventSystemObject;

            public JourneyScene()
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                Root = new GameObject("Root", typeof(RectTransform)).transform;
                Root.SetParent(canvasObject.transform, false);
                var rect = Root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            public Transform Root { get; }
            public EventSystem EventSystem => eventSystemObject.GetComponent<EventSystem>();
            public GraphicRaycaster Raycaster => canvasObject.GetComponent<GraphicRaycaster>();

            public void Dispose()
            {
                UnityEngine.Object.Destroy(canvasObject);
                UnityEngine.Object.Destroy(eventSystemObject);
            }
        }
    }
}
