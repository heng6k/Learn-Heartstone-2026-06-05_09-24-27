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
        private static readonly string[] StressExcludedCategories = { "Marathon" };
        private static readonly HashSet<string> TestMethodAttributeNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "NUnit.Framework.TestAttribute",
            "NUnit.Framework.TestCaseAttribute",
            "NUnit.Framework.TestCaseSourceAttribute"
        };

        public static void RunEditMode()
        {
            RunEditModeWithCategories(null, DefaultExcludedCategories);
        }

        public static void RunStressEditMode()
        {
            RunEditModeWithCategories(new[] { "Stress" }, StressExcludedCategories);
        }

        private static void RunEditModeWithCategories(string[] defaultCategories, string[] defaultExcludedCategories)
        {
            var resultPath = ReadArgument(ResultPathArg) ?? DefaultResultPath;
            var manifestPath = ReadArgument(ManifestPathArg) ?? DefaultManifestPath;
            var categories = ReadListArgument(CategoryArg) ?? defaultCategories;
            var testNames = ReadListArgument(TestNameArg) ?? ReadListFileArgument(TestNameFileArg);
            var excludedCategories = defaultExcludedCategories;

            var shardCount = ReadIntArgument(ShardCountArg, 1);
            var shardIndex = ReadIntArgument(ShardIndexArg, 0);
            ValidateShard(shardIndex, shardCount);
            if (shardCount > 1)
            {
                testNames = testNames ?? DiscoverTestMethodNames();
                testNames = ApplyShard(testNames, shardIndex, shardCount);
            }

            WriteManifest(
                manifestPath,
                resultPath,
                testNames,
                categories,
                excludedCategories,
                shardIndex,
                shardCount,
                null,
                null,
                "Not started");

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ExitOnFinishedCallback(
                resultPath,
                manifestPath,
                testNames,
                categories,
                excludedCategories,
                shardIndex,
                shardCount));

            var filter = new Filter
            {
                testMode = TestMode.EditMode,
                assemblyNames = new[] { "LearnHearthstone.Tests" },
                categoryNames = BuildCategoryFilters(categories, excludedCategories)
            };

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

        private static string[] BuildCategoryFilters(string[] categories, string[] excludedCategories)
        {
            var filters = new List<string>();
            if (categories != null)
            {
                filters.AddRange(categories.Where(category => !string.IsNullOrWhiteSpace(category)));
            }

            if (excludedCategories != null)
            {
                filters.AddRange(excludedCategories
                    .Where(category => !string.IsNullOrWhiteSpace(category))
                    .Select(category => category.StartsWith("!", StringComparison.Ordinal) ? category : "!" + category));
            }

            return filters.Count == 0 ? null : filters.ToArray();
        }

        private static string[] DiscoverTestMethodNames()
        {
            var testAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "LearnHearthstone.Tests");
            if (testAssembly == null)
            {
                throw new InvalidOperationException("LearnHearthstone.Tests assembly was not loaded; EditMode tests cannot be sharded safely.");
            }

            var tests = new List<string>();
            foreach (var type in testAssembly.GetTypes().OrderBy(type => type.FullName, StringComparer.Ordinal))
            {
                if (string.IsNullOrWhiteSpace(type.FullName) ||
                    !type.FullName.StartsWith("LearnHearthstone.Tests.EditMode.", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                             .OrderBy(method => method.Name, StringComparer.Ordinal))
                {
                    if (!method.GetCustomAttributesData()
                            .Any(attribute => TestMethodAttributeNames.Contains(attribute.AttributeType.FullName)))
                    {
                        continue;
                    }

                    tests.Add(type.FullName + "." + method.Name);
                }
            }

            var distinctTests = tests.Distinct(StringComparer.Ordinal).ToArray();
            if (distinctTests.Length == 0)
            {
                throw new InvalidOperationException("No EditMode test methods were discovered for sharding.");
            }

            Debug.Log("Discovered " + distinctTests.Length + " EditMode test method selector(s) for sharding.");
            return distinctTests;
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

            var selectedTests = testNames
                .Where((testName, index) => index % shardCount == shardIndex)
                .ToArray();
            if (selectedTests.Length == 0)
            {
                throw new InvalidOperationException("The requested shard contains no test selectors.");
            }

            return selectedTests;
        }

        private static void ValidateShard(int shardIndex, int shardCount)
        {
            if (shardCount < 1)
            {
                throw new InvalidOperationException("Shard count must be at least 1.");
            }

            if (shardIndex < 0 || shardIndex >= shardCount)
            {
                throw new InvalidOperationException("Shard index must be in [0, shardCount).");
            }
        }

        private static string[] GetLeafTestNames(ITestAdaptor test)
        {
            var names = new List<string>();
            AddLeafTestNames(test, names);
            return names.ToArray();
        }

        private static void AddLeafTestNames(ITestAdaptor test, List<string> names)
        {
            if (test == null)
            {
                return;
            }

            if (!test.HasChildren)
            {
                if (!test.IsSuite && !string.IsNullOrWhiteSpace(test.FullName))
                {
                    names.Add(test.FullName);
                }

                return;
            }

            foreach (var child in test.Children)
            {
                AddLeafTestNames(child, names);
            }
        }

        private static void WriteManifest(
            string path,
            string resultPath,
            string[] requestedTestNames,
            string[] categories,
            string[] excludedCategories,
            int shardIndex,
            int shardCount,
            string[] discoveredTestNames,
            ITestResultAdaptor result,
            string executionStatus)
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
                    "# Requested selector count: " + (requestedTestNames == null ? "<runner-selected>" : requestedTestNames.Length.ToString()),
                    "# Execution status: " + executionStatus,
                    "# Discovered leaf count: " + (discoveredTestNames == null ? "<pending>" : discoveredTestNames.Length.ToString()),
                    "# Result XML: " + resultPath
                };
                if (result != null)
                {
                    var executedCount = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                    lines.Add("# Executed leaf count: " + executedCount);
                    lines.Add("# Passed: " + result.PassCount);
                    lines.Add("# Failed: " + result.FailCount);
                    lines.Add("# Skipped: " + result.SkipCount);
                    lines.Add("# Inconclusive: " + result.InconclusiveCount);
                }
                else
                {
                    lines.Add("# Executed leaf count: <pending>");
                }

                if (requestedTestNames != null)
                {
                    lines.AddRange(requestedTestNames.Select(testName => "# Requested selector: " + testName));
                }

                if (discoveredTestNames != null)
                {
                    lines.Add("# Discovered leaf tests follow; non-comment lines remain valid -batchTestNameFile input.");
                    lines.AddRange(discoveredTestNames);
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
            private readonly string manifestPath;
            private readonly string[] requestedTestNames;
            private readonly string[] categories;
            private readonly string[] excludedCategories;
            private readonly int shardIndex;
            private readonly int shardCount;
            private string[] discoveredTestNames;

            public ExitOnFinishedCallback(
                string resultPath,
                string manifestPath,
                string[] requestedTestNames,
                string[] categories,
                string[] excludedCategories,
                int shardIndex,
                int shardCount)
            {
                this.resultPath = resultPath;
                this.manifestPath = manifestPath;
                this.requestedTestNames = requestedTestNames;
                this.categories = categories;
                this.excludedCategories = excludedCategories;
                this.shardIndex = shardIndex;
                this.shardCount = shardCount;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                discoveredTestNames = GetLeafTestNames(testsToRun);
                WriteManifest(
                    manifestPath,
                    resultPath,
                    requestedTestNames,
                    categories,
                    excludedCategories,
                    shardIndex,
                    shardCount,
                    discoveredTestNames,
                    null,
                    "Running");
                Debug.Log(
                    "LearnHearthstone EditMode test run started. NUnit test cases: " + testsToRun.TestCaseCount +
                    ", manifest leaves: " + discoveredTestNames.Length + ".");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, resultPath);
                discoveredTestNames = discoveredTestNames ?? GetLeafTestNames(result.Test);
                WriteManifest(
                    manifestPath,
                    resultPath,
                    requestedTestNames,
                    categories,
                    excludedCategories,
                    shardIndex,
                    shardCount,
                    discoveredTestNames,
                    result,
                    "Finished");
                var executedCount = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
                Debug.LogFormat(
                    "LearnHearthstone EditMode test run finished. Executed: {0}, Passed: {1}, Failed: {2}, Skipped: {3}, Inconclusive: {4}",
                    executedCount,
                    result.PassCount,
                    result.FailCount,
                    result.SkipCount,
                    result.InconclusiveCount);

                if (executedCount == 0)
                {
                    Debug.LogError("LearnHearthstone EditMode test run selected zero executable test cases.");
                }

                EditorApplication.Exit(result.FailCount == 0 && executedCount > 0 ? 0 : 1);
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
