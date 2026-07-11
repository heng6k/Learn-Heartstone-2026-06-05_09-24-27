using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LearnHearthstone.Domain.Engine;
using LearnHearthstone.Domain.Models;
using UnityEngine;

namespace LearnHearthstone.Application.Services
{
    public interface ITestScenarioRepository
    {
        IReadOnlyList<string> ListScenarioNames();
        void Save(TestScenarioDefinition scenario);
        TestScenarioDefinition Load(string name);
        bool Exists(string name);
    }

    public sealed class InMemoryTestScenarioRepository : ITestScenarioRepository
    {
        private readonly Dictionary<string, TestScenarioDefinition> scenarios = new Dictionary<string, TestScenarioDefinition>(StringComparer.Ordinal);

        public IReadOnlyList<string> ListScenarioNames()
        {
            return scenarios.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        public void Save(TestScenarioDefinition scenario)
        {
            if (scenario == null || string.IsNullOrWhiteSpace(scenario.Name))
            {
                throw new InvalidOperationException("Scenario name is required.");
            }

            scenarios[scenario.Name] = TestScenarioMapper.Clone(scenario);
        }

        public TestScenarioDefinition Load(string name)
        {
            if (!scenarios.TryGetValue(name, out var scenario))
            {
                throw new InvalidOperationException("Scenario does not exist: " + name);
            }

            return TestScenarioMapper.Clone(scenario);
        }

        public bool Exists(string name)
        {
            return scenarios.ContainsKey(name);
        }
    }

    public sealed class FileTestScenarioRepository : ITestScenarioRepository
    {
        private readonly string directory;

        public FileTestScenarioRepository(string directory = null)
        {
            this.directory = string.IsNullOrEmpty(directory)
                ? Path.Combine(UnityEngine.Application.persistentDataPath, "TestScenarios")
                : directory;
        }

        public IReadOnlyList<string> ListScenarioNames()
        {
            if (!Directory.Exists(directory))
            {
                return new List<string>();
            }

            return Directory.GetFiles(directory, "*.json")
                .Select(path => Path.GetFileNameWithoutExtension(path))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();
        }

        public void Save(TestScenarioDefinition scenario)
        {
            if (scenario == null || string.IsNullOrWhiteSpace(scenario.Name))
            {
                throw new InvalidOperationException("Scenario name is required.");
            }

            var current = TestScenarioMapper.Clone(scenario);
            Directory.CreateDirectory(directory);
            var json = JsonUtility.ToJson(current, true);
            File.WriteAllText(PathFor(current.Name), json);
        }

        public TestScenarioDefinition Load(string name)
        {
            var path = PathFor(name);
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Scenario does not exist: " + name);
            }

            try
            {
                return TestScenarioMigration.MigrateToCurrent(JsonUtility.FromJson<TestScenarioDefinition>(File.ReadAllText(path)));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("Scenario file is invalid: " + name, exception);
            }
        }

        public bool Exists(string name)
        {
            return File.Exists(PathFor(name));
        }

        private string PathFor(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Scenario name is required.");
            }

            return Path.Combine(directory, SanitizeFileName(name) + ".json");
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = name.Select(character => invalid.Contains(character) ? '-' : character).ToArray();
            return new string(chars).Trim();
        }
    }
}
