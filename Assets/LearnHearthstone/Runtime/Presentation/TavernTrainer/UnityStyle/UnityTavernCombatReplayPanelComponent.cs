using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Images;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityCombatReplayPanelOptions
    {
        public bool ReplayPlaying;
        public string SpeedLabel = "1x";
        public int MaxSteps = 200;
        public string StatsText;
        public string StatsMetaText;
        public bool TimelineOpen;
        public Action<int> SetFrame;
        public Action TogglePlayback;
        public Action CycleSpeed;
        public Action ToggleTimeline;
        public Action DecreaseMaxSteps;
        public Action IncreaseMaxSteps;
        public Action RunStatistics;
        public Action Close;
    }

    public sealed class UnityTavernCombatReplayPanelComponent : MonoBehaviour
    {
        public const string CombatReplayPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Replay/CombatReplayPanel.prefab";
        public const string CombatReplayPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Replay/CombatReplayPanel";
        private static Dictionary<string, string> localizedCombatMinionNamesByCardId;
        private static bool localizedCombatMinionNamesLoaded;
        private static Dictionary<string, string> localizedCombatSpellNamesByCardId;
        private static bool localizedCombatSpellNamesLoaded;

        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private Text frameText;
        [SerializeField] private Transform controlParent;
        [SerializeField] private Transform eventHighlightParent;
        [SerializeField] private Transform playerBoardParent;
        [SerializeField] private Transform opponentBoardParent;
        [SerializeField] private Transform timelineParent;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text closeButtonText;

        public static GameObject CreatePanelHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var panelObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernCombatReplayPanelComponent));

            panelObject.name = fallbackName;
            panelObject.transform.SetParent(parent, false);
            if (panelObject.GetComponent<Image>() == null)
            {
                panelObject.AddComponent<Image>();
            }

            if (panelObject.GetComponent<UnityTavernCombatReplayPanelComponent>() == null)
            {
                panelObject.AddComponent<UnityTavernCombatReplayPanelComponent>();
            }

            return panelObject;
        }

        public void ConfigureReferences(
            Text title = null,
            Text summary = null,
            Text frame = null,
            Transform controls = null,
            Transform eventHighlights = null,
            Transform playerBoard = null,
            Transform opponentBoard = null,
            Transform timeline = null,
            Button close = null,
            Text closeLabel = null)
        {
            titleText = title;
            summaryText = summary;
            frameText = frame;
            controlParent = controls;
            eventHighlightParent = eventHighlights;
            playerBoardParent = playerBoard;
            opponentBoardParent = opponentBoard;
            timelineParent = timeline;
            closeButton = close;
            closeButtonText = closeLabel;
        }

        public void Build(CombatReplay replay, int frameIndex, Action<int> setFrame, Action close)
        {
            Build(replay, frameIndex, false, "1x", setFrame, null, null, close);
        }

        public void Build(
            CombatReplay replay,
            int frameIndex,
            bool replayPlaying,
            string speedLabel,
            Action<int> setFrame,
            Action togglePlayback,
            Action cycleSpeed,
            Action close)
        {
            Build(
                replay,
                frameIndex,
                new UnityCombatReplayPanelOptions
                {
                    ReplayPlaying = replayPlaying,
                    SpeedLabel = speedLabel,
                    SetFrame = setFrame,
                    TogglePlayback = togglePlayback,
                    CycleSpeed = cycleSpeed,
                    Close = close
                });
        }

        public void Build(CombatReplay replay, int frameIndex, UnityCombatReplayPanelOptions options)
        {
            ConfigureOverlay(gameObject);
            BuildGenerated(replay, frameIndex, options ?? new UnityCombatReplayPanelOptions());
        }

        public static void ConfigureOverlay(GameObject target)
        {
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            image.color = new Color(0f, 0f, 0f, 0.68f);
            image.raycastTarget = true;
        }

        public static void ConfigurePanel(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void ConfigurePanelChrome(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.58f),
                new Vector2(1.5f, -1.5f));
            UnityTavernUiStyle.AddStarLanternRail(target.transform, "UnityCombatReplayStarLantern", UnityTavernUiStyle.ArcaneBlue);
        }

        public static void ConfigureHeader(Transform header)
        {
            if (header == null)
            {
                return;
            }

            UnityTavernUiStyle.ConfigureSurface(header.gameObject, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(
                header.gameObject,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.34f),
                new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(header.gameObject, 56f);

            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(header.gameObject);
            layout.padding = new RectOffset(8, 6, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var accent = header.Find("UnityCombatReplayHeaderAccent");
            if (accent == null)
            {
                var accentObject = new GameObject("UnityCombatReplayHeaderAccent", typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(header, false);
                accent = accentObject.transform;
            }

            accent.SetAsFirstSibling();
            UnityTavernUiStyle.SetFixedSize(accent.gameObject, 4f, 28f);
            UnityTavernUiStyle.ConfigureSurface(accent.gameObject, UnityTavernUiStyle.ArcaneBlue);
        }

        public static void ConfigureBoardRow(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(target);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        public static void ConfigureBoardsLayout(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.24f),
                new Vector2(1f, -1f));

            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        public static void ConfigureControlsLayout(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.SurfaceRaised);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.28f),
                new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(target, 56f);

            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(target);
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 8;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        public static void ConfigureBoardHost(GameObject target, BoardSide side)
        {
            var surface = side == BoardSide.Player
                ? new Color(UnityTavernUiStyle.Blue.r * 0.52f, UnityTavernUiStyle.Blue.g * 0.52f, UnityTavernUiStyle.Blue.b * 0.52f, 1f)
                : new Color(UnityTavernUiStyle.Red.r * 0.52f, UnityTavernUiStyle.Red.g * 0.52f, UnityTavernUiStyle.Red.b * 0.52f, 1f);
            var accent = side == BoardSide.Player ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Red;

            UnityTavernUiStyle.ConfigureSurface(target, surface);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(accent.r, accent.g, accent.b, 0.48f),
                new Vector2(1.2f, -1.2f));
            UnityTavernUiStyle.SetFlexible(target, 1f, 1f);

            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(8, 8, 7, 8);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var accentTransform = target.transform.Find("UnityReplayBoardAccent");
            if (accentTransform == null)
            {
                var accentObject = new GameObject("UnityReplayBoardAccent", typeof(RectTransform), typeof(Image));
                accentObject.transform.SetParent(target.transform, false);
                accentTransform = accentObject.transform;
            }

            accentTransform.SetAsFirstSibling();
            UnityTavernUiStyle.SetPreferredHeight(accentTransform.gameObject, 4f);
            UnityTavernUiStyle.ConfigureSurface(accentTransform.gameObject, accent);
        }

        public static void ConfigureTimelineLayout(GameObject target)
        {
            var scrollRoot = ResolveScrollRoot(target);
            if (scrollRoot != null)
            {
                UnityTavernUiStyle.ConfigureSurface(scrollRoot, UnityTavernUiStyle.SurfaceDark);
                UnityTavernUiStyle.ConfigureOutline(
                    scrollRoot,
                    UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.24f),
                    new Vector2(1f, -1f));
            }

            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void BuildPrefab(
            CombatReplay replay,
            int frameIndex,
            bool replayPlaying,
            string speedLabel,
            Action<int> setFrame,
            Action togglePlayback,
            Action cycleSpeed,
            Action close)
        {
            ConfigureChromeFromReferences();
            ConfigureClose(close);
            var hasFrames = replay != null && replay.Frames != null && replay.Frames.Count > 0;
            var clampedIndex = hasFrames ? Mathf.Clamp(frameIndex, 0, replay.Frames.Count - 1) : 0;
            var frame = hasFrames ? replay.Frames[clampedIndex] : null;
            var previousFrame = hasFrames && clampedIndex > 0 ? replay.Frames[clampedIndex - 1] : null;

            SetText(titleText, "战斗回放");
            SetText(summaryText, hasFrames ? "种子 " + replay.Seed + "  结果 " + ResultText(replay.Result) + "  帧 " + (clampedIndex + 1) + "/" + replay.Frames.Count : "暂无回放帧。");
            SetText(frameText, frame == null ? "运行战斗后可查看回放帧。" : (clampedIndex + 1) + ". " + EventTypeText(frame.EventType) + "  " + FrameLogText(frame));

            BuildControls(controlParent, replay, clampedIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed);
            BuildEventHighlights(eventHighlightParent, frame);
            BuildBoard(playerBoardParent, BoardSide.Player, "Player", frame == null ? null : frame.PlayerBoardSnapshot, previousFrame == null ? null : previousFrame.PlayerBoardSnapshot, frame);
            BuildBoard(opponentBoardParent, BoardSide.Opponent, "Opponent", frame == null ? null : frame.OpponentBoardSnapshot, previousFrame == null ? null : previousFrame.OpponentBoardSnapshot, frame);
            BuildReplayTargetingConnector(transform, frame);
            BuildTimeline(timelineParent, replay, clampedIndex, setFrame);
        }

        private void BuildGenerated(CombatReplay replay, int frameIndex, UnityCombatReplayPanelOptions options)
        {
            ClearChildren(transform);
            var hasFrames = replay != null && replay.Frames != null && replay.Frames.Count > 0;
            var clampedIndex = hasFrames ? Mathf.Clamp(frameIndex, 0, replay.Frames.Count - 1) : 0;
            var frame = hasFrames ? replay.Frames[clampedIndex] : null;
            var previousFrame = hasFrames && clampedIndex > 0 ? replay.Frames[clampedIndex - 1] : null;

            var overlay = new GameObject("UnityCombatReplayOverlay", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(transform, false);
            ConfigureReplayOverlay(overlay);

            var root = new GameObject("UnityCombatBattlefieldRoot", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(overlay.transform, false);
            ConfigureBattlefieldRoot(root);

            var backdrop = new GameObject("UnityCombatBattlefieldBackdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(root.transform, false);
            ConfigureBattlefieldBackdrop(backdrop);

            var safeArea = new GameObject("UnityCombatTitleSafeArea", typeof(RectTransform));
            safeArea.transform.SetParent(root.transform, false);
            ConfigureTitleSafeArea(safeArea);

            var safeLayout = safeArea.AddComponent<VerticalLayoutGroup>();
            safeLayout.spacing = 10;
            safeLayout.childControlWidth = true;
            safeLayout.childControlHeight = true;
            safeLayout.childForceExpandWidth = true;
            safeLayout.childForceExpandHeight = false;

            BuildHudHeader(safeArea.transform, replay, clampedIndex, options);

            var battlefield = new GameObject("UnityCombatBattlefield", typeof(RectTransform), typeof(Image));
            battlefield.transform.SetParent(safeArea.transform, false);
            ConfigureBattlefieldLayout(battlefield);

            BuildCombatSide(
                battlefield.transform,
                BoardSide.Opponent,
                "Opponent",
                frame == null ? null : frame.OpponentBoardSnapshot,
                previousFrame == null ? null : previousFrame.OpponentBoardSnapshot,
                frame);
            BuildCenterEventBand(battlefield.transform, frame, hasFrames, clampedIndex, replay);
            BuildCombatSide(
                battlefield.transform,
                BoardSide.Player,
                "Player",
                frame == null ? null : frame.PlayerBoardSnapshot,
                previousFrame == null ? null : previousFrame.PlayerBoardSnapshot,
                frame);
            BuildReplayTargetingConnector(root.transform, frame);

            BuildCombatRewardDiagnostics(safeArea.transform, replay);
            BuildPlaybackBar(safeArea.transform, replay, clampedIndex, options);
            if (options.TimelineOpen)
            {
                BuildTimelineDrawer(root.transform, replay, clampedIndex, options);
            }
        }

        private void BuildGenerated(
            CombatReplay replay,
            int frameIndex,
            bool replayPlaying,
            string speedLabel,
            Action<int> setFrame,
            Action togglePlayback,
            Action cycleSpeed,
            Action close)
        {
            ClearChildren(transform);

            var panel = new GameObject("UnityCombatReplayPanelSurface", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            ConfigurePanel(panel.GetComponent<RectTransform>());
            ConfigurePanelChrome(panel);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var header = new GameObject("UnityCombatReplayHeader", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            ConfigureHeader(header.transform);

            titleText = UiFactory.Label("UnityCombatReplayTitle", header.transform, "战斗回放", 20, FontStyle.Bold);
            titleText.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(titleText.gameObject, 1f, 0f);
            closeButton = CreateButton("UnityCombatReplayCloseButton", header.transform, "关闭", () => close?.Invoke(), 84f);
            closeButtonText = closeButton.GetComponentInChildren<Text>();

            summaryText = UiFactory.Label("UnityCombatReplaySummary", panel.transform, string.Empty, 14, FontStyle.Bold);
            summaryText.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(summaryText.gameObject, 26f);

            controlParent = new GameObject("UnityCombatReplayControls", typeof(RectTransform)).transform;
            controlParent.SetParent(panel.transform, false);
            ConfigureControlsLayout(controlParent.gameObject);

            frameText = UiFactory.Label("UnityCombatReplayFrameText", panel.transform, string.Empty, 14, FontStyle.Bold);
            frameText.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(frameText.gameObject, 34f);

            eventHighlightParent = new GameObject("UnityCombatReplayEventHighlights", typeof(RectTransform)).transform;
            eventHighlightParent.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(eventHighlightParent.gameObject, 34f);
            ConfigureEventHighlightsLayout(eventHighlightParent.gameObject);

            var boards = new GameObject("UnityCombatReplayBoards", typeof(RectTransform));
            boards.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(boards, 360f);
            UnityTavernUiStyle.SetFlexible(boards, 1f, 1f);
            ConfigureBoardsLayout(boards);

            opponentBoardParent = CreateBoardHost("UnityCombatReplayOpponentBoard", boards.transform, BoardSide.Opponent).transform;
            playerBoardParent = CreateBoardHost("UnityCombatReplayPlayerBoard", boards.transform, BoardSide.Player).transform;

            timelineParent = UiFactory.ScrollView("UnityCombatReplayTimeline", panel.transform, UnityTavernUiStyle.Panel, out _);
            ConfigureTimelineLayout(timelineParent.gameObject);

            BuildPrefab(replay, frameIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed, close);
        }

        private static void ConfigureReplayOverlay(GameObject target)
        {
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.TableDark, true);
            image.raycastTarget = true;
        }

        private static void ConfigureBattlefieldRoot(GameObject target)
        {
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.TableDark);
            image.raycastTarget = false;
        }

        private static void ConfigureBattlefieldBackdrop(GameObject target)
        {
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.ConfigureSurface(target, new Color(0.12f, 0.15f, 0.14f, 1f));
            image.raycastTarget = false;
        }

        private static void ConfigureTitleSafeArea(GameObject target)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.045f);
            rect.anchorMax = new Vector2(0.95f, 0.96f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void ConfigureHudHeader(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.92f));
            UnityTavernUiStyle.ConfigureOutline(target, new Color(0f, 0f, 0f, 0.36f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(target, 68f);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 10, 8, 8);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void ConfigureResultBadge(GameObject target, CombatReplay replay)
        {
            var color = replay != null && replay.SafetyStopped
                ? UnityTavernUiStyle.Red
                : replay != null && replay.Result == CombatWinner.Player
                    ? UnityTavernUiStyle.Blue
                    : replay != null && replay.Result == CombatWinner.Opponent
                        ? UnityTavernUiStyle.Red
                        : UnityTavernUiStyle.PanelRaised;
            UnityTavernUiStyle.ConfigureSurface(target, new Color(color.r, color.g, color.b, 0.72f));
            UnityTavernUiStyle.ConfigureOutline(target, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f), new Vector2(1.2f, -1.2f));
            UnityTavernUiStyle.SetFixedSize(target, 520f, 54f);
        }

        private static void ConfigureBattlefieldLayout(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, new Color(UnityTavernUiStyle.TableDark.r, UnityTavernUiStyle.TableDark.g, UnityTavernUiStyle.TableDark.b, 0.78f));
            UnityTavernUiStyle.ConfigureOutline(target, new Color(0f, 0f, 0f, 0.24f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetFlexible(target, 1f, 1f);

            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigureCombatSide(GameObject target, BoardSide side)
        {
            var baseColor = side == BoardSide.Player
                ? new Color(UnityTavernUiStyle.Blue.r * 0.45f, UnityTavernUiStyle.Blue.g * 0.45f, UnityTavernUiStyle.Blue.b * 0.45f, 0.88f)
                : new Color(UnityTavernUiStyle.Red.r * 0.45f, UnityTavernUiStyle.Red.g * 0.45f, UnityTavernUiStyle.Red.b * 0.45f, 0.88f);
            UnityTavernUiStyle.ConfigureSurface(target, baseColor);
            UnityTavernUiStyle.ConfigureOutline(target, new Color(0f, 0f, 0f, 0.26f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetFlexible(target, 1f, 1f);

            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 9);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigureCombatBoard(GameObject target, BoardSide side)
        {
            UnityTavernUiStyle.ConfigureSurface(target, new Color(0f, 0f, 0f, 0.18f));
            var accent = side == BoardSide.Player ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Red;
            UnityTavernUiStyle.ConfigureOutline(target, new Color(accent.r, accent.g, accent.b, 0.42f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetFlexible(target, 1f, 1f);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private static void ConfigureCombatSlot(GameObject target, BoardSide side, int slot, CombatFrame frame)
        {
            var image = target.GetComponent<Image>();
            image.color = IsAttackPointer(side, slot, frame)
                ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.26f)
                : new Color(0f, 0f, 0f, 0.16f);
            image.raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(target, IsAttackPointer(side, slot, frame) ? UnityTavernUiStyle.Gold : new Color(1f, 1f, 1f, 0.08f), new Vector2(1f, -1f));

            var element = UnityTavernUiStyle.EnsureComponent<LayoutElement>(target);
            element.flexibleWidth = 1f;
            element.flexibleHeight = 1f;
            element.minWidth = 72f;
            element.minHeight = 92f;
        }

        private static void ConfigureEventBand(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.86f));
            UnityTavernUiStyle.ConfigureOutline(target, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.26f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(target, 64f);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void ConfigurePlaybackBar(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.94f));
            UnityTavernUiStyle.ConfigureOutline(target, new Color(0f, 0f, 0f, 0.34f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetPreferredHeight(target, 66f);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 7, 7);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void ConfigureRewardDiagnostics(GameObject target)
        {
            UnityTavernUiStyle.SetPreferredHeight(target, 94f);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }

        private static void ConfigureRewardInfoPanel(GameObject target, Color accent)
        {
            UnityTavernUiStyle.ConfigureSurface(target, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.9f));
            UnityTavernUiStyle.ConfigureOutline(target, new Color(accent.r, accent.g, accent.b, 0.42f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetFlexible(target, 1f, 0f);

            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 6, 6);
            layout.spacing = 3;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigureInlineControlGroup(GameObject target, float width)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.PanelQuiet);
            UnityTavernUiStyle.ConfigureOutline(target, new Color(0f, 0f, 0f, 0.22f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetFixedSize(target, width, 50f);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(6, 6, 4, 4);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void ConfigureTimelineDrawer(GameObject target)
        {
            var rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.72f, 0.13f);
            rect.anchorMax = new Vector2(0.95f, 0.86f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UnityTavernUiStyle.ConfigureSurface(target, new Color(UnityTavernUiStyle.Panel.r, UnityTavernUiStyle.Panel.g, UnityTavernUiStyle.Panel.b, 0.96f), true);
            UnityTavernUiStyle.ConfigureOutline(target, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f), new Vector2(1.2f, -1.2f));

            var layout = target.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static void ConfigureTimelineDrawerHeader(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetPreferredHeight(target, UnityTavernUiStyle.TouchHeight);

            var layout = target.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 6, 5, 5);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private void BuildHudHeader(Transform parent, CombatReplay replay, int frameIndex, UnityCombatReplayPanelOptions options)
        {
            var header = new GameObject("UnityCombatHudHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(parent, false);
            ConfigureHudHeader(header);

            var opponentStatus = UiFactory.Label("UnityCombatOpponentHeroStatus", header.transform, SideStatusText(BoardSide.Opponent, replay, frameIndex), 15, FontStyle.Bold);
            opponentStatus.color = UnityTavernUiStyle.Text;
            opponentStatus.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(opponentStatus.gameObject, 1f, 0f);

            var badge = new GameObject("UnityCombatResultBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(header.transform, false);
            ConfigureResultBadge(badge, replay);
            var badgeLayout = badge.AddComponent<VerticalLayoutGroup>();
            badgeLayout.padding = new RectOffset(12, 12, 5, 5);
            badgeLayout.spacing = 1;
            badgeLayout.childControlWidth = true;
            badgeLayout.childControlHeight = true;
            badgeLayout.childForceExpandWidth = true;
            badgeLayout.childForceExpandHeight = false;

            titleText = UiFactory.Label("UnityCombatResultText", badge.transform, ResultText(replay), 20, FontStyle.Bold);
            titleText.color = UnityTavernUiStyle.Text;
            titleText.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetPreferredHeight(titleText.gameObject, 24f);

            summaryText = UiFactory.Label("UnityCombatStatsText", badge.transform, StatsText(options, replay), 15, FontStyle.Bold);
            summaryText.color = UnityTavernUiStyle.Gold;
            summaryText.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetPreferredHeight(summaryText.gameObject, 20f);

            var statsMeta = UiFactory.Label("UnityCombatStatsMetaText", badge.transform, StatsMetaText(options, replay), 14, FontStyle.Normal);
            statsMeta.color = UnityTavernUiStyle.MutedText;
            statsMeta.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetPreferredHeight(statsMeta.gameObject, 18f);

            var playerStatus = UiFactory.Label("UnityCombatPlayerHeroStatus", header.transform, SideStatusText(BoardSide.Player, replay, frameIndex), 15, FontStyle.Bold);
            playerStatus.color = UnityTavernUiStyle.Text;
            playerStatus.alignment = TextAnchor.MiddleRight;
            UnityTavernUiStyle.SetFlexible(playerStatus.gameObject, 1f, 0f);

            closeButton = CreateButton("UnityCombatCloseButton", header.transform, "\u8fd4\u56de", () => options.Close?.Invoke(), 78f, 52f, UnityTavernUiStyle.PanelRaised, false);
            closeButtonText = closeButton.GetComponentInChildren<Text>();
        }

        private void BuildCenterEventBand(Transform parent, CombatFrame frame, bool hasFrames, int frameIndex, CombatReplay replay)
        {
            var band = new GameObject("UnityCombatCenterEventBand", typeof(RectTransform), typeof(Image));
            band.transform.SetParent(parent, false);
            ConfigureEventBand(band);

            frameText = UiFactory.Label("UnityCombatCurrentEventText", band.transform, CurrentEventText(frame, hasFrames, frameIndex, replay), 16, FontStyle.Bold);
            frameText.color = UnityTavernUiStyle.Text;
            frameText.alignment = TextAnchor.MiddleLeft;
            frameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetFlexible(frameText.gameObject, 1f, 0f);

            eventHighlightParent = new GameObject("UnityCombatEventChips", typeof(RectTransform)).transform;
            eventHighlightParent.SetParent(band.transform, false);
            UnityTavernUiStyle.SetFixedSize(eventHighlightParent.gameObject, 560f, 42f);
            ConfigureEventHighlightsLayout(eventHighlightParent.gameObject);
            BuildEventHighlights(eventHighlightParent, frame);
        }

        private void BuildCombatRewardDiagnostics(Transform parent, CombatReplay replay)
        {
            var diagnostics = new GameObject("UnityCombatRewardDiagnosticsPanel", typeof(RectTransform));
            diagnostics.transform.SetParent(parent, false);
            ConfigureRewardDiagnostics(diagnostics);

            BuildRewardInfoPanel(
                diagnostics.transform,
                "UnityCombatRewardReceiptPanel",
                "奖励结算收据",
                RewardReceiptLines(replay),
                UnityTavernUiStyle.Gold);
            BuildRewardInfoPanel(
                diagnostics.transform,
                "UnityCombatTriggerChainPanel",
                "触发链",
                TriggerChainLines(replay),
                UnityTavernUiStyle.Blue);
        }

        private static void BuildRewardInfoPanel(Transform parent, string name, string title, IReadOnlyList<string> lines, Color accent)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            ConfigureRewardInfoPanel(panel, accent);

            var titleLabel = UiFactory.Label(name + "Title", panel.transform, title, 15, FontStyle.Bold);
            titleLabel.color = accent;
            titleLabel.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(titleLabel.gameObject, 18f);

            var visibleLines = lines == null || lines.Count == 0
                ? new List<string> { "无战斗奖励结算" }
                : lines.Take(3).ToList();
            for (var index = 0; index < visibleLines.Count; index += 1)
            {
                var line = UiFactory.Label(name + "Line" + index, panel.transform, visibleLines[index], 14, FontStyle.Normal);
                line.color = UnityTavernUiStyle.Text;
                line.alignment = TextAnchor.MiddleLeft;
                line.horizontalOverflow = HorizontalWrapMode.Wrap;
                line.verticalOverflow = VerticalWrapMode.Truncate;
                UnityTavernUiStyle.SetPreferredHeight(line.gameObject, 16f);
            }

            if (lines != null && lines.Count > visibleLines.Count)
            {
                var extra = UiFactory.Label(name + "More", panel.transform, "+ " + (lines.Count - visibleLines.Count) + " 条已结算", 14, FontStyle.Bold);
                extra.color = UnityTavernUiStyle.MutedText;
                extra.alignment = TextAnchor.MiddleLeft;
                UnityTavernUiStyle.SetPreferredHeight(extra.gameObject, 16f);
            }
        }

        private void BuildPlaybackBar(Transform parent, CombatReplay replay, int frameIndex, UnityCombatReplayPanelOptions options)
        {
            var bar = new GameObject("UnityCombatPlaybackBar", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(parent, false);
            ConfigurePlaybackBar(bar);

            var maxSteps = new GameObject("UnityCombatMaxStepsGroup", typeof(RectTransform), typeof(Image));
            maxSteps.transform.SetParent(bar.transform, false);
            ConfigureInlineControlGroup(maxSteps, 270f);
            CreateButton("UnityCombatMaxStepsDownButton", maxSteps.transform, "-", () => options.DecreaseMaxSteps?.Invoke(), 46f, 50f).interactable = options.DecreaseMaxSteps != null;
            var maxLabel = UiFactory.Label("UnityCombatMaxStepsLabel", maxSteps.transform, "\u6700\u5927\u8f6e\u6b21 " + Mathf.Max(1, options.MaxSteps), 15, FontStyle.Bold);
            maxLabel.color = UnityTavernUiStyle.Text;
            maxLabel.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetFlexible(maxLabel.gameObject, 1f, 0f);
            CreateButton("UnityCombatMaxStepsUpButton", maxSteps.transform, "+", () => options.IncreaseMaxSteps?.Invoke(), 46f, 50f).interactable = options.IncreaseMaxSteps != null;

            controlParent = new GameObject("UnityCombatReplayControls", typeof(RectTransform)).transform;
            controlParent.SetParent(bar.transform, false);
            UnityTavernUiStyle.SetFlexible(controlParent.gameObject, 1f, 0f);
            BuildControls(controlParent, replay, frameIndex, options.ReplayPlaying, options.SpeedLabel, options.SetFrame, options.TogglePlayback, options.CycleSpeed, 50f);

            CreateButton("UnityCombatStatsButton", bar.transform, "\u7edf\u8ba1100\u573a", () => options.RunStatistics?.Invoke(), 104f, 50f, UnityTavernUiStyle.TableLit, false).interactable = options.RunStatistics != null;
            CreateButton("UnityCombatTimelineToggleButton", bar.transform, options.TimelineOpen ? "\u6536\u8d77\u65e5\u5fd7" : "\u65e5\u5fd7", () => options.ToggleTimeline?.Invoke(), 98f, 50f, UnityTavernUiStyle.PanelRaised, false).interactable = options.ToggleTimeline != null;
            CreateButton("UnityCombatReturnButton", bar.transform, "\u8fd4\u56de", () => options.Close?.Invoke(), 82f, 50f, UnityTavernUiStyle.PanelRaised, false);
        }

        private void BuildTimelineDrawer(Transform parent, CombatReplay replay, int frameIndex, UnityCombatReplayPanelOptions options)
        {
            var drawer = new GameObject("UnityCombatTimelineDrawer", typeof(RectTransform), typeof(Image));
            drawer.transform.SetParent(parent, false);
            ConfigureTimelineDrawer(drawer);

            var header = new GameObject("UnityCombatTimelineDrawerHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(drawer.transform, false);
            ConfigureTimelineDrawerHeader(header);

            var title = UiFactory.Label("UnityCombatTimelineDrawerTitle", header.transform, "\u6218\u6597\u65e5\u5fd7", 16, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);
            CreateButton("UnityCombatTimelineCloseButton", header.transform, "\u6536\u8d77", () => options.ToggleTimeline?.Invoke(), 78f, 42f);

            timelineParent = UiFactory.ScrollView("UnityCombatTimeline", drawer.transform, UnityTavernUiStyle.PanelQuiet, out _);
            BuildTimeline(timelineParent, replay, frameIndex, options.SetFrame);
        }

        private void BuildCombatSide(Transform parent, BoardSide side, string title, CombatBoardSnapshot snapshot, CombatBoardSnapshot previousSnapshot, CombatFrame frame)
        {
            var sideName = side == BoardSide.Player ? "Player" : "Opponent";
            var sideObject = new GameObject("UnityCombat" + sideName + "Side", typeof(RectTransform), typeof(Image));
            sideObject.transform.SetParent(parent, false);
            ConfigureCombatSide(sideObject, side);

            var status = UiFactory.Label("UnityCombat" + sideName + "Status", sideObject.transform, BoardTitleText(title) + " " + (snapshot == null ? "0/7" : snapshot.Minions.Count + "/7"), 15, FontStyle.Bold);
            status.color = UnityTavernUiStyle.Gold;
            status.alignment = side == BoardSide.Player ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            UnityTavernUiStyle.SetPreferredHeight(status.gameObject, 24f);

            var board = new GameObject("UnityCombat" + sideName + "Board", typeof(RectTransform), typeof(Image));
            board.transform.SetParent(sideObject.transform, false);
            ConfigureCombatBoard(board, side);

            var minions = SnapshotByPosition(snapshot);
            var deaths = DeathMarkersByPosition(previousSnapshot, minions, frame);
            for (var slot = 0; slot < 7; slot += 1)
            {
                var slotObject = new GameObject("UnityCombatSlot-" + sideName + "-" + slot, typeof(RectTransform), typeof(Image));
                slotObject.transform.SetParent(board.transform, false);
                ConfigureCombatSlot(slotObject, side, slot, frame);
                if (minions.TryGetValue(slot, out var minion))
                {
                    BuildMinionTile(slotObject.transform, side, minion, frame);
                }
                else if (deaths.TryGetValue(slot, out var deadMinion))
                {
                    BuildDeathMarker(slotObject.transform, side, deadMinion);
                }
                else
                {
                    BuildEmptySlot(slotObject.transform, side, slot, frame);
                }
            }
        }

        private void ConfigureClose(Action close)
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.targetGraphic = ConfigureButtonChrome(closeButton.gameObject, UnityTavernUiStyle.PanelRaised, true);
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => close?.Invoke());
            SetText(closeButtonText, "关闭");
        }

        public static void ConfigureEventHighlightsLayout(GameObject target)
        {
            UnityTavernUiStyle.ConfigureSurface(target, UnityTavernUiStyle.Panel);
            UnityTavernUiStyle.ConfigureOutline(
                target,
                new Color(0f, 0f, 0f, 0.24f),
                new Vector2(1f, -1f));

            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(target);
            layout.padding = new RectOffset(6, 6, 2, 2);
            layout.spacing = 6;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private static void BuildControls(
            Transform parent,
            CombatReplay replay,
            int frameIndex,
            bool replayPlaying,
            string speedLabel,
            Action<int> setFrame,
            Action togglePlayback,
            Action cycleSpeed)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureControlsLayout(parent.gameObject);
            var hasFrames = replay != null && replay.Frames != null && replay.Frames.Count > 0;
            var lastIndex = hasFrames ? replay.Frames.Count - 1 : 0;
            CreateButton("UnityReplayFirstButton", parent, "返回", () => setFrame?.Invoke(0), 54f).interactable = hasFrames && frameIndex > 0;
          CreateButton("UnityReplayPrevButton", parent, "上帧", () => setFrame?.Invoke(frameIndex - 1), 54f).interactable = hasFrames && frameIndex > 0;
          CreateButton("UnityReplayPlayPauseButton", parent, replayPlaying ? "暂停" : "播放", () => togglePlayback?.Invoke(), 74f, replayPlaying ? UnityTavernUiStyle.Green : UnityTavernUiStyle.Blue, true).interactable = hasFrames;
          CreateButton("UnityReplayNextButton", parent, ">", () => setFrame?.Invoke(frameIndex + 1), 58f).interactable = hasFrames && frameIndex < lastIndex;
          CreateButton("UnityReplayLastButton", parent, "跳过", () => setFrame?.Invoke(lastIndex), 72f).interactable = hasFrames && frameIndex < lastIndex;
          CreateButton("UnityReplaySpeedButton", parent, "速度 " + (string.IsNullOrEmpty(speedLabel) ? "1x" : speedLabel), () => cycleSpeed?.Invoke(), 92f, UnityTavernUiStyle.TableLit, false).interactable = hasFrames;
      }

        private static void BuildControls(
            Transform parent,
            CombatReplay replay,
            int frameIndex,
            bool replayPlaying,
            string speedLabel,
            Action<int> setFrame,
            Action togglePlayback,
            Action cycleSpeed,
            float buttonHeight)
        {
            BuildControls(parent, replay, frameIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed);
            SetReplayButtonSizeAndText(parent, "UnityReplayFirstButton", "|<", 54f, buttonHeight);
            SetReplayButtonSizeAndText(parent, "UnityReplayPrevButton", "<", 54f, buttonHeight);
            SetReplayButtonSizeAndText(parent, "UnityReplayPlayPauseButton", replayPlaying ? "\u6682\u505c" : "\u64ad\u653e", 78f, buttonHeight);
            SetReplayButtonSizeAndText(parent, "UnityReplayNextButton", ">", 54f, buttonHeight);
          SetReplayButtonSizeAndText(parent, "UnityReplayLastButton", "跳过", 72f, buttonHeight);
          SetReplayButtonSizeAndText(parent, "UnityReplaySpeedButton", "\u901f\u5ea6 " + (string.IsNullOrEmpty(speedLabel) ? "1x" : speedLabel), 92f, buttonHeight);
      }

        private static void SetReplayButtonSizeAndText(Transform parent, string buttonName, string text, float width, float height)
        {
            var button = parent == null ? null : parent.Find(buttonName);
            if (button == null)
            {
                return;
            }

            UnityTavernUiStyle.SetFixedSize(button.gameObject, width, height);
            var label = button.Find(buttonName + "Text")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = text;
                label.fontSize = 14;
            }
        }

        private static void BuildEventHighlights(Transform parent, CombatFrame frame)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureEventHighlightsLayout(parent.gameObject);

            if (frame == null)
            {
                AddEventChip(parent, "Empty", "暂无事件", UnityTavernUiStyle.PanelQuiet);
                return;
            }

            AddEventChip(parent, "Event", EventTypeText(frame.EventType), EventTypeColor(frame.EventType));
            if (!string.IsNullOrEmpty(frame.ActorId))
            {
                AddEventChip(parent, "Actor", "来源 " + EntityName(frame, frame.ActorSide, frame.ActorId), UnityTavernUiStyle.Gold);
            }

            if (!string.IsNullOrEmpty(frame.TargetId))
            {
                AddEventChip(parent, "Target", "目标 " + EntityName(frame, frame.TargetSide, frame.TargetId), UnityTavernUiStyle.Red);
            }

            var damageText = DamageAmountText(frame);
            if (string.IsNullOrEmpty(damageText))
            {
                AddCountChip(parent, "Damage", "伤害", frame.DamagedEntityIds, UnityTavernUiStyle.Red);
            }
            else
            {
                AddEventChip(parent, "DamageAmount", "伤害 " + damageText, UnityTavernUiStyle.Red);
            }

            AddCountChip(parent, "Dead", "死亡", frame.DeadEntityIds, new Color(0.34f, 0.12f, 0.15f, 1f));
            AddCountChip(parent, "Summon", "召唤", frame.SummonedEntityIds, UnityTavernUiStyle.Blue);
            AddCountChip(parent, "Trigger", "触发", frame.TriggerSourceIds, UnityTavernUiStyle.Gold);

            if (frame.AttackPointerIndex >= 0)
            {
                AddEventChip(parent, "Pointer", "下一个 " + BoardSideText(frame.AttackPointerSide) + " " + (frame.AttackPointerIndex + 1), UnityTavernUiStyle.TableLit);
            }

            var overflow = frame.SummonOverflowCount + frame.RebornOverflowCount + Count(frame.OverflowedEntityIds);
            if (overflow > 0)
            {
                AddEventChip(parent, "Overflow", "溢出 x" + overflow, UnityTavernUiStyle.Red);
            }
        }

        private static void BuildBoard(Transform parent, BoardSide side, string title, CombatBoardSnapshot snapshot, CombatBoardSnapshot previousSnapshot, CombatFrame frame)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureBoardHost(parent.gameObject, side);
            var heading = UiFactory.Label("UnityReplay" + title + "Title", parent, BoardTitleText(title) + " " + (snapshot == null ? "0/7" : snapshot.Minions.Count + "/7"), 14, FontStyle.Bold);
            heading.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(heading.gameObject, 24f);

            var row = new GameObject("UnityReplay" + title + "Row", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFlexible(row, 1f, 1f);
            ConfigureBoardRow(row);

            var minions = SnapshotByPosition(snapshot);
            var deaths = DeathMarkersByPosition(previousSnapshot, minions, frame);
            for (var slot = 0; slot < 7; slot += 1)
            {
                if (minions.TryGetValue(slot, out var minion))
                {
                    BuildMinionTile(row.transform, side, minion, frame);
                }
                else if (deaths.TryGetValue(slot, out var deadMinion))
                {
                    BuildDeathMarker(row.transform, side, deadMinion);
                }
                else
                {
                    BuildEmptySlot(row.transform, side, slot, frame);
                }
            }
        }

        private static void BuildMinionTile(Transform parent, BoardSide side, CombatMinionSnapshot minion, CombatFrame frame)
        {
            var tile = new GameObject("UnityReplayMinion-" + minion.InstanceId, typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            UnityTavernUiStyle.Stretch(tile.GetComponent<RectTransform>());
            var tileColor = ReplayHighlightColor(side, minion.Position, minion.InstanceId, frame);
            var tileImage = tile.GetComponent<Image>();
            tileImage.color = tileColor;
            tileImage.raycastTarget = false;
            ConfigureTileOutline(tile, tileColor);
            ConfigureReplayTargetingOutline(tile, minion.InstanceId, frame);
            UnityTavernUiStyle.SetFlexible(tile, 1f, 1f);

            BuildCombatCardFace(tile.transform, minion, tileColor, false);
            BuildReplayTargetingLabel(tile.transform, minion.InstanceId, frame);
            ConfigureTileMotion(tile, side, minion.InstanceId, tileColor, frame);
        }

        private static void ConfigureReplayTargetingOutline(GameObject tile, string instanceId, CombatFrame frame)
        {
            if (tile == null || frame == null || string.IsNullOrEmpty(instanceId))
            {
                return;
            }

            var actor = string.Equals(frame.ActorId, instanceId, StringComparison.OrdinalIgnoreCase);
            var target = string.Equals(frame.TargetId, instanceId, StringComparison.OrdinalIgnoreCase);
            if (!actor && !target)
            {
                return;
            }

            var color = target ? UnityTavernUiStyle.Red : UnityTavernUiStyle.Gold;
            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(tile);
            outline.enabled = true;
            outline.effectColor = color;
            outline.effectDistance = new Vector2(3f, -3f);
            outline.useGraphicAlpha = false;
        }

        private static void BuildReplayTargetingLabel(Transform parent, string instanceId, CombatFrame frame)
        {
            if (parent == null || frame == null || string.IsNullOrEmpty(instanceId))
            {
                return;
            }

            var actor = string.Equals(frame.ActorId, instanceId, StringComparison.OrdinalIgnoreCase);
            var target = string.Equals(frame.TargetId, instanceId, StringComparison.OrdinalIgnoreCase);
            if (!actor && !target)
            {
                return;
            }

            var color = target ? UnityTavernUiStyle.Red : UnityTavernUiStyle.Gold;
            var labelObject = new GameObject("UnityReplayTargetingLabel-" + instanceId, typeof(RectTransform), typeof(Image));
            labelObject.transform.SetParent(parent, false);
            var image = labelObject.GetComponent<Image>();
            image.color = new Color(color.r, color.g, color.b, 0.94f);
            image.raycastTarget = false;

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -5f);
            rect.sizeDelta = new Vector2(72f, 22f);

            var label = AddCombatCardLabel(
                "UnityReplayTargetingLabelText-" + instanceId,
                labelObject.transform,
                target ? "目标" : "来源",
                11,
                FontStyle.Bold,
                Color.white,
                TextAnchor.MiddleCenter);
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private static void BuildReplayTargetingConnector(Transform root, CombatFrame frame)
        {
            if (root == null || frame == null || string.IsNullOrEmpty(frame.ActorId) || string.IsNullOrEmpty(frame.TargetId))
            {
                return;
            }

            var rootRect = root as RectTransform;
            if (rootRect == null)
            {
                return;
            }

            var source = FindDeepChild(root, "UnityReplayMinion-" + frame.ActorId) as RectTransform;
            var target = FindDeepChild(root, "UnityReplayMinion-" + frame.TargetId) as RectTransform;
            if (source == null || target == null)
            {
                return;
            }

            var connector = new GameObject(
                "UnityReplayTargetingConnector",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(UnityReplayTargetingConnectorComponent));
            connector.transform.SetParent(root, false);
            connector.transform.SetAsLastSibling();
            connector.GetComponent<LayoutElement>().ignoreLayout = true;
            var image = connector.GetComponent<Image>();
            image.color = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.88f);
            image.raycastTarget = false;

            var rect = connector.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 3f);

            var arrow = AddCombatCardLabel("UnityReplayTargetingConnectorArrow", connector.transform, "▶", 14, FontStyle.Bold, UnityTavernUiStyle.Gold, TextAnchor.MiddleRight);
            UnityTavernUiStyle.Stretch(arrow.rectTransform);
            connector.GetComponent<UnityReplayTargetingConnectorComponent>().Configure(rootRect, source, target, arrow);
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var index = 0; index < parent.childCount; index += 1)
            {
                var child = parent.GetChild(index);
                if (child.name == childName)
                {
                    return child;
                }

                var nested = FindDeepChild(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void BuildDeathMarker(Transform parent, BoardSide side, CombatMinionSnapshot minion)
        {
            var tile = new GameObject("UnityReplayDeathMarker-" + minion.InstanceId, typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            UnityTavernUiStyle.Stretch(tile.GetComponent<RectTransform>());
            var tileColor = new Color(0.24f, 0.08f, 0.08f, 0.92f);
            var tileImage = tile.GetComponent<Image>();
            tileImage.color = tileColor;
            tileImage.raycastTarget = false;
            ConfigureTileOutline(tile, UnityTavernUiStyle.Red);
            UnityTavernUiStyle.SetFlexible(tile, 1f, 1f);

            BuildCombatCardFace(tile.transform, minion, tileColor, true);
            UnityTavernUiStyle.EnsureComponent<UnityTavernReplayTileAnimator>(tile)
                .Configure(UnityTavernReplayTileMotion.Death, tileColor, MotionDirection(side));
        }

        private static void BuildEmptySlot(Transform parent, BoardSide side, int slot, CombatFrame frame)
        {
            var tile = new GameObject("UnityReplayEmptySlot-" + side + "-" + slot, typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            UnityTavernUiStyle.Stretch(tile.GetComponent<RectTransform>());
            tile.GetComponent<Image>().color = IsAttackPointer(side, slot, frame)
                ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.36f)
                : UnityTavernUiStyle.PanelQuiet;
            ConfigureTileOutline(tile, IsAttackPointer(side, slot, frame) ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.PanelRaised);
            UnityTavernUiStyle.SetFlexible(tile, 1f, 1f);
        }

        private static void BuildCombatCardFace(Transform parent, CombatMinionSnapshot minion, Color accentColor, bool defeated)
        {
            var face = new GameObject("UnityCombatCardFace-" + minion.InstanceId, typeof(RectTransform), typeof(Image));
            face.transform.SetParent(parent, false);
            var faceRect = face.GetComponent<RectTransform>();
            UnityTavernUiStyle.Stretch(faceRect);
            faceRect.offsetMin = new Vector2(4f, 4f);
            faceRect.offsetMax = new Vector2(-4f, -4f);

            var faceImage = face.GetComponent<Image>();
            faceImage.color = minion.Golden
                ? new Color(0.48f, 0.35f, 0.13f, defeated ? 0.72f : 0.96f)
                : new Color(0.12f, 0.10f, 0.09f, defeated ? 0.72f : 0.96f);
            faceImage.raycastTarget = false;
            ConfigureTileOutline(face, minion.Golden ? UnityTavernUiStyle.Gold : accentColor);

            var artViewport = new GameObject("UnityCombatCardArtViewport-" + minion.InstanceId, typeof(RectTransform), typeof(RectMask2D));
            artViewport.transform.SetParent(face.transform, false);
            SetAnchored(artViewport.GetComponent<RectTransform>(), new Vector2(0.14f, 0.30f), new Vector2(0.86f, 0.84f), Vector2.zero, Vector2.zero);

            var artObject = new GameObject("UnityCombatCardArt-" + minion.InstanceId, typeof(RectTransform), typeof(Image));
            artObject.transform.SetParent(artViewport.transform, false);
            var artImage = artObject.GetComponent<Image>();
            artImage.sprite = CardImageProvider.LoadSprite(null, minion.CardId, CardKind.Minion);
            artImage.preserveAspect = true;
            artImage.raycastTarget = false;
            artImage.color = artImage.sprite == null
                ? CombatCardFallbackColor(minion, defeated)
                : new Color(1f, 1f, 1f, defeated ? 0.42f : 0.92f);
            var cropArt = CardImageProvider.ShouldCropToPortrait(artImage.sprite);
            SetAnchored(
                artImage.rectTransform,
                cropArt ? new Vector2(0f, -1f) : Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            artImage.rectTransform.pivot = new Vector2(0.5f, 1f);

            if (artImage.sprite == null)
            {
                var fallback = AddCombatCardLabel("UnityCombatCardArtFallbackText-" + minion.InstanceId, artObject.transform, CombatCardFallbackText(minion), 14, FontStyle.Bold, UnityTavernUiStyle.MutedText, TextAnchor.MiddleCenter);
                UnityTavernUiStyle.Stretch(fallback.rectTransform);
            }

            var header = AddCombatCardLabel("UnityCombatCardHeader-" + minion.InstanceId, face.transform, HeaderText(minion), 14, FontStyle.Bold, UnityTavernUiStyle.Gold, TextAnchor.MiddleCenter);
            SetAnchored(header.rectTransform, new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.99f), Vector2.zero, Vector2.zero);

            var name = AddCombatCardLabel("UnityCombatCardName-" + minion.InstanceId, face.transform, CombatCardDisplayName(minion), 14, FontStyle.Bold, UnityTavernUiStyle.Text, TextAnchor.MiddleCenter);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetAnchored(name.rectTransform, new Vector2(0.18f, 0.12f), new Vector2(0.82f, 0.30f), Vector2.zero, Vector2.zero);

            var keywordText = KeywordsText(minion);
            if (!string.IsNullOrEmpty(keywordText))
            {
                var keywords = AddCombatCardLabel("UnityCombatCardKeywords-" + minion.InstanceId, face.transform, keywordText, 14, FontStyle.Bold, UnityTavernUiStyle.MutedText, TextAnchor.MiddleCenter);
                SetAnchored(keywords.rectTransform, new Vector2(0.18f, 0.01f), new Vector2(0.82f, 0.14f), Vector2.zero, Vector2.zero);
            }

            AddCombatStatBadge(face.transform, "UnityCombatCardAttack", minion.InstanceId, TavernNumberFormatter.CompactStat(minion.Attack), UnityTavernUiStyle.ColorFromHex(0xBA6A31), new Vector2(0f, 0f), new Vector2(19f, 18f));
            AddCombatStatBadge(face.transform, "UnityCombatCardHealth", minion.InstanceId, TavernNumberFormatter.CompactStat(minion.Health), UnityTavernUiStyle.Red, new Vector2(1f, 0f), new Vector2(-19f, 18f));

            if (defeated)
            {
                var overlay = new GameObject("UnityCombatCardDeathOverlay-" + minion.InstanceId, typeof(RectTransform), typeof(Image));
                overlay.transform.SetParent(face.transform, false);
                var overlayImage = overlay.GetComponent<Image>();
                overlayImage.color = new Color(0.18f, 0.02f, 0.03f, 0.54f);
                overlayImage.raycastTarget = false;
                UnityTavernUiStyle.Stretch(overlay.GetComponent<RectTransform>());

                var defeatedLabel = AddCombatCardLabel("UnityCombatCardDeathText-" + minion.InstanceId, overlay.transform, "阵亡", 16, FontStyle.Bold, UnityTavernUiStyle.Red, TextAnchor.MiddleCenter);
                UnityTavernUiStyle.Stretch(defeatedLabel.rectTransform);
            }
        }

        private static Text AddCombatCardLabel(string name, Transform parent, string text, int size, FontStyle style, Color color, TextAnchor alignment)
        {
            var label = UiFactory.Label(name, parent, text ?? string.Empty, Mathf.Max(14, size), style);
            label.alignment = alignment;
            label.color = color;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = Mathf.Max(14, size);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(label.gameObject, new Color(0f, 0f, 0f, 0.46f), new Vector2(1f, -1f));
            return label;
        }

        private static void AddCombatStatBadge(Transform parent, string prefix, string instanceId, string value, Color color, Vector2 anchor, Vector2 position)
        {
            var badge = new GameObject(prefix + "-" + instanceId, typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(parent, false);
            var badgeImage = badge.GetComponent<Image>();
            badgeImage.color = new Color(color.r, color.g, color.b, 0.96f);
            badgeImage.raycastTarget = false;

            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(34f, 34f);

            var label = AddCombatCardLabel(prefix + "Text-" + instanceId, badge.transform, value, 16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 16;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private static void SetAnchored(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void ConfigureTileMotion(GameObject tile, BoardSide side, string instanceId, Color tileColor, CombatFrame frame)
        {
            var motion = TileMotionFor(instanceId, frame);
            if (motion == UnityTavernReplayTileMotion.None)
            {
                return;
            }

            UnityTavernUiStyle.EnsureComponent<UnityTavernReplayTileAnimator>(tile)
                .Configure(motion, tileColor, MotionDirection(side));
        }

        private static UnityTavernReplayTileMotion TileMotionFor(string instanceId, CombatFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(instanceId))
            {
                return UnityTavernReplayTileMotion.None;
            }

            if (Contains(frame.DeadEntityIds, instanceId))
            {
                return UnityTavernReplayTileMotion.Death;
            }

            if (frame.TargetId == instanceId || Contains(frame.DamagedEntityIds, instanceId))
            {
                return UnityTavernReplayTileMotion.Hit;
            }

            if (frame.ActorId == instanceId)
            {
                return UnityTavernReplayTileMotion.Strike;
            }

            if (Contains(frame.SummonedEntityIds, instanceId))
            {
                return UnityTavernReplayTileMotion.Summon;
            }

            if (Contains(frame.TriggerSourceIds, instanceId))
            {
                return UnityTavernReplayTileMotion.Trigger;
            }

            if (Contains(frame.RelatedEntityIds, instanceId))
            {
                return UnityTavernReplayTileMotion.Related;
            }

            return UnityTavernReplayTileMotion.None;
        }

        private static float MotionDirection(BoardSide side)
        {
            return side == BoardSide.Player ? 1f : -1f;
        }

        private static Dictionary<int, CombatMinionSnapshot> SnapshotByPosition(CombatBoardSnapshot snapshot)
        {
            var result = new Dictionary<int, CombatMinionSnapshot>();
            if (snapshot == null || snapshot.Minions == null)
            {
                return result;
            }

            foreach (var minion in snapshot.Minions.Take(7))
            {
                if (minion != null && minion.Position >= 0 && minion.Position < 7 && !result.ContainsKey(minion.Position))
                {
                    result.Add(minion.Position, minion);
                }
            }

            return result;
        }

        private static Dictionary<int, CombatMinionSnapshot> DeathMarkersByPosition(
            CombatBoardSnapshot previousSnapshot,
            IReadOnlyDictionary<int, CombatMinionSnapshot> currentMinions,
            CombatFrame frame)
        {
            var result = new Dictionary<int, CombatMinionSnapshot>();
            if (previousSnapshot == null || previousSnapshot.Minions == null || frame == null || frame.DeadEntityIds == null)
            {
                return result;
            }

            var currentIds = new HashSet<string>(currentMinions.Values.Where(minion => minion != null).Select(minion => minion.InstanceId));
            foreach (var minion in previousSnapshot.Minions)
            {
                if (minion == null
                    || minion.Position < 0
                    || minion.Position >= 7
                    || string.IsNullOrEmpty(minion.InstanceId)
                    || currentIds.Contains(minion.InstanceId)
                    || !Contains(frame.DeadEntityIds, minion.InstanceId)
                    || result.ContainsKey(minion.Position))
                {
                    continue;
                }

                result.Add(minion.Position, minion);
            }

            return result;
        }

        private static void BuildTimeline(Transform parent, CombatReplay replay, int frameIndex, Action<int> setFrame)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureTimelineLayout(parent.gameObject);
            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                AddTimelineLine(parent, "暂无回放帧。", false, UnityTavernUiStyle.PanelRaised, () => { });
                return;
            }

            var windowStart = replay.Frames.Count <= 16 ? 0 : Mathf.Clamp(frameIndex - 7, 0, replay.Frames.Count - 16);
            var windowEnd = Mathf.Min(replay.Frames.Count, windowStart + 16);
            for (var index = windowStart; index < windowEnd; index += 1)
            {
                var item = replay.Frames[index];
                var target = item.Index;
                AddTimelineLine(
                    parent,
                    (target + 1) + ". " + EventTypeText(item.EventType) + "  " + FrameLogText(item),
                    target == frameIndex,
                    EventTypeColor(item.EventType),
                    () => setFrame?.Invoke(target));
            }
        }

        private static void AddTimelineLine(Transform parent, string text, bool selected, Color eventColor, Action onClick)
        {
            var lineColor = selected ? UnityTavernUiStyle.Blue : new Color(eventColor.r, eventColor.g, eventColor.b, 0.56f);
            var button = CreateButton("UnityReplayEventLine", parent, text, onClick, 0f, lineColor, selected);
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, UnityTavernUiStyle.TouchHeight);
            var image = button.GetComponent<Image>();
            image.color = lineColor;
        }

        private static void AddEventChip(Transform parent, string suffix, string text, Color color)
        {
            var chip = new GameObject("UnityReplayEventChip-" + suffix, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            chip.GetComponent<Image>().color = color;
            UnityTavernUiStyle.ConfigureOutline(chip, new Color(0f, 0f, 0f, 0.34f), new Vector2(1f, -1f));
            UnityTavernUiStyle.SetFixedSize(chip, Mathf.Clamp(52f + (text ?? string.Empty).Length * 6f, 86f, 150f), 30f);

            var label = UiFactory.Label("UnityReplayEventChipText-" + suffix, chip.transform, text, 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            if ((text ?? string.Empty).Length > 10)
            {
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9;
                label.resizeTextMaxSize = 14;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }

            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private static void AddCountChip(Transform parent, string suffix, string label, List<string> ids, Color color)
        {
            var count = Count(ids);
            if (count > 0)
            {
                AddEventChip(parent, suffix, label + " x" + count, color);
            }
        }

        private static string DamageAmountText(CombatFrame frame)
        {
            if (frame == null || (frame.TargetDamageAmount <= 0 && frame.ActorDamageAmount <= 0))
            {
                return string.Empty;
            }

            if (frame.TargetDamageAmount > 0 && frame.ActorDamageAmount > 0)
            {
                return TavernNumberFormatter.FullNumber(frame.TargetDamageAmount) + " / " + TavernNumberFormatter.FullNumber(frame.ActorDamageAmount);
            }

            return TavernNumberFormatter.FullNumber(frame.TargetDamageAmount > 0 ? frame.TargetDamageAmount : frame.ActorDamageAmount);
        }

        private static string FrameLogText(CombatFrame frame)
        {
            if (frame == null)
            {
                return string.Empty;
            }

            var logText = FrameSummaryText(frame);
            var damageText = DamageAmountText(frame);
            if (string.IsNullOrEmpty(damageText))
            {
                return logText;
            }

            return string.IsNullOrEmpty(logText) ? "伤害 " + damageText : logText + "  伤害 " + damageText;
        }

        private static string FrameSummaryText(CombatFrame frame)
        {
            if (frame == null)
            {
                return string.Empty;
            }

            var actor = EntityName(frame, frame.ActorSide, frame.ActorId);
            var target = EntityName(frame, frame.TargetSide, frame.TargetId);
            switch (frame.EventType)
            {
                case CombatEventType.CombatStarted:
                    return "双方进入战斗";
                case CombatEventType.AttackDeclared:
                    return actor + " 攻击 " + target;
                case CombatEventType.DamageResolved:
                    return actor + " 与 " + target + " 结算伤害";
                case CombatEventType.DivineShieldBroken:
                    return target + " 的圣盾破裂";
                case CombatEventType.VenomousResolved:
                    return actor + " 触发烈毒";
                case CombatEventType.DeathQueued:
                    return EntityListText(frame, frame.DeadEntityIds) + " 阵亡";
                case CombatEventType.DeathrattleResolved:
                    return actor + " 触发亡语";
                case CombatEventType.MinionSummoned:
                    return EntityListText(frame, frame.SummonedEntityIds) + " 被召唤";
                case CombatEventType.RebornResolved:
                    return actor + " 复生";
                case CombatEventType.RallyResolved:
                    return actor + " 触发进击";
                case CombatEventType.AvengeProgressed:
                    return "复仇进度推进";
                case CombatEventType.AvengeCounterUpdated:
                    return "复仇计数更新";
                case CombatEventType.DamageTriggered:
                    return actor + " 触发伤害效果";
                case CombatEventType.AttackTriggered:
                    return actor + " 触发攻击效果";
                case CombatEventType.SpellcraftTemporaryApplied:
                    return "塑造法术临时生效";
                case CombatEventType.ImmediateAttackQueued:
                    return actor + " 准备立即攻击";
                case CombatEventType.WindfuryResolved:
                    return actor + " 触发风怒";
                case CombatEventType.AttackPointerRetargeted:
                    return "攻击顺位调整";
                case CombatEventType.SummonOverflowed:
                    return "召唤空间不足";
                case CombatEventType.RebornOverflowed:
                    return "复生空间不足";
                case CombatEventType.CombatRewardQueued:
                    return "战斗奖励入列";
                case CombatEventType.CombatSpellCast:
                    return "战斗法术施放";
                case CombatEventType.CombatEnded:
                    return "战斗结束";
                case CombatEventType.TrinketTriggered:
                    return "饰品效果触发";
                default:
                    return EventTypeText(frame.EventType);
            }
        }

        private static IReadOnlyList<string> RewardReceiptLines(CombatReplay replay)
        {
            var rewards = replay?.PlayerRewards;
            if (rewards == null || rewards.Count == 0)
            {
                return new List<string> { "无战斗奖励结算" };
            }

            return rewards.Select(RewardReceiptLine).ToList();
        }

        private static string RewardReceiptLine(CombatReward reward)
        {
            if (reward == null)
            {
                return "战斗奖励 -> 已结算";
            }

            var amount = Math.Max(1, reward.Amount);
            switch (reward.Type)
            {
                case CombatRewardType.ImproveUndeadAttack:
                    return "亡灵攻击 +" + Math.Max(0, reward.Amount) + " -> 已写入全局";
                case CombatRewardType.AddTavernSpellToHand:
                    return "获得法术：" + RewardCardName(reward.CardId) + " x" + amount + " -> 已加入手牌";
                case CombatRewardType.AddGeneratedSpellToHand:
                    return "获得法术：" + RewardCardName(reward.CardId) + " x" + amount + " -> 已加入手牌";
                case CombatRewardType.AddRandomTavernSpellToHand:
                    return "获得随机酒馆法术 x" + amount + " -> 已加入手牌";
                case CombatRewardType.AddRandomSpellcraftSpellToHand:
                    return "获得塑造法术 x" + amount + " -> 已加入手牌";
                case CombatRewardType.GainNextTurnGold:
                    return "下回合金币 +" + Math.Max(0, reward.Amount) + " -> 已进入下一回合资源";
                case CombatRewardType.ImproveShopStats:
                    return "鲍勃酒馆成长 " + SignedStats(reward.Attack * amount, reward.Health * amount) + " -> 已应用";
                case CombatRewardType.ImproveElementalShopStats:
                    return "元素酒馆成长 +" + amount + "/+" + amount + " -> 已应用";
                case CombatRewardType.ImproveTavernMinionStats:
                    return "酒馆随从成长 +" + amount + "/+" + amount + " -> 已应用";
                case CombatRewardType.ImproveElementalHealth:
                    return "元素酒馆成长 +0/+" + Math.Max(0, reward.Amount) + " -> 已应用";
                case CombatRewardType.ImproveBloodGemAttack:
                    return "鲜血宝石攻击 +" + Math.Max(0, reward.Amount) + " -> 已写入全局";
                case CombatRewardType.ImproveBloodGemHealth:
                    return "鲜血宝石生命 +" + Math.Max(0, reward.Amount) + " -> 已写入全局";
                case CombatRewardType.ImproveBloodGemStats:
                    return "鲜血宝石 " + SignedStats(reward.Attack * amount, reward.Health * amount) + " -> 已写入全局";
                case CombatRewardType.ImproveTavernSpellAttack:
                    return "酒馆法术攻击 +" + Math.Max(0, reward.Amount) + " -> 已写入全局";
                case CombatRewardType.ImproveTavernSpellStats:
                    return "酒馆法术 " + SignedStats(reward.Attack * amount, reward.Health * amount) + " -> 已写入全局";
                case CombatRewardType.GainFreeRefresh:
                    return "免费刷新 +" + Math.Max(0, reward.Amount) + " -> 已写入酒馆";
                case CombatRewardType.TavernSpellCostReduction:
                    return "下一张酒馆法术费用 -" + Math.Max(0, reward.Amount) + " -> 已写入资源";
                case CombatRewardType.BuffHandMinion:
                case CombatRewardType.BuffTargetHandMinion:
                    return "手牌增益 " + SignedStats(reward.Attack, reward.Health) + " -> 已应用";
                default:
                    return RewardTypeName(reward.Type) + " x" + amount + " -> 已结算";
            }
        }

        private static IReadOnlyList<string> TriggerChainLines(CombatReplay replay)
        {
            var rewards = replay?.PlayerRewards;
            if (rewards == null || rewards.Count == 0)
            {
                return new List<string> { "无战斗奖励触发" };
            }

            var lines = new List<string>();
            foreach (var reward in rewards)
            {
                var frame = FindRewardQueueFrame(replay, reward);
                var source = RewardSourceName(replay, reward);
                var effect = RewardTypeName(reward.Type);
                var target = RewardWriteBackTargetText(reward);
                if (frame != null && !string.IsNullOrWhiteSpace(frame.ActorId))
                {
                    var actor = EntityName(frame, frame.ActorSide, frame.ActorId);
                    var battlecry = string.IsNullOrWhiteSpace(frame.TargetId)
                        ? source
                        : EntityName(frame, frame.TargetSide, frame.TargetId);
                    if (!string.Equals(actor, battlecry, StringComparison.OrdinalIgnoreCase))
                    {
                        lines.Add(actor + " -> " + battlecry + " -> " + effect + " -> " + target);
                        continue;
                    }
                }

                lines.Add(source + " -> " + effect + " -> " + target);
            }

            return lines;
        }

        private static CombatFrame FindRewardQueueFrame(CombatReplay replay, CombatReward reward)
        {
            if (replay?.Frames == null || reward == null)
            {
                return null;
            }

            return replay.Frames.LastOrDefault(frame =>
                frame != null &&
                frame.EventType == CombatEventType.CombatRewardQueued &&
                (MatchesRewardSource(frame.TargetId, reward) ||
                 MatchesRewardSource(frame.ActorId, reward) ||
                 Contains(frame.RelatedEntityIds, reward.SourceInstanceId) ||
                 Contains(frame.RelatedEntityIds, reward.SourceCardId)));
        }

        private static bool MatchesRewardSource(string id, CombatReward reward)
        {
            return !string.IsNullOrWhiteSpace(id) &&
                ((!string.IsNullOrWhiteSpace(reward.SourceInstanceId) && string.Equals(id, reward.SourceInstanceId, StringComparison.OrdinalIgnoreCase)) ||
                 (!string.IsNullOrWhiteSpace(reward.SourceCardId) && string.Equals(id, reward.SourceCardId, StringComparison.OrdinalIgnoreCase)));
        }

        private static string RewardSourceName(CombatReplay replay, CombatReward reward)
        {
            var minion = FindSnapshotMinion(replay, reward?.SourceInstanceId) ??
                FindSnapshotMinion(replay, reward?.SourceCardId);
            if (minion != null)
            {
                return CombatCardDisplayName(minion);
            }

            return RewardCardName(reward?.SourceCardId);
        }

        private static string RewardCardName(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return "战斗奖励";
            }

            var minionName = LocalizedCombatMinionName(cardId);
            if (!string.IsNullOrWhiteSpace(minionName))
            {
                return minionName;
            }

            var spellName = LocalizedCombatSpellName(cardId);
            return string.IsNullOrWhiteSpace(spellName) ? cardId : spellName;
        }

        private static string RewardWriteBackTargetText(CombatReward reward)
        {
            if (reward == null)
            {
                return "已结算";
            }

            switch (reward.Type)
            {
                case CombatRewardType.ImproveUndeadAttack:
                    return "UndeadAttackBonus +" + Math.Max(0, reward.Amount);
                case CombatRewardType.AddTavernSpellToHand:
                case CombatRewardType.AddGeneratedSpellToHand:
                case CombatRewardType.AddRandomTavernSpellToHand:
                case CombatRewardType.AddRandomSpellcraftSpellToHand:
                    return "Hand +" + Math.Max(1, reward.Amount);
                case CombatRewardType.GainNextTurnGold:
                    return "NextTurnBonusGold +" + Math.Max(0, reward.Amount);
                case CombatRewardType.ImproveShopStats:
                    return "BobTavernGrowth " + SignedStats(reward.Attack * Math.Max(1, reward.Amount), reward.Health * Math.Max(1, reward.Amount));
                case CombatRewardType.ImproveElementalShopStats:
                case CombatRewardType.ImproveTavernMinionStats:
                    return "BobTavernGrowth +" + Math.Max(1, reward.Amount) + "/+" + Math.Max(1, reward.Amount);
                case CombatRewardType.ImproveElementalHealth:
                    return "BobTavernGrowth +0/+" + Math.Max(0, reward.Amount);
                case CombatRewardType.GainFreeRefresh:
                    return "FreeRefreshes +" + Math.Max(0, reward.Amount);
                default:
                    return "已结算";
            }
        }

        private static string SignedStats(int attack, int health)
        {
            return SignedNumber(attack) + "/" + SignedNumber(health);
        }

        private static string SignedNumber(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        private static string RewardTypeName(CombatRewardType type)
        {
            switch (type)
            {
                case CombatRewardType.ImproveUndeadAttack:
                    return "ImproveUndeadAttack";
                case CombatRewardType.AddTavernSpellToHand:
                    return "AddTavernSpellToHand";
                case CombatRewardType.AddGeneratedSpellToHand:
                    return "AddGeneratedSpellToHand";
                case CombatRewardType.GainNextTurnGold:
                    return "GainNextTurnGold";
                case CombatRewardType.ImproveShopStats:
                    return "ImproveShopStats";
                default:
                    return type.ToString();
            }
        }

        private static string EntityName(CombatFrame frame, BoardSide side, string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return BoardSideText(side);
            }

            var snapshot = side == BoardSide.Player ? frame.PlayerBoardSnapshot : frame.OpponentBoardSnapshot;
            var minion = FindSnapshotMinion(snapshot, instanceId)
                ?? FindSnapshotMinion(frame.PlayerBoardSnapshot, instanceId)
                ?? FindSnapshotMinion(frame.OpponentBoardSnapshot, instanceId);
            return minion == null ? instanceId : CombatCardDisplayName(minion);
        }

        private static string EntityListText(CombatFrame frame, List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return "随从";
            }

            var names = ids.Take(2).Select(id => EntityName(frame, BoardSide.Player, id)).ToList();
            if (ids.Count > names.Count)
            {
                names.Add("+" + (ids.Count - names.Count));
            }

            return string.Join("、", names);
        }

        private static CombatMinionSnapshot FindSnapshotMinion(CombatBoardSnapshot snapshot, string instanceId)
        {
            return snapshot?.Minions?.FirstOrDefault(minion => minion != null && minion.InstanceId == instanceId);
        }

        private static CombatMinionSnapshot FindSnapshotMinion(CombatReplay replay, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || replay?.Frames == null)
            {
                return null;
            }

            foreach (var frame in replay.Frames)
            {
                var minion = FindSnapshotMinion(frame.PlayerBoardSnapshot, id) ??
                    FindSnapshotMinion(frame.OpponentBoardSnapshot, id);
                if (minion != null)
                {
                    return minion;
                }
            }

            return null;
        }

        private static bool Contains(IEnumerable<string> values, string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                values != null &&
                values.Any(candidate => string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase));
        }

        private static Text AddTileLine(Transform parent, string text, int size, FontStyle style, Color color, float height, bool resizeForFit = false)
        {
            var label = UiFactory.Label("UnityReplayTileLine", parent, text ?? string.Empty, Mathf.Max(14, size), style);
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            if (resizeForFit)
            {
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9;
                label.resizeTextMaxSize = Mathf.Max(14, size);
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
            }

            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, height);
            return label;
        }

        private static GameObject CreateBoardHost(string name, Transform parent, BoardSide side)
        {
            var board = new GameObject(name, typeof(RectTransform), typeof(Image));
            board.transform.SetParent(parent, false);
            ConfigureBoardHost(board, side);
            return board;
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick, float width)
        {
            return CreateButton(name, parent, text, onClick, width, UnityTavernUiStyle.TouchHeight, UnityTavernUiStyle.SurfaceRaised, false);
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick, float width, Color backgroundColor, bool emphasized)
        {
            return CreateButton(name, parent, text, onClick, width, UnityTavernUiStyle.TouchHeight, backgroundColor, emphasized);
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick, float width, float height)
        {
            return CreateButton(name, parent, text, onClick, width, height, UnityTavernUiStyle.PanelRaised, false);
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick, float width, float height, Color backgroundColor, bool emphasized)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = ConfigureButtonChrome(buttonObject, backgroundColor, emphasized);
            if (width > 0f)
            {
                UnityTavernUiStyle.SetFixedSize(buttonObject, width, height);
            }

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.ConfigureButton(button, emphasized ? UnityTavernUiStyle.Gold : UnityTavernUiStyle.ArcaneBlue, emphasized);

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        public static Image ConfigureButtonChrome(GameObject buttonObject, Color backgroundColor, bool emphasized)
        {
            var image = UnityTavernUiStyle.ConfigureSurface(buttonObject, backgroundColor, true);
            UnityTavernUiStyle.ConfigureOutline(
                buttonObject,
                emphasized
                    ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.56f)
                    : new Color(0f, 0f, 0f, 0.26f),
                new Vector2(1f, -1f));
            return image;
        }

        private static void ConfigureTileOutline(GameObject tile, Color color)
        {
            UnityTavernUiStyle.ConfigureOutline(
                tile,
                new Color(color.r, color.g, color.b, 0.42f),
                new Vector2(1f, -1f));
        }

        private static Color ReplayHighlightColor(BoardSide side, int position, string instanceId, CombatFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(instanceId))
            {
                return UnityTavernUiStyle.PanelQuiet;
            }

            if (frame.ActorId == instanceId)
            {
                return UnityTavernUiStyle.Gold;
            }

            if (Contains(frame.DeadEntityIds, instanceId))
            {
                return new Color(0.34f, 0.12f, 0.15f, 1f);
            }

            if (frame.TargetId == instanceId || Contains(frame.DamagedEntityIds, instanceId))
            {
                return UnityTavernUiStyle.Red;
            }

            if (Contains(frame.SummonedEntityIds, instanceId))
            {
                return UnityTavernUiStyle.Blue;
            }

            if (Contains(frame.TriggerSourceIds, instanceId))
            {
                return UnityTavernUiStyle.Gold;
            }

            if (Contains(frame.RelatedEntityIds, instanceId))
            {
                return UnityTavernUiStyle.TableLit;
            }

            if (IsAttackPointer(side, position, frame))
            {
                return UnityTavernUiStyle.TableLit;
            }

            return UnityTavernUiStyle.PanelQuiet;
        }

        private static Color EventTypeColor(CombatEventType eventType)
        {
            switch (eventType)
            {
                case CombatEventType.AttackDeclared:
                case CombatEventType.AttackTriggered:
                case CombatEventType.ImmediateAttackQueued:
                case CombatEventType.WindfuryResolved:
                    return UnityTavernUiStyle.Green;
                case CombatEventType.DamageResolved:
                case CombatEventType.DivineShieldBroken:
                case CombatEventType.VenomousResolved:
                case CombatEventType.DamageTriggered:
                    return UnityTavernUiStyle.Red;
                case CombatEventType.DeathQueued:
                case CombatEventType.DeathrattleResolved:
                    return new Color(0.34f, 0.12f, 0.15f, 1f);
                case CombatEventType.MinionSummoned:
                case CombatEventType.RebornResolved:
                case CombatEventType.SummonOverflowed:
                case CombatEventType.RebornOverflowed:
                    return UnityTavernUiStyle.Blue;
                case CombatEventType.RallyResolved:
                case CombatEventType.AvengeProgressed:
                case CombatEventType.AvengeCounterUpdated:
                case CombatEventType.SpellcraftTemporaryApplied:
                case CombatEventType.CombatRewardQueued:
                case CombatEventType.CombatSpellCast:
                case CombatEventType.TrinketTriggered:
                    return UnityTavernUiStyle.Gold;
                default:
                    return UnityTavernUiStyle.PanelRaised;
            }
        }

        private static string ResultText(CombatReplay replay)
        {
            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                return "\u6218\u6597";
            }

            return replay.SafetyStopped ? "\u8d85\u9650 / \u672a\u51b3" : ResultText(replay.Result);
        }

        private static string StatsText(UnityCombatReplayPanelOptions options, CombatReplay replay)
        {
            if (options != null && !string.IsNullOrEmpty(options.StatsText))
            {
                return options.StatsText;
            }

            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                return "\u80dc 0%  \u5e73 0%  \u8d1f 0%  \u8d85\u9650 0%";
            }

            if (replay.SafetyStopped)
            {
                return "\u80dc 0%  \u5e73 0%  \u8d1f 0%  \u8d85\u9650 100%";
            }

            return replay.Result == CombatWinner.Player
                ? "\u80dc 100%  \u5e73 0%  \u8d1f 0%  \u8d85\u9650 0%"
                : replay.Result == CombatWinner.Draw
                    ? "\u80dc 0%  \u5e73 100%  \u8d1f 0%  \u8d85\u9650 0%"
                    : "\u80dc 0%  \u5e73 0%  \u8d1f 100%  \u8d85\u9650 0%";
        }

        private static string StatsMetaText(UnityCombatReplayPanelOptions options, CombatReplay replay)
        {
            if (options != null && !string.IsNullOrEmpty(options.StatsMetaText))
            {
                return options.StatsMetaText;
            }

            var sample = replay != null && replay.Frames != null && replay.Frames.Count > 0 ? 1 : 0;
            var maxSteps = options == null ? 200 : Mathf.Max(1, options.MaxSteps);
            return "\u6837\u672c " + sample + " / \u6700\u5927\u8f6e\u6b21 " + maxSteps;
        }

        private static string SideStatusText(BoardSide side, CombatReplay replay, int frameIndex)
        {
            var snapshot = CurrentSnapshot(side, replay, frameIndex);
            var count = snapshot == null || snapshot.Minions == null ? 0 : snapshot.Minions.Count;
            return BoardSideText(side) + "  " + count + "/7";
        }

        private static string CurrentEventText(CombatFrame frame, bool hasFrames, int frameIndex, CombatReplay replay)
        {
            if (!hasFrames || frame == null)
            {
                return "\u8fd0\u884c\u6218\u6597\u540e\u53ef\u67e5\u770b\u56de\u653e\u5e27\u3002";
            }

            var safety = replay != null && replay.SafetyStopped ? "  \u8d85\u9650" : string.Empty;
            return (frameIndex + 1) + "/" + replay.Frames.Count + "  " + EventTypeText(frame.EventType) + "  " + FrameLogText(frame) + safety;
        }

        private static CombatBoardSnapshot CurrentSnapshot(BoardSide side, CombatReplay replay, int frameIndex)
        {
            if (replay == null || replay.Frames == null || replay.Frames.Count == 0)
            {
                return null;
            }

            var frame = replay.Frames[Mathf.Clamp(frameIndex, 0, replay.Frames.Count - 1)];
            return side == BoardSide.Player ? frame.PlayerBoardSnapshot : frame.OpponentBoardSnapshot;
        }

        private static string ResultText(CombatWinner winner)
        {
            switch (winner)
            {
                case CombatWinner.Player: return "我方胜利";
                case CombatWinner.Opponent: return "对手胜利";
                case CombatWinner.Draw: return "平局";
                default: return winner.ToString();
            }
        }

        private static string BoardSideText(BoardSide side)
        {
            return side == BoardSide.Player ? "我方" : "对手";
        }

        private static string BoardTitleText(string title)
        {
            if (title == "Player")
            {
                return "我方";
            }

            if (title == "Opponent")
            {
                return "对手";
            }

            return title;
        }

        private static string HeaderText(CombatMinionSnapshot minion)
        {
            if (minion == null)
            {
                return string.Empty;
            }

            var tierText = minion.TavernTier > 0 ? minion.TavernTier + "本" : "随从";
            var tribeText = TribesText(minion);
            return string.IsNullOrEmpty(tribeText) ? tierText : tierText + " " + tribeText;
        }

        private static string CombatCardDisplayName(CombatMinionSnapshot minion)
        {
            if (minion == null)
            {
                return string.Empty;
            }

            var localized = LocalizedCombatMinionName(minion.CardId);
            if (!string.IsNullOrWhiteSpace(localized))
            {
                return localized;
            }

            return minion.Name ?? string.Empty;
        }

        private static string LocalizedCombatMinionName(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return null;
            }

            EnsureLocalizedCombatMinionNamesLoaded();
            return localizedCombatMinionNamesByCardId != null &&
                 localizedCombatMinionNamesByCardId.TryGetValue(cardId, out var name)
               ? name
               : null;
       }

       private static string LocalizedCombatSpellName(string cardId)
       {
           if (string.IsNullOrWhiteSpace(cardId))
           {
               return null;
           }

           EnsureLocalizedCombatSpellNamesLoaded();
           return localizedCombatSpellNamesByCardId != null &&
                  localizedCombatSpellNamesByCardId.TryGetValue(cardId, out var name)
               ? name
               : null;
       }

       private static void EnsureLocalizedCombatMinionNamesLoaded()
       {
           if (localizedCombatMinionNamesLoaded)
           {
                return;
            }

            localizedCombatMinionNamesLoaded = true;
            localizedCombatMinionNamesByCardId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var catalog = MinionCatalogLoader.LoadFromResources();
                foreach (var definition in catalog.All)
                {
                    AddLocalizedCombatMinionName(definition.CardId, definition.Name);
                    if (definition.Golden != null)
                    {
                        AddLocalizedCombatMinionName(definition.Golden.CardId, definition.Name);
                    }
                }
            }
            catch (Exception)
            {
                localizedCombatMinionNamesByCardId.Clear();
            }
        }

        private static void AddLocalizedCombatMinionName(string cardId, string name)
        {
            if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(name) ||
                localizedCombatMinionNamesByCardId.ContainsKey(cardId))
            {
                return;
            }

           localizedCombatMinionNamesByCardId.Add(cardId, name);
       }

       private static void EnsureLocalizedCombatSpellNamesLoaded()
       {
           if (localizedCombatSpellNamesLoaded)
           {
               return;
           }

           localizedCombatSpellNamesLoaded = true;
           localizedCombatSpellNamesByCardId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
           try
           {
               var catalog = SpellCatalogLoader.LoadFromResources();
               foreach (var definition in catalog.All)
               {
                   AddLocalizedCombatSpellName(definition.CardNumber, definition.Name);
                   AddLocalizedCombatSpellName(definition.Id, definition.Name);
               }
           }
           catch (Exception)
           {
               localizedCombatSpellNamesByCardId.Clear();
           }
       }

       private static void AddLocalizedCombatSpellName(string cardId, string name)
       {
           if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(name) ||
               localizedCombatSpellNamesByCardId.ContainsKey(cardId))
           {
               return;
           }

           localizedCombatSpellNamesByCardId.Add(cardId, name);
       }

        private static string TribesText(CombatMinionSnapshot minion)
        {
            if (minion == null || minion.Tribes == null || minion.Tribes.Count == 0 || minion.Tribes.All(tribe => tribe == Tribe.None))
            {
                return "中立";
            }

            if (minion.Tribes.Contains(Tribe.All))
            {
                return "全部种族";
            }

            var tribes = minion.Tribes
                .Where(tribe => tribe != Tribe.None)
                .Take(2)
                .Select(TribeName)
                .ToArray();
            return tribes.Length == 0 ? "中立" : string.Join("/", tribes);
        }

        private static string KeywordsText(CombatMinionSnapshot minion)
        {
            if (minion == null || minion.Keywords == null || minion.Keywords.Count == 0)
            {
                return string.Empty;
            }

            var distinct = minion.Keywords.Distinct().ToList();
            var keywords = distinct.Take(3).Select(KeywordName).ToArray();
            var suffix = distinct.Count > keywords.Length ? " +" + (distinct.Count - keywords.Length) : string.Empty;
            return string.Join(" ", keywords) + suffix;
        }

        private static string CombatCardFallbackText(CombatMinionSnapshot minion)
        {
            if (minion == null)
            {
                return "卡牌";
            }

            var tribes = TribesText(minion);
            return string.IsNullOrEmpty(tribes) ? "卡牌" : tribes;
        }

        private static Color CombatCardFallbackColor(CombatMinionSnapshot minion, bool defeated)
        {
            var color = UnityTavernUiStyle.ColorFromHex(0x2F4050);
            if (minion != null && minion.Tribes != null)
            {
                var tribe = minion.Tribes.FirstOrDefault(value => value != Tribe.None);
                switch (tribe)
                {
                    case Tribe.Beast: color = UnityTavernUiStyle.ColorFromHex(0x4A3823); break;
                    case Tribe.Murloc: color = UnityTavernUiStyle.ColorFromHex(0x244C55); break;
                    case Tribe.Mech: color = UnityTavernUiStyle.ColorFromHex(0x3D434B); break;
                    case Tribe.Demon: color = UnityTavernUiStyle.ColorFromHex(0x44223E); break;
                    case Tribe.Dragon: color = UnityTavernUiStyle.ColorFromHex(0x4A2730); break;
                    case Tribe.Pirate: color = UnityTavernUiStyle.ColorFromHex(0x3B2D4F); break;
                    case Tribe.Elemental: color = UnityTavernUiStyle.ColorFromHex(0x294A44); break;
                    case Tribe.Quilboar: color = UnityTavernUiStyle.ColorFromHex(0x4A3326); break;
                    case Tribe.Undead: color = UnityTavernUiStyle.ColorFromHex(0x364029); break;
                    case Tribe.Naga: color = UnityTavernUiStyle.ColorFromHex(0x263F58); break;
                }
            }

            return new Color(color.r, color.g, color.b, defeated ? 0.48f : 0.88f);
        }

        private static string TribeName(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return "野兽";
                case Tribe.Murloc: return "鱼人";
                case Tribe.Mech: return "机械";
                case Tribe.Demon: return "恶魔";
                case Tribe.Dragon: return "龙";
                case Tribe.Pirate: return "海盗";
                case Tribe.Elemental: return "元素";
                case Tribe.Quilboar: return "野猪人";
                case Tribe.Undead: return "亡灵";
                case Tribe.Naga: return "纳迦";
                case Tribe.All: return "全部种族";
                case Tribe.None: return "中立";
                default: return tribe.ToString();
            }
        }

        private static string KeywordName(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Taunt: return "嘲讽";
                case Keyword.DivineShield: return "圣盾";
                case Keyword.Poisonous: return "剧毒";
                case Keyword.Venomous: return "烈毒";
                case Keyword.Reborn: return "复生";
                case Keyword.Deathrattle: return "亡语";
                case Keyword.Battlecry: return "战吼";
                case Keyword.Windfury: return "风怒";
                case Keyword.Cleave: return "顺劈";
                case Keyword.Magnetic: return "磁力";
                case Keyword.Avenge: return "复仇";
                case Keyword.StartOfCombat: return "战斗开始";
                case Keyword.EndOfTurn: return "回合结束";
                case Keyword.Rally: return "进击";
                case Keyword.Spellcraft: return "塑造法术";
                case Keyword.Trigger: return "触发";
                case Keyword.BloodGem: return "鲜血宝石";
                case Keyword.Discover: return "发现";
                case Keyword.Refresh: return "刷新";
                case Keyword.Pass: return "传递";
                case Keyword.Aura: return "光环";
                case Keyword.Devour: return "吞噬";
                case Keyword.TavernSpell: return "酒馆法术";
                case Keyword.ChooseOne: return "抉择";
                case Keyword.HiddenDeathrattle: return "隐藏亡语";
                case Keyword.Stealth: return "潜行";
                case Keyword.Bounty: return "悬赏";
                default: return keyword.ToString();
            }
        }

        private static string EventTypeText(CombatEventType eventType)
        {
            switch (eventType)
            {
                case CombatEventType.CombatStarted: return "战斗开始";
                case CombatEventType.AttackDeclared: return "声明攻击";
                case CombatEventType.DamageResolved: return "伤害结算";
                case CombatEventType.DivineShieldBroken: return "圣盾破裂";
                case CombatEventType.VenomousResolved: return "烈毒结算";
                case CombatEventType.DeathQueued: return "死亡入列";
                case CombatEventType.DeathrattleResolved: return "亡语结算";
                case CombatEventType.MinionSummoned: return "随从召唤";
                case CombatEventType.RebornResolved: return "复生结算";
                case CombatEventType.RallyResolved: return "集结结算";
                case CombatEventType.AvengeProgressed: return "复仇推进";
                case CombatEventType.AvengeCounterUpdated: return "复仇计数";
                case CombatEventType.DamageTriggered: return "伤害触发";
                case CombatEventType.AttackTriggered: return "攻击触发";
                case CombatEventType.SpellcraftTemporaryApplied: return "塑造法术";
                case CombatEventType.ImmediateAttackQueued: return "立即攻击";
                case CombatEventType.WindfuryResolved: return "风怒结算";
                case CombatEventType.AttackPointerRetargeted: return "攻击指针调整";
                case CombatEventType.SummonOverflowed: return "召唤溢出";
                case CombatEventType.RebornOverflowed: return "复生溢出";
                case CombatEventType.CombatRewardQueued: return "奖励入列";
                case CombatEventType.CombatSpellCast: return "战斗法术";
                case CombatEventType.CombatEnded: return "战斗结束";
                case CombatEventType.TrinketTriggered: return "饰品触发";
                default: return eventType.ToString();
            }
        }

        private static bool IsAttackPointer(BoardSide side, int position, CombatFrame frame)
        {
            return frame != null && frame.AttackPointerSide == side && frame.AttackPointerIndex == position;
        }

       private static bool Contains(List<string> ids, string id)
       {
           return !string.IsNullOrWhiteSpace(id) &&
               ids != null &&
               ids.Any(candidate => string.Equals(candidate, id, StringComparison.OrdinalIgnoreCase));
       }

        private static int Count(List<string> ids)
        {
            return ids == null ? 0 : ids.Count;
        }

        private bool HasPrefabReferences()
        {
            return titleText != null
                || summaryText != null
                || frameText != null
                || controlParent != null
                || eventHighlightParent != null
                || playerBoardParent != null
                || opponentBoardParent != null
                || timelineParent != null
                || closeButton != null;
        }

        private void ConfigureChromeFromReferences()
        {
            var header = titleText != null ? titleText.transform.parent : closeButton != null ? closeButton.transform.parent : null;
            if (header != null)
            {
                if (header.parent != null)
                {
                    ConfigurePanelChrome(header.parent.gameObject);
                }

                ConfigureHeader(header);
            }

            if (controlParent != null)
            {
                ConfigureControlsLayout(controlParent.gameObject);
            }

            if (eventHighlightParent != null)
            {
                ConfigureEventHighlightsLayout(eventHighlightParent.gameObject);
            }

            if (playerBoardParent != null)
            {
                ConfigureBoardHost(playerBoardParent.gameObject, BoardSide.Player);
            }

            if (opponentBoardParent != null)
            {
                ConfigureBoardHost(opponentBoardParent.gameObject, BoardSide.Opponent);
            }

            if (playerBoardParent != null && playerBoardParent.parent != null)
            {
                ConfigureBoardsLayout(playerBoardParent.parent.gameObject);
            }
            else if (opponentBoardParent != null && opponentBoardParent.parent != null)
            {
                ConfigureBoardsLayout(opponentBoardParent.parent.gameObject);
            }

            if (timelineParent != null)
            {
                ConfigureTimelineLayout(timelineParent.gameObject);
            }
        }

        private static GameObject ResolveScrollRoot(GameObject content)
        {
            if (content == null)
            {
                return null;
            }

            var viewport = content.transform.parent;
            if (viewport == null)
            {
                return content;
            }

            var root = viewport.parent;
            return root == null ? content : root.gameObject;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CombatReplayPanelPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(CombatReplayPanelPrefabResourcePath);
        }

        private static void ClearChildren(Transform parent)
        {
            for (var index = parent.childCount - 1; index >= 0; index -= 1)
            {
                var child = parent.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(child);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(child);
                }
            }
        }
    }

    [ExecuteAlways]
    public sealed class UnityReplayTargetingConnectorComponent : MonoBehaviour
    {
        private RectTransform root;
        private RectTransform source;
        private RectTransform target;
        private RectTransform line;
        private Image lineImage;
        private Text arrow;

        public void Configure(RectTransform rootRect, RectTransform sourceRect, RectTransform targetRect, Text arrowText)
        {
            root = rootRect;
            source = sourceRect;
            target = targetRect;
            line = transform as RectTransform;
            lineImage = GetComponent<Image>();
            arrow = arrowText;
            Canvas.willRenderCanvases -= Refresh;
            Canvas.willRenderCanvases += Refresh;
            Refresh();
        }

        private void OnEnable()
        {
            Canvas.willRenderCanvases -= Refresh;
            Canvas.willRenderCanvases += Refresh;
        }

        private void OnDisable()
        {
            Canvas.willRenderCanvases -= Refresh;
        }

        private void LateUpdate()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (root == null || source == null || target == null || line == null || lineImage == null)
            {
                return;
            }

            var sourceBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, source);
            var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(root, target);
            var direction = ((Vector2)targetBounds.center - (Vector2)sourceBounds.center).normalized;
            var start = EdgePoint(sourceBounds, direction);
            var end = EdgePoint(targetBounds, -direction);
            var delta = end - start;
            var canvas = root.GetComponentInParent<Canvas>();
            var renderedWidth = root.rect.width * (canvas == null ? 1f : canvas.scaleFactor);
            var visible = renderedWidth > 1000f && delta.sqrMagnitude >= 1f;
            lineImage.enabled = visible;
            if (arrow != null)
            {
                arrow.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            line.anchoredPosition = start;
            line.sizeDelta = new Vector2(delta.magnitude, 3f);
            line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        private static Vector2 EdgePoint(Bounds bounds, Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.001f)
            {
                return bounds.center;
            }

            var xDistance = Mathf.Abs(direction.x) < 0.001f ? float.PositiveInfinity : bounds.extents.x / Mathf.Abs(direction.x);
            var yDistance = Mathf.Abs(direction.y) < 0.001f ? float.PositiveInfinity : bounds.extents.y / Mathf.Abs(direction.y);
            return (Vector2)bounds.center + direction * Mathf.Min(xDistance, yDistance);
        }
    }
}
