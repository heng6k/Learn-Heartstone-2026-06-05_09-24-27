using System;
using System.IO;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Persistence
{
    public interface ICardPoolVersionRepository
    {
        CardPoolVersionStore Load();
        void Save(CardPoolVersionStore store);
    }

    public sealed class JsonCardPoolVersionRepository : ICardPoolVersionRepository
    {
        private readonly string path;

        public JsonCardPoolVersionRepository(string fileName = "card-pool-versions.json")
        {
            path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
        }

        public JsonCardPoolVersionRepository(string directory, string fileName)
        {
            path = Path.Combine(directory, fileName);
        }

        public CardPoolVersionStore Load()
        {
            if (!File.Exists(path))
            {
                return new CardPoolVersionStore();
            }

            try
            {
                return CardPoolVersionFactory.NormalizeStore(JsonUtility.FromJson<CardPoolVersionStore>(File.ReadAllText(path)));
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to load card pool versions: " + exception.Message);
                return new CardPoolVersionStore();
            }
        }

        public void Save(CardPoolVersionStore store)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(CardPoolVersionFactory.NormalizeStore(store), true));
        }
    }
}
