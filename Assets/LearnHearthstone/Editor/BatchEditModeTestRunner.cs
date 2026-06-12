using System;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace LearnHearthstone.Editor
{
    public static class BatchEditModeTestRunner
    {
        private const string ResultPathArg = "-batchTestResults";
        private const string CategoryArg = "-batchTestCategory";
        private const string DefaultResultPath = "TestResults-OfficialConsistency.xml";

        public static void RunEditMode()
        {
            RunEditModeWithCategories(null);
        }

        public static void RunStressEditMode()
        {
            RunEditModeWithCategories(new[] { "Stress" });
        }

        private static void RunEditModeWithCategories(string[] defaultCategories)
        {
            var resultPath = ReadArgument(ResultPathArg) ?? DefaultResultPath;
            var categories = ReadListArgument(CategoryArg) ?? defaultCategories;
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

            var settings = new ExecutionSettings(filter)
            {
                runSynchronously = true
            };

            Debug.Log("Starting LearnHearthstone EditMode tests. Categories: " + FormatCategories(categories));
            api.Execute(settings);
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

        private static string FormatCategories(string[] categories)
        {
            return categories == null || categories.Length == 0 ? "<all>" : string.Join(", ", categories);
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
            }

            public void TestFinished(ITestResultAdaptor result)
            {
            }
        }
    }
}
