using System;
using System.IO;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Adapters.Persistence
{
    public interface ISaveRepository
    {
        void Save(MatchState state);
        MatchState Load();
        bool Exists();
    }

    public sealed class JsonSaveRepository : ISaveRepository
    {
        private readonly string path;

        public JsonSaveRepository(string fileName = "learn-hearthstone-save.json")
        {
            path = Path.Combine(UnityEngine.Application.persistentDataPath, fileName);
        }

        public JsonSaveRepository(string directory, string fileName)
        {
            path = Path.Combine(directory, fileName);
        }

        public void Save(MatchState state)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(state, true));
        }

        public MatchState Load()
        {
            if (!Exists())
            {
                return null;
            }

            return JsonUtility.FromJson<MatchState>(File.ReadAllText(path));
        }

        public bool Exists()
        {
            return File.Exists(path);
        }
    }
}
