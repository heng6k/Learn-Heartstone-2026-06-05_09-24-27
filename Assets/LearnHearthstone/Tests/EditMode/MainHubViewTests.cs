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
