using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LearnHearthstone.Domain.Models;
using LearnHearthstone.Presentation.TavernTrainer.UnityStyle;
using UnityEngine;
using UnityEngine.UI;

namespace LearnHearthstone.Presentation.MainHub
{
    public sealed class StrategyGuideShareCardExportResult
    {
        public string Path;
        public int Width;
        public int Height;
        public string ContentHash;
    }

    public static class StrategyGuideShareCardPngExporter
    {
        public const int Width = 1600;
        public const int Height = 900;

#if UNITY_WEBGL && !UNITY_EDITOR
        public const bool CanExportLocally = false;
#else
        public const bool CanExportLocally = true;
#endif
        public const bool CanExport = true;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void LearnHearthstoneDownloadPng(byte[] data, int length, string fileName);
#endif

        public static StrategyGuideShareCardExportResult Export(
            StrategyGuideShareCardModel model,
            bool useEnglish,
            string outputDirectory)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Share card output directory is required.", nameof(outputDirectory));
            }
            if (!CanExportLocally)
            {
                throw new PlatformNotSupportedException("Local PNG export is not available in WebGL.");
            }

            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, BuildFileName(model));
            File.WriteAllBytes(outputPath, RenderPng(model, useEnglish));
            return new StrategyGuideShareCardExportResult
            {
                Path = outputPath,
                Width = Width,
                Height = Height,
                ContentHash = model.ContentHash
            };
        }

        public static StrategyGuideShareCardExportResult ExportToBrowser(
            StrategyGuideShareCardModel model,
            bool useEnglish)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var fileName = BuildFileName(model);
            var png = RenderPng(model, useEnglish);
            LearnHearthstoneDownloadPng(png, png.Length, fileName);
            return new StrategyGuideShareCardExportResult
            {
                Path = fileName,
                Width = Width,
                Height = Height,
                ContentHash = model.ContentHash
            };
#else
            throw new PlatformNotSupportedException("Browser PNG download is only available in WebGL.");
#endif
        }

        public static string BuildFileName(StrategyGuideShareCardModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            if (string.IsNullOrWhiteSpace(model.ContentHashShort))
            {
                throw new InvalidOperationException("Share card content hash is required.");
            }
            if (string.IsNullOrWhiteSpace(model.ProfileId))
            {
                throw new InvalidOperationException("Share card profile id is required.");
            }

            return SafeSegment(model.GuideId) + "_" +
                SafeSegment(model.ProfileId) + "_" +
                SafeSegment(model.RevisionId) + "_" +
                SafeSegment(model.ContentHashShort) + ".png";
        }

        private static byte[] RenderPng(StrategyGuideShareCardModel model, bool useEnglish)
        {
            GameObject cameraObject = null;
            GameObject canvasObject = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;
            try
            {
                cameraObject = new GameObject("StrategyGuideShareExportCamera", typeof(Camera));
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = UnityTavernUiStyle.BackWall;
                camera.orthographic = true;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 100f;
                camera.transform.position = new Vector3(0f, 0f, -10f);

                renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                camera.targetTexture = renderTexture;

                canvasObject = new GameObject(
                    "StrategyGuideShareExportCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster));
                var canvasRect = canvasObject.GetComponent<RectTransform>();
                canvasRect.sizeDelta = new Vector2(Width, Height);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1f;
                var scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                scaler.scaleFactor = 1f;
                scaler.referencePixelsPerUnit = 100f;

                new StrategyGuideShareCardView(
                    canvasObject.transform,
                    model,
                    UnityTavernLayoutContext.ForSize(Width, Height),
                    useEnglish,
                    null,
                    includeActions: false).Build();

                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);
                Canvas.ForceUpdateCanvases();
                camera.Render();

                RenderTexture.active = renderTexture;
                texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                texture.Apply();
                return texture.EncodeToPNG();
            }
            finally
            {
                RenderTexture.active = previousActive;
                if (cameraObject != null)
                {
                    cameraObject.GetComponent<Camera>().targetTexture = null;
                }
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
                if (canvasObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(canvasObject);
                }
                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }
            }
        }

        private static string SafeSegment(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder();
            foreach (var character in value ?? string.Empty)
            {
                if (!invalid.Contains(character) && !char.IsWhiteSpace(character))
                {
                    builder.Append(character);
                }
            }
            return builder.Length == 0 ? "guide" : builder.ToString();
        }
    }
}
