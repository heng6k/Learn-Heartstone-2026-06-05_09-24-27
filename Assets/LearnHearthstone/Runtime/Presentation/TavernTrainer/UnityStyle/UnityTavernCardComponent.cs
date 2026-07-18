using System;
using System.Linq;
using LearnHearthstone.Adapters.Images;
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

    public enum UnityTavernTargetingState
    {
        None,
        Source,
        Candidate,
        InvalidTarget,
        ConfirmedTarget,
        OpponentTarget
    }

    public sealed class UnityTavernCardComponent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private const string ContainedArtViewportName = "UnityCardArtViewport";
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
        private Action<MinionInstance> contextAction;
        private Action<MinionInstance, RectTransform> hoverStartAction;
        private Action<MinionInstance> hoverEndAction;
        private Outline feedbackOutline;
        private Shadow feedbackShadow;
        private Color baseFrameColor;
        private bool selected;
        private bool hovered;
        private int handSlotSiblingIndex = -1;
        private bool pressed;
        private UnityTavernCardMode boundMode;
        private float layoutScale = 1f;
        private bool handFocusLiftEnabled;
        private UnityTavernTargetingState targetingState;
        private string targetingLabelOverride;
        private GameObject targetingLabel;
        private Text targetingLabelText;

        private void Update()
        {
            if (targetingState != UnityTavernTargetingState.None)
            {
                ApplyFeedbackVisuals();
            }
        }

        public MinionInstance Card => card;
        public bool IsSelected => selected;
        public bool IsHovered => hovered;
        public UnityTavernTargetingState TargetingState => targetingState;
        public static bool ReduceTargetingMotion { get; set; }

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
            contextAction = null;
            hoverStartAction = null;
            hoverEndAction = null;
            boundMode = mode;
            layoutScale = 1f;
            handFocusLiftEnabled = false;
            selected = isSelected && card != null;
            hovered = false;
            pressed = false;
            targetingState = UnityTavernTargetingState.None;
            targetingLabelOverride = null;
            targetingLabel = null;
            targetingLabelText = null;

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

        public void SetTargetingState(UnityTavernTargetingState state, string labelOverride = null)
        {
            targetingState = card == null ? UnityTavernTargetingState.None : state;
            targetingLabelOverride = targetingState == UnityTavernTargetingState.None ? null : labelOverride;
            ApplyFeedbackVisuals();
        }

        public void SetLayoutScale(float value)
        {
            layoutScale = Mathf.Max(0.01f, value);
            ApplyFeedbackVisuals();
        }

        public void SetHandFocusLiftEnabled(bool value)
        {
            handFocusLiftEnabled = value;
            handSlotSiblingIndex = value && transform.parent != null
                ? transform.parent.GetSiblingIndex()
                : -1;
            ApplyFeedbackVisuals();
        }

        public void ConfigureInteractionCallbacks(
            Action<MinionInstance> onContextAction,
            Action<MinionInstance, RectTransform> onHoverStart,
            Action<MinionInstance> onHoverEnd)
        {
            contextAction = onContextAction;
            hoverStartAction = onHoverStart;
            hoverEndAction = onHoverEnd;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (card == null)
            {
                return;
            }

            hovered = true;
            ApplyFeedbackVisuals();
            hoverStartAction?.Invoke(card, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (card != null)
            {
                hoverEndAction?.Invoke(card);
            }

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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (card == null || eventData == null || eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            contextAction?.Invoke(card);
        }

        private void BindPrefabReferences(UnityTavernCardMode mode, string primaryActionLabel)
        {
            var frame = frameImage != null ? frameImage : UnityTavernUiStyle.EnsureComponent<Image>(gameObject);
            frameImage = frame;
            frame.color = card == null ? EmptyFrameColor() : FrameColor(mode);
            baseFrameColor = frame.color;
            frame.raycastTarget = true;
            UnityTavernUiStyle.ConfigureOutline(gameObject, UnityTavernUiStyle.WithAlpha(CardFrameAccent(card), 0.56f), new Vector2(1.2f, -1.2f));

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
                ConfigureReadableCardText();
                return;
            }

            var sprite = LoadSprite(card);
            var usesFullCardArt = sprite != null;
            var isSpell = IsSpellLike(card);
            var usesContainedFullArtHud = usesFullCardArt && UsesContainedFullArtHud(card, mode);
            if (artImage != null)
            {
                ConfigureArtImage(artImage, sprite, card, mode, mode == UnityTavernCardMode.Board ? 9 : 11);
            }

            SetTextVisible(nameText, usesContainedFullArtHud || mode != UnityTavernCardMode.Board && !usesFullCardArt, card.Name);
            if (usesContainedFullArtHud)
            {
                ConfigureContainedFullArtName(nameText, mode);
            }

            SetText(kindText, CardKindText(card));
            if (kindText != null)
            {
                kindText.gameObject.SetActive(!usesFullCardArt);
            }

            ConfigureTextDescription(subtitleText, DescriptionText(card, usesFullCardArt), mode, isSpell && !usesFullCardArt);
            SetBadge(tierBadge, tierText, !usesFullCardArt, HeaderBadgeText(card));
            ConfigurePrefabBadge(
                tierBadge,
                tierText,
                HeaderBadgeColor(card),
                new Vector2(0f, 1f),
                mode == UnityTavernCardMode.Board ? new Vector2(17f, -17f) : new Vector2(19f, -19f),
                mode);
            SetBadge(attackBadge, attackText, !isSpell, TavernNumberFormatter.CompactStat(card.Attack));
            ConfigurePrefabBadge(attackBadge, attackText, UnityTavernUiStyle.ColorFromHex(0xBA6A31), new Vector2(0f, 0f), new Vector2(19f, 20f), mode);
            SetBadge(healthBadge, healthText, !isSpell, TavernNumberFormatter.CompactStat(card.Health));
            ConfigurePrefabBadge(healthBadge, healthText, UnityTavernUiStyle.Red, new Vector2(1f, 0f), new Vector2(-19f, 20f), mode);
            SetBadge(costBadge, costText, card.CardKind == CardKind.TavernSpell && !usesFullCardArt, Math.Max(0, card.Cost).ToString());
            ConfigurePrefabBadge(costBadge, costText, UnityTavernUiStyle.Blue, new Vector2(1f, 0f), new Vector2(-19f, 20f), mode);
            BindPrimaryButton(primaryActionLabel, usesFullCardArt);
            ConfigureReadableCardText();
            ApplyFeedbackVisuals();
        }

        private void BindEmptyPrefabCard()
        {
            if (artImage != null)
            {
                artImage.sprite = null;
                artImage.color = EmptyFrameColor();
                ClearArtFallbackLabel(artImage.transform);
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

        private void BindPrimaryButton(string primaryActionLabel, bool usesFullCardArt = false)
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
                    ConfigurePrimaryActionButton(actionButton, actionText, usesFullCardArt);
                }
            }

            SetText(actionText, hasAction ? primaryActionLabel : string.Empty);
        }

        private static void ConfigurePrimaryActionButton(Button button, Text label, bool usesFullCardArt)
        {
            ConfigurePrimaryActionRect(button.GetComponent<RectTransform>(), usesFullCardArt);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = usesFullCardArt
                    ? Color.Lerp(UnityTavernUiStyle.Panel, UnityTavernUiStyle.Gold, 0.28f)
                    : new Color(0.09f, 0.12f, 0.12f, 0.9f);
            }

            if (usesFullCardArt)
            {
                UnityTavernUiStyle.ConfigureOutline(
                    button.gameObject,
                    new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.72f),
                    new Vector2(1f, -1f));
            }

            if (label == null)
            {
                return;
            }

            label.fontSize = 14;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = usesFullCardArt ? Color.white : UnityTavernUiStyle.Text;
            label.resizeTextForBestFit = usesFullCardArt;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = label.fontSize;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (usesFullCardArt)
            {
                UnityTavernUiStyle.ConfigureOutline(label.gameObject, new Color(0f, 0f, 0f, 0.64f), new Vector2(1f, -1f));
            }
        }

        private static void ConfigurePrimaryActionRect(RectTransform rect, bool usesFullCardArt)
        {
            if (usesFullCardArt)
            {
            rect.anchorMin = new Vector2(0.22f, 0f);
            rect.anchorMax = new Vector2(0.78f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, 4f);
            rect.offsetMax = new Vector2(0f, 52f);
                return;
            }

            rect.anchorMin = new Vector2(0.14f, 0f);
            rect.anchorMax = new Vector2(0.86f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, 4f);
            rect.offsetMax = new Vector2(0f, 52f);
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
            ConfigureBadgeText(label, label == null ? 16 : label.fontSize);
        }

        private static void ConfigurePrefabBadge(GameObject badge, Text label, Color color, Vector2 anchor, Vector2 position, UnityTavernCardMode mode)
        {
            if (badge == null)
            {
                return;
            }

            var rect = UnityTavernUiStyle.EnsureComponent<RectTransform>(badge);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = mode == UnityTavernCardMode.Board ? new Vector2(30f, 30f) : new Vector2(34f, 34f);

            var image = UnityTavernUiStyle.EnsureComponent<Image>(badge);
            image.color = new Color(color.r, color.g, color.b, 0.94f);
            image.raycastTarget = false;

            if (label == null)
            {
                return;
            }

            ConfigureBadgeText(label, mode == UnityTavernCardMode.Board ? 14 : 16);
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private static void ConfigureTextDescription(Text label, string value, UnityTavernCardMode mode, bool visible)
        {
            if (label == null)
            {
                return;
            }

            UiFactory.EnsureFont(label);
            label.text = visible ? value ?? string.Empty : string.Empty;
            label.gameObject.SetActive(visible && !string.IsNullOrWhiteSpace(value));
            if (!label.gameObject.activeSelf)
            {
                return;
            }

            label.fontSize = mode == UnityTavernCardMode.Detail ? 11 : 9;
            label.fontStyle = FontStyle.Normal;
            label.alignment = TextAnchor.UpperCenter;
            label.color = UnityTavernUiStyle.Gold;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;

            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.10f, 0f);
            rect.anchorMax = new Vector2(0.90f, 0f);
            rect.offsetMin = new Vector2(0f, 34f);
            rect.offsetMax = new Vector2(0f, 80f);

            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(label.gameObject);
            outline.enabled = false;
        }

        private static void SetText(Text label, string value)
        {
            if (label != null)
            {
                UiFactory.EnsureFont(label);
                label.text = value ?? string.Empty;
            }
        }

        private static void SetTextVisible(Text label, bool visible, string value)
        {
            if (label == null)
            {
                return;
            }

            UiFactory.EnsureFont(label);
            label.text = visible ? value ?? string.Empty : string.Empty;
            label.gameObject.SetActive(visible);
        }

        private static void ConfigureContainedFullArtName(Text label, UnityTavernCardMode mode)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = mode == UnityTavernCardMode.Board ? 10 : 12;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.12f, 0.16f);
            rect.anchorMax = new Vector2(0.88f, 0.34f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            UnityTavernUiStyle.ConfigureOutline(label.gameObject, new Color(0f, 0f, 0f, 0.48f), new Vector2(1f, -1f));
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
            image.color = card == null ? EmptyFrameColor() : FrameColor(mode);
            frameImage = image;
            baseFrameColor = image.color;
            image.raycastTarget = true;
            UnityTavernUiStyle.ConfigureOutline(gameObject, UnityTavernUiStyle.WithAlpha(CardFrameAccent(card), 0.56f), new Vector2(1.2f, -1.2f));

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
                var empty = UiFactory.Label("UnityEmptyCardText", transform, "空位", 14, FontStyle.Bold);
                empty.alignment = TextAnchor.MiddleCenter;
                empty.color = EmptyTextColor();
                UnityTavernUiStyle.Stretch(empty.rectTransform);
                return;
            }

            var sprite = BuildArt(mode);
            var usesFullCardArt = sprite != null;
            if (!usesFullCardArt)
            {
                BuildHeader(mode);
                BuildStats(mode);

                if (mode != UnityTavernCardMode.Board)
                {
                    BuildName(mode);
                    BuildDescription(mode);
                }
            }
            else
            {
                if (UsesContainedFullArtHud(card, mode))
                {
                    BuildName(mode, true);
                    BuildStats(mode);
                }
            }

            if (!string.IsNullOrEmpty(primaryActionLabel))
            {
                BuildPrimaryAction(primaryActionLabel, usesFullCardArt);
            }
        }

        private Sprite BuildArt(UnityTavernCardMode mode)
        {
            var art = new GameObject("UnityCardArt", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(transform, false);

            var image = art.GetComponent<Image>();
            var sprite = LoadSprite(card);
            ConfigureArtImage(image, sprite, card, mode, mode == UnityTavernCardMode.Board ? 9 : 11);
            return sprite;
        }

        private void BuildHeader(UnityTavernCardMode mode)
        {
            var tier = Badge("UnityTierBadge", HeaderBadgeText(card), HeaderBadgeColor(card));
            var rect = tier.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mode == UnityTavernCardMode.Board ? new Vector2(17f, -17f) : new Vector2(19f, -19f);

            var kind = UiFactory.Label("UnityCardKind", transform, CardKindText(card), 14, FontStyle.Bold);
            kind.alignment = TextAnchor.MiddleRight;
            kind.color = UnityTavernUiStyle.MutedText;
            var kindRect = kind.rectTransform;
            kindRect.anchorMin = new Vector2(0.34f, 1f);
            kindRect.anchorMax = new Vector2(0.94f, 1f);
            kindRect.offsetMin = new Vector2(0f, -36f);
            kindRect.offsetMax = new Vector2(0f, -10f);
        }

        private void BuildName(UnityTavernCardMode mode, bool containedFullArt = false)
        {
            var name = UiFactory.Label("UnityCardName", transform, card.Name, 14, FontStyle.Bold);
            name.alignment = TextAnchor.MiddleCenter;
            name.color = UnityTavernUiStyle.Text;
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            if (containedFullArt)
            {
                ConfigureContainedFullArtName(name, mode);
                return;
            }

            var rect = name.rectTransform;
            rect.anchorMin = new Vector2(0.08f, 0f);
            rect.anchorMax = new Vector2(0.92f, 0f);
            rect.offsetMin = new Vector2(0f, 42f);
            rect.offsetMax = new Vector2(0f, 72f);
        }

        private void BuildDescription(UnityTavernCardMode mode)
        {
            var description = DescriptionText(card, false);
            if (string.IsNullOrWhiteSpace(description))
            {
                return;
            }

            var subtitle = UiFactory.Label("UnityCardSubtitle", transform, description, 14, FontStyle.Normal);
            ConfigureTextDescription(subtitle, description, mode, true);
        }

        private void BuildStats(UnityTavernCardMode mode)
        {
            if (card.CardKind == CardKind.TavernSpell)
            {
                BadgeAt("UnityCostBadge", Math.Max(0, card.Cost).ToString(), UnityTavernUiStyle.Blue, new Vector2(1f, 0f), new Vector2(-19f, 20f));
                return;
            }

            if (IsSpellLike(card))
            {
                return;
            }

            BadgeAt("UnityAttackBadge", TavernNumberFormatter.CompactStat(card.Attack), UnityTavernUiStyle.ColorFromHex(0xBA6A31), new Vector2(0f, 0f), new Vector2(19f, 20f));
            BadgeAt("UnityHealthBadge", TavernNumberFormatter.CompactStat(card.Health), UnityTavernUiStyle.Red, new Vector2(1f, 0f), new Vector2(-19f, 20f));
        }

        private void BuildPrimaryAction(string primaryActionLabel, bool usesFullCardArt)
        {
            var actionButtonObject = new GameObject("UnityCardAction-" + card.InstanceId, typeof(RectTransform), typeof(Image), typeof(Button));
            actionButtonObject.transform.SetParent(transform, false);
            var rect = actionButtonObject.GetComponent<RectTransform>();
            ConfigurePrimaryActionRect(rect, usesFullCardArt);

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

            var label = UiFactory.Label("UnityCardActionText", actionButtonObject.transform, primaryActionLabel, 14, FontStyle.Bold);
            ConfigurePrimaryActionButton(actionButton, label, usesFullCardArt);
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private GameObject Badge(string name, string value, Color color)
        {
            var badge = new GameObject(name, typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(transform, false);
            badge.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.94f);
            badge.GetComponent<Image>().raycastTarget = false;
            badge.GetComponent<RectTransform>().sizeDelta = new Vector2(34f, 34f);

            var label = UiFactory.Label(name + "Text", badge.transform, value, 16, FontStyle.Bold);
            ConfigureBadgeText(label, 16);
            label.color = Color.white;
            UnityTavernUiStyle.Stretch(label.rectTransform);
            return badge;
        }

        private static void ConfigureBadgeText(Text label, int maxSize)
        {
            if (label == null)
            {
                return;
            }

            label.fontSize = maxSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = maxSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
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

            var previewTarget = targetingState == UnityTavernTargetingState.Candidate && hovered;
            var source = targetingState == UnityTavernTargetingState.Source;
            var invalid = targetingState == UnityTavernTargetingState.InvalidTarget;
            var target = previewTarget ||
                         targetingState == UnityTavernTargetingState.ConfirmedTarget ||
                         targetingState == UnityTavernTargetingState.OpponentTarget;
            var candidate = targetingState == UnityTavernTargetingState.Candidate && !previewTarget;
            var targeting = source || target || candidate || invalid;

            var pulse = ReduceTargetingMotion
                ? 1f
                : invalid
                    ? 1f + Mathf.Sin(UnityEngine.Time.unscaledTime * 14f) * 0.018f
                    : target || source || candidate
                        ? 1f + Mathf.Sin(UnityEngine.Time.unscaledTime * 5f) * 0.008f
                        : 1f;
            var feedbackScale = pressed ? 0.985f : hovered || target ? 1.035f : selected || source ? 1.015f : 1f;
            feedbackScale *= pulse;
            var scale = layoutScale * feedbackScale;
            transform.localScale = new Vector3(scale, scale, 1f);

            ApplyHandFocusLift();

            var frame = frameImage != null ? frameImage : gameObject.GetComponent<Image>();
            if (frame != null)
            {
                var targetColor = baseFrameColor;
                if (invalid)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Red, 0.64f);
                }
                else if (target)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Red, 0.58f);
                }
                else if (source || candidate)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Gold, source ? 0.54f : 0.30f);
                }
                else if (selected)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Gold, 0.42f);
                }

                if (hovered && !targeting)
                {
                    targetColor = Color.Lerp(targetColor, UnityTavernUiStyle.Blue, 0.28f);
                }

                frame.color = targetColor;
            }

            if (feedbackOutline != null)
            {
                feedbackOutline.enabled = targeting || selected || hovered;
                var outlineColor = invalid || target
                    ? UnityTavernUiStyle.Red
                    : source || candidate || selected
                        ? UnityTavernUiStyle.Gold
                        : UnityTavernUiStyle.Blue;
                feedbackOutline.effectColor = new Color(
                    outlineColor.r,
                    outlineColor.g,
                    outlineColor.b,
                    invalid ? 0.92f : target || source ? 1f : candidate ? 0.68f : hovered ? 0.72f : 0.82f);
                feedbackOutline.effectDistance = target || source ? new Vector2(3f, -3f) : new Vector2(2f, -2f);
                feedbackOutline.useGraphicAlpha = false;
            }

            if (feedbackShadow != null)
            {
                feedbackShadow.enabled = targeting || selected || hovered;
                feedbackShadow.effectColor = new Color(0f, 0f, 0f, hovered ? 0.48f : 0.34f);
                feedbackShadow.effectDistance = hovered ? new Vector2(3f, -4f) : new Vector2(2f, -2f);
                feedbackShadow.useGraphicAlpha = true;
            }

            UpdateTargetingLabel(previewTarget);
        }

        private void UpdateTargetingLabel(bool previewTarget)
        {
            var label = TargetingLabel(previewTarget);
            if (string.IsNullOrEmpty(label))
            {
                if (targetingLabel != null)
                {
                    targetingLabel.SetActive(false);
                }

                return;
            }

            EnsureTargetingLabel();
            targetingLabel.SetActive(true);
            targetingLabel.transform.SetAsLastSibling();
            targetingLabelText.text = label;

            var target = previewTarget ||
                         targetingState == UnityTavernTargetingState.InvalidTarget ||
                         targetingState == UnityTavernTargetingState.ConfirmedTarget ||
                         targetingState == UnityTavernTargetingState.OpponentTarget;
            var color = target ? UnityTavernUiStyle.Red : UnityTavernUiStyle.Gold;
            targetingLabel.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.94f);
        }

        private string TargetingLabel(bool previewTarget)
        {
            if (!string.IsNullOrEmpty(targetingLabelOverride))
            {
                return targetingLabelOverride;
            }

            switch (targetingState)
            {
                case UnityTavernTargetingState.Source:
                    if (card != null && card.CardKind == CardKind.HeroPower)
                    {
                        return "英雄技能";
                    }

                    return card != null && (card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.Spell)
                        ? "法术"
                        : card != null && card.CardKind == CardKind.Minion
                            ? "随从效果"
                            : "来源";
                case UnityTavernTargetingState.Candidate:
                    return previewTarget ? "目标" : "可选";
                case UnityTavernTargetingState.InvalidTarget:
                    return "不可选";
                case UnityTavernTargetingState.ConfirmedTarget:
                    return "目标";
                case UnityTavernTargetingState.OpponentTarget:
                    return "敌技目标";
                default:
                    return null;
            }
        }

        private void EnsureTargetingLabel()
        {
            if (targetingLabel != null && targetingLabelText != null)
            {
                return;
            }

            targetingLabel = new GameObject("UnityTargetingLabel", typeof(RectTransform), typeof(Image));
            targetingLabel.transform.SetParent(transform, false);
            var image = targetingLabel.GetComponent<Image>();
            image.raycastTarget = false;

            var rect = targetingLabel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f);
            rect.sizeDelta = new Vector2(92f, 24f);

            targetingLabelText = UiFactory.Label("UnityTargetingLabelText", targetingLabel.transform, string.Empty, 14, FontStyle.Bold);
            targetingLabelText.alignment = TextAnchor.MiddleCenter;
            targetingLabelText.color = Color.white;
            targetingLabelText.raycastTarget = false;
            UnityTavernUiStyle.Stretch(targetingLabelText.rectTransform);
            UnityTavernUiStyle.ConfigureOutline(targetingLabelText.gameObject, new Color(0f, 0f, 0f, 0.64f), new Vector2(1f, -1f));
        }

        private void ApplyHandFocusLift()
        {
            if (!handFocusLiftEnabled || boundMode != UnityTavernCardMode.Hand)
            {
                return;
            }

            var rect = transform as RectTransform;
            if (rect != null)
            {
                var lift = hovered ? 26f : selected ? 16f : 0f;
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, lift);
            }

            if (transform.parent == null)
            {
                return;
            }

            if (hovered || selected)
            {
                transform.parent.SetAsLastSibling();
            }
            else if (handSlotSiblingIndex >= 0 && transform.parent.parent != null)
            {
                transform.parent.SetSiblingIndex(Mathf.Min(handSlotSiblingIndex, transform.parent.parent.childCount - 1));
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
            return CardImageProvider.LoadSprite(minion);
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

        private void ConfigureReadableCardText()
        {
            var labels = GetComponentsInChildren<Text>(true);
            for (var index = 0; index < labels.Length; index += 1)
            {
                UnityTavernUiStyle.ConfigureLabel(labels[index], labels[index].color, 14);
            }
        }

        private static Color CardFrameAccent(MinionInstance minion)
        {
            if (minion == null)
            {
                return UnityTavernUiStyle.Disabled;
            }

            if (minion.Golden)
            {
                return UnityTavernUiStyle.Gold;
            }

            switch (minion.CardKind)
            {
                case CardKind.TavernSpell:
                case CardKind.Spell:
                case CardKind.HeroPower:
                    return UnityTavernUiStyle.ArcaneBlue;
                case CardKind.Hero:
                    return UnityTavernUiStyle.CombatRed;
                case CardKind.HeroBuddy:
                    return UnityTavernUiStyle.SuccessGreen;
                default:
                    return UnityTavernUiStyle.Brass;
            }
        }

        private static Color EmptyFrameColor()
        {
            var color = UnityTavernUiStyle.PanelQuiet;
            color.a = 0.42f;
            return color;
        }

        private static Color EmptyTextColor()
        {
            var color = UnityTavernUiStyle.MutedText;
            color.a = 0.48f;
            return color;
        }

        private Color FrameColor(UnityTavernCardMode mode)
        {
            if (card != null && card.Golden)
            {
                return UnityTavernUiStyle.ColorFromHex(0x735425);
            }

            if (card != null && (card.CardKind == CardKind.TavernSpell || card.CardKind == CardKind.HeroPower))
            {
                return UnityTavernUiStyle.ColorFromHex(0x223A4B);
            }

            if (card != null && card.CardKind == CardKind.Hero)
            {
                return UnityTavernUiStyle.ColorFromHex(0x4B2525);
            }

            if (card != null && card.CardKind == CardKind.HeroBuddy)
            {
                return UnityTavernUiStyle.ColorFromHex(0x263C2A);
            }

            return mode == UnityTavernCardMode.Board
                ? UnityTavernUiStyle.ColorFromHex(0x3A2B20)
                : UnityTavernUiStyle.ColorFromHex(0x34281E);
        }

        private static Color FallbackArtColor(MinionInstance minion)
        {
            Color color;
            if (minion != null && (minion.CardKind == CardKind.TavernSpell || minion.CardKind == CardKind.HeroPower))
            {
                color = UnityTavernUiStyle.ColorFromHex(0x2A526D);
            }
            else if (minion != null && minion.CardKind == CardKind.Hero)
            {
                color = UnityTavernUiStyle.ColorFromHex(0x5F3434);
            }
            else if (minion != null && minion.CardKind == CardKind.HeroBuddy)
            {
                color = UnityTavernUiStyle.ColorFromHex(0x365436);
            }
            else
            {
                color = UnityTavernUiStyle.ColorFromHex(0x4A3525);
            }

            var key = minion == null ? null : minion.CardId ?? minion.Name;
            if (string.IsNullOrEmpty(key))
            {
                return color;
            }

            uint hash = 2166136261;
            foreach (var character in key)
            {
                hash = (hash ^ character) * 16777619u;
            }

            var shift = ((hash & 15u) - 7.5f) / 100f;
            return new Color(
                Mathf.Clamp01(color.r + shift),
                Mathf.Clamp01(color.g + shift),
                Mathf.Clamp01(color.b + shift),
                color.a);
        }

        private static void ConfigureArtImage(Image image, Sprite sprite, MinionInstance minion, UnityTavernCardMode mode, int fallbackFontSize)
        {
            if (sprite != null && UsesCroppedArtViewport(sprite, minion, mode))
            {
                ConfigureContainedArtViewport(image, sprite, mode);
            }
            else
            {
                RestoreArtFromViewport(image);
                ConfigureArtRect(image.rectTransform, sprite != null, minion, mode);
            }

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = sprite == null ? FallbackArtColor(minion) : Color.white;
            image.raycastTarget = false;
            image.gameObject.SetActive(true);

            if (sprite == null)
            {
                BuildArtFallbackLabel(image.transform, minion, fallbackFontSize);
                return;
            }

            ClearArtFallbackLabel(image.transform);
        }

        private static void ConfigureContainedArtViewport(Image image, Sprite sprite, UnityTavernCardMode mode)
        {
            var viewport = EnsureContainedArtViewport(image);
            var cardSize = SizeFor(mode);
            var artWidth = cardSize.x * 0.90f;
            var spriteAspect = sprite.rect.width <= 0.01f ? 1.35f : sprite.rect.height / sprite.rect.width;
            var artHeight = artWidth * spriteAspect;

            viewport.anchorMin = new Vector2(0.5f, 1f);
            viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.sizeDelta = new Vector2(artWidth, artHeight * 0.5f);
            viewport.anchoredPosition = new Vector2(0f, -cardSize.y * 0.03f);

            var artRect = image.rectTransform;
            artRect.anchorMin = new Vector2(0f, -1f);
            artRect.anchorMax = new Vector2(1f, 1f);
            artRect.pivot = new Vector2(0.5f, 1f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;
        }

        private static RectTransform EnsureContainedArtViewport(Image image)
        {
            var existing = image.transform.parent as RectTransform;
            if (existing != null && existing.name == ContainedArtViewportName && existing.GetComponent<RectMask2D>() != null)
            {
                return existing;
            }

            var parent = image.transform.parent;
            var siblingIndex = image.transform.GetSiblingIndex();
            var viewportObject = new GameObject(ContainedArtViewportName, typeof(RectTransform), typeof(RectMask2D));
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.SetParent(parent, false);
            viewport.SetSiblingIndex(siblingIndex);
            image.transform.SetParent(viewport, false);
            return viewport;
        }

        private static void RestoreArtFromViewport(Image image)
        {
            var viewport = image.transform.parent as RectTransform;
            if (viewport == null || viewport.name != ContainedArtViewportName || viewport.GetComponent<RectMask2D>() == null)
            {
                return;
            }

            var parent = viewport.parent;
            var siblingIndex = viewport.GetSiblingIndex();
            image.transform.SetParent(parent, false);
            image.transform.SetSiblingIndex(siblingIndex);
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(viewport.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(viewport.gameObject);
            }
        }

        private static void ConfigureArtRect(RectTransform rect, bool fullCardArt, MinionInstance minion, UnityTavernCardMode mode)
        {
            if (fullCardArt)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                return;
            }

            rect.anchorMin = mode == UnityTavernCardMode.Board ? new Vector2(0.06f, 0.20f) : new Vector2(0.06f, 0.28f);
            rect.anchorMax = new Vector2(0.94f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static bool UsesContainedFullArtHud(MinionInstance minion, UnityTavernCardMode mode)
        {
            return mode != UnityTavernCardMode.Detail && !IsSpellLike(minion);
        }

        private static bool UsesCroppedArtViewport(Sprite sprite, MinionInstance minion, UnityTavernCardMode mode)
        {
            if (mode == UnityTavernCardMode.Detail || minion == null)
            {
                return false;
            }

            return UsesContainedFullArtHud(minion, mode) && CardImageProvider.ShouldCropToPortrait(sprite, minion.Tags);
        }

        private static void BuildArtFallbackLabel(Transform parent, MinionInstance minion, int fontSize)
        {
            ClearArtFallbackLabel(parent);
            var label = UiFactory.Label("UnityCardArtFallbackText", parent, ArtFallbackText(minion), Math.Max(20, fontSize), FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = UnityTavernUiStyle.Text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            UnityTavernUiStyle.ConfigureOutline(label.gameObject, new Color(0f, 0f, 0f, 0.78f), new Vector2(1f, -1f));
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private static void ClearArtFallbackLabel(Transform parent)
        {
            if (parent == null)
            {
                return;
            }

            var existing = parent.Find("UnityCardArtFallbackText");
            if (existing == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(existing.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
        }

        private static string DescriptionText(MinionInstance minion, bool usesFullCardArt)
        {
            if (minion == null || usesFullCardArt)
            {
                return string.Empty;
            }

            return string.IsNullOrWhiteSpace(minion.Text)
                ? string.Empty
                : minion.Text.Replace("[x]", string.Empty).Replace("\r", string.Empty).Trim();
        }

        private static string ArtFallbackText(MinionInstance minion)
        {
            if (minion == null)
            {
                return string.Empty;
            }

            return UnityTavernUiStyle.ArtFallbackText(minion.Name, ArtFallbackTypeText(minion));
        }

        private static string ArtFallbackTypeText(MinionInstance minion)
        {

            if (minion.CardKind == CardKind.TavernSpell || minion.CardKind == CardKind.Spell)
            {
                return "SPELL";
            }

            if (minion.CardKind == CardKind.HeroPower)
            {
                return "POWER";
            }

            if (minion.CardKind == CardKind.Hero)
            {
                return "HERO";
            }

            if (minion.CardKind == CardKind.HeroBuddy)
            {
                return "BUDDY";
            }

            if (minion.Tribes != null)
            {
                var tribe = minion.Tribes.FirstOrDefault(value => value != Tribe.None);
                if (tribe != Tribe.None)
                {
                    return tribe.ToString().ToUpperInvariant();
                }
            }

            return "CARD";
        }

        private static bool IsSpellLike(MinionInstance minion)
        {
            return minion != null &&
                (minion.CardKind == CardKind.TavernSpell ||
                    minion.CardKind == CardKind.Spell ||
                    minion.CardKind == CardKind.HeroPower);
        }

        private static string HeaderBadgeText(MinionInstance minion)
        {
            if (minion == null)
            {
                return string.Empty;
            }

            if (minion.CardKind == CardKind.Hero)
            {
                return "英";
            }

            return minion.CardKind == CardKind.HeroPower ? "技" : minion.TavernTier.ToString();
        }

        private static Color HeaderBadgeColor(MinionInstance minion)
        {
            if (minion != null && minion.CardKind == CardKind.Hero)
            {
                return UnityTavernUiStyle.Red;
            }

            if (minion != null && (minion.CardKind == CardKind.TavernSpell || minion.CardKind == CardKind.Spell || minion.CardKind == CardKind.HeroPower))
            {
                return UnityTavernUiStyle.Blue;
            }

            return minion != null && minion.CardKind == CardKind.HeroBuddy
                ? UnityTavernUiStyle.Green
                : UnityTavernUiStyle.Gold;
        }

        private static string CardKindText(MinionInstance minion)
        {
            if (minion == null)
            {
                return string.Empty;
            }

            switch (minion.CardKind)
            {
                case CardKind.TavernSpell:
                case CardKind.Spell:
                    return "法术";
                case CardKind.Hero:
                    return "英雄";
                case CardKind.HeroPower:
                    return "英雄技能";
                case CardKind.HeroBuddy:
                    return "英雄宝宝";
                default:
                    return TribeText(minion);
            }
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
    }
}
