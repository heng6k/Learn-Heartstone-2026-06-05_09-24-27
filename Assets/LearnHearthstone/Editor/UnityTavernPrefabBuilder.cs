using System;
using System.IO;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Editor
{
    public static class UnityTavernPrefabBuilder
    {
        [MenuItem("Learn Heartstone/UI/Rebuild Unity Tavern Prefabs")]
        public static void RebuildAllPrefabs()
        {
            var root = CreateRootPrefab();
            CreateCardSlotPrefab();
            CreateTavernCardPrefab();
            CreateBoardMinionPrefab();
            CreateZonePrefabs();
            CreatePanelPrefabs();
            CreateModalPrefabs();
            CreateReplayPrefabs();
            Selection.activeObject = root;
            Debug.Log("Unity tavern prefabs rebuilt.");
        }

        [MenuItem("Learn Heartstone/UI/Rebuild Unity Tavern Root Prefab")]
        public static void RebuildRootPrefab()
        {
            var prefab = CreateRootPrefab();
            Selection.activeObject = prefab;
            Debug.Log("Unity tavern root prefab rebuilt: " + UnityTavernTrainerView.RootPrefabAssetPath);
        }

        public static void RebuildRootPrefabBatch()
        {
            RebuildRootPrefab();
        }

        public static void RebuildAllPrefabsBatch()
        {
            RebuildAllPrefabs();
        }

        private static GameObject CreateRootPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernTrainerView.RootPrefabAssetPath));

            var root = new GameObject(
                "UnityTavernTrainer",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernTrainerController));

            try
            {
                var rect = root.GetComponent<RectTransform>();
                UnityTavernUiStyle.Stretch(rect);

                var image = root.GetComponent<Image>();
                image.color = UnityTavernUiStyle.BackWall;
                image.raycastTarget = false;

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernTrainerView.RootPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateTavernCardPrefab()
        {
            return CreateCardPrefab(
                UnityTavernCardComponent.TavernCardPrefabAssetPath,
                "TavernCard",
                UnityTavernCardMode.Shop,
                new Vector2(128f, 184f));
        }

        private static GameObject CreateBoardMinionPrefab()
        {
            return CreateCardPrefab(
                UnityTavernCardComponent.BoardMinionPrefabAssetPath,
                "BoardMinion",
                UnityTavernCardMode.Board,
                new Vector2(112f, 126f));
        }

        private static GameObject CreateCardSlotPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernCardComponent.CardSlotPrefabAssetPath));

            var slot = new GameObject("CardSlot", typeof(RectTransform), typeof(Image));
            try
            {
                var rect = slot.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(136f, 190f);

                var image = slot.GetComponent<Image>();
                image.color = new Color(0.05f, 0.065f, 0.065f, 0.62f);
                image.raycastTarget = false;

                return PrefabUtility.SaveAsPrefabAsset(slot, UnityTavernCardComponent.CardSlotPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(slot);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static void CreateZonePrefabs()
        {
            CreateZonePrefab(UnityTavernZoneComponent.ShopZonePrefabAssetPath, "ShopZone");
            CreateZonePrefab(UnityTavernZoneComponent.HandZonePrefabAssetPath, "HandZone");
            CreateZonePrefab(UnityTavernZoneComponent.PlayerBoardZonePrefabAssetPath, "PlayerBoardZone");
            CreateZonePrefab(UnityTavernZoneComponent.OpponentBoardZonePrefabAssetPath, "OpponentBoardZone");
        }

        private static void CreatePanelPrefabs()
        {
            CreateRightPanelPrefab();
            CreateActionPanelPrefab();
            CreateSelectedCardPanelPrefab();
            CreateAdvisorPanelPrefab();
            CreateLogPanelPrefab(UnityTavernLogPanelComponent.RecruitLogPanelPrefabAssetPath, "RecruitLogPanel", "招募日志");
            CreateLogPanelPrefab(UnityTavernLogPanelComponent.CombatLogPanelPrefabAssetPath, "CombatLogPanel", "战斗日志");
        }

        private static void CreateModalPrefabs()
        {
            CreateDiscoverModalPrefab();
            CreateCardDetailModalPrefab();
            CreateToolsModalPrefab();
            CreateErrorToastPrefab();
        }

        private static void CreateReplayPrefabs()
        {
            CreateCombatReplayPanelPrefab();
        }

        private static GameObject CreateZonePrefab(string assetPath, string prefabName)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath));

            var zone = new GameObject(prefabName, typeof(RectTransform), typeof(Image), typeof(UnityTavernZoneComponent));
            try
            {
                var image = zone.GetComponent<Image>();
                image.color = UnityTavernUiStyle.Panel;
                image.raycastTarget = false;

                var vertical = zone.AddComponent<VerticalLayoutGroup>();
                vertical.padding = new RectOffset(12, 12, 10, 12);
                vertical.spacing = 8;
                vertical.childControlWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandWidth = true;
                vertical.childForceExpandHeight = false;

                var header = new GameObject("UnityZoneHeader", typeof(RectTransform));
                header.transform.SetParent(zone.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(header, 28f);
                var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 8;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = true;
                headerLayout.childForceExpandHeight = true;

                var titleText = CreateText("UnityZoneTitle", header.transform, "区域", 15, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Text);
                var subtitleText = CreateText("UnityZoneSubtitle", header.transform, "0/0", 11, FontStyle.Bold, TextAnchor.MiddleRight, UnityTavernUiStyle.MutedText);

                var row = new GameObject("UnityZoneCardRow", typeof(RectTransform));
                row.transform.SetParent(zone.transform, false);
                UnityTavernUiStyle.SetFlexible(row, 1f, 1f);
                var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8;
                rowLayout.childControlWidth = false;
                rowLayout.childControlHeight = false;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;

                zone.GetComponent<UnityTavernZoneComponent>().ConfigureReferences(
                    title: titleText,
                    subtitle: subtitleText,
                    slots: row.transform,
                    slotPrefabAsset: AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardComponent.CardSlotPrefabAssetPath),
                    tavernCardPrefabAsset: AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardComponent.TavernCardPrefabAssetPath),
                    boardMinionPrefabAsset: AssetDatabase.LoadAssetAtPath<GameObject>(UnityTavernCardComponent.BoardMinionPrefabAssetPath));

                return PrefabUtility.SaveAsPrefabAsset(zone, assetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(zone);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateRightPanelPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernRightPanelComponent.RightPanelPrefabAssetPath));

            var root = new GameObject(
                "RightInspectorPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernRightPanelComponent));
            try
            {
                var image = root.GetComponent<Image>();
                image.color = UnityTavernUiStyle.PanelQuiet;
                image.raycastTarget = false;

                var layout = root.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(14, 14, 14, 14);
                layout.spacing = 10;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                var header = new GameObject("UnityRightPanelHeader", typeof(RectTransform));
                header.transform.SetParent(root.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(header, 30f);
                var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 8;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = true;
                headerLayout.childForceExpandHeight = true;

                var title = CreateText("UnityRightPanelTitle", header.transform, "功能面板", 18, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Text);
                UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);

                var floatingToggle = CreateFloatingToggle(header.transform, out var floatingToggleText);

                var action = CreatePanelSection("UnityRightPanelActionHost", root.transform, 190f, 0f);
                var detail = CreatePanelSection("UnityRightPanelDetailHost", root.transform, 354f, 0f);
                var advisor = CreatePanelSection("UnityRightPanelAdvisorHost", root.transform, 112f, 0f);
                var log = CreatePanelSection("UnityRightPanelLogHost", root.transform, 0f, 1f);

                root.GetComponent<UnityTavernRightPanelComponent>().ConfigureReferences(
                    title: title,
                    actions: action.transform,
                    detail: detail.transform,
                    advisor: advisor.transform,
                    log: log.transform,
                    floatingToggle: floatingToggle,
                    floatingToggleLabel: floatingToggleText);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernRightPanelComponent.RightPanelPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateActionPanelPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernActionPanelComponent.ActionPanelPrefabAssetPath));

            var root = new GameObject(
                "ActionPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernActionPanelComponent));
            try
            {
                var image = root.GetComponent<Image>();
                image.color = UnityTavernUiStyle.Panel;
                image.raycastTarget = false;

                var grid = new GameObject("UnityActionButtonGrid", typeof(RectTransform));
                grid.transform.SetParent(root.transform, false);
                UnityTavernUiStyle.Stretch(grid.GetComponent<RectTransform>());
                UnityTavernActionPanelComponent.ConfigureGrid(grid);

                root.GetComponent<UnityTavernActionPanelComponent>().ConfigureReferences(grid.transform);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernActionPanelComponent.ActionPanelPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateSelectedCardPanelPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernSelectedCardPanelComponent.SelectedCardPanelPrefabAssetPath));

            var root = new GameObject(
                "SelectedCardDetailPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernSelectedCardPanelComponent));
            try
            {
                var image = root.GetComponent<Image>();
                image.color = UnityTavernUiStyle.Panel;
                image.raycastTarget = false;

                var content = new GameObject("UnitySelectedCardContent", typeof(RectTransform));
                content.transform.SetParent(root.transform, false);
                UnityTavernUiStyle.Stretch(content.GetComponent<RectTransform>());
                UnityTavernSelectedCardPanelComponent.ConfigureLayout(content);

                root.GetComponent<UnityTavernSelectedCardPanelComponent>().ConfigureReferences(content.transform);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernSelectedCardPanelComponent.SelectedCardPanelPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateAdvisorPanelPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernAdvisorPanelComponent.AdvisorPanelPrefabAssetPath));

            var root = new GameObject(
                "AdvisorPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernAdvisorPanelComponent));
            try
            {
                var image = root.GetComponent<Image>();
                image.color = UnityTavernUiStyle.Panel;
                image.raycastTarget = false;
                UnityTavernAdvisorPanelComponent.ConfigureRootLayout(root);

                var title = CreateText("UnityAdvisorTitle", root.transform, "建议", 14, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Gold);
                UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 22f);

                var content = new GameObject("UnityAdvisorContent", typeof(RectTransform));
                content.transform.SetParent(root.transform, false);
                UnityTavernUiStyle.SetFlexible(content, 1f, 1f);
                UnityTavernAdvisorPanelComponent.ConfigureContentLayout(content);

                root.GetComponent<UnityTavernAdvisorPanelComponent>().ConfigureReferences(title, content.transform);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernAdvisorPanelComponent.AdvisorPanelPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateLogPanelPrefab(string assetPath, string prefabName, string titleValue)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath));

            var root = new GameObject(
                prefabName,
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernLogPanelComponent));
            try
            {
                var image = root.GetComponent<Image>();
                image.color = UnityTavernUiStyle.Panel;
                image.raycastTarget = false;
                UnityTavernLogPanelComponent.ConfigureRootLayout(root);

                var title = CreateText("UnityLogTitle", root.transform, titleValue, 13, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Gold);
                UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 22f);

                var content = UiFactory.ScrollView("UnityLogScrollView", root.transform, UnityTavernUiStyle.Panel, out var scrollRect);
                UnityTavernLogPanelComponent.ConfigureContentLayout(content.gameObject);

                root.GetComponent<UnityTavernLogPanelComponent>().ConfigureReferences(title, content, scrollRect);

                return PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateDiscoverModalPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernDiscoverModalComponent.DiscoverModalPrefabAssetPath));

            var root = new GameObject(
                "DiscoverModal",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernDiscoverModalComponent));
            try
            {
                UnityTavernUiStyle.Stretch(root.GetComponent<RectTransform>());
                var overlay = root.GetComponent<Image>();
                overlay.color = new Color(0f, 0f, 0f, 0.56f);
                overlay.raycastTarget = true;

                var panel = new GameObject("UnityDiscoverPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                panel.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;
                var panelRect = panel.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(560f, 310f);
                panelRect.anchoredPosition = Vector2.zero;

                var layout = panel.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 16, 18);
                layout.spacing = 12;
                layout.childControlWidth = true;
                layout.childControlHeight = true;

                var title = CreateText("UnityDiscoverTitle", panel.transform, "发现奖励", 20, FontStyle.Bold, TextAnchor.MiddleCenter, UnityTavernUiStyle.Text);
                UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 34f);

                var options = new GameObject("UnityDiscoverOptions", typeof(RectTransform));
                options.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetFlexible(options, 1f, 1f);
                var row = options.AddComponent<HorizontalLayoutGroup>();
                row.spacing = 10;
                row.childControlWidth = false;
                row.childControlHeight = false;

                root.GetComponent<UnityTavernDiscoverModalComponent>().ConfigureReferences(
                    title: title,
                    options: options.transform);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernDiscoverModalComponent.DiscoverModalPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateCardDetailModalPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernCardDetailModalComponent.CardDetailModalPrefabAssetPath));

            var root = new GameObject(
                "CardDetailModal",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernCardDetailModalComponent));
            try
            {
                UnityTavernCardDetailModalComponent.ConfigureOverlay(root);

                var panel = new GameObject("UnityCardDetailPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                UnityTavernCardDetailModalComponent.ConfigurePanel(panel.GetComponent<RectTransform>());
                panel.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;

                var layout = panel.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 16, 18);
                layout.spacing = 12;
                layout.childControlWidth = true;
                layout.childControlHeight = true;

                var header = new GameObject("UnityCardDetailHeader", typeof(RectTransform));
                header.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(header, 34f);
                var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 8;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = true;

                var title = CreateText("UnityCardDetailTitle", header.transform, "卡牌详情", 20, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Text);
                UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);
                var close = CreateModalButton("UnityCardDetailCloseButton", header.transform, "关闭", 84f, out var closeText);

                var body = new GameObject("UnityCardDetailBody", typeof(RectTransform));
                body.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetFlexible(body, 1f, 1f);
                var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
                bodyLayout.spacing = 14;
                bodyLayout.childControlWidth = true;
                bodyLayout.childControlHeight = true;
                bodyLayout.childForceExpandWidth = false;

                var card = new GameObject("UnityCardDetailCardHost", typeof(RectTransform));
                card.transform.SetParent(body.transform, false);
                UnityTavernUiStyle.SetFixedSize(card, 170f, 330f);

                var info = new GameObject("UnityCardDetailInfo", typeof(RectTransform));
                info.transform.SetParent(body.transform, false);
                UnityTavernUiStyle.SetFlexible(info, 1f, 1f);
                UnityTavernCardDetailModalComponent.ConfigureInfoLayout(info);

                root.GetComponent<UnityTavernCardDetailModalComponent>().ConfigureReferences(
                    title: title,
                    card: card.transform,
                    info: info.transform,
                    close: close,
                    closeLabel: closeText);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernCardDetailModalComponent.CardDetailModalPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateToolsModalPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernToolsModalComponent.ToolsModalPrefabAssetPath));

            var root = new GameObject(
                "TrainerToolsModal",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernToolsModalComponent));
            try
            {
                UnityTavernToolsModalComponent.ConfigureOverlay(root);

                var panel = new GameObject("UnityTrainerToolsPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                UnityTavernToolsModalComponent.ConfigurePanel(panel.GetComponent<RectTransform>());
                panel.GetComponent<Image>().color = UnityTavernUiStyle.PanelRaised;

                var layout = panel.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(18, 18, 16, 18);
                layout.spacing = 12;
                layout.childControlWidth = true;
                layout.childControlHeight = true;

                var header = new GameObject("UnityTrainerToolsHeader", typeof(RectTransform));
                header.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(header, 34f);
                var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
                headerLayout.spacing = 8;
                headerLayout.childControlWidth = true;
                headerLayout.childControlHeight = true;
                headerLayout.childForceExpandWidth = true;

                var title = CreateText("UnityTrainerToolsTitle", header.transform, "训练工具", 20, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Text);
                UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);
                var close = CreateModalButton("UnityTrainerToolsCloseButton", header.transform, "关闭", 84f, out var closeText);

                var content = UiFactory.ScrollView("UnityTrainerToolsScroll", panel.transform, UnityTavernUiStyle.Panel, out _);
                UnityTavernToolsModalComponent.ConfigureContentLayout(content.gameObject);

                root.GetComponent<UnityTavernToolsModalComponent>().ConfigureReferences(
                    title: title,
                    content: content,
                    close: close,
                    closeLabel: closeText);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernToolsModalComponent.ToolsModalPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateErrorToastPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernToastComponent.ErrorToastPrefabAssetPath));

            var root = new GameObject(
                "ErrorToast",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernToastComponent));
            try
            {
                UnityTavernToastComponent.ConfigureRect(root.GetComponent<RectTransform>());
                var image = root.GetComponent<Image>();
                image.color = new Color(0.42f, 0.10f, 0.09f, 0.94f);
                image.raycastTarget = false;

                var message = CreateText("UnityErrorToastText", root.transform, "错误", 13, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
                UnityTavernUiStyle.Stretch(message.rectTransform);
                root.GetComponent<UnityTavernToastComponent>().ConfigureReferences(message);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernToastComponent.ErrorToastPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateCombatReplayPanelPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(UnityTavernCombatReplayPanelComponent.CombatReplayPanelPrefabAssetPath));

            var root = new GameObject(
                "CombatReplayPanel",
                typeof(RectTransform),
                typeof(Image),
                typeof(UnityTavernCombatReplayPanelComponent));
            try
            {
                UnityTavernCombatReplayPanelComponent.ConfigureOverlay(root);

                var panel = new GameObject("UnityCombatReplayPanelSurface", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(root.transform, false);
                UnityTavernCombatReplayPanelComponent.ConfigurePanel(panel.GetComponent<RectTransform>());
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

                var title = CreateText("UnityCombatReplayTitle", header.transform, "战斗回放", 20, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Text);
                UnityTavernUiStyle.SetFlexible(title.gameObject, 1f, 0f);
                var close = CreateModalButton("UnityCombatReplayCloseButton", header.transform, "关闭", 84f, out var closeText);

                var summary = CreateText("UnityCombatReplaySummary", panel.transform, "暂无回放帧。", 13, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.Gold);
                UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, 26f);

                var controls = new GameObject("UnityCombatReplayControls", typeof(RectTransform));
                controls.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(controls, 34f);
                var controlsLayout = controls.AddComponent<HorizontalLayoutGroup>();
                controlsLayout.spacing = 8;
                controlsLayout.childControlWidth = false;
                controlsLayout.childControlHeight = true;

                var frame = CreateText("UnityCombatReplayFrameText", panel.transform, "运行战斗后可查看回放帧。", 13, FontStyle.Bold, TextAnchor.MiddleLeft, UnityTavernUiStyle.MutedText);
                UnityTavernUiStyle.SetPreferredHeight(frame.gameObject, 34f);

                var eventHighlights = new GameObject("UnityCombatReplayEventHighlights", typeof(RectTransform));
                eventHighlights.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(eventHighlights, 30f);
                UnityTavernCombatReplayPanelComponent.ConfigureEventHighlightsLayout(eventHighlights);

                var boards = new GameObject("UnityCombatReplayBoards", typeof(RectTransform));
                boards.transform.SetParent(panel.transform, false);
                UnityTavernUiStyle.SetPreferredHeight(boards, 170f);
                var boardsLayout = boards.AddComponent<HorizontalLayoutGroup>();
                boardsLayout.spacing = 10;
                boardsLayout.childControlWidth = true;
                boardsLayout.childControlHeight = true;
                boardsLayout.childForceExpandWidth = true;
                boardsLayout.childForceExpandHeight = true;

                var playerBoard = CreateReplayBoardHost("UnityCombatReplayPlayerBoard", boards.transform);
                var opponentBoard = CreateReplayBoardHost("UnityCombatReplayOpponentBoard", boards.transform);

                var timeline = UiFactory.ScrollView("UnityCombatReplayTimeline", panel.transform, UnityTavernUiStyle.Panel, out _);
                UnityTavernCombatReplayPanelComponent.ConfigureTimelineLayout(timeline.gameObject);

                root.GetComponent<UnityTavernCombatReplayPanelComponent>().ConfigureReferences(
                    title: title,
                    summary: summary,
                    frame: frame,
                    controls: controls.transform,
                    eventHighlights: eventHighlights.transform,
                    playerBoard: playerBoard.transform,
                    opponentBoard: opponentBoard.transform,
                    timeline: timeline,
                    close: close,
                    closeLabel: closeText);

                return PrefabUtility.SaveAsPrefabAsset(root, UnityTavernCombatReplayPanelComponent.CombatReplayPanelPrefabAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static GameObject CreateCardPrefab(string assetPath, string prefabName, UnityTavernCardMode mode, Vector2 size)
        {
            EnsureFolder(Path.GetDirectoryName(assetPath));

            var root = new GameObject(prefabName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = size;

                var frame = root.GetComponent<Image>();
                frame.color = mode == UnityTavernCardMode.Board
                    ? UnityTavernUiStyle.ColorFromHex(0x3A2B20)
                    : UnityTavernUiStyle.ColorFromHex(0x34281E);
                frame.raycastTarget = true;

                var rootButton = root.GetComponent<Button>();
                rootButton.targetGraphic = frame;
                UnityTavernUiStyle.TintSelectable(
                    rootButton,
                    Color.white,
                    new Color(1f, 0.94f, 0.72f, 1f),
                    new Color(0.84f, 0.76f, 0.54f, 1f));

                var outline = root.AddComponent<Outline>();
                outline.enabled = false;
                outline.effectColor = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.82f);
                outline.effectDistance = new Vector2(3f, -3f);
                outline.useGraphicAlpha = false;

                var shadow = root.AddComponent<Shadow>();
                shadow.enabled = false;
                shadow.effectColor = new Color(0f, 0f, 0f, 0.34f);
                shadow.effectDistance = new Vector2(2f, -2f);
                shadow.useGraphicAlpha = true;

                var art = CreateImage("UnityCardArt", root.transform, UnityTavernUiStyle.ColorFromHex(0x4A3525), false);
                SetAnchors(
                    art.rectTransform,
                    mode == UnityTavernCardMode.Board ? new Vector2(0.06f, 0.20f) : new Vector2(0.06f, 0.28f),
                    new Vector2(0.94f, 0.92f),
                    Vector2.zero,
                    Vector2.zero);
                art.preserveAspect = true;

                var tierBadge = CreateBadge(
                    "UnityTierBadge",
                    root.transform,
                    "1",
                    UnityTavernUiStyle.Gold,
                    new Vector2(0f, 1f),
                    mode == UnityTavernCardMode.Board ? new Vector2(17f, -17f) : new Vector2(19f, -19f),
                    out var tierText);

                var kindText = CreateText("UnityCardKind", root.transform, "种族", 9, FontStyle.Bold, TextAnchor.MiddleRight, UnityTavernUiStyle.MutedText);
                SetAnchors(
                    kindText.rectTransform,
                    new Vector2(0.34f, 1f),
                    new Vector2(0.94f, 1f),
                    new Vector2(0f, -36f),
                    new Vector2(0f, -10f));

                var nameText = CreateText("UnityCardName", root.transform, "随从名", mode == UnityTavernCardMode.Board ? 11 : 12, FontStyle.Bold, TextAnchor.MiddleCenter, UnityTavernUiStyle.Text);
                SetAnchors(
                    nameText.rectTransform,
                    new Vector2(0.08f, 0f),
                    new Vector2(0.92f, 0f),
                    new Vector2(0f, 42f),
                    new Vector2(0f, 72f));

                var subtitleText = CreateText("UnityCardSubtitle", root.transform, "关键词", 9, FontStyle.Bold, TextAnchor.MiddleCenter, UnityTavernUiStyle.Gold);
                SetAnchors(
                    subtitleText.rectTransform,
                    new Vector2(0.08f, 0f),
                    new Vector2(0.92f, 0f),
                    new Vector2(0f, 24f),
                    new Vector2(0f, 42f));

                var attackBadge = CreateBadge("UnityAttackBadge", root.transform, "0", UnityTavernUiStyle.ColorFromHex(0xBA6A31), new Vector2(0f, 0f), new Vector2(19f, 20f), out var attackText);
                var healthBadge = CreateBadge("UnityHealthBadge", root.transform, "0", UnityTavernUiStyle.Red, new Vector2(1f, 0f), new Vector2(-19f, 20f), out var healthText);
                var costBadge = CreateBadge("UnityCostBadge", root.transform, "0", UnityTavernUiStyle.Blue, new Vector2(1f, 0f), new Vector2(-19f, 20f), out var costText);
                costBadge.SetActive(false);

                var action = new GameObject("UnityCardAction", typeof(RectTransform), typeof(Image), typeof(Button));
                action.transform.SetParent(root.transform, false);
                SetAnchors(
                    action.GetComponent<RectTransform>(),
                    new Vector2(0.14f, 0f),
                    new Vector2(0.86f, 0f),
                    new Vector2(0f, 4f),
                    new Vector2(0f, 28f));
                var actionImage = action.GetComponent<Image>();
                actionImage.color = new Color(0.09f, 0.12f, 0.12f, 0.9f);
                var actionButton = action.GetComponent<Button>();
                actionButton.targetGraphic = actionImage;
                var actionText = CreateText("UnityCardActionText", action.transform, "操作", 11, FontStyle.Bold, TextAnchor.MiddleCenter, UnityTavernUiStyle.Text);
                UnityTavernUiStyle.Stretch(actionText.rectTransform);
                action.SetActive(false);

                root.GetComponent<UnityTavernCardComponent>().ConfigureReferences(
                    frame: frame,
                    art: art,
                    name: nameText,
                    subtitle: subtitleText,
                    kind: kindText,
                    tier: tierText,
                    attack: attackText,
                    health: healthText,
                    cost: costText,
                    rootButton: rootButton,
                    primaryButton: actionButton,
                    primaryText: actionText,
                    tierBadgeObject: tierBadge,
                    attackBadgeObject: attackBadge,
                    healthBadgeObject: healthBadge,
                    costBadgeObject: costBadge);

                return PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(Image));
            target.transform.SetParent(parent, false);
            var image = target.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static GameObject CreatePanelSection(string name, Transform parent, float preferredHeight, float flexibleHeight)
        {
            var section = new GameObject(name, typeof(RectTransform));
            section.transform.SetParent(parent, false);
            if (preferredHeight > 0f)
            {
                UnityTavernUiStyle.SetPreferredHeight(section, preferredHeight);
            }

            if (flexibleHeight > 0f)
            {
                UnityTavernUiStyle.SetFlexible(section, 1f, flexibleHeight);
            }

            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            return section;
        }

        private static Button CreateModalButton(string name, Transform parent, string value, float width, out Text label)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, width, 32f);

            var image = buttonObject.GetComponent<Image>();
            image.color = UnityTavernUiStyle.Panel;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            label = CreateText(name + "Text", buttonObject.transform, value, 12, FontStyle.Bold, TextAnchor.MiddleCenter, UnityTavernUiStyle.Text);
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static GameObject CreateReplayBoardHost(string name, Transform parent)
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

        private static Button CreateFloatingToggle(Transform parent, out Text label)
        {
            var buttonObject = new GameObject("UnityRightPanelFloatToggle", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, 76f, 30f);

            var image = buttonObject.GetComponent<Image>();
            image.color = UnityTavernUiStyle.PanelRaised;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.91f, 0.62f, 1f),
                new Color(0.72f, 0.62f, 0.42f, 1f));

            label = CreateText(
                "UnityRightPanelFloatToggleText",
                buttonObject.transform,
                "展开",
                12,
                FontStyle.Bold,
                TextAnchor.MiddleCenter,
                UnityTavernUiStyle.Text);
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style, TextAnchor alignment, Color color)
        {
            var label = UiFactory.Label(name, parent, text, size, style);
            label.alignment = alignment;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static GameObject CreateBadge(string name, Transform parent, string value, Color color, Vector2 anchor, Vector2 position, out Text text)
        {
            var badge = new GameObject(name, typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(parent, false);
            var rect = badge.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(34f, 34f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;

            var image = badge.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            text = CreateText(name + "Text", badge.transform, value, 16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            UnityTavernUiStyle.Stretch(text.rectTransform);
            return badge;
        }

        private static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                throw new ArgumentException("Folder path is required.", nameof(folderPath));
            }

            var normalized = folderPath.Replace("\\", "/");
            var parts = normalized.Split('/');
            var current = parts[0];
            for (var index = 1; index < parts.Length; index += 1)
            {
                var next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }
    }
}
