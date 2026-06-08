using LearnHearthstone.Presentation;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LearnHearthstone.Editor
{
    [InitializeOnLoad]
    public static class LearnHearthstoneSceneSetup
    {
        private const string PlayHubRequest = "hub";
        private const string PlayTrainerRequest = "trainer";
        private const string PlayRequestPath = "Library/LearnHearthstonePlayRequest.txt";
        private const string PlaySessionKey = "LearnHearthstone.PlayRequest";

        private static string playRequest;

        static LearnHearthstoneSceneSetup()
        {
            EditorApplication.update += ConsumePlayRequestWhenReady;
            EditorApplication.playModeStateChanged += ShowTrainerAfterEnteringPlayMode;
        }

        [MenuItem("Learn Heartstone/Setup Sample Scene")]
        public static void ConfigureSampleScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var existing = Object.FindAnyObjectByType<LearnHearthstoneBootstrap>();
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
            if (service.State.Player.Tavern.Gold != 3 || service.State.Player.Tavern.Shop.Count != 4)
            {
                throw new System.InvalidOperationException("Initial tavern state is invalid.");
            }

            Debug.Log("Learn Heartstone smoke test passed.");
        }

        [MenuItem("Learn Heartstone/Play Main Hub")]
        public static void OpenSampleSceneAndPlay()
        {
            ConfigureSampleScene();
            RequestPlayMode(PlayHubRequest);
        }

        [MenuItem("Learn Heartstone/Play Tavern Trainer")]
        public static void OpenTrainerAndPlay()
        {
            ConfigureSampleScene();
            RequestPlayMode(PlayTrainerRequest);
        }

        private static void RequestPlayMode(string request)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlayRequestPath));
            File.WriteAllText(PlayRequestPath, request);
            EditorApplication.update -= ConsumePlayRequestWhenReady;
            EditorApplication.update += ConsumePlayRequestWhenReady;
        }

        private static void ConsumePlayRequestWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!File.Exists(PlayRequestPath))
            {
                return;
            }

            playRequest = File.ReadAllText(PlayRequestPath).Trim();
            File.Delete(PlayRequestPath);
            EditorApplication.update -= ConsumePlayRequestWhenReady;
            SessionState.SetString(PlaySessionKey, playRequest);

            EditorApplication.EnterPlaymode();
        }

        private static void ShowTrainerAfterEnteringPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            var request = SessionState.GetString(PlaySessionKey, string.Empty);
            SessionState.EraseString(PlaySessionKey);
            if (request != PlayTrainerRequest)
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                var bootstrap = Object.FindAnyObjectByType<LearnHearthstoneBootstrap>();
                var method = typeof(LearnHearthstoneBootstrap).GetMethod("ShowTrainer", BindingFlags.Instance | BindingFlags.NonPublic);
                method?.Invoke(bootstrap, null);
            };
        }
    }
}
