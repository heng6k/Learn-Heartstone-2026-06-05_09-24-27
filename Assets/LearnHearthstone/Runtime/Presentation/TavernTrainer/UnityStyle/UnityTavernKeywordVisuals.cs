using System.Collections.Generic;
using System.Linq;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    internal static class UnityTavernKeywordVisuals
    {
        public const string RootName = "UnityKeywordVisualRoot";

        private static readonly Keyword[] DisplayOrder =
        {
            Keyword.Taunt,
            Keyword.DivineShield,
            Keyword.Venomous,
            Keyword.Reborn,
            Keyword.Windfury,
            Keyword.Stealth,
            Keyword.Poisonous,
            Keyword.Rally,
            Keyword.Deathrattle
        };

        public static void Rebuild(Transform parent, IEnumerable<Keyword> keywords, bool compact, bool useEnglish)
        {
            if (parent == null)
            {
                return;
            }

            RemoveExisting(parent);
            var active = new HashSet<Keyword>(keywords ?? Enumerable.Empty<Keyword>());
            if (!DisplayOrder.Any(active.Contains))
            {
                return;
            }

            var root = new GameObject(RootName, typeof(RectTransform), typeof(CanvasGroup));
            root.transform.SetParent(parent, false);
            root.transform.SetAsLastSibling();
            UnityTavernUiStyle.Stretch(root.GetComponent<RectTransform>());
            var group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            BuildCardEffects(root.transform, active);
            foreach (var keyword in DisplayOrder.Where(active.Contains))
            {
                BuildBadge(root.transform, keyword, compact, useEnglish);
            }
        }

        private static void BuildCardEffects(Transform parent, ISet<Keyword> active)
        {
            if (active.Contains(Keyword.Stealth))
            {
                BuildEffect(parent, Keyword.Stealth, new Color(0.05f, 0.07f, 0.10f, 0.20f), Color.clear, Vector2.zero);
            }

            if (active.Contains(Keyword.Reborn))
            {
                BuildEffect(parent, Keyword.Reborn, new Color(0.08f, 0.76f, 0.94f, 0.06f), new Color(0.15f, 0.86f, 1f, 0.82f), new Vector2(2f, -2f));
            }

            if (active.Contains(Keyword.Taunt))
            {
                BuildEffect(parent, Keyword.Taunt, new Color(0.58f, 0.48f, 0.30f, 0.04f), new Color(0.76f, 0.65f, 0.42f, 0.88f), new Vector2(3f, -3f));
            }

            if (active.Contains(Keyword.DivineShield))
            {
                BuildEffect(parent, Keyword.DivineShield, new Color(1f, 0.82f, 0.22f, 0.06f), new Color(1f, 0.82f, 0.24f, 0.95f), new Vector2(2f, -2f));
            }
        }

        private static void BuildEffect(Transform parent, Keyword keyword, Color fill, Color outlineColor, Vector2 outlineDistance)
        {
            var effect = UiFactory.Panel("UnityKeywordEffect-" + keyword, parent, fill);
            UnityTavernUiStyle.Stretch(effect.GetComponent<RectTransform>());
            if (outlineColor.a <= 0f)
            {
                return;
            }

            // Unity's Outline duplicates the full rectangular Graphic. With useGraphicAlpha=false,
            // those opaque copies overlap the centre and hide the card art. Four edge strips keep
            // the Battlegrounds-style glow readable without covering the card face.
            var thickness = Mathf.Max(2f, Mathf.Max(Mathf.Abs(outlineDistance.x), Mathf.Abs(outlineDistance.y)));
            BuildBorderStrip(effect.transform, keyword, "Top", outlineColor, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, thickness), new Vector2(0f, -thickness * 0.5f));
            BuildBorderStrip(effect.transform, keyword, "Bottom", outlineColor, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, thickness), new Vector2(0f, thickness * 0.5f));
            BuildBorderStrip(effect.transform, keyword, "Left", outlineColor, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(thickness, 0f), new Vector2(thickness * 0.5f, 0f));
            BuildBorderStrip(effect.transform, keyword, "Right", outlineColor, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(thickness, 0f), new Vector2(-thickness * 0.5f, 0f));
        }

        private static void BuildBorderStrip(
            Transform parent,
            Keyword keyword,
            string edge,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 sizeDelta,
            Vector2 anchoredPosition)
        {
            var strip = UiFactory.Panel("UnityKeywordEffectBorder-" + keyword + "-" + edge, parent, color);
            var rect = strip.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        private static void BuildBadge(Transform parent, Keyword keyword, bool compact, bool useEnglish)
        {
            var badge = UiFactory.Panel("UnityKeywordBadge-" + keyword, parent, BadgeColor(keyword));
            var badgeRect = badge.GetComponent<RectTransform>();
            var anchor = BadgeAnchor(keyword, compact);
            badgeRect.anchorMin = anchor;
            badgeRect.anchorMax = anchor;
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = Vector2.zero;
            var size = compact ? 17f : 20f;
            badgeRect.sizeDelta = new Vector2(size, size);

            var outline = UnityTavernUiStyle.EnsureComponent<Outline>(badge);
            outline.effectColor = new Color(0.04f, 0.03f, 0.02f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            outline.useGraphicAlpha = false;

            var label = UiFactory.Label("UnityKeywordBadgeLabel-" + keyword, badge.transform, BadgeLabel(keyword, useEnglish), 14, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 14;
            UnityTavernUiStyle.Stretch(label.rectTransform);
        }

        private static Vector2 BadgeAnchor(Keyword keyword, bool compact)
        {
            var lowerBadgeY = compact ? 0.29f : 0.18f;
            switch (keyword)
            {
                case Keyword.Taunt: return new Vector2(0.10f, 0.70f);
                case Keyword.DivineShield: return new Vector2(0.10f, 0.50f);
                case Keyword.Reborn: return new Vector2(0.10f, 0.30f);
                case Keyword.Venomous: return new Vector2(0.90f, 0.70f);
                case Keyword.Windfury: return new Vector2(0.90f, 0.50f);
                case Keyword.Stealth: return new Vector2(0.90f, 0.30f);
                case Keyword.Poisonous: return new Vector2(0.36f, lowerBadgeY);
                case Keyword.Rally: return new Vector2(0.50f, lowerBadgeY);
                case Keyword.Deathrattle: return new Vector2(0.64f, lowerBadgeY);
                default: return new Vector2(0.5f, 0.5f);
            }
        }

        private static string BadgeLabel(Keyword keyword, bool useEnglish)
        {
            if (useEnglish)
            {
                switch (keyword)
                {
                    case Keyword.Taunt: return "T";
                    case Keyword.DivineShield: return "S";
                    case Keyword.Venomous: return "V";
                    case Keyword.Reborn: return "R";
                    case Keyword.Windfury: return "W";
                    case Keyword.Stealth: return "H";
                    case Keyword.Poisonous: return "P";
                    case Keyword.Rally: return "A";
                    case Keyword.Deathrattle: return "D";
                }
            }

            switch (keyword)
            {
                case Keyword.Taunt: return "嘲";
                case Keyword.DivineShield: return "盾";
                case Keyword.Venomous: return "烈";
                case Keyword.Reborn: return "生";
                case Keyword.Windfury: return "风";
                case Keyword.Stealth: return "潜";
                case Keyword.Poisonous: return "毒";
                case Keyword.Rally: return "进";
                case Keyword.Deathrattle: return "亡";
                default: return string.Empty;
            }
        }

        private static Color BadgeColor(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Taunt: return new Color(0.53f, 0.44f, 0.29f, 0.96f);
                case Keyword.DivineShield: return new Color(0.93f, 0.67f, 0.12f, 0.96f);
                case Keyword.Venomous: return new Color(0.18f, 0.68f, 0.24f, 0.96f);
                case Keyword.Reborn: return new Color(0.08f, 0.66f, 0.88f, 0.96f);
                case Keyword.Windfury: return new Color(0.27f, 0.58f, 0.92f, 0.96f);
                case Keyword.Stealth: return new Color(0.28f, 0.29f, 0.36f, 0.96f);
                case Keyword.Poisonous: return new Color(0.06f, 0.42f, 0.14f, 0.96f);
                case Keyword.Rally: return new Color(0.72f, 0.34f, 0.10f, 0.96f);
                case Keyword.Deathrattle: return new Color(0.34f, 0.25f, 0.40f, 0.96f);
                default: return UnityTavernUiStyle.MutedText;
            }
        }

        private static void RemoveExisting(Transform parent)
        {
            var existing = parent.Find(RootName);
            if (existing == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Object.Destroy(existing.gameObject);
            }
            else
            {
                Object.DestroyImmediate(existing.gameObject);
            }
        }
    }
}
