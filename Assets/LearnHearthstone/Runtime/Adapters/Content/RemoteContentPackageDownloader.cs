using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using LearnHearthstone.Application.Content;
using UnityEngine.Networking;

namespace LearnHearthstone.Adapters.Content
{
    public sealed class RemoteContentPackageDownloader
    {
        private const int RequestTimeoutSeconds = 10;

        public IEnumerator Download(
            string manifestUrl,
            string clientVersion,
            Action<byte[], byte[], string> completed)
        {
            if (completed == null)
            {
                throw new ArgumentNullException(nameof(completed));
            }

            ContentPackageDownload package = null;
            string failureReason = null;
            yield return Download(
                manifestUrl,
                clientVersion,
                (download, failure) =>
                {
                    package = download;
                    failureReason = failure;
                });
            if (package == null)
            {
                completed(null, null, failureReason);
                yield break;
            }

            try
            {
                var manifest = ContentPackageValidator.ParseManifest(package.ManifestBytes);
                if (!manifest.IsLegacyV1 || manifest.Minions == null ||
                    !package.Files.TryGetValue(manifest.Minions.FileName, out var contentBytes))
                {
                    throw new InvalidDataException("Legacy download callback only supports protocol v1.");
                }

                completed(package.ManifestBytes, contentBytes, null);
            }
            catch (Exception exception)
            {
                completed(null, null, exception.Message);
            }
        }

        public IEnumerator Download(
            string manifestUrl,
            string clientVersion,
            Action<ContentPackageDownload, string> completed)
        {
            if (completed == null)
            {
                throw new ArgumentNullException(nameof(completed));
            }
            if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out var manifestUri))
            {
                completed(null, "Content manifest URL is invalid.");
                yield break;
            }

            byte[] manifestBytes;
            using (var request = UnityWebRequest.Get(manifestUri.AbsoluteUri))
            {
                request.timeout = RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed(null, Failure("manifest", request));
                    yield break;
                }

                manifestBytes = request.downloadHandler.data;
            }

            ContentPackageManifest manifest;
            try
            {
                manifest = ContentPackageValidator.ParseManifest(manifestBytes);
                ContentPackageValidator.ValidateManifest(manifest, clientVersion);
            }
            catch (Exception exception)
            {
                completed(null, exception.Message);
                yield break;
            }

            var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var file in manifest.Files)
            {
                var contentUri = new Uri(manifestUri, file.FileName);
                using (var request = UnityWebRequest.Get(contentUri.AbsoluteUri))
                {
                    request.timeout = RequestTimeoutSeconds;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        completed(null, Failure("content file " + file.FileName, request));
                        yield break;
                    }

                    files.Add(file.FileName, request.downloadHandler.data);
                }
            }

            try
            {
                ContentPackageValidator.Validate(manifest, files, clientVersion);
                completed(new ContentPackageDownload(manifestBytes, files), null);
            }
            catch (Exception exception)
            {
                completed(null, exception.Message);
            }
        }

        private static string Failure(string resource, UnityWebRequest request)
        {
            return "Failed to download remote " + resource + " (HTTP " + request.responseCode + "): " + request.error;
        }
    }
}
