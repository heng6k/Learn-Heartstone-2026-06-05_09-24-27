using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Application.Services
{
    public sealed class FileStrategyGuideAuthoringRepository
    {
        private const int FrozenRecordSchemaVersion = 1;
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string draftsDirectory;
        private readonly string frozenDirectory;

        public FileStrategyGuideAuthoringRepository(string rootDirectory = null)
        {
            var root = string.IsNullOrWhiteSpace(rootDirectory)
                ? Path.Combine(UnityEngine.Application.persistentDataPath, "StrategyGuideAuthoring")
                : rootDirectory;
            draftsDirectory = Path.Combine(root, "Drafts");
            frozenDirectory = Path.Combine(root, "Frozen");
        }

        public IReadOnlyList<string> ListDraftIds()
        {
            if (!Directory.Exists(draftsDirectory))
            {
                return Array.Empty<string>();
            }

            return Directory.GetFiles(draftsDirectory, "*.json")
                .Select(path => ReadDraft(path).DraftId)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToList();
        }

        public void SaveDraft(StrategyGuideAuthoringDraft draft)
        {
            ValidateDraftIdentity(draft);
            WriteMutableAtomically(
                DraftPath(draft.DraftId),
                JsonUtility.ToJson(Clone(draft), true));
        }

        public StrategyGuideAuthoringDraft LoadDraft(string draftId)
        {
            var path = DraftPath(draftId);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Strategy guide draft does not exist: " + draftId + ".");
            }

            var draft = ReadDraft(path);
            if (!string.Equals(draft.DraftId, draftId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Strategy guide draft identity does not match its file.");
            }
            return draft;
        }

        public bool DeleteDraft(string draftId)
        {
            var path = DraftPath(draftId);
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }

        public bool ContainsFrozen(string contentHash)
        {
            return File.Exists(FrozenPath(contentHash));
        }

        public void SaveFrozen(StrategyGuideAuthoringFreezeResult frozen)
        {
            if (frozen == null || !frozen.Succeeded)
            {
                throw new InvalidOperationException("Only a successful strategy guide freeze can be stored.");
            }
            ValidateContentHash(frozen.ContentHash);

            var record = new FrozenRecord
            {
                SchemaVersion = FrozenRecordSchemaVersion,
                ContentHash = frozen.ContentHash,
                RevisionId = frozen.Guide.RevisionId,
                Guide = Clone(frozen.Guide)
            };
            var json = JsonUtility.ToJson(record, true);
            var path = FrozenPath(frozen.ContentHash);
            if (File.Exists(path))
            {
                RequireSameFrozenContent(path, json);
                return;
            }

            WriteImmutableAtomically(path, json);
        }

        public StrategyGuideAuthoringFreezeResult LoadFrozen(string contentHash)
        {
            var path = FrozenPath(contentHash);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Frozen strategy guide does not exist: " + contentHash + ".");
            }

            var record = ReadFrozen(path);
            if (!string.Equals(record.ContentHash, contentHash, StringComparison.Ordinal) ||
                !string.Equals(record.RevisionId, record.Guide?.RevisionId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Frozen strategy guide identity does not match its file.");
            }
            return new StrategyGuideAuthoringFreezeResult
            {
                ContentHash = record.ContentHash,
                Guide = record.Guide
            };
        }

        private StrategyGuideAuthoringDraft ReadDraft(string path)
        {
            try
            {
                var draft = JsonUtility.FromJson<StrategyGuideAuthoringDraft>(File.ReadAllText(path, Utf8NoBom));
                ValidateDraftIdentity(draft);
                return draft;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is IOException ||
                exception is InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "Strategy guide draft file is invalid: " + Path.GetFileName(path) + ".",
                    exception);
            }
        }

        private FrozenRecord ReadFrozen(string path)
        {
            try
            {
                var record = JsonUtility.FromJson<FrozenRecord>(File.ReadAllText(path, Utf8NoBom));
                if (record == null || record.SchemaVersion != FrozenRecordSchemaVersion || record.Guide == null)
                {
                    throw new InvalidOperationException("Frozen record schema is invalid.");
                }
                ValidateContentHash(record.ContentHash);
                return record;
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is IOException ||
                exception is InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "Frozen strategy guide file is invalid: " + Path.GetFileName(path) + ".",
                    exception);
            }
        }

        private void RequireSameFrozenContent(string path, string expectedJson)
        {
            var current = File.ReadAllText(path, Utf8NoBom);
            if (!string.Equals(current, expectedJson, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Frozen strategy guide content hash already exists with different content: " +
                    Path.GetFileNameWithoutExtension(path) + ".");
            }
        }

        private static void WriteMutableAtomically(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = TemporaryPath(path);
            try
            {
                File.WriteAllText(temporary, json, Utf8NoBom);
                if (File.Exists(path))
                {
                    File.Replace(temporary, path, null);
                }
                else
                {
                    File.Move(temporary, path);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private void WriteImmutableAtomically(string path, string json)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = TemporaryPath(path);
            try
            {
                File.WriteAllText(temporary, json, Utf8NoBom);
                try
                {
                    File.Move(temporary, path);
                }
                catch (IOException) when (File.Exists(path))
                {
                    RequireSameFrozenContent(path, json);
                }
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
        }

        private string DraftPath(string draftId)
        {
            ValidateStableId(draftId, "draft id");
            return Path.Combine(draftsDirectory, draftId + ".json");
        }

        private string FrozenPath(string contentHash)
        {
            ValidateContentHash(contentHash);
            return Path.Combine(frozenDirectory, contentHash + ".json");
        }

        private static void ValidateDraftIdentity(StrategyGuideAuthoringDraft draft)
        {
            if (draft == null || draft.SchemaVersion != StrategyGuideAuthoringFreezeService.SchemaVersion)
            {
                throw new InvalidOperationException("Strategy guide draft schema is not supported.");
            }
            ValidateStableId(draft.DraftId, "draft id");
        }

        private static void ValidateStableId(string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 96 || value.Any(character =>
                    !char.IsLetterOrDigit(character) && character != '-' && character != '_' && character != '.'))
            {
                throw new InvalidOperationException("Strategy guide " + label + " is invalid.");
            }
        }

        private static void ValidateContentHash(string value)
        {
            if (value == null || value.Length != 64 || value.Any(character =>
                    !(character >= '0' && character <= '9') &&
                    !(character >= 'a' && character <= 'f')))
            {
                throw new InvalidOperationException("Strategy guide content hash is invalid.");
            }
        }

        private static string TemporaryPath(string path)
        {
            return path + ".tmp-" + Guid.NewGuid().ToString("N");
        }

        private static T Clone<T>(T value)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        [Serializable]
        private sealed class FrozenRecord
        {
            public int SchemaVersion;
            public string ContentHash;
            public string RevisionId;
            public StrategyGuideDefinition Guide;
        }
    }
}
