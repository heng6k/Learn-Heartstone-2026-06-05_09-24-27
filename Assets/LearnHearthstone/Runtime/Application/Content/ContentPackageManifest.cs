namespace LearnHearthstone.Application.Content
{
    public sealed class ContentPackageManifest
    {
        public ContentPackageManifest(
            int protocolVersion,
            string contentVersion,
            string requiredClientVersion,
            string generatedAtUtc,
            ContentPackageFile minions)
        {
            ProtocolVersion = protocolVersion;
            ContentVersion = contentVersion;
            RequiredClientVersion = requiredClientVersion;
            GeneratedAtUtc = generatedAtUtc;
            Minions = minions;
        }

        public int ProtocolVersion { get; }
        public string ContentVersion { get; }
        public string RequiredClientVersion { get; }
        public string GeneratedAtUtc { get; }
        public ContentPackageFile Minions { get; }
    }

    public sealed class ContentPackageFile
    {
        public ContentPackageFile(string fileName, long bytes, string sha256)
        {
            FileName = fileName;
            Bytes = bytes;
            Sha256 = sha256;
        }

        public string FileName { get; }
        public long Bytes { get; }
        public string Sha256 { get; }
    }
}
