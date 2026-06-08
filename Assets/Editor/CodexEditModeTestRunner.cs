using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class CodexEditModeTestRunner
{
    public static void Run()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var callbacks = new ResultCallbacks();
        api.RegisterCallbacks(callbacks);
        api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }) { runSynchronously = true });
        if (!callbacks.Finished)
        {
            callbacks.WriteSummary(0, 0, 1, 0, "RunFinished callback was not invoked.");
            EditorApplication.Exit(1);
        }
    }

    private sealed class ResultCallbacks : ICallbacks
    {
        public bool Finished { get; private set; }

        public void RunStarted(ITestAdaptor testsToRun)
        {
            Debug.Log("CODEX_EDITMODE_STARTED total=" + testsToRun.TestCaseCount);
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            Finished = true;
            WriteSummary(result.Test.TestCaseCount, result.PassCount, result.FailCount, result.SkipCount, result.ResultState);
            Debug.Log("CODEX_EDITMODE_RESULT total=" + result.Test.TestCaseCount + " passed=" + result.PassCount + " failed=" + result.FailCount + " skipped=" + result.SkipCount + " state=" + result.ResultState);
            EditorApplication.Exit(result.FailCount == 0 ? 0 : 1);
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result.Test.HasChildren || !result.ResultState.StartsWith("Failed", StringComparison.Ordinal))
            {
                return;
            }

            Debug.LogError("CODEX_EDITMODE_FAILED " + result.FullName + " :: " + result.Message + "\n" + result.StackTrace);
        }

        public void WriteSummary(int total, int passed, int failed, int skipped, string state)
        {
            var projectPath = Path.GetDirectoryName(Application.dataPath);
            var path = Path.Combine(projectPath, "CodexEditModeResults.xml");
            var escapedState = (state ?? string.Empty).Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
            File.WriteAllText(path, "<codex-editmode total=\"" + total + "\" passed=\"" + passed + "\" failed=\"" + failed + "\" skipped=\"" + skipped + "\" state=\"" + escapedState + "\" />", Encoding.UTF8);
        }
    }
}
