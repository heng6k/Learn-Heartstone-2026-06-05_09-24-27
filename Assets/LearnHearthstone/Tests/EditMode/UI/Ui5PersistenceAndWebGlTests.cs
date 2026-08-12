using System;
using System.IO;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Application.Commands;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class Ui5PersistenceAndWebGlTests
    {
        [Test]
        public void MatchAndSaveToolsShowLockedVersionAndCompatibleSave()
        {
            var root = Root(1280, 720);
            try
            {
                var repository = new InMemoryTestScenarioRepository();
                var service = MatchService.CreateWithDefaultCatalog(73, repository);
                service.Apply(new GameCommand(GameCommandType.SaveTestScenario, "ui5-compatible", new CombatTestOptions()));
                Build(root, service);

                var matchBadge = Text(root.transform, "UnityMatchVersionBadge");
                StringAssert.Contains("只读", matchBadge);
                StringAssert.Contains(service.State.GameVersionId, matchBadge);

                OpenAdvancedTools(root);
                Assert.IsNotNull(Find(root.transform, "UnityToolsSaveReplaySection"));
                StringAssert.Contains("只读", Text(root.transform, "UnityToolsSaveVersionBadgeText"));
                StringAssert.Contains(service.State.GameVersionId, Text(root.transform, "UnityToolsSaveVersionBadgeText"));
                var load = Find(root.transform, "UnityToolsLoadScenarioButton").GetComponent<Button>();
                Assert.IsTrue(load.interactable);
                StringAssert.Contains("ui5-compatible", load.GetComponentInChildren<Text>().text);
                StringAssert.Contains("可恢复", load.GetComponentInChildren<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SaveToolsExposeExactSnapshotBlockerBeforeLoad()
        {
            var root = Root(1280, 720);
            try
            {
                var repository = new InMemoryTestScenarioRepository();
                var service = MatchService.CreateWithDefaultCatalog(79, repository);
                var scenario = TestScenarioMapper.Capture(service.State, "ui5-missing-snapshot");
                scenario.ContentSnapshotId = string.Empty;
                scenario.ContentFingerprint = string.Empty;
                repository.Save(scenario);
                Build(root, service);
                OpenAdvancedTools(root);

                var summary = service.TestScenarioSummaries[0];
                Assert.AreEqual(TestScenarioRestoreStatus.MissingContentSnapshot, summary.RestoreStatus);
                Assert.IsFalse(summary.CanLoad);
                var load = Find(root.transform, "UnityToolsLoadScenarioButton").GetComponent<Button>();
                Assert.IsFalse(load.interactable);
                StringAssert.Contains("阻塞", load.GetComponentInChildren<Text>().text);
                StringAssert.Contains("精确快照", load.GetComponentInChildren<Text>().text);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void WebGlTemplateAndCloudflareReleaseContractsAreCanonical()
        {
            var template = File.ReadAllText(ProjectPath("Assets", "WebGLTemplates", "LearnHeartstone", "index.html"));
            StringAssert.Contains("Math.min(window.devicePixelRatio || 1, 1.25)", template);
            StringAssert.Contains("Math.min(window.devicePixelRatio || 1, 2)", template);
            StringAssert.DoesNotContain("config.devicePixelRatio = 1", template);
            StringAssert.Contains("config.autoSyncPersistentDataPath = true", template);
            StringAssert.Contains("window.addEventListener(\"resize\", resizeUnityCanvas)", template);
            StringAssert.Contains("window.addEventListener(\"orientationchange\", resizeUnityCanvas)", template);
            StringAssert.Contains("window.visualViewport.addEventListener(\"resize\", resizeUnityCanvas)", template);
            StringAssert.Contains("resolveChunkedDataUrl(config.dataUrl", template);
            StringAssert.Contains("data.br.chunks.json", template);
            StringAssert.Contains("var workerCount = Math.min(6, manifest.chunks.length)", template);
            StringAssert.Contains("var maxChunkRetries = 3", template);
            StringAssert.Contains("750 * Math.pow(2, attempt)", template);
            StringAssert.Contains("await Promise.all(chunkWorkers)", template);
            StringAssert.Contains("chunkBuffers[index] = chunkBuffer", template);
            StringAssert.Contains("new Blob(chunkBuffers", template);
            StringAssert.Contains("URL.revokeObjectURL(chunkedDataUrl)", template);

            var pngDownloadBridge = File.ReadAllText(ProjectPath(
                "Assets",
                "Plugins",
                "WebGL",
                "LearnHearthstoneDownload.jslib"));
            StringAssert.Contains("LearnHearthstoneDownloadPng", pngDownloadBridge);
            StringAssert.Contains("new Blob([bytes], { type: \"image/png\" })", pngDownloadBridge);
            StringAssert.Contains("var fileName = UTF8ToString(fileNamePointer)", pngDownloadBridge);
            StringAssert.Contains("link.download = fileName", pngDownloadBridge);
            StringAssert.Contains("link.click()", pngDownloadBridge);
            StringAssert.Contains("URL.revokeObjectURL", pngDownloadBridge);

            var headersPath = ProjectPath("Deploy", "Cloudflare", "_headers");
            Assert.IsTrue(File.Exists(headersPath), headersPath);
            var headers = File.ReadAllText(headersPath);
            StringAssert.Contains("/Build/*.wasm.br", headers);
            StringAssert.Contains("Content-Type: application/wasm", headers);
            StringAssert.Contains("Content-Encoding: br", headers);
            StringAssert.Contains("/Build/*.framework.js.br", headers);
            StringAssert.Contains("/Build/*.data.br", headers);
            StringAssert.Contains("/Build/*.data.br.chunks.json", headers);
            StringAssert.Contains("/Build/*.data-chunk.br", headers);
            StringAssert.Contains("/content/:asset.v:version.json", headers);
            StringAssert.DoesNotContain("/content/*.v*.json", headers);
            StringAssert.Contains("Cache-Control: public, max-age=31536000, immutable", headers);
            Assert.IsFalse(File.Exists(ProjectPath("Deploy", "Vercel", "vercel.json")));

            var assembler = File.ReadAllText(ProjectPath("Tools", "Release", "assemble-release-candidate.mjs"));
            StringAssert.Contains("Deploy\", \"Cloudflare\", \"_headers", assembler);
            StringAssert.Contains("25 * 1024 * 1024", assembler);
            StringAssert.Contains("Cloudflare Pages", assembler);
            StringAssert.Contains("replaceBrotliDataWithChunks", assembler);
            StringAssert.Contains("validateCandidateWebGLSite", assembler);
            StringAssert.Contains("darkGiftLocalizationZhCN", assembler);
            StringAssert.Contains("sourceFile: \"battlegroundsGameVersions.json\"", assembler);
            StringAssert.Contains("sourceFile: \"battlegroundsRulesets.json\"", assembler);
            StringAssert.Contains("sourceFile: \"battlegroundsDarkGifts.json\"", assembler);
            StringAssert.DoesNotContain("case \"versions\"", assembler);
            StringAssert.DoesNotContain("case \"dark-gifts\"", assembler);
            StringAssert.DoesNotContain("vercelConfigPath", assembler);
            StringAssert.DoesNotContain("vercel.json", assembler);

            var webGlBuilder = File.ReadAllText(ProjectPath(
                "Assets",
                "LearnHearthstone",
                "Editor",
                "WebGLReleaseBuild.cs"));
            StringAssert.Contains("EditorUserBuildSettings.buildScriptsOnly = false", webGlBuilder);
            StringAssert.Contains("PlayerSettings.WebGL.template = \"PROJECT:LearnHeartstone\"", webGlBuilder);
            StringAssert.Contains("PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli", webGlBuilder);
            StringAssert.Contains("PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off", webGlBuilder);
            StringAssert.Contains("PlayerSettings.WebGL.emscriptenArgs = string.Empty", webGlBuilder);

            var runtimeAssembly = File.ReadAllText(ProjectPath(
                "Assets",
                "LearnHearthstone",
                "LearnHearthstone.Runtime.asmdef"));
            StringAssert.DoesNotContain("\"Wx\"", runtimeAssembly);

            var distributionChannel = File.ReadAllText(ProjectPath(
                "Assets",
                "LearnHearthstone",
                "Runtime",
                "Presentation",
                "LearnHearthstoneDistributionChannel.cs"));
            StringAssert.Contains("System.Type.GetType(\"WeChatWASM.WX, Wx\"", distributionChannel);
            StringAssert.DoesNotContain("WeChatWASM.WX.GetSystemInfoSync", distributionChannel);
        }

        private static void OpenAdvancedTools(GameObject root)
        {
            Find(root.transform, "UnityQuickToolsButton").GetComponent<Button>().onClick.Invoke();
            Find(root.transform, "UnityToolsOpenAdvancedButton").GetComponent<Button>().onClick.Invoke();
        }

        private static GameObject Root(int width, int height)
        {
            var root = new GameObject("Root", typeof(RectTransform));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);
            return root;
        }

        private static void Build(GameObject root, MatchService service)
        {
            new UnityTavernTrainerView(root.transform, service, new LocalAdvisorService(), () => { }).Build();
        }

        private static string ProjectPath(params string[] segments)
        {
            var result = Directory.GetCurrentDirectory();
            foreach (var segment in segments)
            {
                result = Path.Combine(result, segment);
            }

            return result;
        }

        private static string Text(Transform root, string name)
        {
            var target = Find(root, name);
            Assert.IsNotNull(target, "Missing text object: " + name);
            return target.GetComponent<Text>().text;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var index = 0; index < root.childCount; index += 1)
            {
                var found = Find(root.GetChild(index), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
