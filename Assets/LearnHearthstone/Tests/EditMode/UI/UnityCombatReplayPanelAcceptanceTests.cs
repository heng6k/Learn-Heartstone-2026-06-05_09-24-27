using System.Collections.Generic;
using System.IO;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
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
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    CreateAcceptanceReplay(),
                    1,
                    AcceptanceOptions(400, "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%", "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400"));

                Assert.AreEqual(
                    "\u6700\u5927\u8f6e\u6b21 400",
                    FindChild(panelObject.transform, "UnityCombatMaxStepsLabel").GetComponent<Text>().text);
                Assert.AreEqual(
                    "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%",
                    FindChild(panelObject.transform, "UnityCombatStatsText").GetComponent<Text>().text);
                Assert.AreEqual(
                    "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400",
                    FindChild(panelObject.transform, "UnityCombatStatsMetaText").GetComponent<Text>().text);
                Assert.IsNull(FindChild(panelObject.transform, "UnityCombatTimelineDrawer"));
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
                Assert.AreSame(playerArtViewport, playerArt.transform.parent);
                Assert.AreEqual(new Vector2(0f, -1f), playerArt.rectTransform.anchorMin);
                Assert.AreEqual(new Vector2(1f, 1f), playerArt.rectTransform.anchorMax);
                Assert.AreEqual("4本 野兽", FindChild(playerTile, "UnityCombatCardHeader-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("复活的骑兵", FindChild(playerTile, "UnityCombatCardName-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("圣盾 嘲讽", FindChild(playerTile, "UnityCombatCardKeywords-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("6", FindChild(playerTile, "UnityCombatCardAttackText-accept-player-1").GetComponent<Text>().text);
                Assert.AreEqual("7", FindChild(playerTile, "UnityCombatCardHealthText-accept-player-1").GetComponent<Text>().text);

                var opponentTile = FindChild(panelObject.transform, "UnityReplayMinion-accept-opponent-1");
                var opponentArt = FindChild(opponentTile, "UnityCombatCardArt-accept-opponent-1").GetComponent<Image>();
                Assert.IsNotNull(FindChild(opponentTile, "UnityCombatCardArtViewport-accept-opponent-1").GetComponent<RectMask2D>());
                Assert.AreEqual(new Vector2(0f, -1f), opponentArt.rectTransform.anchorMin);
                Assert.AreEqual(new Vector2(1f, 1f), opponentArt.rectTransform.anchorMax);
                Assert.AreEqual("烈毒", FindChild(opponentTile, "UnityCombatCardKeywords-accept-opponent-1").GetComponent<Text>().text);
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
        public void CombatReplayPanel_RendersRewardReceiptAndTriggerChain()
        {
            var panelObject = new GameObject("ReplayPanel", typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    CreateAcceptanceReplay(),
                    2,
                    AcceptanceOptions(400, "\u80dc 45%", "\u6837\u672c 20"));

                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatRewardReceiptPanel"));
                Assert.IsNotNull(FindChild(panelObject.transform, "UnityCombatTriggerChainPanel"));
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
        public void CombatReplayPanel_CapturesFullscreenAcceptanceAtTargetResolutions()
        {
            var replay = CreateAcceptanceReplay();
            CaptureAndAssert(replay, 1920, 1080, "step5-combat-fullscreen-1920x1080.png");
            CaptureAndAssert(replay, 1366, 768, "step5-combat-fullscreen-1366x768.png");
            CaptureAndAssert(replay, 1280, 720, "step5-combat-fullscreen-1280x720.png");
            CaptureAndAssert(replay, 1000, 600, "step5-combat-fullscreen-1000x600.png");
            CaptureAndAssert(replay, 994, 384, "step5-combat-fullscreen-994x384.png");
        }

        private static void CaptureAndAssert(CombatReplay replay, int width, int height, string fileName)
        {
            Directory.CreateDirectory(CaptureDirectory);
            var path = Path.Combine(CaptureDirectory, fileName);
            var nonBackgroundSamples = CaptureCombatPanel(replay, width, height, path);

            Assert.IsTrue(File.Exists(path), path);
            Assert.Greater(new FileInfo(path).Length, 0, path);
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Assert.Greater(nonBackgroundSamples, 20, path);
            }
        }

        private static int CaptureCombatPanel(CombatReplay replay, int width, int height, string path)
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
                panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>().Build(
                    replay,
                    1,
                    AcceptanceOptions(400, "\u80dc 45%  \u5e73 20%  \u8d1f 25%  \u8d85\u9650 10%", "\u6837\u672c 20 / \u6700\u5927\u8f6e\u6b21 400"));

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
