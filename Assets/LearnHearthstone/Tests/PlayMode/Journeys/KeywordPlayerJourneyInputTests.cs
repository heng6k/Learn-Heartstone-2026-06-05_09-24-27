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
    public sealed class KeywordPlayerJourneyInputTests
    {
        [UnityTest]
        public IEnumerator PlayMode_PJ05_RebornTooltipCombatReplayAndConsumptionCompleteThroughRaycast()
        {
            using (var scene = new KeywordJourneyScene())
            {
                var service = CreateService(5101);
                var attacker = Minion("pj05-reborn-attacker", "进攻随从", BoardSide.Player, 5, 20);
                var support = Minion("pj05-reborn-support", "支援随从", BoardSide.Player, 0, 20);
                var reborn = Minion("pj05-reborn-target", "复生目标", BoardSide.Opponent, 0, 1, Keyword.Reborn);
                service.State.Player.Board.Add(attacker);
                service.State.Player.Board.Add(support);
                service.State.Opponent.Board.Add(reborn);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return AssertOpponentKeywordTooltip(scene, reborn, Keyword.Reborn, "复生");
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");

                var frameIndex = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.RebornResolved);
                Assert.GreaterOrEqual(frameIndex, 0);
                var frame = service.State.LastReplay.Frames[frameIndex];
                Assert.AreEqual(reborn.InstanceId, frame.ActorId);
                Assert.AreNotEqual(reborn.InstanceId, frame.TargetId);
                var snapshot = frame.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == frame.TargetId);
                Assert.AreEqual(1, snapshot.Health);
                Assert.IsFalse(snapshot.Keywords.Contains(Keyword.Reborn));
                yield return AdvanceReplayToFrame(scene, frameIndex);
                StringAssert.Contains("复生", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ05_WindfuryTooltipTwoAttacksAndReplayCompleteThroughRaycast()
        {
            using (var scene = new KeywordJourneyScene())
            {
                var service = CreateService(5102);
                var windfury = Minion("pj05-windfury", "风怒随从", BoardSide.Player, 1, 30, Keyword.Windfury);
                service.State.Player.Board.Add(windfury);
                service.State.Player.Board.Add(Minion("pj05-windfury-support", "支援随从", BoardSide.Player, 0, 30));
                service.State.Opponent.Board.Add(Minion("pj05-windfury-wall", "训练假人", BoardSide.Opponent, 0, 100));

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return AssertKeywordTooltip(scene, windfury, Keyword.Windfury, "风怒");
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");

                Assert.GreaterOrEqual(
                    service.State.LastReplay.Frames.Count(frame => frame.EventType == CombatEventType.AttackDeclared && frame.ActorId == windfury.InstanceId),
                    2);
                var frameIndex = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.WindfuryResolved);
                Assert.GreaterOrEqual(frameIndex, 0);
                Assert.IsTrue(service.State.LastReplay.Frames[frameIndex].PlayerBoardSnapshot.Minions
                    .Single(minion => minion.InstanceId == windfury.InstanceId).Keywords.Contains(Keyword.Windfury));
                yield return AdvanceReplayToFrame(scene, frameIndex);
                StringAssert.Contains("风怒", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ05_StealthTooltipTargetRestrictionAndReplayCompleteThroughRaycast()
        {
            using (var scene = new KeywordJourneyScene())
            {
                var service = CreateService(5103);
                service.State.Player.Board.Add(Minion("pj05-stealth-attacker", "进攻随从", BoardSide.Player, 1, 30));
                service.State.Player.Board.Add(Minion("pj05-stealth-support-a", "支援随从甲", BoardSide.Player, 0, 30));
                service.State.Player.Board.Add(Minion("pj05-stealth-support-b", "支援随从乙", BoardSide.Player, 0, 30));
                var stealth = Minion("pj05-stealth", "潜行目标", BoardSide.Opponent, 1, 30, Keyword.Stealth);
                var visible = Minion("pj05-visible", "可见目标", BoardSide.Opponent, 0, 30);
                service.State.Opponent.Board.Add(stealth);
                service.State.Opponent.Board.Add(visible);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return AssertOpponentKeywordTooltip(scene, stealth, Keyword.Stealth, "潜行");
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");

                var frameIndex = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.AttackDeclared);
                Assert.GreaterOrEqual(frameIndex, 0);
                var attack = service.State.LastReplay.Frames[frameIndex];
                Assert.AreEqual(visible.InstanceId, attack.TargetId);
                Assert.AreNotEqual(stealth.InstanceId, attack.TargetId);
                Assert.IsTrue(attack.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == stealth.InstanceId).Keywords.Contains(Keyword.Stealth));
                yield return AdvanceReplayToFrame(scene, frameIndex);
                StringAssert.Contains("攻击", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ05_VenomousAndPoisonousTooltipKillConsumptionMatrixCompleteThroughRaycast()
        {
            yield return RunPoisonJourney(Keyword.Venomous, "烈毒", 5104, true);
            yield return RunPoisonJourney(Keyword.Poisonous, "剧毒", 5105, false);
        }

        [UnityTest]
        public IEnumerator PlayMode_PJ05_TauntTooltipTargetRestrictionAndReplayCompleteThroughRaycast()
        {
            using (var scene = new KeywordJourneyScene())
            {
                var service = CreateService(5106);
                service.State.Player.Board.Add(Minion("pj05-taunt-attacker", "进攻随从", BoardSide.Player, 1, 30));
                service.State.Player.Board.Add(Minion("pj05-taunt-support-a", "支援随从甲", BoardSide.Player, 0, 30));
                service.State.Player.Board.Add(Minion("pj05-taunt-support-b", "支援随从乙", BoardSide.Player, 0, 30));
                var visible = Minion("pj05-taunt-visible", "普通目标", BoardSide.Opponent, 0, 30);
                var taunt = Minion("pj05-taunt", "嘲讽目标", BoardSide.Opponent, 0, 30, Keyword.Taunt);
                service.State.Opponent.Board.Add(visible);
                service.State.Opponent.Board.Add(taunt);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return AssertOpponentKeywordTooltip(scene, taunt, Keyword.Taunt, "嘲讽");
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");

                var frameIndex = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == CombatEventType.AttackDeclared);
                Assert.GreaterOrEqual(frameIndex, 0);
                var attack = service.State.LastReplay.Frames[frameIndex];
                Assert.AreEqual(taunt.InstanceId, attack.TargetId);
                Assert.IsTrue(attack.OpponentBoardSnapshot.Minions.Single(minion => minion.InstanceId == taunt.InstanceId).Keywords.Contains(Keyword.Taunt));
                yield return AdvanceReplayToFrame(scene, frameIndex);
                StringAssert.Contains("攻击", FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        private static IEnumerator RunPoisonJourney(Keyword keyword, string chineseName, int seed, bool consumed)
        {
            using (var scene = new KeywordJourneyScene())
            {
                var service = CreateService(seed);
                var poison = Minion("pj05-poison-" + keyword, chineseName + "随从", BoardSide.Player, 1, 30, keyword);
                service.State.Player.Board.Add(poison);
                service.State.Player.Board.Add(Minion("pj05-poison-support-" + keyword, "支援随从", BoardSide.Player, 0, 30));
                var target = Minion("pj05-poison-target-" + keyword, "高生命目标", BoardSide.Opponent, 0, 100);
                service.State.Opponent.Board.Add(target);

                new UnityTavernTrainerView(scene.Root, service, new LocalAdvisorService(), () => { }).Build();
                yield return AssertKeywordTooltip(scene, poison, keyword, chineseName);
                Click(scene, FindChild(scene.Root, "UnityQuickNextTurnButton"));
                yield return WaitForChild(scene.Root, "UnityCombatCurrentEventText");

                Assert.IsFalse(service.State.LastResult.FinalOpponentBoard.Any(minion => minion.InstanceId == target.InstanceId));
                var eventType = consumed ? CombatEventType.VenomousResolved : CombatEventType.DeathQueued;
                var frameIndex = service.State.LastReplay.Frames.FindIndex(frame => frame.EventType == eventType);
                Assert.GreaterOrEqual(frameIndex, 0);
                var poisonSnapshot = service.State.LastReplay.Frames[frameIndex].PlayerBoardSnapshot.Minions
                    .FirstOrDefault(minion => minion.InstanceId == poison.InstanceId);
                Assert.IsNotNull(poisonSnapshot);
                Assert.AreEqual(!consumed, poisonSnapshot.Keywords.Contains(keyword));
                yield return AdvanceReplayToFrame(scene, frameIndex);
                StringAssert.Contains(
                    consumed ? "烈毒" : "阵亡",
                    FindChild(scene.Root, "UnityCombatCurrentEventText").GetComponent<Text>().text);
            }
        }

        private static MatchService CreateService(int seed)
        {
            var service = MatchService.CreateWithDefaultCatalog(seed, new InMemoryTestScenarioRepository());
            service.State.Player.Board.Clear();
            service.State.Opponent.Board.Clear();
            return service;
        }

        private static MinionInstance Minion(string instanceId, string name, BoardSide owner, int attack, int health, params Keyword[] keywords)
        {
            return new MinionInstance
            {
                CardKind = CardKind.Minion,
                InstanceId = instanceId,
                DefinitionId = instanceId,
                CardId = instanceId.ToUpperInvariant(),
                Name = name,
                Cost = 3,
                BaseAttack = attack,
                BaseHealth = health,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 1,
                Owner = owner,
                Keywords = keywords.ToList(),
                OfficialKeywords = keywords.ToList(),
                OriginPoolSource = PoolSource.Debug,
                PoolSource = PoolSource.Debug
            };
        }

        private static IEnumerator AssertKeywordTooltip(KeywordJourneyScene scene, MinionInstance card, Keyword keyword, string chineseName)
        {
            yield return WaitForChild(scene.Root, "UnityCard-" + card.InstanceId);
            Hover(scene, FindChild(scene.Root, "UnityCard-" + card.InstanceId));
            yield return WaitForChild(scene.Root, "UnityKeywordTooltipLine-" + keyword);
            StringAssert.Contains(chineseName, FindChild(scene.Root, "UnityKeywordTooltipLine-" + keyword).GetComponent<Text>().text);
        }

        private static IEnumerator AssertOpponentKeywordTooltip(KeywordJourneyScene scene, MinionInstance card, Keyword keyword, string chineseName)
        {
            yield return WaitForChild(scene.Root, "UnityOpponentEntryButton");
            Click(scene, FindChild(scene.Root, "UnityOpponentEntryButton"));
            yield return WaitForChild(scene.Root, "UnityOpponentPanelCloseButton");
            yield return ScrollUntilReachable(scene, "UnityOpponentPanelScroll", FindChild(scene.Root, "UnityCard-" + card.InstanceId));
            yield return AssertKeywordTooltip(scene, card, keyword, chineseName);
            Click(scene, FindChild(scene.Root, "UnityOpponentPanelCloseButton"));
            yield return WaitForMissing(scene.Root, "UnityOpponentPanel");
        }

        private static IEnumerator ScrollUntilReachable(KeywordJourneyScene scene, string scrollName, Transform target)
        {
            var scroll = FindChild(scene.Root, scrollName).GetComponent<ScrollRect>();
            for (var attempt = 0; attempt < 40; attempt += 1)
            {
                Canvas.ForceUpdateCanvases();
                var rect = target.GetComponent<RectTransform>();
                var pointer = new PointerEventData(scene.EventSystem)
                {
                    position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
                };
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

        private static IEnumerator AdvanceReplayToFrame(KeywordJourneyScene scene, int frameIndex)
        {
            for (var index = 0; index < frameIndex; index += 1)
            {
                yield return WaitForChild(scene.Root, "UnityReplayNextButton");
                Click(scene, FindChild(scene.Root, "UnityReplayNextButton"));
                yield return null;
            }
        }

        private static void Click(KeywordJourneyScene scene, Transform target)
        {
            DispatchPointer(scene, target, false);
        }

        private static void Hover(KeywordJourneyScene scene, Transform target)
        {
            DispatchPointer(scene, target, true);
        }

        private static void DispatchPointer(KeywordJourneyScene scene, Transform target, bool hover)
        {
            var rect = target.GetComponent<RectTransform>();
            var localPoint = hover
                ? new Vector3(rect.rect.center.x, Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, 0.72f), 0f)
                : new Vector3(rect.rect.center.x, rect.rect.center.y, 0f);
            var pointer = new PointerEventData(scene.EventSystem)
            {
                button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(localPoint))
            };
            var hits = new List<RaycastResult>();
            scene.Raycaster.Raycast(pointer, hits);
            var hit = hits.FirstOrDefault(result => result.gameObject == target.gameObject || result.gameObject.transform.IsChildOf(target));
            Assert.IsNotNull(
                hit.gameObject,
                target.name + " was not reachable through GraphicRaycaster. " +
                "rect=" + rect.rect + ", screen=" + pointer.position +
                ", hits=[" + string.Join(", ", hits.Select(result => result.gameObject.name)) + "]");
            if (hover)
            {
                ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
                return;
            }

            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(hit.gameObject, pointer, ExecuteEvents.pointerClickHandler);
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

        private sealed class KeywordJourneyScene : IDisposable
        {
            private readonly GameObject canvasObject;
            private readonly GameObject eventSystemObject;

            public KeywordJourneyScene()
            {
                canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                Root = new GameObject("Root", typeof(RectTransform)).transform;
                Root.SetParent(canvasObject.transform, false);
                var rootRect = Root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
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
