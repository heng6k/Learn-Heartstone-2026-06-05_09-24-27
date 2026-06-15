using System;
using System.Collections.Generic;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernMinionEditModalComponent : MonoBehaviour
    {
        private static readonly Keyword[] EditableKeywords =
        {
            Keyword.Taunt,
            Keyword.DivineShield,
            Keyword.Venomous,
            Keyword.Reborn,
            Keyword.Deathrattle,
            Keyword.Windfury,
            Keyword.Stealth
        };

        private readonly Dictionary<Keyword, Toggle> keywordToggles = new Dictionary<Keyword, Toggle>();
        private InputField attackInput;
        private InputField healthInput;
        private Text validationText;

        public static GameObject CreateModalHost(Transform parent, string name)
        {
            var host = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(UnityTavernMinionEditModalComponent));
            host.transform.SetParent(parent, false);
            UnityTavernUiStyle.Stretch(host.GetComponent<RectTransform>());
            var image = host.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.46f);
            image.raycastTarget = true;
            return host;
        }

        public void Build(
            MinionInstance target,
            BoardSide side,
            Action<MinionPatch> saveCurrent,
            Action<MinionPatch> applyPlayerBoard,
            Action<MinionPatch> applyOpponentBoard,
            Action close)
        {
            ClearChildren();
            keywordToggles.Clear();

            var panel = new GameObject("UnityMinionEditPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(430f, 456f);
            panelRect.anchoredPosition = Vector2.zero;
            UnityTavernUiStyle.ConfigureSurface(panel, new Color(0.08f, 0.11f, 0.11f, 0.98f), true);
            UnityTavernUiStyle.ConfigureOutline(panel, new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.42f), new Vector2(2f, -2f));

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 8;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildHeader(panel.transform, target, side);
            BuildStatInputs(panel.transform, target);
            BuildKeywordGrid(panel.transform, target);
            validationText = BuildValidation(panel.transform);
            BuildActionButtons(panel.transform, saveCurrent, applyPlayerBoard, applyOpponentBoard, close);
        }

        private void BuildHeader(Transform parent, MinionInstance target, BoardSide side)
        {
            var title = UiFactory.Label("UnityMinionEditTitle", parent, "随从编辑", 18, FontStyle.Bold);
            title.color = UnityTavernUiStyle.Text;
            title.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(title.gameObject, 28f);

            var targetName = target == null ? "未选择" : target.Name;
            var sideText = side == BoardSide.Opponent ? "敌方" : "己方";
            var summary = UiFactory.Label("UnityMinionEditTargetText", parent, "目标：" + targetName + "（" + sideText + "）", 12, FontStyle.Bold);
            summary.color = UnityTavernUiStyle.MutedText;
            summary.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, 24f);
        }

        private void BuildStatInputs(Transform parent, MinionInstance target)
        {
            var row = new GameObject("UnityMinionEditStatsRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(row, 58f);

            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            attackInput = BuildInput(row.transform, "UnityMinionEditAttackInput", "攻击", target == null ? 0 : target.Attack);
            healthInput = BuildInput(row.transform, "UnityMinionEditHealthInput", "生命", target == null ? 1 : target.Health);
        }

        private InputField BuildInput(Transform parent, string name, string label, int value)
        {
            var group = new GameObject(name + "Group", typeof(RectTransform));
            group.transform.SetParent(parent, false);
            var groupLayout = group.AddComponent<VerticalLayoutGroup>();
            groupLayout.spacing = 4;
            groupLayout.childControlWidth = true;
            groupLayout.childControlHeight = true;
            groupLayout.childForceExpandWidth = true;
            groupLayout.childForceExpandHeight = false;

            var caption = UiFactory.Label(name + "Label", group.transform, label, 12, FontStyle.Bold);
            caption.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(caption.gameObject, 18f);

            var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(group.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(inputObject, 34f);
            UnityTavernUiStyle.ConfigureSurface(inputObject, UnityTavernUiStyle.PanelRaised, true);
            UnityTavernUiStyle.ConfigureOutline(inputObject, new Color(1f, 1f, 1f, 0.16f), new Vector2(1f, -1f));

            var input = inputObject.GetComponent<InputField>();
            input.contentType = InputField.ContentType.IntegerNumber;
            input.text = value.ToString();
            input.caretColor = UnityTavernUiStyle.Text;
            input.selectionColor = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.35f);

            var placeholder = UiFactory.Label(name + "Placeholder", inputObject.transform, "0", 15, FontStyle.Normal);
            placeholder.color = new Color(UnityTavernUiStyle.MutedText.r, UnityTavernUiStyle.MutedText.g, UnityTavernUiStyle.MutedText.b, 0.55f);
            placeholder.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.Stretch(placeholder.rectTransform);

            var text = UiFactory.Label(name + "Text", inputObject.transform, value.ToString(), 16, FontStyle.Bold);
            text.color = UnityTavernUiStyle.Text;
            text.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.Stretch(text.rectTransform);

            input.placeholder = placeholder;
            input.textComponent = text;
            return input;
        }

        private void BuildKeywordGrid(Transform parent, MinionInstance target)
        {
            var label = UiFactory.Label("UnityMinionEditKeywordTitle", parent, "关键词", 13, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, 22f);

            var grid = new GameObject("UnityMinionEditKeywordGrid", typeof(RectTransform), typeof(Image));
            grid.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(grid, 120f);
            UnityTavernUiStyle.ConfigureSurface(grid, new Color(0.04f, 0.055f, 0.055f, 0.62f), false);

            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.cellSize = new Vector2(124f, 30f);

            foreach (var keyword in EditableKeywords)
            {
                BuildKeywordToggle(grid.transform, target, keyword);
            }
        }

        private void BuildKeywordToggle(Transform parent, MinionInstance target, Keyword keyword)
        {
            var toggleObject = new GameObject("UnityMinionEditKeywordToggle-" + keyword, typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);
            var image = UnityTavernUiStyle.ConfigureSurface(toggleObject, UnityTavernUiStyle.Panel, true);
            UnityTavernUiStyle.ConfigureOutline(toggleObject, new Color(1f, 1f, 1f, 0.12f), new Vector2(1f, -1f));

            var toggle = toggleObject.GetComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.isOn = HasKeyword(target, keyword);

            var checkmark = new GameObject("UnityMinionEditKeywordCheckmark-" + keyword, typeof(RectTransform), typeof(Image));
            checkmark.transform.SetParent(toggleObject.transform, false);
            var checkmarkImage = checkmark.GetComponent<Image>();
            checkmarkImage.color = UnityTavernUiStyle.Gold;
            checkmarkImage.raycastTarget = false;
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0f, 0.5f);
            checkRect.anchorMax = new Vector2(0f, 0.5f);
            checkRect.pivot = new Vector2(0f, 0.5f);
            checkRect.sizeDelta = new Vector2(8f, 18f);
            checkRect.anchoredPosition = new Vector2(8f, 0f);
            toggle.graphic = checkmarkImage;

            var text = UiFactory.Label("UnityMinionEditKeywordText-" + keyword, toggleObject.transform, KeywordName(keyword), 11, FontStyle.Bold);
            text.color = UnityTavernUiStyle.Text;
            text.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.Stretch(text.rectTransform);

            toggle.onValueChanged.AddListener(isOn =>
            {
                image.color = isOn ? new Color(0.24f, 0.30f, 0.20f, 0.96f) : UnityTavernUiStyle.Panel;
            });
            image.color = toggle.isOn ? new Color(0.24f, 0.30f, 0.20f, 0.96f) : UnityTavernUiStyle.Panel;
            keywordToggles[keyword] = toggle;
        }

        private Text BuildValidation(Transform parent)
        {
            var text = UiFactory.Label("UnityMinionEditValidationText", parent, string.Empty, 11, FontStyle.Bold);
            text.color = UnityTavernUiStyle.Red;
            text.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(text.gameObject, 18f);
            text.gameObject.SetActive(false);
            return text;
        }

        private void BuildActionButtons(
            Transform parent,
            Action<MinionPatch> saveCurrent,
            Action<MinionPatch> applyPlayerBoard,
            Action<MinionPatch> applyOpponentBoard,
            Action close)
        {
            var primaryRow = BuildButtonRow(parent, "UnityMinionEditPrimaryActions", 3);
            BuildActionButton(primaryRow.transform, "UnityMinionEditSaveButton", "保存当前", () => InvokePatch(saveCurrent), UnityTavernUiStyle.Green);
            BuildActionButton(primaryRow.transform, "UnityMinionEditApplyPlayerButton", "套用己方", () => InvokePatch(applyPlayerBoard), UnityTavernUiStyle.Gold);
            BuildActionButton(primaryRow.transform, "UnityMinionEditApplyOpponentButton", "套用敌方", () => InvokePatch(applyOpponentBoard), UnityTavernUiStyle.Blue);

            var secondaryRow = BuildButtonRow(parent, "UnityMinionEditSecondaryActions", 2);
            BuildActionButton(secondaryRow.transform, "UnityMinionEditClearKeywordsButton", "清空关键词", ClearKeywordToggles, UnityTavernUiStyle.Red);
            BuildActionButton(secondaryRow.transform, "UnityMinionEditCancelButton", "取消", close, UnityTavernUiStyle.PanelRaised);
        }

        private GameObject BuildButtonRow(Transform parent, string name, int columns)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(row, 42f);
            var layout = row.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.cellSize = columns == 3 ? new Vector2(124f, 34f) : new Vector2(190f, 34f);
            layout.spacing = new Vector2(10f, 0f);
            return row;
        }

        private void BuildActionButton(Transform parent, string name, string label, Action action, Color accent)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var image = UnityTavernUiStyle.ConfigureSurface(buttonObject, new Color(accent.r, accent.g, accent.b, 0.72f), true);
            UnityTavernUiStyle.ConfigureOutline(buttonObject, new Color(accent.r, accent.g, accent.b, 0.34f), new Vector2(1f, -1f));

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }

            UnityTavernUiStyle.TintSelectable(
                button,
                image.color,
                new Color(Mathf.Min(1f, accent.r + 0.18f), Mathf.Min(1f, accent.g + 0.18f), Mathf.Min(1f, accent.b + 0.18f), 0.92f),
                new Color(Mathf.Max(0.05f, accent.r * 0.62f), Mathf.Max(0.05f, accent.g * 0.62f), Mathf.Max(0.05f, accent.b * 0.62f), 0.98f));

            var text = UiFactory.Label(name + "Text", buttonObject.transform, label, 12, FontStyle.Bold);
            text.color = UnityTavernUiStyle.Text;
            text.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.Stretch(text.rectTransform);
        }

        private void InvokePatch(Action<MinionPatch> action)
        {
            if (action == null || !TryBuildPatch(out var patch))
            {
                return;
            }

            action(patch);
        }

        private bool TryBuildPatch(out MinionPatch patch)
        {
            patch = null;
            if (!int.TryParse(attackInput.text, out var attack) || !int.TryParse(healthInput.text, out var health))
            {
                ShowValidation("请输入有效的攻击和生命。");
                return false;
            }

            attack = Mathf.Clamp(attack, 0, int.MaxValue);
            health = Mathf.Clamp(health, 1, int.MaxValue);
            patch = new MinionPatch
            {
                Attack = attack,
                Health = health,
                MaxHealth = health,
                Keywords = SelectedKeywords()
            };
            HideValidation();
            return true;
        }

        private List<Keyword> SelectedKeywords()
        {
            var keywords = new List<Keyword>();
            foreach (var keyword in EditableKeywords)
            {
                if (keywordToggles.TryGetValue(keyword, out var toggle) && toggle.isOn)
                {
                    keywords.Add(keyword);
                }
            }

            return keywords;
        }

        private void ClearKeywordToggles()
        {
            foreach (var toggle in keywordToggles.Values)
            {
                toggle.isOn = false;
            }
        }

        private void ShowValidation(string message)
        {
            if (validationText == null)
            {
                return;
            }

            validationText.text = message;
            validationText.gameObject.SetActive(true);
        }

        private void HideValidation()
        {
            if (validationText == null)
            {
                return;
            }

            validationText.text = string.Empty;
            validationText.gameObject.SetActive(false);
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

        private static bool HasKeyword(MinionInstance target, Keyword keyword)
        {
            return target != null && target.Keywords != null && target.Keywords.Contains(keyword);
        }

        private static string KeywordName(Keyword keyword)
        {
            switch (keyword)
            {
                case Keyword.Taunt: return "嘲讽";
                case Keyword.DivineShield: return "圣盾";
                case Keyword.Venomous: return "烈毒";
                case Keyword.Reborn: return "复生";
                case Keyword.Deathrattle: return "亡语";
                case Keyword.Windfury: return "风怒";
                case Keyword.Stealth: return "潜行";
                default: return keyword.ToString();
            }
        }
    }
}
