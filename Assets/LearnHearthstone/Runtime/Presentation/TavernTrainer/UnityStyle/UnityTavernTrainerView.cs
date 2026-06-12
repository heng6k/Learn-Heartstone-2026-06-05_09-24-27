using System;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Services;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.TavernTrainer.UnityStyle
{
    public sealed class UnityTavernTrainerView
    {
        public const string RootPrefabAssetPath = "Assets/LearnHearthstone/Runtime/Presentation/TavernTrainer/UnityStyle/Prefabs/UnityTavernRoot.prefab";
        public const string RootPrefabResourcePath = "TavernTrainer/UnityStyle/UnityTavernRoot";

        private readonly Transform root;
        private readonly MatchService service;
        private readonly IAdvisorService advisor;
        private readonly Action backToHub;
        private readonly Action openLegacyTools;
        private readonly GameObject rootPrefab;
        private GameObject shell;

        public UnityTavernTrainerView(
            Transform root,
            MatchService service,
            IAdvisorService advisor,
            Action backToHub,
            Action openLegacyTools = null,
            GameObject rootPrefab = null)
        {
            this.root = root;
            this.service = service;
            this.advisor = advisor;
            this.backToHub = backToHub;
            this.openLegacyTools = openLegacyTools;
            this.rootPrefab = rootPrefab;
        }

        public void Build()
        {
            DestroyShell();
            shell = CreateShell();
            shell.transform.SetParent(root, false);
            shell.name = "UnityTavernTrainer";
            UnityTavernUiStyle.Stretch(shell.GetComponent<RectTransform>());
            var image = UnityTavernUiStyle.EnsureComponent<Image>(shell);
            image.color = UnityTavernUiStyle.BackWall;
            var controller = UnityTavernUiStyle.EnsureComponent<UnityTavernTrainerController>(shell);
            controller.Initialize(service, advisor, backToHub, openLegacyTools);
        }

        private GameObject CreateShell()
        {
            var prefab = ResolveRootPrefab();
            if (prefab != null)
            {
                return UnityEngine.Object.Instantiate(prefab);
            }

            return new GameObject("UnityTavernTrainer", typeof(RectTransform), typeof(Image), typeof(UnityTavernTrainerController));
        }

        private GameObject ResolveRootPrefab()
        {
            if (rootPrefab != null)
            {
                return rootPrefab;
            }

#if UNITY_EDITOR
            var editorPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabAssetPath);
            if (editorPrefab != null)
            {
                return editorPrefab;
            }
#endif

            return Resources.Load<GameObject>(RootPrefabResourcePath);
        }

        private void DestroyShell()
        {
            if (shell == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(shell);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(shell);
            }
        }
    }
}
