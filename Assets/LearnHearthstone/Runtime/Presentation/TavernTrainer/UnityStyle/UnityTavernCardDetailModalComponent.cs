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

        public void Build(MinionInstance card, Action close, bool showCardId = true)
        {
            ConfigureOverlay(gameObject);
            if (HasPrefabReferences())
            {
                SetText(titleText, card == null ? "卡牌详情" : card.Name);
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
            image.color = new Color(0f, 0f, 0f, 0.54f);
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
            headerLayout.childForceExpandWidth = false;

            titleText = UiFactory.Label("UnityCardDetailTitle", header.transform, card == null ? "卡牌详情" : card.Name, 20, FontStyle.Bold);
            titleText.color = UnityTavernUiStyle.Text;
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
            UnityTavernUiStyle.SetFixedSize(closeButton.gameObject, 84f, 32f);
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
            SetText(closeButtonText, "关闭");
            if (closeButtonText != null)
            {
                closeButtonText.fontSize = Math.Max(14, closeButtonText.fontSize);
                closeButtonText.color = Color.white;
                UnityTavernUiStyle.ConfigureOutline(closeButtonText.gameObject, new Color(0f, 0f, 0f, 0.72f), new Vector2(1f, -1f));
            }
        }

        private static void BuildCard(Transform parent, MinionInstance card)
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
            cardObject.GetComponent<UnityTavernCardComponent>().Bind(card, UnityTavernCardMode.Detail, null, null, null);
        }

        private static void BuildInfo(Transform parent, MinionInstance card, bool showCardId)
        {
            if (parent == null)
            {
                return;
            }

            ClearChildren(parent);
            ConfigureInfoLayout(parent.gameObject);
            if (card == null)
            {
                AddLine(parent, "暂未选择卡牌。", 14, FontStyle.Bold, UnityTavernUiStyle.MutedText, 30f);
                return;
            }

            AddLine(parent, "酒馆等级 " + card.TavernTier + "  " + KindText(card), 14, FontStyle.Bold, UnityTavernUiStyle.Gold, 30f);
            AddLine(parent, card.CardKind == CardKind.TavernSpell ? "消耗 " + Math.Max(0, card.Cost) : TavernNumberFormatter.FullStats(card.Attack, card.Health) + "（上限 " + TavernNumberFormatter.FullNumber(card.MaxHealth) + "）", 14, FontStyle.Bold, UnityTavernUiStyle.Text, 30f);

            var keywords = card.OfficialKeywords != null && card.OfficialKeywords.Count > 0
                ? card.OfficialKeywords
                : card.Keywords;
            AddLine(parent, keywords == null || keywords.Count == 0 ? "关键词：无" : "关键词：" + string.Join("、", keywords.Select(KeywordName).ToArray()), 14, FontStyle.Normal, UnityTavernUiStyle.MutedText, 36f);
            if (showCardId)
            {
                AddLine(parent, "卡牌ID：" + card.CardId, 14, FontStyle.Normal, UnityTavernUiStyle.MutedText, 28f);
            }

            var body = AddLine(parent, string.IsNullOrWhiteSpace(card.Text) ? "暂无规则文本。" : card.Text, 14, FontStyle.Normal, UnityTavernUiStyle.Text, 120f);
            body.alignment = TextAnchor.UpperLeft;
            body.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private static string KindText(MinionInstance card)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                return "酒馆法术";
            }

            if (card.Tribes == null || card.Tribes.Count == 0)
            {
                return "随从";
            }

            var tribes = card.Tribes.Where(tribe => tribe != Tribe.None).Select(TribeName).ToArray();
            return tribes.Length == 0 ? "随从" : string.Join("/", tribes);
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
                case Tribe.All: return "全部";
                default: return "中立";
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
                case Keyword.Rally: return "集结";
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

        private static Text AddLine(Transform parent, string value, int size, FontStyle style, Color color, float height)
        {
            var label = UiFactory.Label("UnityCardDetailLine", parent, value, size, style);
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, height);
            return label;
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
            UnityTavernUiStyle.SetFixedSize(buttonObject, 84f, 32f);
            buttonObject.GetComponent<Image>().color = UnityTavernUiStyle.Panel;
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            UnityTavernUiStyle.TintSelectable(button, Color.white, new Color(1f, 0.91f, 0.62f, 1f), new Color(0.72f, 0.62f, 0.42f, 1f));

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
