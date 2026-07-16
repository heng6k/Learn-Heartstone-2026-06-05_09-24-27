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
            image.color = new Color(0f, 0f, 0f, 0.68f);
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
            panelRect.sizeDelta = new Vector2(500f, 560f);
            panelRect.anchoredPosition = Vector2.zero;
            UnityTavernUiStyle.ConfigureSurface(panel, UnityTavernUiStyle.SurfaceDark, true);
            UnityTavernUiStyle.ConfigureOutline(panel, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.Brass, 0.62f), new Vector2(2f, -2f));
            UnityTavernUiStyle.AddStarLanternRail(panel.transform, "UnityMinionEditStarLantern", UnityTavernUiStyle.ArcaneBlue);

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
            var summary = UiFactory.Label("UnityMinionEditTargetText", parent, "目标：" + targetName + "（" + sideText + "）", 14, FontStyle.Bold);
            summary.color = UnityTavernUiStyle.MutedText;
            summary.alignment = TextAnchor.MiddleLeft;
            UnityTavernUiStyle.SetPreferredHeight(summary.gameObject, 24f);
        }

        private void BuildStatInputs(Transform parent, MinionInstance target)
        {
            var row = new GameObject("UnityMinionEditStatsRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(row, 72f);

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

            var caption = UiFactory.Label(name + "Label", group.transform, label, 14, FontStyle.Bold);
            caption.color = UnityTavernUiStyle.MutedText;
            UnityTavernUiStyle.SetPreferredHeight(caption.gameObject, 18f);

            var inputObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObject.transform.SetParent(group.transform, false);
            UnityTavernUiStyle.SetPreferredHeight(inputObject, UnityTavernUiStyle.TouchHeight);

            var input = inputObject.GetComponent<InputField>();
            UnityTavernUiStyle.ConfigureInputField(input, UnityTavernUiStyle.ArcaneBlue);
            input.contentType = InputField.ContentType.IntegerNumber;
            input.text = value.ToString();
            input.caretColor = UnityTavernUiStyle.Text;
            input.selectionColor = new Color(UnityTavernUiStyle.Gold.r, UnityTavernUiStyle.Gold.g, UnityTavernUiStyle.Gold.b, 0.35f);

            var text = UiFactory.Label(name + "Text", inputObject.transform, value.ToString(), 16, FontStyle.Bold);
            text.color = UnityTavernUiStyle.Text;
            text.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.Stretch(text.rectTransform);

            input.textComponent = text;
            return input;
        }

        private void BuildKeywordGrid(Transform parent, MinionInstance target)
        {
            var label = UiFactory.Label("UnityMinionEditKeywordTitle", parent, "关键词", 14, FontStyle.Bold);
            label.color = UnityTavernUiStyle.Gold;
            UnityTavernUiStyle.SetPreferredHeight(label.gameObject, 22f);

            var grid = new GameObject("UnityMinionEditKeywordGrid", typeof(RectTransform), typeof(Image));
            grid.transform.SetParent(parent, false);
            UnityTavernUiStyle.SetPreferredHeight(grid, 176f);
            UnityTavernUiStyle.ConfigureSurface(grid, UnityTavernUiStyle.WithAlpha(UnityTavernUiStyle.TableDark, 0.76f), false);

            var layout = grid.AddComponent<GridLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = new Vector2(8f, 8f);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.cellSize = new Vector2(142f, UnityTavernUiStyle.TouchHeight);

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

            var text = UiFactory.Label("UnityMinionEditKeywordText-" + keyword, toggleObject.transform, (toggle.isOn ? "✓ " : string.Empty) + KeywordName(keyword), 14, FontStyle.Bold);
            text.color = UnityTavernUiStyle.Text;
            text.alignment = TextAnchor.MiddleCenter;
            UnityTavernUiStyle.Stretch(text.rectTransform);

            toggle.onValueChanged.AddListener(isOn =>
            {
                image.color = isOn ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.SurfaceRaised;
                text.text = (isOn ? "✓ " : string.Empty) + KeywordName(keyword);
            });
            image.color = toggle.isOn ? UnityTavernUiStyle.SuccessGreen : UnityTavernUiStyle.SurfaceRaised;
            keywordToggles[keyword] = toggle;
        }

        private Text BuildValidation(Transform parent)
        {
            var text = UiFactory.Label("UnityMinionEditValidationText", parent, string.Empty, 14, FontStyle.Bold);
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
            UnityTavernUiStyle.SetPreferredHeight(row, 56f);
            var layout = row.AddComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns;
            layout.cellSize = columns == 3 ? new Vector2(144f, UnityTavernUiStyle.TouchHeight) : new Vector2(220f, UnityTavernUiStyle.TouchHeight);
            layout.spacing = new Vector2(10f, 0f);
            return row;
        }

        private void BuildActionButton(Transform parent, string name, string label, Action action, Color accent)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            var button = buttonObject.GetComponent<Button>();
            if (action != null)
            {
                button.onClick.AddListener(() => action());
            }

            UnityTavernUiStyle.ConfigureButton(button, accent, emphasized: accent == UnityTavernUiStyle.Gold || accent == UnityTavernUiStyle.Green);

            var text = UiFactory.Label(name + "Text", buttonObject.transform, label, 14, FontStyle.Bold);
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
