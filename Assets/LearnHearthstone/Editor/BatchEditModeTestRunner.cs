using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LearnHearthstone.Editor
{
    public static class BatchEditModeTestRunner
    {
        private const string ResultPathArg = "-batchTestResults";
        private const string CategoryArg = "-batchTestCategory";
        private const string TestNameArg = "-batchTestName";
        private const string TestNameFileArg = "-batchTestNameFile";
        private const string ManifestPathArg = "-batchTestManifest";
        private const string ShardIndexArg = "-batchTestShardIndex";
        private const string ShardCountArg = "-batchTestShardCount";
        private const string DefaultResultPath = "TestResults-OfficialConsistency.xml";
        private const string DefaultManifestPath = "Logs/EditModeDefaultManifest.txt";
        private static readonly string[] DefaultExcludedCategories = { "Stress", "Marathon" };

        public static void RunEditMode()
        {
            RunEditModeWithCategories(null, DefaultExcludedCategories);
        }

        public static void RunStressEditMode()
        {
            RunEditModeWithCategories(new[] { "Stress" }, null);
        }

        private static void RunEditModeWithCategories(string[] defaultCategories, string[] defaultExcludedCategories)
        {
            var resultPath = ReadArgument(ResultPathArg) ?? DefaultResultPath;
            var manifestPath = ReadArgument(ManifestPathArg) ?? DefaultManifestPath;
            var categories = ReadListArgument(CategoryArg) ?? defaultCategories;
            var testNames = ReadListArgument(TestNameArg) ?? ReadListFileArgument(TestNameFileArg);
            var excludedCategories = categories == null && testNames == null
                ? defaultExcludedCategories
                : null;
            if (excludedCategories != null && excludedCategories.Length > 0)
            {
                testNames = DiscoverTestNamesExcludingCategories(excludedCategories);
            }

            var shardCount = ReadIntArgument(ShardCountArg, 1);
            var shardIndex = ReadIntArgument(ShardIndexArg, 0);
            if (testNames != null && shardCount > 1)
            {
                testNames = ApplyShard(testNames, shardIndex, shardCount);
            }

            WriteManifest(manifestPath, testNames, categories, excludedCategories, shardIndex, shardCount);

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ExitOnFinishedCallback(resultPath));

            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "LearnHearthstone.Tests" }
            };
            if (categories != null && categories.Length > 0)
            {
                filter.categoryNames = categories;
            }

            if (testNames != null && testNames.Length > 0)
            {
                filter.testNames = testNames;
            }

            var settings = new ExecutionSettings(filter)
            {
                runSynchronously = true
            };

            Debug.Log(
                "Starting LearnHearthstone EditMode tests. Categories: " + FormatCategories(categories) +
                ", Excluded: " + FormatCategories(excludedCategories) +
                ", Shard: " + FormatShard(shardIndex, shardCount) +
                ", Tests: " + FormatCategories(testNames));
            api.Execute(settings);
        }

        private static string[] DiscoverTestNamesExcludingCategories(string[] excludedCategories)
        {
            var excluded = new HashSet<string>(excludedCategories ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            var testAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "LearnHearthstone.Tests");
            if (testAssembly == null)
            {
                Debug.LogWarning("LearnHearthstone.Tests assembly was not loaded; default category exclusion could not be applied.");
                return null;
            }

            var tests = new List<string>();
            foreach (var type in testAssembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                if (!type.FullName.StartsWith("LearnHearthstone.Tests.EditMode.", StringComparison.Ordinal) ||
                    HasExcludedCategory(type, excluded))
                {
                    continue;
                }

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .OrderBy(method => method.Name, StringComparer.Ordinal))
                {
                    if (!HasAttribute(method, "NUnit.Framework.TestAttribute") ||
                        HasExcludedCategory(method, excluded))
                    {
                        continue;
                    }

                    tests.Add(type.FullName + "." + method.Name);
                }
            }

            Debug.Log("Discovered " + tests.Count + " default EditMode test(s) after excluding categories: " + string.Join(", ", excluded));
            return tests.ToArray();
        }

        private static bool HasExcludedCategory(MemberInfo member, HashSet<string> excludedCategories)
        {
            return member.GetCustomAttributesData()
                .Where(attribute => attribute.AttributeType.FullName == "NUnit.Framework.CategoryAttribute")
                .Select(attribute => attribute.ConstructorArguments.Count > 0 ? attribute.ConstructorArguments[0].Value as string : null)
                .Any(category => !string.IsNullOrWhiteSpace(category) && excludedCategories.Contains(category));
        }

        private static bool HasAttribute(MemberInfo member, string attributeTypeName)
        {
            return member.GetCustomAttributesData()
                .Any(attribute => attribute.AttributeType.FullName == attributeTypeName);
        }

        private static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length - 1; index += 1)
            {
                if (string.Equals(args[index], name, StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }

            return null;
        }

        private static string[] ReadListArgument(string name)
        {
            var value = ReadArgument(name);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var values = value
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToArray();
            return values.Length == 0 ? null : values;
        }

        private static string[] ReadListFileArgument(string name)
        {
            var path = ReadArgument(name);
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            var values = File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith("#", StringComparison.Ordinal))
                .ToArray();
            return values.Length == 0 ? null : values;
        }

        private static int ReadIntArgument(string name, int defaultValue)
        {
            var value = ReadArgument(name);
            return int.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        private static string[] ApplyShard(string[] testNames, int shardIndex, int shardCount)
        {
            if (shardCount <= 1)
            {
                return testNames;
            }

            if (shardIndex < 0 || shardIndex >= shardCount)
            {
                throw new InvalidOperationException("Shard index must be in [0, shardCount).");
            }

            return testNames
                .Where((testName, index) => index % shardCount == shardIndex)
                .ToArray();
        }

        private static void WriteManifest(string path, string[] testNames, string[] categories, string[] excludedCategories, int shardIndex, int shardCount)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var lines = new List<string>
                {
                    "# LearnHearthstone EditMode manifest",
                    "# Categories: " + FormatCategories(categories),
                    "# Excluded: " + FormatCategories(excludedCategories),
                    "# Shard: " + FormatShard(shardIndex, shardCount),
                    "# Count: " + (testNames == null ? "<runner-selected>" : testNames.Length.ToString())
                };
                if (testNames != null)
                {
                    lines.AddRange(testNames);
                }

                File.WriteAllLines(path, lines);
                Debug.Log("LearnHearthstone EditMode manifest written: " + path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Failed to write LearnHearthstone EditMode manifest: " + exception.Message);
            }
        }

        private static string FormatCategories(string[] categories)
        {
            return categories == null || categories.Length == 0 ? "<all>" : string.Join(", ", categories);
        }

        private static string FormatShard(int shardIndex, int shardCount)
        {
            return shardCount <= 1 ? "<none>" : shardIndex + "/" + shardCount;
        }

        private sealed class ExitOnFinishedCallback : ICallbacks
        {
            private readonly string resultPath;

            public ExitOnFinishedCallback(string resultPath)
            {
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("LearnHearthstone EditMode test run started.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, resultPath);
                Debug.LogFormat(
                    "LearnHearthstone EditMode test run finished. Passed: {0}, Failed: {1}, Skipped: {2}, Inconclusive: {3}",
                    result.PassCount,
                    result.FailCount,
                    result.SkipCount,
                    result.InconclusiveCount);

                EditorApplication.Exit(result.FailCount == 0 ? 0 : 1);
            }

            public void TestStarted(ITestAdaptor test)
            {
                Debug.Log("LearnHearthstone EditMode test started: " + test.FullName);
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                Debug.LogFormat(
                    "LearnHearthstone EditMode test finished: {0} [{1}]",
                    result.Test.FullName,
                    result.TestStatus);
            }
        }
    }
}
