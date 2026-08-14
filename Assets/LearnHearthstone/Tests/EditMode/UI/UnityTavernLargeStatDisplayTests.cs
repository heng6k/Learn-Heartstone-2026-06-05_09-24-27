using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class UnityTavernLargeStatDisplayTests
    {
        [TestCase(9999, "9999")]
        [TestCase(10000, "1万")]
        [TestCase(123456, "12.3万")]
        [TestCase(199999, "19.9万")]
        [TestCase(100000000, "1亿")]
        [TestCase(int.MaxValue, "21.4亿")]
        public void NumberFormatter_CompactsStatsWithoutRoundingPastTheRealValue(int value, string expected)
        {
            Assert.AreEqual(expected, TavernNumberFormatter.CompactStat(value));
        }

        [Test]
        public void NumberFormatter_FullNumbersUseThousandsSeparators()
        {
            Assert.AreEqual("2,147,483,647", TavernNumberFormatter.FullNumber(int.MaxValue));
            Assert.AreEqual("2,147,483,647 / 456,789", TavernNumberFormatter.FullStats(int.MaxValue, 456789));
        }

        [Test]
        public void CardComponent_LargePersistentStatsUseCompactBestFitBadges()
        {
            var cardObject = new GameObject("LargeStatCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                cardObject.GetComponent<UnityTavernCardComponent>().Bind(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "large-stat-card",
                        CardId = "MISSING_LARGE_STAT_CARD",
                        ImagePath = "CardImages/does-not-exist",
                        Name = "Large Stat Card",
                        Attack = int.MaxValue,
                        Health = 456789,
                        MaxHealth = 456789,
                        TavernTier = 6,
                        Tribes = new List<Tribe> { Tribe.Elemental }
                    },
                    UnityTavernCardMode.Shop,
                    "Buy",
                    null,
                    null);

                var attack = FindChild(cardObject.transform, "UnityAttackBadgeText").GetComponent<Text>();
                var health = FindChild(cardObject.transform, "UnityHealthBadgeText").GetComponent<Text>();

                Assert.AreEqual("21.4亿", attack.text);
                Assert.AreEqual("45.6万", health.text);
                Assert.IsTrue(attack.resizeTextForBestFit);
                Assert.IsTrue(health.resizeTextForBestFit);
                Assert.AreEqual(8, attack.resizeTextMinSize);
            }
            finally
            {
                Object.DestroyImmediate(cardObject);
            }
        }

        [Test]
        public void CombatReplayTilesCompactLargePersistentStats()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = new CombatReplay
                {
                    Seed = 9,
                    Result = CombatWinner.Player
                };
                replay.Frames.Add(new CombatFrame
                {
                    Index = 0,
                    EventType = CombatEventType.CombatStarted,
                    PlayerBoardSnapshot = BoardSnapshot(
                        BoardSide.Player,
                        MinionSnapshot("large-player", "Large Player", 0, int.MaxValue, 456789)),
                    OpponentBoardSnapshot = BoardSnapshot(
                        BoardSide.Opponent,
                        MinionSnapshot("large-opponent", "Large Opponent", 0, 123456, int.MaxValue)),
                    LogText = "large stat start"
                });

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    replay,
                    0,
                    false,
                    "1x",
                    _ => { },
                    () => { },
                    () => { },
                    () => { });

                var playerTile = FindChild(panelObject.transform, "UnityReplayMinion-large-player");
                var attack = FindChild(playerTile, "UnityCombatCardAttackText-large-player").GetComponent<Text>();
                var health = FindChild(playerTile, "UnityCombatCardHealthText-large-player").GetComponent<Text>();

                Assert.AreEqual("21.4亿", attack.text);
                Assert.AreEqual("45.6万", health.text);
                Assert.IsTrue(attack.resizeTextForBestFit);
                Assert.IsTrue(health.resizeTextForBestFit);
                Assert.AreEqual(8, attack.resizeTextMinSize);
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatReplayDamageFeedbackShowsFullGroupedDamageNumbers()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                var replay = new CombatReplay
                {
                    Seed = 10,
                    Result = CombatWinner.Player
                };
                replay.Frames.Add(new CombatFrame
                {
                    Index = 0,
                    EventType = CombatEventType.DamageResolved,
                    ActorSide = BoardSide.Player,
                    ActorId = "large-player",
                    TargetSide = BoardSide.Opponent,
                    TargetId = "large-opponent",
                    DamagedEntityIds = new List<string> { "large-opponent", "large-player" },
                    TargetDamageAmount = int.MaxValue,
                    ActorDamageAmount = 456789,
                    PlayerBoardSnapshot = BoardSnapshot(
                        BoardSide.Player,
                        MinionSnapshot("large-player", "Large Player", 0, int.MaxValue, 456789)),
                    OpponentBoardSnapshot = BoardSnapshot(
                        BoardSide.Opponent,
                        MinionSnapshot("large-opponent", "Large Opponent", 0, 456789, int.MaxValue)),
                    LogText = "large attack"
                });

                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    replay,
                    0,
                    false,
                    "1x",
                    _ => { },
                    () => { },
                    () => { },
                    () => { });

                var damageChip = FindChild(panelObject.transform, "UnityReplayEventChipText-DamageAmount").GetComponent<Text>();

                Assert.AreEqual("伤害 2,147,483,647 / 456,789", damageChip.text);
                Assert.IsTrue(damageChip.resizeTextForBestFit);
                Assert.AreEqual(HorizontalWrapMode.Wrap, damageChip.horizontalOverflow);
                Assert.AreEqual(VerticalWrapMode.Truncate, damageChip.verticalOverflow);
                Assert.IsNull(FindChild(panelObject.transform, "UnityReplayEventChipText-Damage"));
                Assert.IsTrue(
                    panelObject.GetComponentsInChildren<Text>(true)
                        .Select(label => label.text)
                        .Any(text => text.Contains("伤害 2,147,483,647 / 456,789")));
            }
            finally
            {
                Object.DestroyImmediate(panelObject);
            }
        }

        [Test]
        public void CombatEngineDamageResolvedFramesExposeFullDamageAmounts()
        {
            var player = new MinionInstance
            {
                InstanceId = "p-large",
                CardId = "p-large",
                Name = "Large Player",
                CardKind = CardKind.Minion,
                Attack = int.MaxValue,
                Health = int.MaxValue,
                MaxHealth = int.MaxValue,
                TavernTier = 6
            };
            var opponent = new MinionInstance
            {
                InstanceId = "o-large",
                CardId = "o-large",
                Name = "Large Opponent",
                CardKind = CardKind.Minion,
                Attack = 456789,
                Health = int.MaxValue,
                MaxHealth = int.MaxValue,
                TavernTier = 6
            };
            var nonAttacker = new MinionInstance
            {
                InstanceId = "p-large-non-attacker",
                CardId = "p-large-non-attacker",
                Name = "Large Player Non-Attacker",
                CardKind = CardKind.Minion,
                Attack = 0,
                Health = 1,
                MaxHealth = 1,
                TavernTier = 1,
                CanAttack = false
            };

            var result = LearnHearthstone.Domain.Engine.CombatEngine.SimulateBasicCombat(
                new[] { player, nonAttacker },
                new[] { opponent },
                19,
                1);
            var damage = result.Replay.Frames.First(frame => frame.EventType == CombatEventType.DamageResolved);

            Assert.AreEqual(int.MaxValue, damage.TargetDamageAmount);
            Assert.AreEqual(456789, damage.ActorDamageAmount);
        }

        [Test]
        public void CardDetailKeepsLargeStatsAsFullExactNumbers()
        {
            var detailObject = new GameObject("CardDetail", typeof(RectTransform), typeof(Image), typeof(UnityTavernCardDetailModalComponent));
            try
            {
                detailObject.GetComponent<UnityTavernCardDetailModalComponent>().Build(
                    new MinionInstance
                    {
                        CardKind = CardKind.Minion,
                        InstanceId = "large-detail-card",
                        CardId = "MISSING_DETAIL_LARGE_STAT_CARD",
                        ImagePath = "CardImages/does-not-exist",
                        Name = "Large Detail Card",
                        Attack = int.MaxValue,
                        Health = 456789,
                        MaxHealth = 789012,
                        TavernTier = 6,
                        Tribes = new List<Tribe> { Tribe.Dragon }
                    },
                    null);

                var texts = detailObject.GetComponentsInChildren<Text>(true).Select(label => label.text).ToList();

                Assert.IsTrue(texts.Any(text => text.Contains("2,147,483,647 / 456,789")), string.Join("\n", texts));
                Assert.IsTrue(texts.Any(text => text.Contains("789,012")), string.Join("\n", texts));
                Assert.IsTrue(texts.Any(text => text == "21.4亿"), string.Join("\n", texts));
            }
            finally
            {
                Object.DestroyImmediate(detailObject);
            }
        }

        private static CombatBoardSnapshot BoardSnapshot(BoardSide side, params CombatMinionSnapshot[] minions)
        {
            return new CombatBoardSnapshot
            {
                Side = side,
                Minions = minions.ToList()
            };
        }

        private static CombatMinionSnapshot MinionSnapshot(string id, string name, int position, int attack, int health)
        {
            return new CombatMinionSnapshot
            {
                InstanceId = id,
                CardId = id,
                Name = name,
                Position = position,
                Attack = attack,
                Health = health,
                MaxHealth = health,
                TavernTier = 6
            };
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

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
