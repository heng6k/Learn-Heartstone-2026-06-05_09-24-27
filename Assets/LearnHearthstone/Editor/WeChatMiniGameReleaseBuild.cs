using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEngine;
using WeChatWASM;

/// <summary>
/// Reproducible one-page-guide build entry point for the WeChat mini-game channel.
/// </summary>
public static class WeChatMiniGameReleaseBuild
{
    public const string ChannelDefine = "LEARN_HEARTHSTONE_WECHAT_MINIGAME";
    public const string DefaultLocalAppId = "touristappid";
    private const string RequestPath = "Temp/WeChatMiniGameReleaseBuild.request";
    private const string ResultPath = "Temp/WeChatMiniGameReleaseBuild.result";
    private const string AppIdEnvironmentVariable = "LEARN_HEARTHSTONE_WECHAT_MINIGAME_APPID";
    private const string CdnEnvironmentVariable = "LEARN_HEARTHSTONE_WECHAT_MINIGAME_CDN";
    private const string SwitchAttemptPath = "Temp/WeChatMiniGameReleaseBuild.switching";

    [InitializeOnLoadMethod]
    private static void RegisterEditorRequestRunner()
    {
        EditorApplication.update -= TryRunEditorRequest;
        EditorApplication.update += TryRunEditorRequest;
    }

    public static void BuildFromCommandLine()
    {
        var output = GetArgument("-wechatMiniGameOutput");
        if (string.IsNullOrWhiteSpace(output))
        {
            output = DefaultOutputPath();
        }

        Build(output, GetArgument("-wechatMiniGameAppId"));
    }

    private static void TryRunEditorRequest()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            if (File.Exists(SwitchAttemptPath))
            {
                File.Delete(RequestPath);
                File.Delete(SwitchAttemptPath);
                File.WriteAllText(
                    ResultPath,
                    "failed" + Environment.NewLine + "Unity 未能稳定切换到 WebGL。请检查当前激活的 Build Profile。",
                    System.Text.Encoding.UTF8);
                return;
            }

            Debug.Log("WeChat mini-game build is staging the WebGL target; the request will resume after Unity is ready.");
            var activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null)
            {
                Debug.Log("Temporarily deactivating Build Profile before switching to WebGL: " + activeProfile.name);
                BuildProfile.SetActiveBuildProfile(null);
            }

            File.WriteAllText(SwitchAttemptPath, DateTime.UtcNow.ToString("O"));
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                File.Delete(RequestPath);
                File.Delete(SwitchAttemptPath);
                File.WriteAllText(
                    ResultPath,
                    "failed" + Environment.NewLine + "无法切换到 WebGL 构建目标。",
                    System.Text.Encoding.UTF8);
            }

            return;
        }

        var output = File.ReadAllText(RequestPath).Trim();
        File.Delete(RequestPath);
        if (File.Exists(SwitchAttemptPath))
        {
            File.Delete(SwitchAttemptPath);
        }
        try
        {
            var miniGamePath = Build(output, null);
            File.WriteAllText(ResultPath, "success" + Environment.NewLine + miniGamePath);
        }
        catch (Exception exception)
        {
            File.WriteAllText(ResultPath, "failed" + Environment.NewLine + exception);
            Debug.LogException(exception);
        }
    }

    public static string Build(string output, string appId)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new BuildFailedException("微信小游戏输出路径不能为空。");
        }

        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
        {
            throw new BuildFailedException("Unity WebGL 模块未安装，无法转换微信小游戏。");
        }

        var outputPath = Path.GetFullPath(output);
        Directory.CreateDirectory(outputPath);
        ConfigureOfficialSdk(output, outputPath, ResolveAppId(appId));

        var namedTarget = NamedBuildTarget.WebGL;
        var originalDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
        PlayerSettings.SetScriptingDefineSymbols(namedTarget, AddDefine(originalDefines, ChannelDefine));
        try
        {
            StageUnity6WebGlBootConfigCompatibility();
            var result = WXConvertCore.DoExport(true);
            if (result != WXConvertCore.WXExportError.SUCCEED)
            {
                throw new BuildFailedException("微信小游戏转换失败：" + result + "。");
            }
        }
        finally
        {
            PlayerSettings.SetScriptingDefineSymbols(namedTarget, originalDefines);
        }

        var miniGamePath = Path.Combine(outputPath, WXConvertCore.miniGameDir);
        ValidateArtifact(miniGamePath);
        Debug.Log("WeChat mini-game build completed: " + miniGamePath);
        return miniGamePath;
    }

    private static void StageUnity6WebGlBootConfigCompatibility()
    {
#if UNITY_6000_0_OR_NEWER
        var expectedPath = Path.GetFullPath("Library/PlayerDataCache/WebGL/Data/boot.config");
        if (File.Exists(expectedPath))
        {
            return;
        }

        var unityGeneratedWebGl2Path = Path.GetFullPath("Library/PlayerDataCache/WebGL2/Data/boot.config");
        if (!File.Exists(unityGeneratedWebGl2Path))
        {
            throw new BuildFailedException(
                "Unity 6 尚未生成 WebGL2 boot.config；请先在当前编辑器完成一次 WebGL 平台初始化。");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(expectedPath));
        File.Copy(unityGeneratedWebGl2Path, expectedPath, false);
        Debug.Log(
            "Staged Unity-generated WebGL2 boot.config for the official WeChat SDK WebGL build compatibility path.");
#endif
    }

    private static void ConfigureOfficialSdk(string relativeOutput, string outputPath, string appId)
    {
        var config = WXConvertCore.config;
        var cdn = (Environment.GetEnvironmentVariable(CdnEnvironmentVariable) ?? string.Empty).Trim();
        config.ProjectConf.projectName = "炉石学习助手一图流";
        config.ProjectConf.Appid = appId;
        config.ProjectConf.relativeDST = relativeOutput.Replace('\\', '/');
        config.ProjectConf.DST = outputPath;
        config.ProjectConf.CDN = cdn;
        config.ProjectConf.assetLoadType = string.IsNullOrWhiteSpace(cdn) ? 1 : 0;
        config.ProjectConf.compressDataPackage = true;
        config.ProjectConf.MemorySize = 256;
        config.ProjectConf.Orientation = WXScreenOritation.Landscape;
        config.ProjectConf.bundleHashLength = 16;
        config.CompileOptions.DevelopBuild = false;
        config.CompileOptions.AutoProfile = false;
        config.CompileOptions.ProfilingMemory = false;
        config.CompileOptions.Webgl2 = true;
        config.CompileOptions.enableRenderThread = false;
        config.CompileOptions.autoAdaptScreen = true;
        config.CompileOptions.CleanBuild = false;
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private static void ValidateArtifact(string miniGamePath)
    {
        var required = new[] { "game.js", "game.json", "project.config.json" };
        var missing = required
            .Where(file => !File.Exists(Path.Combine(miniGamePath, file)))
            .ToArray();
        if (missing.Length > 0)
        {
            throw new BuildFailedException("微信小游戏产物不完整，缺少：" + string.Join("、", missing) + "。");
        }

        if (WXConvertCore.config.ProjectConf.assetLoadType == 0 &&
            string.IsNullOrWhiteSpace(WXConvertCore.config.ProjectConf.CDN))
        {
            throw new BuildFailedException(
                "首资源包超过微信包内限制。产物已生成，但必须配置 " +
                CdnEnvironmentVariable + " 后重新构建才能真机运行。");
        }
    }

    public static string AddDefine(string symbols, string define)
    {
        var values = (symbols ?? string.Empty)
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim())
            .Where(item => item.Length > 0)
            .ToList();
        if (!values.Contains(define, StringComparer.Ordinal))
        {
            values.Add(define);
        }

        return string.Join(";", values);
    }

    private static string ResolveAppId(string appId)
    {
        if (!string.IsNullOrWhiteSpace(appId))
        {
            return appId.Trim();
        }

        var configured = Environment.GetEnvironmentVariable(AppIdEnvironmentVariable);
        return string.IsNullOrWhiteSpace(configured) ? DefaultLocalAppId : configured.Trim();
    }

    private static string DefaultOutputPath()
    {
        return Path.Combine(
            "Builds",
            "WeChatMiniGame",
            "LearnHeartstoneOnePage_" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
    }

    private static string GetArgument(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var index = 0; index < args.Length - 1; index += 1)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[index + 1];
            }
        }

        return null;
    }
}
