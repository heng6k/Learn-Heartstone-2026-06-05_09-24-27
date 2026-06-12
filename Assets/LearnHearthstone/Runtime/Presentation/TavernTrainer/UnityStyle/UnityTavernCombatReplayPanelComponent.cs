using System;
using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernCombatReplayPanelComponent : MonoBehaviour
    {
        public const string CombatReplayPanelPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Replay/CombatReplayPanel.prefab";
        public const string CombatReplayPanelPrefabResourcePath = "TavernTrainer/UnityStyle/Replay/CombatReplayPanel";

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
            ConfigureOverlay(gameObject);
            if (HasPrefabReferences())
            {
                BuildPrefab(replay, frameIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed, close);
                return;
            }

            BuildGenerated(replay, frameIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed, close);
        }

        public static void ConfigureOverlay(GameObject target)
        {
            UnityTavernUiStyle.Stretch(target.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.EnsureComponent<Image>(target);
            image.color = new Color(0f, 0f, 0f, 0.46f);
            image.raycastTarget = true;
        }

        public static void ConfigurePanel(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(920f, 540f);
            rect.anchoredPosition = Vector2.zero;
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

        public static void ConfigureTimelineLayout(GameObject target)
        {
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
            ConfigureClose(close);
            var hasFrames = replay != null && replay.Frames != null && replay.Frames.Count > 0;
            var clampedIndex = hasFrames ? Mathf.Clamp(frameIndex, 0, replay.Frames.Count - 1) : 0;
            var frame = hasFrames ? replay.Frames[clampedIndex] : null;
            var previousFrame = hasFrames && clampedIndex > 0 ? replay.Frames[clampedIndex - 1] : null;

            SetText(titleText, "战斗回放");
            SetText(summaryText, hasFrames ? "种子 " + replay.Seed + "  结果 " + ResultText(replay.Result) + "  帧 " + (clampedIndex + 1) + "/" + replay.Frames.Count : "暂无回放帧。");
            SetText(frameText, frame == null ? "运行战斗后可查看回放帧。" : (clampedIndex + 1) + ". " + EventTypeText(frame.EventType) + "  " + frame.LogText);

            BuildControls(controlParent, replay, clampedIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed);
            BuildEventHighlights(eventHighlightParent, frame);
            BuildBoard(playerBoardParent, BoardSide.Player, "Player", frame == null ? null : frame.PlayerBoardSnapshot, previousFrame == null ? null : previousFrame.PlayerBoardSnapshot, frame);
            BuildBoard(opponentBoardParent, BoardSide.Opponent, "Opponent", frame == null ? null : frame.OpponentBoardSnapshot, previousFrame == null ? null : previousFrame.OpponentBoardSnapshot, frame);
            BuildTimeline(timelineParent, replay, clampedIndex, setFrame);
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
            panel.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 10;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var header = new GameObject("UnityCombatReplayHeader", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(header, 34f);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;

            titleText = UiFactory.Label("UnityCombatReplayTitle", header.transform, "战斗回放", 20, FontStyle.Bold);
            titleText.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.SetFlexible(titleText.gameObject, 1f, 0f);
            closeButton = CreateButton("UnityCombatReplayCloseButton", header.transform, "关闭", () => close?.Invoke(), 84f);
            closeButtonText = closeButton.GetComponentInChildren<Text>();

            summaryText = UiFactory.Label("UnityCombatReplaySummary", panel.transform, string.Empty, 13, FontStyle.Bold);
            summaryText.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(summaryText.gameObject, 26f);

            controlParent = new GameObject("UnityCombatReplayControls", typeof(RectTransform)).transform;
            controlParent.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(controlParent.gameObject, 34f);
            var controlsLayout = controlParent.gameObject.AddComponent<HorizontalLayoutGroup>();
            controlsLayout.spacing = 8;
            controlsLayout.childControlWidth = false;
            controlsLayout.childControlHeight = true;

            frameText = UiFactory.Label("UnityCombatReplayFrameText", panel.transform, string.Empty, 13, FontStyle.Bold);
            frameText.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(frameText.gameObject, 34f);

            eventHighlightParent = new GameObject("UnityCombatReplayEventHighlights", typeof(RectTransform)).transform;
            eventHighlightParent.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(eventHighlightParent.gameObject, 30f);
            ConfigureEventHighlightsLayout(eventHighlightParent.gameObject);

            var boards = new GameObject("UnityCombatReplayBoards", typeof(RectTransform));
            boards.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(boards, 170f);
            var boardsLayout = boards.AddComponent<HorizontalLayoutGroup>();
            boardsLayout.spacing = 10;
            boardsLayout.childControlWidth = true;
            boardsLayout.childControlHeight = true;
            boardsLayout.childForceExpandWidth = true;
            boardsLayout.childForceExpandHeight = true;

            playerBoardParent = CreateBoardHost("UnityCombatReplayPlayerBoard", boards.transform).transform;
            opponentBoardParent = CreateBoardHost("UnityCombatReplayOpponentBoard", boards.transform).transform;

            timelineParent = UiFactory.ScrollView("UnityCombatReplayTimeline", panel.transform, UnityTavernUiStyle.Panel, out _);
            ConfigureTimelineLayout(timelineParent.gameObject);

            BuildPrefab(replay, frameIndex, replayPlaying, speedLabel, setFrame, togglePlayback, cycleSpeed, close);
        }

        private void ConfigureClose(Action close)
        {
            if (closeButton == null)
            {
                return;
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => close?.Invoke());
            SetText(closeButtonText, "关闭");
        }

        public static void ConfigureEventHighlightsLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<HorizontalLayoutGroup>(target);
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
            var hasFrames = replay != null && replay.Frames != null && replay.Frames.Count > 0;
            var lastIndex = hasFrames ? replay.Frames.Count - 1 : 0;
            CreateButton("UnityReplayFirstButton", parent, "|<", () => setFrame?.Invoke(0), 58f).interactable = hasFrames && frameIndex > 0;
            CreateButton("UnityReplayPrevButton", parent, "<", () => setFrame?.Invoke(frameIndex - 1), 58f).interactable = hasFrames && frameIndex > 0;
            CreateButton("UnityReplayPlayPauseButton", parent, replayPlaying ? "暂停" : "播放", () => togglePlayback?.Invoke(), 74f).interactable = hasFrames;
            CreateButton("UnityReplayNextButton", parent, ">", () => setFrame?.Invoke(frameIndex + 1), 58f).interactable = hasFrames && frameIndex < lastIndex;
            CreateButton("UnityReplayLastButton", parent, ">|", () => setFrame?.Invoke(lastIndex), 58f).interactable = hasFrames && frameIndex < lastIndex;
            CreateButton("UnityReplaySpeedButton", parent, "速度 " + (string.IsNullOrEmpty(speedLabel) ? "1x" : speedLabel), () => cycleSpeed?.Invoke(), 92f).interactable = hasFrames;
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
                AddEventChip(parent, "Actor", "攻击方", UnityTavernUiStyle.Green);
            }

            if (!string.IsNullOrEmpty(frame.TargetId))
            {
                AddEventChip(parent, "Target", "目标", UnityTavernUiStyle.Red);
            }

            AddCountChip(parent, "Damage", "伤害", frame.DamagedEntityIds, UnityTavernUiStyle.Red);
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
            var heading = UiFactory.Label("UnityReplay" + title + "Title", parent, BoardTitleText(title) + " " + (snapshot == null ? "0/7" : snapshot.Minions.Count + "/7"), 13, FontStyle.Bold);
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
            var tileColor = ReplayHighlightColor(side, minion.Position, minion.InstanceId, frame);
            tile.GetComponent<Image>().color = tileColor;
            UnityTavernUiStyle.SetFlexible(tile, 1f, 1f);
            var layout = tile.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 2;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            AddTileLine(tile.transform, minion.Name, 10, FontStyle.Bold, UnityTavernUiStyle.Text, 32f);
            AddTileLine(tile.transform, minion.Attack + "/" + minion.Health, 11, FontStyle.Bold, UnityTavernUiStyle.Gold, 22f);
            AddTileLine(tile.transform, string.Join(" ", minion.Keywords.Take(2).Select(keyword => keyword.ToString()).ToArray()), 9, FontStyle.Normal, UnityTavernUiStyle.MutedText, 24f);
            ConfigureTileMotion(tile, side, minion.InstanceId, tileColor, frame);
        }

        private static void BuildDeathMarker(Transform parent, BoardSide side, CombatMinionSnapshot minion)
        {
            var tile = new GameObject("UnityReplayDeathMarker-" + minion.InstanceId, typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            var tileColor = new Color(0.24f, 0.08f, 0.08f, 0.92f);
            tile.GetComponent<Image>().color = tileColor;
            UnityTavernUiStyle.SetFlexible(tile, 1f, 1f);
            var layout = tile.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(5, 5, 5, 5);
            layout.spacing = 2;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            AddTileLine(tile.transform, minion.Name, 10, FontStyle.Bold, UnityTavernUiStyle.Text, 32f);
            AddTileLine(tile.transform, "阵亡", 11, FontStyle.Bold, UnityTavernUiStyle.Red, 22f);
            AddTileLine(tile.transform, minion.Attack + "/" + minion.Health, 9, FontStyle.Normal, UnityTavernUiStyle.MutedText, 24f);
            UnityTavernUiStyle.EnsureComponent<UnityTavernReplayTileAnimator>(tile)
                .Configure(UnityTavernReplayTileMotion.Death, tileColor, MotionDirection(side));
        }

        private static void BuildEmptySlot(Transform parent, BoardSide side, int slot, CombatFrame frame)
        {
            var tile = new GameObject("UnityReplayEmptySlot-" + side + "-" + slot, typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(parent, false);
            tile.GetComponent<Image>().color = IsAttackPointer(side, slot, frame)
                ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.36f)
                : UnityTavernUiStyle.PanelQuiet;
            UnityTavernUiStyle.SetFlexible(tile, 1f, 1f);
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
                    (target + 1) + ". " + EventTypeText(item.EventType) + "  " + item.LogText,
                    target == frameIndex,
                    EventTypeColor(item.EventType),
                    () => setFrame?.Invoke(target));
            }
        }

        private static void AddTimelineLine(Transform parent, string text, bool selected, Color eventColor, Action onClick)
        {
            var button = CreateButton("UnityReplayEventLine", parent, text, onClick, 0f);
            UnityTavernUiStyle.SetPreferredHeight(button.gameObject, 28f);
            var image = button.GetComponent<Image>();
            image.color = selected ? UnityTavernUiStyle.Blue : new Color(eventColor.r, eventColor.g, eventColor.b, 0.56f);
        }

        private static void AddEventChip(Transform parent, string suffix, string text, Color color)
        {
            var chip = new GameObject("UnityReplayEventChip-" + suffix, typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            chip.GetComponent<Image>().color = color;
            UnityTavernUiStyle.SetFixedSize(chip, Mathf.Clamp(42f + (text ?? string.Empty).Length * 5.5f, 78f, 136f), 26f);

            var label = UiFactory.Label("UnityReplayEventChipText-" + suffix, chip.transform, text, 11, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
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

        private static Text AddTileLine(Transform parent, string text, int size, FontStyle style, Color color, float height)
        {
            var label = UiFactory.Label("UnityReplayTileLine", parent, text ?? string.Empty, size, style);
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, height);
            return label;
        }

        private static GameObject CreateBoardHost(string name, Transform parent)
        {
            var board = new GameObject(name, typeof(RectTransform), typeof(Image));
            board.transform.SetParent(parent, false);
            board.GetComponent<Image>().color = UnityTavernUiStyle.Panel;
            UnityTavernUiStyle.SetFlexible(board, 1f, 1f);
            var layout = board.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return board;
        }

        private static Button CreateButton(string name, Transform parent, string text, Action onClick, float width)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;
            if (width > 0f)
            {
                UnityTavernUiStyle.SetFixedSize(buttonObject, width, 32f);
            }

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(() => onClick?.Invoke());
            UnityTavernUiStyle.TintSelectable(button, Color.white, new Color(1f, 0.91f, 0.62f, 1f), new Color(0.72f, 0.62f, 0.42f, 1f));

            var label = UiFactory.Label(name + "Text", buttonObject.transform, text, 12, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static Color ReplayHighlightColor(BoardSide side, int position, string instanceId, CombatFrame frame)
        {
            if (frame == null || string.IsNullOrEmpty(instanceId))
            {
                return UnityTavernUiStyle.PanelQuiet;
            }

            if (frame.ActorId == instanceId)
            {
                return UnityTavernUiStyle.Green;
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
                    return UnityTavernUiStyle.Gold;
                default:
                    return UnityTavernUiStyle.PanelRaised;
            }
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
                default: return eventType.ToString();
            }
        }

        private static bool IsAttackPointer(BoardSide side, int position, CombatFrame frame)
        {
            return frame != null && frame.AttackPointerSide == side && frame.AttackPointerIndex == position;
        }

        private static bool Contains(List<string> ids, string id)
        {
            return ids != null && ids.Contains(id);
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
}
