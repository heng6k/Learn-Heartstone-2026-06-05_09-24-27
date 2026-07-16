using System.Linq;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class MainHubViewTests
    {
        [Test]
        public void Build_CreatesSingleTavernTrainerEntry()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var trainerOpened = false;

                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => trainerOpened = true,
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                var buttons = FindChildren(rootObject.transform, "酒馆训练器Button");

                Assert.AreEqual(1, buttons.Count);

                buttons[0].GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(trainerOpened);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void Build_AppliesStarLanternThemeAndReadableControls()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => { },
                    UnityTavernLayoutContext.ForSize(1366f, 768f)).Build();

                Assert.AreEqual(UnityTavernUiStyle.BackWall, FindChildren(rootObject.transform, "MainHub")[0].GetComponent<Image>().color);
                var header = FindChildren(rootObject.transform, "MainHubHeader")[0];
                var rail = FindChildren(header, "MainHubStarLanternRail")[0];
                var facet = FindChildren(header, "MainHubStarLanternFacet")[0];
                Assert.IsFalse(rail.GetComponent<Image>().raycastTarget);
                Assert.IsFalse(facet.GetComponent<Image>().raycastTarget);
                Assert.AreEqual(0f, Mathf.DeltaAngle(45f, facet.localEulerAngles.z), 0.001f);

                var language = FindChildren(rootObject.transform, "MainHubLanguageChineseButton")[0];
                Assert.GreaterOrEqual(language.GetComponent<LayoutElement>().preferredHeight, UnityTavernUiStyle.TouchHeight);
                Assert.IsTrue(language.GetComponentInChildren<Text>().text.StartsWith("✓ "));
                Assert.IsTrue(rootObject.GetComponentsInChildren<Text>(true).All(text => text.fontSize >= 14));
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        [Test]
        public void PackagedUiFont_CoversChineseAndLatinGlyphs()
        {
            var font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");

            Assert.IsNotNull(font);
            Assert.IsTrue(font.HasCharacter('中'));
            Assert.IsTrue(font.HasCharacter('A'));
        }

        private static System.Collections.Generic.List<Transform> FindChildren(Transform root, string name)
        {
            var results = new System.Collections.Generic.List<Transform>();
            Collect(root, name, results);
            return results;
        }

        private static void Collect(Transform root, string name, System.Collections.Generic.List<Transform> results)
        {
            if (root.name == name)
            {
                results.Add(root);
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                Collect(root.GetChild(index), name, results);
            }
        }
    }
}
