using System;
using System.Linq;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public enum UnityTavernCardMode
    {
        Shop,
        Hand,
        Board,
        Detail
    }

    public sealed class UnityTavernCardComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public const string TavernCardPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Card/TavernCard.prefab";
        public const string BoardMinionPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Card/BoardMinion.prefab";
        public const string CardSlotPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/Card/CardSlot.prefab";
        public const string TavernCardPrefabResourcePath = "TavernTrainer/UnityStyle/Card/TavernCard";
        public const string BoardMinionPrefabResourcePath = "TavernTrainer/UnityStyle/Card/BoardMinion";
        public const string CardSlotPrefabResourcePath = "TavernTrainer/UnityStyle/Card/CardSlot";

        [SerializeField] private Image frameImage;
        [SerializeField] private Image artImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text subtitleText;
        [SerializeField] private Text kindText;
        [SerializeField] private Text tierText;
        [SerializeField] private Text attackText;
        [SerializeField] private Text healthText;
        [SerializeField] private Text costText;
        [SerializeField] private Button cardButton;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionText;
        [SerializeField] private GameObject tierBadge;
        [SerializeField] private GameObject attackBadge;
        [SerializeField] private GameObject healthBadge;
        [SerializeField] private GameObject costBadge;

        private MinionInstance card;
        private Action<MinionInstance> selectAction;
        private Action<MinionInstance> primaryAction;
        private Outline feedbackOutline;
        private Shadow feedbackShadow;
        private Color baseFrameColor;
        private bool selected;
        private bool hovered;
        private bool pressed;

        public MinionInstance Card => card;
        public bool IsSelected => selected;
        public bool IsHovered => hovered;

        public static GameObject CreateCardHost(UnityTavernCardMode mode, Transform parent, string fallbackName, GameObject prefabOverride = null)
        {
            var prefab = prefabOverride != null ? prefabOverride : ResolveCardPrefab(mode);
            var cardObject = prefab != null
                ? UnityEngine.Object.Instantiate(prefab)
                : new GameObject(fallbackName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(UnityTavernCardComponent));

            cardObject.name = fallbackName;
            cardObject.transform.SetParent(parent, false);
            if (cardObject.GetComponent<RectTransform>() == null)
            {
                cardObject.AddComponent<RectTransform>();
            }

            if (cardObject.GetComponent<Image>() == null)
            {
                cardObject.AddComponent<Image>();
            }

            if (cardObject.GetComponent<Button>() == null)
            {
                cardObject.AddComponent<Button>();
            }

            if (cardObject.GetComponent<UnityTavernCardComponent>() == null)
            {
                cardObject.AddComponent<UnityTavernCardComponent>();
            }

            return cardObject;
        }

        public void ConfigureReferences(
            Image frame = null,
            Image art = null,
            Text name = null,
            Text subtitle = null,
            Text kind = null,
            Text tier = null,
            Text attack = null,
            Text health = null,
            Text cost = null,
            Button rootButton = null,
            Button primaryButton = null,
            Text primaryText = null,
            GameObject tierBadgeObject = null,
            GameObject attackBadgeObject = null,
            GameObject healthBadgeObject = null,
            GameObject costBadgeObject = null)
        {
            frameImage = frame;
            artImage = art;
            nameText = name;
            subtitleText = subtitle;
            kindText = kind;
            tierText = tier;
            attackText = attack;
            healthText = health;
            costText = cost;
            cardButton = rootButton;
            actionButton = primaryButton;
            actionText = primaryText;
            tierBadge = tierBadgeObject;
            attackBadge = attackBadgeObject;
            healthBadge = healthBadgeObject;
            costBadge = costBadgeObject;
        }

        public void Bind(
            MinionInstance value,
            UnityTavernCardMode mode,
            string primaryActionLabel,
            Action<MinionInstance> onSelect,
            Action<MinionInstance> onPrimaryAction,
            bool isSelected = false)
        {
            card = value;
            selectAction = onSelect;
            primaryAction = onPrimaryAction;
            selected = isSelected && card != null;
            hovered = false;
            pressed = false;

            gameObject.name = card == null ? "UnityEmptyCard" : "UnityCard-" + card.InstanceId;
            if (HasPrefabReferences())
            {
                BindPrefabReferences(mode, primaryActionLabel);
                return;
            }

            ClearChildren();
            BuildGenerated(mode, primaryActionLabel);
            CaptureFrameColor();
            ApplyFeedbackVisuals();
        }

        public void SetSelected(bool value)
        {
            selected = value && card != null;
            ApplyFeedbackVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (card == null)
            {
                return;
            }

            hovered = true;
            ApplyFeedbackVisuals();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
            ApplyFeedbackVisuals();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (card == null || eventData != null && eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            pressed = true;
            ApplyFeedbackVisuals();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            ApplyFeedbackVisuals();
        }

        private void BindPrefabReferences(UnityTavernCardMode mode, string primaryActionLabel)
        {
            var frame = frameImage != null ? frameImage : UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            frameImage = frame;
            frame.color = card == null ? UnityTavernUiStyle.PanelQuiet : FrameColor(mode);
            baseFrameColor = frame.color;
            frame.raycastTarget = true;

            var button = cardButton != null ? cardButton : UnityTavernUiStyle.EnsureComponent<Button>(gameObject);
            cardButton = button;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selectAction?.Invoke(card));
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.94f, 0.72f, 1f),
                new Color(0.84f, 0.76f, 0.54f, 1f));

            var size = SizeFor(mode);
            UnityTavernUiStyle.SetFixedSize(gameObject, size.x, size.y);

            if (card == null)
            {
                BindEmptyPrefabCard();
                return;
            }

            var sprite = LoadSprite(card);
            if (artImage != null)
            {
                artImage.sprite = sprite;
                artImage.preserveAspect = true;
                artImage.color = sprite == null ? FallbackArtColor(card) : Color.white;
                artImage.gameObject.SetActive(true);
            }

            SetText(nameText, mode == UnityTavernCardMode.Board ? string.Empty : card.Name);
            SetText(subtitleText, mode == UnityTavernCardMode.Board ? string.Empty : KeywordText(card));
            SetText(kindText, card.CardKind == CardKind.TavernSpell ? "法术" : TribeText(card));
            SetText(tierText, card.TavernTier.ToString());
            SetActive(tierBadge, true);

            var isSpell = card.CardKind == CardKind.TavernSpell;
            SetBadge(attackBadge, attackText, !isSpell, card.Attack.ToString());
            SetBadge(healthBadge, healthText, !isSpell, card.Health.ToString());
            SetBadge(costBadge, costText, isSpell, Math.Max(0, card.Cost).ToString());
            BindPrimaryButton(primaryActionLabel);
            ApplyFeedbackVisuals();
        }

        private void BindEmptyPrefabCard()
        {
            if (artImage != null)
            {
                artImage.sprite = null;
                artImage.color = UnityTavernUiStyle.PanelQuiet;
            }

            SetText(nameText, "空位");
            SetText(subtitleText, string.Empty);
            SetText(kindText, string.Empty);
            SetText(tierText, string.Empty);
            SetBadge(attackBadge, attackText, false, string.Empty);
            SetBadge(healthBadge, healthText, false, string.Empty);
            SetBadge(costBadge, costText, false, string.Empty);
            SetActive(tierBadge, false);
            BindPrimaryButton(null);
            ApplyFeedbackVisuals();
        }

        private void BindPrimaryButton(string primaryActionLabel)
        {
            var hasAction = card != null && !string.IsNullOrEmpty(primaryActionLabel) && actionButton != null;
            if (actionButton != null)
            {
                actionButton.gameObject.SetActive(hasAction);
                actionButton.gameObject.name = hasAction ? "UnityCardAction-" + card.InstanceId : "UnityCardAction";
                actionButton.onClick.RemoveAllListeners();
                if (hasAction)
                {
                    actionButton.onClick.AddListener(() =>
                    {
                        TriggerActionPressFeedback();
                        primaryAction?.Invoke(card);
                    });
                    UnityTavernUiStyle.TintSelectable(
                        actionButton,
                        Color.white,
                        new Color(1f, 0.92f, 0.65f, 1f),
                        new Color(0.78f, 0.66f, 0.42f, 1f));
                }
            }

            SetText(actionText, hasAction ? primaryActionLabel : string.Empty);
        }

        private bool HasPrefabReferences()
        {
            return frameImage != null
                || artImage != null
                || nameText != null
                || subtitleText != null
                || kindText != null
                || tierText != null
                || attackText != null
                || healthText != null
                || costText != null
                || cardButton != null
                || actionButton != null
                || actionText != null
                || tierBadge != null
                || attackBadge != null
                || healthBadge != null
                || costBadge != null;
        }

        private static void SetBadge(GameObject badge, Text label, bool visible, string value)
        {
            SetActive(badge, visible);
            SetText(label, visible ? value : string.Empty);
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }

        private void BuildGenerated(UnityTavernCardMode mode, string primaryActionLabel)
        {
            var image = UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            image.color = card == null ? UnityTavernUiStyle.PanelQuiet : FrameColor(mode);
            frameImage = image;
            baseFrameColor = image.color;
            image.raycastTarget = true;

            var button = UnityTavernUiStyle.EnsureComponent<Button>(gameObject);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => selectAction?.Invoke(card));
            UnityTavernUiStyle.TintSelectable(
                button,
                Color.white,
                new Color(1f, 0.94f, 0.72f, 1f),
                new Color(0.84f, 0.76f, 0.54f, 1f));

            var size = SizeFor(mode);
            UnityTavernUiStyle.SetFixedSize(gameObject, size.x, size.y);

            if (card == null)
            {
                var empty = UiFactory.Label("UnityEmptyCardText", transform, "空位", 13, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = UnityTavernUiStyle.MutedText;
                UnityTavernUiStyle.Stretch(empty.rectTransform);
                return;
            }

            BuildArt(mode);
            BuildHeader(mode);
            BuildStats(mode);

            if (mode != UnityTavernCardMode.Board)
            {
                BuildName(mode);
                BuildSubtitle(mode);
            }

            if (!string.IsNullOrEmpty(primaryActionLabel))
            {
                BuildPrimaryAction(primaryActionLabel);
            }
        }

        private void BuildArt(UnityTavernCardMode mode)
        {
            var art = new GameObject("UnityCardArt", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(transform, false);
            var rect = art.GetComponent<RectTransform>();
            rect.anchorMin = mode == UnityTavernCardMode.Board ? new Vector2(0.06f, 0.20f) : new Vector2(0.06f, 0.28f);
            rect.anchorMax = new Vector2(0.94f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = art.GetComponent<Image>();
            var sprite = LoadSprite(card);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = sprite == null ? FallbackArtColor(card) : Color.white;
            image.raycastTarget = false;
        }

        private void BuildHeader(UnityTavernCardMode mode)
        {
            var tier = Badge("UnityTierBadge", card.TavernTier.ToString(), card.CardKind == CardKind.TavernSpell ? UnityTavernUiStyle.Blue : UnityTavernUiStyle.Gold);
            var rect = tier.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mode == UnityTavernCardMode.Board ? new Vector2(17f, -17f) : new Vector2(19f, -19f);

            var kind = UiFactory.Label("UnityCardKind", transform, card.CardKind == CardKind.TavernSpell ? "法术" : TribeText(card), 9, FontStyle.Bold);
            kind.alignment = TextAnchor.MiddleRight;
            kind.color = UnityTavernUiStyle.MutedText;
            var kindRect = kind.rectTransform;
            kindRect.anchorMin = new Vector2(0.34f, 1f);
            kindRect.anchorMax = new Vector2(0.94f, 1f);
            kindRect.offsetMin = new Vector2(0f, -36f);
            kindRect.offsetMax = new Vector2(0f, -10f);
        }

        private void BuildName(UnityTavernCardMode mode)
        {
            var name = UiFactory.Label("UnityCardName", transform, card.Name, mode == UnityTavernCardMode.Hand ? 11 : 12, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = name.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0f);
            rect.anchorMax = new Vector2(0.92f, 0f);
            rect.offsetMin = new Vector2(0f, 42f);
            rect.offsetMax = new Vector2(0f, 72f);
        }

        private void BuildSubtitle(UnityTavernCardMode mode)
        {
            var subtitle = UiFactory.Label("UnityCardSubtitle", transform, KeywordText(card), 9, FontStyle.Bold);
            subtitle.alignment = TextAnchor.MiddleCenter;
            subtitle.color = UnityTavernUiStyle.Gold;
            var rect = subtitle.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0f);
            rect.anchorMax = new Vector2(0.92f, 0f);
            rect.offsetMin = new Vector2(0f, 24f);
            rect.offsetMax = new Vector2(0f, 42f);
        }

        private void BuildStats(UnityTavernCardMode mode)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                BadgeAt("UnityCostBadge", Math.Max(0, card.Cost).ToString(), UnityTavernUiStyle.Blue, new Vector2(1f, 0f), new Vector2(-19f, 20f));
                return;
            }

            BadgeAt("UnityAttackBadge", card.Attack.ToString(), UnityTavernUiStyle.ColorFromHex(0xBA6A31), new Vector2(0f, 0f), new Vector2(19f, 20f));
            BadgeAt("UnityHealthBadge", card.Health.ToString(), UnityTavernUiStyle.Red, new Vector2(1f, 0f), new Vector2(-19f, 20f));
        }

        private void BuildPrimaryAction(string primaryActionLabel)
        {
            var actionButtonObject = new GameObject("UnityCardAction-" + card.InstanceId, typeof(RectTransform), typeof(Image), typeof(Button));
            actionButtonObject.transform.SetParent(transform, false);
            var rect = actionButtonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.14f, 0f);
            rect.anchorMax = new Vector2(0.86f, 0f);
            rect.offsetMin = new Vector2(0f, 4f);
            rect.offsetMax = new Vector2(0f, 28f);

            actionButtonObject.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.12f, 0.9f);
            var actionButton = actionButtonObject.GetComponent<Button>();
            actionButton.onClick.AddListener(() =>
            {
                TriggerActionPressFeedback();
                primaryAction?.Invoke(card);
            });
            UnityTavernUiStyle.TintSelectable(
                actionButton,
                Color.white,
                new Color(1f, 0.92f, 0.65f, 1f),
                new Color(0.78f, 0.66f, 0.42f, 1f));

            var label = UiFactory.Label("UnityCardActionText", actionButtonObject.transform, primaryActionLabel, 11, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private GameObject Badge(string name, string value, Color color)
        {
            var badge = new GameObject(name, typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(transform, false);
            badge.GetComponent<Image>().color = color;
            badge.GetComponent<Image>().raycastTarget = false;
            badge.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);

            var label = UiFactory.Label(name + "Text", badge.transform, value, 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return badge;
        }

        private void BadgeAt(string name, string value, Color color, Vector2 anchor, Vector2 position)
        {
            var badge = Badge(name, value, color);
            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
        }

        private void ClearChildren()
        {
            for (var index = transform.childCount - 1; index >= 0; index -= 1)
            {
                var child = transform.GetChild(index).gameObject;
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private void CaptureFrameColor()
        {
            var frame = frameImage != null ? frameImage : gameObject.GetComponent<Image>();
            if (frame != null)
            {
                baseFrameColor = frame.color;
            }
        }

        private void ApplyFeedbackVisuals()
        {
            EnsureFeedbackEffects();

            var scale = pressed ? 0.985f : hovered ? 1.035f : selected ? 1.015f : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);

            var frame = frameImage != null ? frameImage : gameObject.GetComponent<Image>();
            if (frame != null)
            {
                var targetColor = baseFrameColor;
                if (selected)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Gold, 0.42f);
                }

                if (hovered)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Blue, 0.28f);
                }

                frame.color = targetColor;
            }

            if (feedbackOutline != null)
            {
                feedbackOutline.enabled = selected || hovered;
                feedbackOutline.effectColor = selected
                    ? new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, hovered ? 1f : 0.82f)
                    : new Color(UnityTavernUiStyle.Blue.r, UnityTavernUiStyle.Blue.g, UnityTavernUiStyle.Blue.b, 0.72f);
                feedbackOutline.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
                feedbackOutline.useGraphicAlpha = false;
            }

            if (feedbackShadow != null)
            {
                feedbackShadow.enabled = selected || hovered;
                feedbackShadow.effectColor = new Color(0f, 0f, 0f, hovered ? 0.48f : 0.34f);
                feedbackShadow.effectDistance = hovered ? new Vector2(3f, -4f) : new Vector2(2f, -2f);
                feedbackShadow.useGraphicAlpha = true;
            }
        }

        private void TriggerActionPressFeedback()
        {
            pressed = true;
            ApplyFeedbackVisuals();
            if (UnityEngine.Application.isPlaying)
            {
                CancelInvoke(nameof(ClearPressedFeedback));
                Invoke(nameof(ClearPressedFeedback), 0.08f);
                return;
            }

            ClearPressedFeedback();
        }

        private void ClearPressedFeedback()
        {
            pressed = false;
            ApplyFeedbackVisuals();
        }

        private void EnsureFeedbackEffects()
        {
            if (feedbackOutline == null)
            {
                feedbackOutline = UnityTavernUiStyle.EnsureComponent<Outline>(gameObject);
            }

            if (feedbackShadow == null)
            {
                feedbackShadow = gameObject.GetComponents<Shadow>().FirstOrDefault(effect => effect != feedbackOutline);
                if (feedbackShadow == null)
                {
                    feedbackShadow = gameObject.AddComponent<Shadow>();
                }
            }
        }

        private static Sprite LoadSprite(MinionInstance minion)
        {
            if (minion == null || string.IsNullOrEmpty(minion.ImagePath))
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(minion.ImagePath);
            if (sprite != null)
            {
                return sprite;
            }

            return Resources.LoadAll<Sprite>(minion.ImagePath).FirstOrDefault();
        }

        private static GameObject ResolveCardPrefab(UnityTavernCardMode mode)
        {
            var assetPath = mode == UnityTavernCardMode.Board ? BoardMinionPrefabAssetPath : TavernCardPrefabAssetPath;
            var resourcePath = mode == UnityTavernCardMode.Board ? BoardMinionPrefabResourcePath : TavernCardPrefabResourcePath;

#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(resourcePath);
        }

        private static Vector2 SizeFor(UnityTavernCardMode mode)
        {
            switch (mode)
            {
                case UnityTavernCardMode.Hand:
                    return new Vector2(110f, 154f);
                case UnityTavernCardMode.Board:
                    return new Vector2(112f, 126f);
                case UnityTavernCardMode.Detail:
                    return new Vector2(216f, 304f);
                default:
                    return new Vector2(128f, 184f);
            }
        }

        private Color FrameColor(UnityTavernCardMode mode)
        {
            if (card != null && card.Golden)
            {
                return UnityTavernUiStyle.ColorFromHex(0x735425);
            }

            if (card != null && card.CardKind == CardKind.TavernSpell)
            {
                return UnityTavernUiStyle.ColorFromHex(0x223A4B);
            }

            return mode == UnityTavernCardMode.Board
                ? UnityTavernUiStyle.ColorFromHex(0x3A2B20)
                : UnityTavernUiStyle.ColorFromHex(0x34281E);
        }

        private static Color FallbackArtColor(MinionInstance minion)
        {
            if (minion != null && minion.CardKind == CardKind.TavernSpell)
            {
                return UnityTavernUiStyle.ColorFromHex(0x2A526D);
            }

            return UnityTavernUiStyle.ColorFromHex(0x4A3525);
        }

        private static string KeywordText(MinionInstance minion)
        {
            var keywords = minion.OfficialKeywords != null && minion.OfficialKeywords.Count > 0
                ? minion.OfficialKeywords
                : minion.Keywords;
            if (keywords == null || keywords.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" ", keywords.Take(3).Select(KeywordName).ToArray());
        }

        private static string TribeText(MinionInstance minion)
        {
            if (minion.Tribes == null || minion.Tribes.Count == 0)
            {
                return "中立";
            }

            var tribes = minion.Tribes.Where(tribe => tribe != Tribe.None).Take(2).Select(TribeName).ToArray();
            return tribes.Length == 0 ? "中立" : string.Join("/", tribes);
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
                case Keyword.Magnetic: return "磁力";
                case Keyword.Stealth: return "潜行";
                case Keyword.TavernSpell: return "酒馆法术";
                default: return keyword.ToString();
            }
        }
    }
}
