using System;
using System.Collections;
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
            if (!Uri.TryCreate(manifestUrl, UriKind.Absolute, out var manifestUri))
            {
                completed(null, null, "Content manifest URL is invalid.");
                yield break;
            }

            byte[] manifestBytes;
            using (var request = UnityWebRequest.Get(manifestUri.AbsoluteUri))
            {
                request.timeout = RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed(null, null, Failure("manifest", request));
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
                completed(null, null, exception.Message);
                yield break;
            }

            var contentUri = new Uri(manifestUri, manifest.Minions.FileName);
            using (var request = UnityWebRequest.Get(contentUri.AbsoluteUri))
            {
                request.timeout = RequestTimeoutSeconds;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    completed(null, null, Failure("content", request));
                    yield break;
                }

                completed(manifestBytes, request.downloadHandler.data, null);
            }
        }

        private static string Failure(string resource, UnityWebRequest request)
        {
            return "Failed to download remote " + resource + " (HTTP " + request.responseCode + "): " + request.error;
        }
    }
}
