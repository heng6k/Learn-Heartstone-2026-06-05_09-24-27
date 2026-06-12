using System;
using System.Linq;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.Realistic
{
    public enum TavernCardVisualMode
    {
        Shop,
        Hand,
        Board,
        Detail
    }

    public static class TavernCardView
    {
        public static GameObject Create(
            Transform parent,
            MinionInstance card,
            TavernCardVisualMode mode,
            Action<MinionInstance> onSelect)
        {
            if (card == null)
            {
                return Empty(parent, mode);
            }

            var size = SizeFor(mode);
            var root = new GameObject("Card-" + card.InstanceId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            var element = root.AddComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
            element.minWidth = size.x;
            element.minHeight = size.y;

            var sprite = LoadSprite(card);
            var frame = root.GetComponent<Image>();
            frame.color = sprite == null ? FrameColor(card, mode) : Color.clear;
            frame.raycastTarget = true;

            var button = root.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => onSelect?.Invoke(card));

            var motion = root.AddComponent<RealisticCardHoverMotion>();
            motion.Initialize(mode == TavernCardVisualMode.Board ? 1.05f : 1.035f);

            BuildArtwork(root.transform, card, mode, sprite);
            if (sprite == null)
            {
                BuildTierBadge(root.transform, card, mode);
                BuildName(root.transform, card, mode);
                BuildKeywords(root.transform, card, mode);
                BuildStats(root.transform, card, mode);
                BuildKindTag(root.transform, card, mode);
            }

            return root;
        }

        public static Sprite LoadSprite(MinionInstance card)
        {
            if (card == null || string.IsNullOrEmpty(card.ImagePath))
            {
                return null;
            }

            var sprite = Resources.Load<Sprite>(card.ImagePath);
            if (sprite != null)
            {
                return sprite;
            }

            return Resources.LoadAll<Sprite>(card.ImagePath).FirstOrDefault();
        }

        private static void BuildArtwork(Transform parent, MinionInstance card, TavernCardVisualMode mode, Sprite sprite)
        {
            if (sprite == null)
            {
                BuildFallbackPortrait(parent, card, mode);
                return;
            }

            var art = new GameObject("CardArtImage", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(parent, false);
            var artRect = art.GetComponent<RectTransform>();
            ApplyRect(artRect, Vector2.zero, Vector2.one, new Vector2(2f, 2f), new Vector2(-2f, -2f));

            var image = art.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
        }

        private static void BuildFallbackPortrait(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            var portrait = new GameObject("CardPortraitMask", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            portrait.transform.SetParent(parent, false);
            var portraitRect = portrait.GetComponent<RectTransform>();
            ApplyRect(portraitRect, PortraitMin(mode), PortraitMax(mode), Vector2.zero, Vector2.zero);
            portrait.GetComponent<Image>().color = ColorFromHex(0x2B2018);
            portrait.GetComponent<Image>().raycastTarget = false;

            var art = new GameObject("CardArtImage", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(portrait.transform, false);
            var artRect = art.GetComponent<RectTransform>();
            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.sizeDelta = mode == TavernCardVisualMode.Board ? new Vector2(116f, 116f) : new Vector2(150f, 190f);
            artRect.anchoredPosition = mode == TavernCardVisualMode.Board ? new Vector2(0f, 10f) : new Vector2(0f, 16f);

            var image = art.GetComponent<Image>();
            image.sprite = null;
            image.preserveAspect = true;
            image.color = ColorFromHex(0x443123);
            image.raycastTarget = false;
        }

        private static void BuildTierBadge(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            var badge = Circle("TierBadge", parent, card.CardKind == CardKind.TavernSpell ? ColorFromHex(0x45677E) : ColorFromHex(0xB6892D));
            var rect = badge.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = mode == TavernCardVisualMode.Board ? new Vector2(30f, 30f) : new Vector2(34f, 34f);
            rect.anchoredPosition = mode == TavernCardVisualMode.Board ? new Vector2(16f, -15f) : new Vector2(18f, -18f);

            var label = Label("TierBadgeText", badge.transform, card.TavernTier.ToString(), mode == TavernCardVisualMode.Board ? 14 : 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.Stretch(label.rectTransform);
        }

        private static void BuildName(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            if (mode == TavernCardVisualMode.Board)
            {
                return;
            }

            var label = Label("CardName", parent, card.Name, mode == TavernCardVisualMode.Hand ? 11 : 12, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            ApplyRect(label.rectTransform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0f), new Vector2(0f, 42f), new Vector2(0f, 68f));
        }

        private static void BuildKeywords(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            if (mode == TavernCardVisualMode.Board)
            {
                return;
            }

            var text = KeywordsText(card);
            var label = Label("CardKeywords", parent, text, 9, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetTextColor(label, ColorFromHex(0xF1C968));
            ApplyRect(label.rectTransform, new Vector2(0.07f, 0f), new Vector2(0.93f, 0f), new Vector2(0f, 24f), new Vector2(0f, 40f));
        }

        private static void BuildStats(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            if (card.CardKind != CardKind.Minion)
            {
                BuildCostGem(parent, card, mode);
                return;
            }

            StatGem("AttackGem", parent, card.Attack.ToString(), ColorFromHex(0xC67B35), true, mode);
            StatGem("HealthGem", parent, card.Health.ToString(), ColorFromHex(0xB7353D), false, mode);
        }

        private static void BuildCostGem(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            var gem = Circle("CostGem", parent, ColorFromHex(0x3B6FA4));
            var rect = gem.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(34f, 34f);
            rect.anchoredPosition = new Vector2(-18f, -18f);
            var label = Label("CostGemText", gem.transform, Math.Max(0, card.Cost).ToString(), 16, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.Stretch(label.rectTransform);
        }

        private static void BuildKindTag(Transform parent, MinionInstance card, TavernCardVisualMode mode)
        {
            if (mode == TavernCardVisualMode.Board)
            {
                return;
            }

            var tag = Label("CardKindTag", parent, card.CardKind == CardKind.TavernSpell ? "酒馆法术" : TribeText(card), 8, FontStyle.Bold);
            tag.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetTextColor(tag, ColorFromHex(0xD8C08A));
            ApplyRect(tag.rectTransform, new Vector2(0.12f, 1f), new Vector2(0.88f, 1f), new Vector2(0f, -44f), new Vector2(0f, -26f));
        }

        private static GameObject Empty(Transform parent, TavernCardVisualMode mode)
        {
            var size = SizeFor(mode);
            var slot = new GameObject("EmptyCardSlot", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);
            var rect = slot.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            var element = slot.AddComponent<LayoutElement>();
            element.preferredWidth = size.x;
            element.preferredHeight = size.y;
            element.minWidth = size.x;
            element.minHeight = size.y;
            slot.GetComponent<Image>().color = ColorFromHex(0x2B2118);

            var label = Label("EmptyCardSlotText", slot.transform, "空位", 12, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.SetTextColor(label, ColorFromHex(0x9D8B72));
            UiFactory.Stretch(label.rectTransform);
            return slot;
        }

        private static void StatGem(string name, Transform parent, string value, Color color, bool left, TavernCardVisualMode mode)
        {
            var gem = Circle(name, parent, color);
            var rect = gem.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = mode == TavernCardVisualMode.Board ? new Vector2(34f, 34f) : new Vector2(36f, 36f);
            rect.anchoredPosition = mode == TavernCardVisualMode.Board
                ? new Vector2(left ? 18f : -18f, 18f)
                : new Vector2(left ? 20f : -20f, 20f);

            var label = Label(name + "Text", gem.transform, value, mode == TavernCardVisualMode.Board ? 15 : 17, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            UiFactory.Stretch(label.rectTransform);
        }

        private static GameObject Circle(string name, Transform parent, Color color)
        {
            var circle = new GameObject(name, typeof(RectTransform), typeof(Image));
            circle.transform.SetParent(parent, false);
            var image = circle.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return circle;
        }

        private static Text Label(string name, Transform parent, string text, int size, FontStyle style)
        {
            var label = UiFactory.Label(name, parent, text, size, style);
            label.raycastTarget = false;
            return label;
        }

        private static Vector2 SizeFor(TavernCardVisualMode mode)
        {
            switch (mode)
            {
                case TavernCardVisualMode.Hand:
                    return new Vector2(108f, 150f);
                case TavernCardVisualMode.Board:
                    return new Vector2(108f, 122f);
                case TavernCardVisualMode.Detail:
                    return new Vector2(220f, 312f);
                default:
                    return new Vector2(124f, 178f);
            }
        }

        private static Vector2 PortraitMin(TavernCardVisualMode mode)
        {
            return mode == TavernCardVisualMode.Board ? new Vector2(0.08f, 0.16f) : new Vector2(0.08f, 0.34f);
        }

        private static Vector2 PortraitMax(TavernCardVisualMode mode)
        {
            return mode == TavernCardVisualMode.Board ? new Vector2(0.92f, 0.92f) : new Vector2(0.92f, 0.92f);
        }

        private static Color FrameColor(MinionInstance card, TavernCardVisualMode mode)
        {
            if (card.Golden)
            {
                return ColorFromHex(0x7A571D);
            }

            if (card.CardKind == CardKind.TavernSpell)
            {
                return ColorFromHex(0x243A4C);
            }

            return mode == TavernCardVisualMode.Board ? ColorFromHex(0x463325) : ColorFromHex(0x38281D);
        }

        private static string KeywordsText(MinionInstance card)
        {
            var keywords = card.OfficialKeywords != null && card.OfficialKeywords.Count > 0
                ? card.OfficialKeywords
                : card.Keywords;
            return keywords == null || keywords.Count == 0
                ? string.Empty
                : string.Join(" ", keywords.Take(3).Select(KeywordName).ToArray());
        }

        private static string TribeText(MinionInstance card)
        {
            if (card.Tribes == null || card.Tribes.Count == 0)
            {
                return "无种族";
            }

            return string.Join("/", card.Tribes.Where(tribe => tribe != Tribe.None).Take(2).Select(TribeName).ToArray());
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
                default: return "无";
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
                case Keyword.Bounty: return "Bounty";
                default: return keyword.ToString();
            }
        }

        private static void ApplyRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Color ColorFromHex(int rgb)
        {
            return new Color(
                ((rgb >> 16) & 0xFF) / 255f,
                ((rgb >> 8) & 0xFF) / 255f,
                (rgb & 0xFF) / 255f);
        }
    }
}
