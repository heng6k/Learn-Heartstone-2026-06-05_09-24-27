using LearnHearthstone.Presentation;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LearnHearthstone.Editor
{
    public static class LearnHearthstoneSceneSetup
    {
        [MenuItem("Learn Heartstone/Setup Sample Scene")]
        public static void ConfigureSampleScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var existing = Object.FindObjectOfType<LearnHearthstoneBootstrap>();
            if (existing == null)
            {
                var bootstrap = new GameObject("LearnHearthstoneBootstrap");
                bootstrap.AddComponent<LearnHearthstoneBootstrap>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void SmokeTest()
        {
            var catalog = MinionCatalogLoader.LoadFromResources();
            if (catalog.All.Count != 279)
            {
                throw new System.InvalidOperationException("Expected 279 minions, got " + catalog.All.Count);
            }

            var service = MatchService.CreateWithDefaultCatalog(12345);
            if (service.State.Player.Tavern.Gold != 3 || service.State.Player.Tavern.Shop.Count != 3)
            {
                throw new System.InvalidOperationException("Initial tavern state is invalid.");
            }

            Debug.Log("Learn Heartstone smoke test passed.");
        }
    }
}
