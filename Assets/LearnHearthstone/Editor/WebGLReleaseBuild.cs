using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Reproducible WebGL release entry point used by CI and local release checks.
/// </summary>
public static class WebGLReleaseBuild
{
    private const string RequestPath = "Temp/WebGLReleaseBuild.request";
    private const string ResultPath = "Temp/WebGLReleaseBuild.result";

    public static void BuildFromCommandLine()
    {
        var output = GetArgument("-webglOutput");
        if (string.IsNullOrWhiteSpace(output))
        {
            output = Path.Combine("Builds", "WebGL", "LearnHeartstone_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        }

        Build(output);
    }

    [InitializeOnLoadMethod]
    private static void RegisterEditorRequestRunner()
    {
        EditorApplication.update -= TryRunEditorRequest;
        EditorApplication.update += TryRunEditorRequest;
    }

    private static void TryRunEditorRequest()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        var output = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        try
        {
            Build(output);
            File.WriteAllText(ResultPath, "success" + Environment.NewLine + Path.GetFullPath(output));
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, "failed" + Environment.NewLine + exception);
            Debug.LogException(exception);
        }
    }

    private static void Build(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new BuildFailedException("WebGL 输出路径不能为空。");
        }

        output = Path.GetFullPath(output);
        Directory.CreateDirectory(output);

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            throw new BuildFailedException("Unity WebGL 模块未安装或不可用。");
        }

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            throw new BuildFailedException("无法切换到 WebGL 构建目标。");
        }

        var scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            throw new BuildFailedException("没有启用的构建场景。");
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = output,
            target = BuildTarget.WebGL,
            options = BuildOptions.StrictMode
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log($"WebGL build result={summary.result}, size={summary.totalSize} bytes, duration={summary.totalTime}, output={output}");

        if (summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"WebGL 构建失败：{summary.result}");
        }

        // Tools/Release owns portable release metadata and candidate assembly.
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        var enabled = new System.Collections.Generic.List<string>();
        foreach (var scene in scenes)
        {
            if (scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            {
                enabled.Add(scene.path);
            }
        }

        return enabled.ToArray();
    }

    private static string GetArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }
}
