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
        public void Build_CreatesIdenticalTavernMirrorEntry()
        {
            var rootObject = new GameObject("Root", typeof(RectTransform));
            try
            {
                var originalOpened = false;
                var mirrorOpened = false;

                new MainHubView(
                    rootObject.transform,
                    () => { },
                    () => { },
                    () => originalOpened = true,
                    UnityTavernLayoutContext.ForSize(1366f, 768f),
                    () => mirrorOpened = true).Build();

                var buttons = FindChildren(rootObject.transform, "酒馆训练器Button");

                Assert.AreEqual(2, buttons.Count);
                Assert.AreEqual(
                    buttons[0].GetComponentInChildren<Text>().text,
                    buttons[1].GetComponentInChildren<Text>().text);

                buttons[0].GetComponent<Button>().onClick.Invoke();
                buttons[1].GetComponent<Button>().onClick.Invoke();

                Assert.IsTrue(originalOpened);
                Assert.IsTrue(mirrorOpened);
            }
            finally
            {
                Object.DestroyImmediate(rootObject);
            }
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
