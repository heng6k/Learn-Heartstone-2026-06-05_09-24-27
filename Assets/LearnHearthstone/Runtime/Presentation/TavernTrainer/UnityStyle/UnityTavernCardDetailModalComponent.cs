using System;
using System.Linq;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernCardDetailModalComponent : MonoBehaviour
    {
        public const string CardDetailModalPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Modals/CardDetailModal.prefab";
        public const string CardDetailModalPrefabResourcePath = "TavernTrainer/UnityStyle/Modals/CardDetailModal";

        [SerializeField] private Text titleText;
        [SerializeField] private Transform cardParent;
        [SerializeField] private Transform infoParent;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text closeButtonText;
        private bool useEnglish;

        public static GameObject CreateModalHost(Transform parent, string fallbackName)
        {
            var prefab = ResolvePrefab();
            var modalObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(UnityTavernCardDetailModalComponent));

            modalObject.name = fallbackName;
            modalObject.transform.SetParent(parent, false);
            if (modalObject.GetComponent<Image>() == null)
            {
                modalObject.AddComponent<Image>();
            }

            if (modalObject.GetComponent<UnityTavernCardDetailModalComponent>() == null)
            {
                modalObject.AddComponent<UnityTavernCardDetailModalComponent>();
            }

            return modalObject;
        }

        public void ConfigureReferences(
            Text title = null,
            Transform card = null,
            Transform info = null,
            Button close = null,
            Text closeLabel = null)
        {
            titleText = title;
            cardParent = card;
            infoParent = info;
            closeButton = close;
            closeButtonText = closeLabel;
        }

        public void Build(MinionInstance card, Action close, bool showCardId = true, bool useEnglish = false)
        {
            this.useEnglish = useEnglish;
            ConfigureOverlay(gameObject);
            if (HasPrefabReferences())
            {
                SetText(titleText, card == null ? T("卡牌详情", "Card Details") : card.Name);
                UnityTavernUiStyle.ConfigureLabel(titleText, UnityTavernUiStyle.Gold, 20);
                ConfigureChromeFromReferences();
                ConfigureClose(close);
                BuildCard(cardParent, card);
                BuildInfo(infoParent, card, showCardId);
                return;
            }

            BuildGenerated(card, close, showCardId);
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
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(720f, 430f);
            rect.anchoredPosition = Vector2.zero;
        }

        public static void ConfigureInfoLayout(GameObject target)
        {
            var layout = UnityTavernUiStyle.EnsureComponent<VerticalLayoutGroup>(target);
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 6;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private void BuildGenerated(MinionInstance card, Action close, bool showCardId)
        {
            ClearChildren(transform);

            var panel = new GameObject("UnityCardDetailPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            ConfigurePanel(panel.GetComponent<RectTransform>());
            ConfigurePanelChrome(panel);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 16, 18);
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            var header = new GameObject("UnityCardDetailHeader", typeof(RectTransform));
            header.transform.SetParent(panel.transform, false);
            ConfigureHeaderChrome(header.transform);
            var headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = false;

            titleText = UiFactory.Label("UnityCardDetailTitle", header.transform, card == null ? T("卡牌详情", "Card Details") : card.Name, 20, FontStyle.Bold);
            UnityTavernUiStyle.ConfigureLabel(titleText, UnityTavernUiStyle.Gold, 20);
            UnityTavernUiStyle.SetFlexible(titleText.gameObject, 1f, 0f);
            closeButton = CreateCloseButton(header.transform, out closeButtonText);
            ConfigureClose(close);

            var body = new GameObject("UnityCardDetailBody", typeof(RectTransform));
            body.transform.SetParent(panel.transform, false);
            UnityTavernUiStyle.SetFlexible(body, 1f, 1f);
            var bodyLayout = body.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 14;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;

            cardParent = new GameObject("UnityCardDetailCardHost", typeof(RectTransform)).transform;
            cardParent.SetParent(body.transform, false);
            UnityTavernUiStyle.SetFixedSize(cardParent.gameObject, 170f, 330f);

            infoParent = new GameObject("UnityCardDetailInfo", typeof(RectTransform)).transform;
            infoParent.SetParent(body.transform, false);
            UnityTavernUiStyle.SetFlexible(infoParent.gameObject, 1f, 1f);
            ConfigureInfoLayout(infoParent.gameObject);

            BuildCard(cardParent, card);
            BuildInfo(infoParent, card, showCardId);
        }

        private void ConfigureClose(Action close)
        {
            if (closeButton == null)
            {
                return;
            }

            closeButtonText = closeButtonText != null ? closeButtonText : closeButton.GetComponentInChildren<Text>(true);
            UnityTavernUiStyle.SetFixedSize(closeButton.gameObject, 92f, UnityTavernUiStyle.TouchHeight);
            if (titleText != null)
            {
                UnityTavernUiStyle.SetFlexible(titleText.gameObject, 1f, 0f);
                titleText.fontSize = Math.Max(20, titleText.fontSize);
                titleText.color = UnityTavernUiStyle.Gold;
                UnityTavernUiStyle.ConfigureOutline(titleText.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(1f, -1f));
            }

            var headerLayout = closeButton.transform.parent == null
                ? null
                : closeButton.transform.parent.GetComponent<HorizontalLayoutGroup>();
            if (headerLayout != null)
            {
                headerLayout.childForceExpandWidth = false;
            }

            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => close?.Invoke());
            UnityTavernUiStyle.ConfigureButton(closeButton, UnityTavernUiStyle.Brass);
            SetText(closeButtonText, T("关闭", "Close"));
            if (closeButtonText != null)
            {
                closeButtonText.fontSize = Math.Max(14, closeButtonText.fontSize);
                closeButtonText.color = UnityTavernUiStyle.TextLight;
                UnityTavernUiStyle.ConfigureOutline(closeButtonText.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(1f, -1f));
            }
        }

        private void BuildCard(Transform parent, MinionInstance card)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            if (card == null)
            {
                return;
            }

            var cardObject = UnityTavernCardComponent.CreateCardHost(UnityTavernCardMode.Detail, parent, "UnityCardDetailCard");
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(card, UnityTavernCardMode.Detail, null, null, null, useEnglish: useEnglish);
        }

        private void BuildInfo(Transform parent, MinionInstance card, bool showCardId)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureInfoLayout(parent.gameObject);
            if (card == null)
            {
                AddLine(parent, T("暂未选择卡牌。", "No card selected."), 14, FontStyle.Bold, UnityTavernUiStyle.MutedText, 30f);
                return;
            }

            AddLine(parent, T("酒馆等级 ", "Tavern Tier ") + card.TavernTier + "  " + KindText(card), 14, FontStyle.Bold, UnityTavernUiStyle.Gold, 30f);
            AddLine(parent, card.CardKind == CardKind.TavernSpell
                ? T("消耗 ", "Cost ") + Math.Max(0, card.Cost)
                : TavernNumberFormatter.FullStats(card.Attack, card.Health) + T("（上限 ", " (Maximum ") + TavernNumberFormatter.FullNumber(card.MaxHealth) + (useEnglish ? ")" : "）"), 14, FontStyle.Bold, UnityTavernUiStyle.Text, 30f);

            var keywords = card.OfficialKeywords != null && card.OfficialKeywords.Count > 0
                ? card.OfficialKeywords
                : card.Keywords;
            AddLine(parent, keywords == null || keywords.Count == 0
                ? T("关键词：无", "Keywords: None")
                : T("关键词：", "Keywords: ") + string.Join(useEnglish ? ", " : "、", keywords.Select(KeywordName).ToArray()), 14, FontStyle.Normal, UnityTavernUiStyle.MutedText, 36f);
            if (showCardId)
            {
                AddLine(parent, T("卡牌ID：", "Card ID: ") + card.CardId, 14, FontStyle.Normal, UnityTavernUiStyle.MutedText, 28f);
            }

            var body = AddLine(parent, string.IsNullOrWhiteSpace(card.Text) ? T("暂无规则文本。", "No rules text.") : card.Text, 14, FontStyle.Normal, UnityTavernUiStyle.Text, 120f);
            body.alignment = TextAnchor.UpperLeft;
            body.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private string KindText(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return T("酒馆法术", "Tavern Spell");
            }

            if (card.Tribes == null || card.Tribes.Count == 0)
            {
                return T("随从", "Minion");
            }

            var tribes = card.Tribes.Where(tribe => tribe != Tribe.None).Select(TribeName).ToArray();
            return tribes.Length == 0 ? T("随从", "Minion") : string.Join("/", tribes);
        }

        private string TribeName(Tribe tribe)
        {
            switch (tribe)
            {
                case Tribe.Beast: return T("野兽", "Beast");
                case Tribe.Murloc: return T("鱼人", "Murloc");
                case Tribe.Mech: return T("机械", "Mech");
                case Tribe.Demon: return T("恶魔", "Demon");
                case Tribe.Dragon: return T("龙", "Dragon");
                case Tribe.Pirate: return T("海盗", "Pirate");
                case Tribe.Elemental: return T("元素", "Elemental");
                case Tribe.Quilboar: return T("野猪人", "Quilboar");
                case Tribe.Undead: return T("亡灵", "Undead");
                case Tribe.Naga: return T("纳迦", "Naga");
                case Tribe.All: return T("全部", "All");
                default: return T("中立", "Neutral");
            }
        }

        private string KeywordName(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Taunt: return T("嘲讽", "Taunt");
                case Keyword.DivineShield: return T("圣盾", "Divine Shield");
                case Keyword.Poisonous: return T("剧毒", "Poisonous");
                case Keyword.Venomous: return T("烈毒", "Venomous");
                case Keyword.Reborn: return T("复生", "Reborn");
                case Keyword.Deathrattle: return T("亡语", "Deathrattle");
                case Keyword.Battlecry: return T("战吼", "Battlecry");
                case Keyword.Windfury: return T("风怒", "Windfury");
                case Keyword.Cleave: return T("顺劈", "Cleave");
                case Keyword.Magnetic: return T("磁力", "Magnetic");
                case Keyword.Avenge: return T("复仇", "Avenge");
                case Keyword.StartOfCombat: return T("战斗开始", "Start of Combat");
                case Keyword.EndOfTurn: return T("回合结束", "End of Turn");
                case Keyword.Rally: return T("集结", "Rally");
                case Keyword.Spellcraft: return T("塑造法术", "Spellcraft");
                case Keyword.Trigger: return T("触发", "Trigger");
                case Keyword.BloodGem: return T("鲜血宝石", "Blood Gem");
                case Keyword.Discover: return T("发现", "Discover");
                case Keyword.Refresh: return T("刷新", "Refresh");
                case Keyword.Pass: return T("传递", "Pass");
                case Keyword.Aura: return T("光环", "Aura");
                case Keyword.Devour: return T("吞噬", "Devour");
                case Keyword.TavernSpell: return T("酒馆法术", "Tavern Spell");
                case Keyword.ChooseOne: return T("抉择", "Choose One");
                case Keyword.HiddenDeathrattle: return T("隐藏亡语", "Hidden Deathrattle");
                case Keyword.Stealth: return T("潜行", "Stealth");
                case Keyword.Bounty: return T("悬赏", "Bounty");
                default: return keyword.ToString();
            }
        }

        private string T(string chinese, string english)
        {
            return useEnglish ? english : chinese;
        }

        private static Text AddLine(Transform parent, string value, int size, FontStyle style, Color color, float height)
        {
            var label = UiFactory.Label("UnityCardDetailLine", parent, value, size, style);
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, height);
            return label;
        }

        private static void ConfigurePanelChrome(GameObject panel)
        {
            if (panel == null)
            {
                return;
            }

            UnityTavernUiStyle.ConfigureSurface(panel, UnityTavernUiStyle.SurfaceDark);
            UnityTavernUiStyle.ConfigureOutline(
                panel,
                UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.62f),
                new Vector2(1.5f, -1.5f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityCardDetailStarLantern", UnityTavernUiStyle.ArcaneBlue);
        }

        private static void ConfigureHeaderChrome(Transform header)
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
        }

        private void ConfigureChromeFromReferences()
        {
            var header = titleText != null
                ? titleText.transform.parent
                : closeButton != null ? closeButton.transform.parent : null;
            ConfigureHeaderChrome(header);
            ConfigurePanelChrome(header == null || header.parent == null ? null : header.parent.gameObject);
        }

        private bool HasPrefabReferences()
        {
            return titleText != null || cardParent != null || infoParent != null || closeButton != null;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                UiFactory.EnsureFont(label);
                label.text = value ?? string.Empty;
            }
        }

        private static Button CreateCloseButton(Transform parent, out Text label)
        {
            var buttonObject = new GameObject("UnityCardDetailCloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetFixedSize(buttonObject, 92f, UnityTavernUiStyle.TouchHeight);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            UnityTavernUiStyle.ConfigureButton(button, UnityTavernUiStyle.Brass);

            label = UiFactory.Label("UnityCardDetailCloseText", buttonObject.transform, "关闭", 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return button;
        }

        private static GameObject ResolvePrefab()
        {
#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(CardDetailModalPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(CardDetailModalPrefabResourcePath);
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
