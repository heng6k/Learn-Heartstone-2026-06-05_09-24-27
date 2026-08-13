using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Advisor;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Adapters.Persistence;
using LearnHearthstone.Application.Content;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Data;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.Common;
using LearnHearthstone.Presentation.MainHub;
using LearnHearthstone.Presentation.TavernTrainer;
using LearnHearthstone.Presentation.TavernTrainer.Realistic;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine.InputSystem.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation
{
    public sealed class LearnHearthstoneBootstrap : MonoBehaviour
    {
        private enum ViewRoute
        {
            Hub,
            VersionCenter,
            StrategyGuides,
            Setup,
            UnityTrainer,
            RealisticTrainer,
            LegacyTrainer
        }

        private const string UiFontResourcePath = "Fonts/NotoSansSC-Regular";

        private Canvas canvas;
        private Transform routeRoot;
        private GameCatalogSnapshot catalogSnapshot;
        private ICardPoolVersionRepository cardPoolVersionRepository;
        private MatchService matchService;
        private StrategyGuideSession strategyGuideSession;
        private IAdvisorService advisor;
        private bool useEnglish;
        private ViewRoute currentRoute;
        private UnityTavernTribeSelectionView tribeSelectionView;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private UnityTavernLayoutMode lastLayoutMode;
        private bool initialized;

        private void Awake()
        {
            LearnHearthstoneDistributionChannel.ConfigureRuntime();
            ConfigureUiFont();
            EnsureEventSystem();
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }

            ConfigureCanvas(canvas);
            RememberScreenLayout();

            cardPoolVersionRepository = new JsonCardPoolVersionRepository();
            advisor = new LocalAdvisorService();
        }

        private IEnumerator Start()
        {
            var clientVersion = UnityEngine.Application.version;
            ContentPackageDownload remotePackage = null;
            string remoteFailureReason = null;
            var manifestUrl = ResolveContentManifestUrl();
            if (!string.IsNullOrWhiteSpace(manifestUrl))
            {
                yield return new RemoteContentPackageDownloader().Download(
                    manifestUrl,
                    clientVersion,
                    (package, failure) =>
                    {
                        remotePackage = package;
                        remoteFailureReason = failure;
                    });
            }

            catalogSnapshot = new GameCatalogSnapshotResolver(
                clientVersion,
                preferEmbeddedFallback: UnityEngine.Application.isEditor).Resolve(
                remotePackage,
                remoteFailureReason);
            initialized = true;
            if (TryOpenRequestedStrategyGuide(UnityEngine.Application.absoluteURL))
            {
                yield break;
            }
            ShowChannelHome();
        }

        private bool TryOpenRequestedStrategyGuide(string absoluteUrl)
        {
            if (!TryResolveStrategyGuideLaunch(absoluteUrl, out var guideId, out var profileId))
            {
                return false;
            }

            try
            {
                StartStrategyGuide(guideId, profileId);
                return true;
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogWarning("Ignored invalid strategy guide launch parameters: " + exception.Message);
                return false;
            }
        }

        public static bool TryResolveStrategyGuideLaunch(string absoluteUrl, out string guideId, out string profileId)
        {
            guideId = null;
            profileId = null;
            if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            foreach (var part in uri.Query.TrimStart('?').Split('&'))
            {
                var separator = part.IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                var key = Uri.UnescapeDataString(part.Substring(0, separator));
                var value = Uri.UnescapeDataString(part.Substring(separator + 1));
                if (string.Equals(key, "guide", StringComparison.OrdinalIgnoreCase))
                {
                    guideId = value;
                }
                else if (string.Equals(key, "profile", StringComparison.OrdinalIgnoreCase))
                {
                    profileId = value;
                }
            }

            return !string.IsNullOrWhiteSpace(guideId) && !string.IsNullOrWhiteSpace(profileId);
        }

        private void ShowChannelHome()
        {
            if (LearnHearthstoneDistributionChannel.IsWeChatMiniGame)
            {
                ShowStrategyGuides();
                return;
            }

            ShowHub();
        }

        private void Update()
        {
            if (!initialized)
            {
                return;
            }
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight)
            {
                return;
            }

            var layout = UnityTavernLayoutContext.Current();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastLayoutMode = layout.Mode;
            ConfigureCanvas(canvas, layout);
            RebuildCurrentRoute(layout);
        }

        private void ShowHub()
        {
            currentRoute = ViewRoute.Hub;
            tribeSelectionView = null;
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            var versionCatalog = catalogSnapshot.VersionedContent?.Versions ?? GameVersionCatalog.CreateBuiltIn();
            var currentVersionId = ResolveCurrentVersionId(versionCatalog);
            var currentVersion = versionCatalog.Summaries.First(summary => string.Equals(summary.Id, currentVersionId, StringComparison.OrdinalIgnoreCase));
            new MainHubView(
                routeRoot,
                ShowLegacyTrainer,
                ShowRealisticTrainer,
                ShowUnityTrainer,
                useEnglish: useEnglish,
                languageChanged: SetLanguage,
                currentGameVersion: currentVersion,
                openVersionCenter: catalogSnapshot.VersionedContent != null ? ShowVersionCenter : (Action)null,
                openStrategyGuides: catalogSnapshot.VersionedContent != null ? ShowStrategyGuides : (Action)null).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowVersionCenter()
        {
            var versionedContent = catalogSnapshot.VersionedContent;
            if (versionedContent == null)
            {
                ShowHub();
                return;
            }

            currentRoute = ViewRoute.VersionCenter;
            tribeSelectionView = null;
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            new GameVersionCenterView(
                routeRoot,
                versionedContent,
                ResolveCurrentVersionId(versionedContent.Versions),
                ShowHub,
                useEnglish: useEnglish).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowStrategyGuides()
        {
            var versionedContent = catalogSnapshot.VersionedContent;
            if (versionedContent == null ||
                !versionedContent.Versions.Versions.Any(version =>
                    string.Equals(version.Id, GameVersionIds.Season14Preview, StringComparison.Ordinal)))
            {
                ShowHub();
                return;
            }

            currentRoute = ViewRoute.StrategyGuides;
            tribeSelectionView = null;
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            var resolver = versionedContent.CreateResolver();
            var resolvedVersion = resolver.Resolve(
                GameVersionIds.Season14Preview,
                catalogSnapshot.AsVersionResolutionSource());
            new StrategyGuideSelectionView(
                routeRoot,
                StrategyGuideCatalogLoader.LoadFromResources(),
                catalogSnapshot.ForLanguage(useEnglish),
                GameVersionIds.Season14Preview,
                StartStrategyGuide,
                LearnHearthstoneDistributionChannel.IsWeChatMiniGame ? (Action)null : ShowHub,
                useEnglish,
                resolvedVersion: resolvedVersion,
                startImportedGuide: StartImportedStrategyGuide,
                mobileOnePageOnly: LearnHearthstoneDistributionChannel.IsWeChatMiniGame).Build();
            AddDebugAspectRatioOverlay();
        }

        private void StartStrategyGuide(string guideId, string profileId)
        {
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var guide = catalog.GetGuide(guideId);
            var resolver = catalogSnapshot.VersionedContent?.CreateResolver() ?? GameVersionResolver.CreateBuiltIn();
            var version = resolver.Resolve(guide.GameVersionId, catalogSnapshot.AsVersionResolutionSource());
            OpenStrategyGuideSession(catalog, guideId, profileId, version);
        }

        private void StartImportedStrategyGuide(StrategyGuideImportResult imported)
        {
            if (imported == null || !imported.IsCompatible)
            {
                ShowStrategyGuides();
                return;
            }

            var resolver = catalogSnapshot.VersionedContent?.CreateResolver() ?? GameVersionResolver.CreateBuiltIn();
            var version = resolver.Resolve(imported.Guide.GameVersionId, catalogSnapshot.AsVersionResolutionSource());
            OpenStrategyGuideSession(
                imported.Catalog,
                imported.Guide.GuideId,
                imported.Profile.ProfileId,
                version);
        }

        private void OpenStrategyGuideSession(
            StrategyGuideCatalog catalog,
            string guideId,
            string profileId,
            ResolvedGameVersion version)
        {
            strategyGuideSession = StrategyGuideSession.Start(catalog, guideId, version, useEnglish, profileId);
            matchService = strategyGuideSession.MatchService;
            currentRoute = ViewRoute.UnityTrainer;
            tribeSelectionView = null;
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            new UnityTavernTrainerView(
                routeRoot,
                matchService,
                advisor,
                ShowChannelHome,
                ShowLegacyTrainer,
                strategyGuideSession: strategyGuideSession).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowUnityTrainer()
        {
            currentRoute = ViewRoute.Setup;
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            tribeSelectionView = new UnityTavernTribeSelectionView(
                routeRoot,
                StartUnityTrainer,
                ShowHub,
                repository: cardPoolVersionRepository,
                useEnglish: useEnglish,
                catalogs: catalogSnapshot.ForLanguage(useEnglish),
                catalogSnapshot: catalogSnapshot);
            tribeSelectionView.Build();
            AddDebugAspectRatioOverlay();
        }

        private void SetLanguage(bool nextUseEnglish)
        {
            if (useEnglish == nextUseEnglish)
            {
                return;
            }

            useEnglish = nextUseEnglish;
            ShowHub();
        }

        private void StartUnityTrainer(MatchSetupOptions setup)
        {
            currentRoute = ViewRoute.UnityTrainer;
            tribeSelectionView = null;
            var effectiveSetup = setup ?? new MatchSetupOptions();
            strategyGuideSession = null;
            matchService = CreateMatchService(effectiveSetup);
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            new UnityTavernTrainerView(routeRoot, matchService, advisor, ShowHub, ShowLegacyTrainer).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowRealisticTrainer()
        {
            currentRoute = ViewRoute.RealisticTrainer;
            tribeSelectionView = null;
            var setup = new MatchSetupOptions { UseEnglish = useEnglish };
            matchService = CreateMatchService(setup);
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            new RealisticTavernTrainerView(routeRoot, matchService, advisor, ShowHub, ShowLegacyTrainer).Build();
            AddDebugAspectRatioOverlay();
        }

        private void ShowLegacyTrainer()
        {
            currentRoute = ViewRoute.LegacyTrainer;
            tribeSelectionView = null;
            var setup = new MatchSetupOptions { UseEnglish = useEnglish };
            matchService = CreateMatchService(setup);
            ClearCanvas();
            routeRoot = CreateSafeAreaRoot(canvas.transform);
            new TavernTrainerView(routeRoot, matchService, advisor, ShowHub).Build();
            AddDebugAspectRatioOverlay();
        }

        private MatchService CreateMatchService(MatchSetupOptions setup)
        {
            var versionId = !string.IsNullOrWhiteSpace(setup.GameVersionId)
                ? setup.GameVersionId
                : !string.IsNullOrWhiteSpace(catalogSnapshot.Info.GameVersionId)
                    ? catalogSnapshot.Info.GameVersionId
                    : GameVersionIds.LegacyCompositeSandbox;
            var versionResolver = catalogSnapshot.VersionedContent?.CreateResolver() ?? GameVersionResolver.CreateBuiltIn();
            var resolvedVersion = versionResolver.Resolve(versionId, catalogSnapshot.AsVersionResolutionSource());
            return MatchService.CreateWithResolvedVersion(resolvedVersion, CreateMatchSeed(), setup: setup);
        }

        private void RebuildCurrentRoute(UnityTavernLayoutContext layout)
        {
            switch (currentRoute)
            {
                case ViewRoute.Hub:
                    ShowHub();
                    break;
                case ViewRoute.VersionCenter:
                    ShowVersionCenter();
                    break;
                case ViewRoute.StrategyGuides:
                    ShowStrategyGuides();
                    break;
                case ViewRoute.Setup:
                    tribeSelectionView?.RebuildForLayout(layout);
                    break;
                case ViewRoute.UnityTrainer:
                    canvas.GetComponentInChildren<UnityTavernTrainerController>(true)?.Rebuild();
                    break;
            }
        }

        private void RememberScreenLayout()
        {
            var layout = UnityTavernLayoutContext.Current();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastLayoutMode = layout.Mode;
        }

        private string ResolveCurrentVersionId(GameVersionCatalog versions)
        {
            var snapshotVersionId = catalogSnapshot?.Info?.GameVersionId;
            return !string.IsNullOrWhiteSpace(snapshotVersionId) &&
                   versions.Versions.Any(version => string.Equals(version.Id, snapshotVersionId, StringComparison.OrdinalIgnoreCase))
                ? snapshotVersionId
                : versions.Default.Id;
        }

        private static void ConfigureUiFont()
        {
            var font = Resources.Load<Font>(UiFontResourcePath);
            if (font == null)
            {
                Debug.LogError("Bundled UI font is missing at Resources/" + UiFontResourcePath + ".");
                return;
            }

            UiFactory.SetFontOverride(font);
        }

        private static int CreateMatchSeed()
        {
            return System.Guid.NewGuid().GetHashCode();
        }

        private static string ResolveContentManifestUrl()
        {
#if LEARN_HEARTHSTONE_WECHAT_MINIGAME
            return null;
#elif UNITY_WEBGL && !UNITY_EDITOR
            if (Uri.TryCreate(UnityEngine.Application.absoluteURL, UriKind.Absolute, out var pageUri))
            {
                return new Uri(pageUri, "content/content-manifest.json").AbsoluteUri;
            }

            Debug.LogWarning("Remote content disabled because Application.absoluteURL is invalid.");
#endif
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            return ResolveStandaloneContentManifestUrl(UnityEngine.Application.dataPath);
#else
            return null;
#endif
        }

        public static string ResolveStandaloneContentManifestUrl(string dataPath)
        {
            if (string.IsNullOrWhiteSpace(dataPath))
            {
                return null;
            }

            var manifestPath = Path.GetFullPath(Path.Combine(
                dataPath,
                "..",
                "content",
                "content-manifest.json"));
            return new Uri(manifestPath).AbsoluteUri;
        }

        private Canvas CreateCanvas()
        {
            var canvasObject = new GameObject("LearnHearthstoneCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            return canvasObject.GetComponent<Canvas>();
        }

        public static void ConfigureCanvas(Canvas target)
        {
            ConfigureCanvas(target, UnityTavernLayoutContext.Current());
        }

        public static void ConfigureCanvas(Canvas target, UnityTavernLayoutContext layout)
        {
            if (target == null)
            {
                return;
            }

            target.renderMode = RenderMode.ScreenSpaceOverlay;
            target.pixelPerfect = false;

            var scaler = UnityTavernUiStyle.EnsureComponent<CanvasScaler>(target.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = layout.CanvasMatchWidthOrHeight;

            UnityTavernUiStyle.EnsureComponent<GraphicRaycaster>(target.gameObject);
        }

        private void ClearCanvas()
        {
            routeRoot = null;
            for (var index = canvas.transform.childCount - 1; index >= 0; index -= 1)
            {
                Destroy(canvas.transform.GetChild(index).gameObject);
            }
        }

        public static Transform CreateSafeAreaRoot(Transform parent)
        {
            return UnitySafeAreaPanel.Create(parent, includeTitleSafe: true);
        }

        private void AddDebugAspectRatioOverlay()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DebugAspectRatioOverlay.Build(canvas.transform);
#endif
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            var eventSystemObject = eventSystem != null
                ? eventSystem.gameObject
                : new GameObject("EventSystem", typeof(EventSystem));

            var legacyModule = eventSystemObject.GetComponent<StandaloneInputModule>();
            if (legacyModule != null)
            {
                legacyModule.enabled = false;
                Destroy(legacyModule);
            }

            if (eventSystemObject.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystemObject.AddComponent<InputSystemUIInputModule>();
            }

            if (eventSystemObject.GetComponent<UnityInputDeviceTracker>() == null)
            {
                eventSystemObject.AddComponent<UnityInputDeviceTracker>();
            }
        }
    }
}
