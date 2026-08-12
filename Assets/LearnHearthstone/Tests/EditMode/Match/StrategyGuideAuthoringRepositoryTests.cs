using System;
using System.IO;
using System.Linq;
using LearnHearthstone.Adapters.Content;
using LearnHearthstone.Adapters.Data;
using LearnHearthstone.Application.Services;
using LearnHearthstone.Domain.Models;
using NUnit.Framework;
using UnityEngine;

namespace LearnHearthstone.Tests.EditMode
{
    public sealed class StrategyGuideAuthoringRepositoryTests
    {
        [Test]
        public void DraftSaveLoadIsAtomicSortedAndDetachedFromTheCaller()
        {
            var directory = TemporaryDirectory();
            try
            {
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                var first = Draft("draft-zeta");
                var originalTitle = first.Guide.Title;
                repository.SaveDraft(first);
                first.Guide.Title = "调用方已修改";

                Assert.AreEqual(originalTitle, repository.LoadDraft(first.DraftId).Guide.Title);

                var second = Draft("draft-alpha");
                repository.SaveDraft(second);
                CollectionAssert.AreEqual(
                    new[] { "draft-alpha", "draft-zeta" },
                    repository.ListDraftIds());

                var updated = repository.LoadDraft(first.DraftId);
                updated.Guide.Title = "草稿新标题";
                repository.SaveDraft(updated);
                Assert.AreEqual("草稿新标题", repository.LoadDraft(first.DraftId).Guide.Title);
                Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp-*", SearchOption.AllDirectories).Length);
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void FrozenRevisionIsIdempotentAndRejectsContentConflict()
        {
            var directory = TemporaryDirectory();
            try
            {
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                var frozen = Freeze(Draft("draft-frozen"));
                repository.SaveFrozen(frozen);
                repository.SaveFrozen(frozen);

                Assert.IsTrue(repository.ContainsFrozen(frozen.ContentHash));
                var loaded = repository.LoadFrozen(frozen.ContentHash);
                Assert.AreEqual(frozen.ContentHash, loaded.ContentHash);
                Assert.AreEqual(frozen.Guide.RevisionId, loaded.Guide.RevisionId);

                var conflict = new StrategyGuideAuthoringFreezeResult
                {
                    ContentHash = frozen.ContentHash,
                    Guide = Clone(frozen.Guide)
                };
                conflict.Guide.Title = "冲突标题";
                Assert.Throws<InvalidOperationException>(() => repository.SaveFrozen(conflict));
                Assert.AreNotEqual("冲突标题", repository.LoadFrozen(frozen.ContentHash).Guide.Title);
                Assert.AreEqual(0, Directory.GetFiles(directory, "*.tmp-*", SearchOption.AllDirectories).Length);
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void DeleteDraftRemovesOnlyTheRequestedLocalDraft()
        {
            var directory = TemporaryDirectory();
            try
            {
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                var removed = Draft("draft-remove");
                var kept = Draft("draft-keep");
                var frozen = Freeze(removed);
                repository.SaveDraft(removed);
                repository.SaveDraft(kept);
                repository.SaveFrozen(frozen);

                Assert.IsTrue(repository.DeleteDraft(removed.DraftId));
                Assert.IsFalse(repository.DeleteDraft(removed.DraftId));
                CollectionAssert.AreEqual(new[] { kept.DraftId }, repository.ListDraftIds());
                Assert.Throws<InvalidOperationException>(() => repository.LoadDraft(removed.DraftId));
                Assert.IsTrue(repository.ContainsFrozen(frozen.ContentHash));
                Assert.AreEqual(frozen.ContentHash, repository.LoadFrozen(frozen.ContentHash).ContentHash);
                Assert.Throws<InvalidOperationException>(() => repository.DeleteDraft("../outside"));
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        [Test]
        public void RepositoryRejectsTraversalAndUnknownIdentity()
        {
            var directory = TemporaryDirectory();
            try
            {
                var repository = new FileStrategyGuideAuthoringRepository(directory);
                var draft = Draft("../outside");
                Assert.Throws<InvalidOperationException>(() => repository.SaveDraft(draft));
                Assert.Throws<InvalidOperationException>(() => repository.LoadDraft("../outside"));
                Assert.Throws<InvalidOperationException>(() => repository.LoadFrozen(new string('a', 63) + "z"));
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        }

        private static StrategyGuideAuthoringFreezeResult Freeze(StrategyGuideAuthoringDraft draft)
        {
            var snapshot = EmbeddedGameCatalogSnapshotLoader.Load("0.1.0-alpha");
            var catalog = StrategyGuideCatalogLoader.LoadFromResources();
            var version = snapshot.VersionedContent.CreateResolver().Resolve(GameVersionIds.Season14Preview, snapshot);
            var result = StrategyGuideAuthoringFreezeService.Freeze(draft, catalog, version);
            Assert.IsTrue(result.Succeeded, string.Join(" | ", result.Diagnostics));
            return result;
        }

        private static StrategyGuideAuthoringDraft Draft(string draftId)
        {
            var guide = StrategyGuideCatalogLoader.LoadFromResources().Guides[0];
            return new StrategyGuideAuthoringDraft
            {
                DraftId = draftId,
                Guide = Clone(guide)
            };
        }

        private static T Clone<T>(T value)
        {
            return JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
        }

        private static string TemporaryDirectory()
        {
            return Path.Combine(
                UnityEngine.Application.temporaryCachePath,
                "strategy-guide-authoring-" + Guid.NewGuid().ToString("N"));
        }

        private static void DeleteTemporaryDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
