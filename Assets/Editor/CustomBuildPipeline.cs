#if UNITY_EDITOR
using System;

using UnityEditor;
using UnityEditor.Build.Reporting;

using UnityEngine;

/// <summary>
/// Custom build pipeline
/// </summary>
public static class CustomBuildPipeline
{
    private const string ANDROID = "android";
    private const string IOS = "ios";
    private const string LINUX = "linux";
    private const string MACOS = "macos";
    private const string UWP = "uwp";
    private const string WEBGL = "webgl";
    private const string WINDOWS32 = "windows32";
    private const string WINDOWS64 = "windows64";

    /// <summary>
    /// Start build process for <see cref="BuildTarget.Android"/>
    /// </summary>
    [MenuItem("Build/Android", false)]
    public static void Android()
    {
        if (EditorMessage(ANDROID, out string path))
        {
            if (Batch(path, BuildTargetGroup.Android, BuildTarget.Android))
            {
                OnBuildSucceeded(ANDROID, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.iOS"/>
    /// </summary>
    [MenuItem("Build/iOS", false)]
    public static void Ios()
    {
        if (EditorMessage(IOS, out string path))
        {
            if (Batch(path, BuildTargetGroup.iOS, BuildTarget.iOS))
            {
                OnBuildSucceeded(IOS, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.StandaloneLinux64"/>
    /// </summary>
    [MenuItem("Build/Linux", false)]
    public static void Linux()
    {
        if (EditorMessage(LINUX, out string path))
        {
            if (Batch(path, BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64))
            {
                OnBuildSucceeded(LINUX, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.StandaloneOSX"/>
    /// </summary>
    [MenuItem("Build/MacOS", false)]
    public static void Macos()
    {
        if (EditorMessage(MACOS, out string path))
        {
            if (Batch(path, BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX))
            {
                OnBuildSucceeded(MACOS, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.WSA"/>
    /// </summary>
    [MenuItem("Build/UWP", false)]
    public static void Uwp()
    {
        if (EditorMessage(UWP, out string path))
        {
            if (Batch(path, BuildTargetGroup.WSA, BuildTarget.WSAPlayer))
            {
                OnBuildSucceeded(UWP, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.WebGL"/>
    /// </summary>
    [MenuItem("Build/WebGL", false)]
    public static void Webgl()
    {
        if (EditorMessage(WEBGL, out string path))
        {
            if (Batch(path, BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                OnBuildSucceeded(WEBGL, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.StandaloneWindows"/>
    /// </summary>
    [MenuItem("Build/Windows32", false)]
    public static void Windows32()
    {
        if (EditorMessage(WINDOWS32, out string path))
        {
            if (Batch(path, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows))
            {
                OnBuildSucceeded(WINDOWS32, path);
            }
        }
    }
    /// <summary>
    /// Start build process for <see cref="BuildTarget.StandaloneWindows64"/>
    /// </summary>
    [MenuItem("Build/Windows64", false)]
    public static void Windows64()
    {
        if (EditorMessage(WINDOWS64, out string path))
        {
            if (Batch(path, BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                OnBuildSucceeded(WINDOWS64, path);
            }
        }
    }

    #region Build
    /// <summary>
    /// Batch process
    /// </summary>
    private static bool Batch(string path, BuildTargetGroup group, BuildTarget target)
    {
        try
        {
            // IL2CPP_Minimal
            Build(path, group, target, ScriptingImplementation.IL2CPP, ApiCompatibilityLevel.NET_Standard, ManagedStrippingLevel.Minimal);
            Build(path, group, target, ScriptingImplementation.IL2CPP, ApiCompatibilityLevel.NET_Unity_4_8, ManagedStrippingLevel.Minimal);

            // IL2CPP_High
            Build(path, group, target, ScriptingImplementation.IL2CPP, ApiCompatibilityLevel.NET_Standard, ManagedStrippingLevel.High);
            Build(path, group, target, ScriptingImplementation.IL2CPP, ApiCompatibilityLevel.NET_Unity_4_8, ManagedStrippingLevel.High);

            // MONO_Disabled
            Build(path, group, target, ScriptingImplementation.Mono2x, ApiCompatibilityLevel.NET_Standard, ManagedStrippingLevel.Disabled);
            Build(path, group, target, ScriptingImplementation.Mono2x, ApiCompatibilityLevel.NET_Unity_4_8, ManagedStrippingLevel.Disabled);

            // MONO_Minimal
            Build(path, group, target, ScriptingImplementation.Mono2x, ApiCompatibilityLevel.NET_Standard, ManagedStrippingLevel.Minimal);
            Build(path, group, target, ScriptingImplementation.Mono2x, ApiCompatibilityLevel.NET_Unity_4_8, ManagedStrippingLevel.Minimal);

            // MONO_High
            Build(path, group, target, ScriptingImplementation.Mono2x, ApiCompatibilityLevel.NET_Standard, ManagedStrippingLevel.High);
            Build(path, group, target, ScriptingImplementation.Mono2x, ApiCompatibilityLevel.NET_Unity_4_8, ManagedStrippingLevel.High);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    /// <summary>
    /// Build process
    /// </summary>
    private static bool Build(string path, BuildTargetGroup group, BuildTarget target, ScriptingImplementation backend, ApiCompatibilityLevel api, ManagedStrippingLevel stripping)
    {
        PlayerSettings.SetScriptingBackend(group, backend);
        if (backend == ScriptingImplementation.IL2CPP)
        {
            PlayerSettings.SetIl2CppCompilerConfiguration(group, Il2CppCompilerConfiguration.Debug);
            //EditorUserBuildSettings.il2CppCodeGeneration = UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed;
            PlayerSettings.SetIl2CppCodeGeneration(UnityEditor.Build.NamedBuildTarget.Standalone, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSpeed);
        }
        PlayerSettings.SetApiCompatibilityLevel(group, api);
        PlayerSettings.SetManagedStrippingLevel(group, stripping);

        string ext = target switch
        {
            BuildTarget.StandaloneOSX => ".app",
            BuildTarget.StandaloneWindows => ".exe",
            BuildTarget.iOS => throw new NotImplementedException(),
            BuildTarget.Android => throw new NotImplementedException(),
            BuildTarget.StandaloneWindows64 => ".exe",
            BuildTarget.WebGL => throw new NotImplementedException(),
            BuildTarget.WSAPlayer => throw new NotImplementedException(),
            BuildTarget.StandaloneLinux64 => "x86_64",
            _ => throw new NotImplementedException()
        };

        // Build option
        BuildPlayerOptions option = new()
        {
            scenes = new[] { @$"Assets\Sample\SampleScene.unity" },
            targetGroup = group,
            target = target,
            locationPathName = @$"{path}\{target}_{backend}_{api}_{stripping}\{target}_{backend}_{api}_{stripping}{ext}",
            options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler
        };

        BuildReport report = BuildPipeline.BuildPlayer(option);
        switch (report.summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Build succeeded");
                return true;

            case BuildResult.Failed:
                Debug.LogError($"Build failed with {report.summary.totalErrors} errors");
                return false;

            case BuildResult.Cancelled:
                Debug.LogWarning("Build cancel by user");
                return false;

            case BuildResult.Unknown:
            default:
                Debug.LogWarning("Build unknown result");
                return false;
        }
    }
    #endregion

    #region Utility
    /// <summary>
    /// Generic Unity Editor message for select build folder
    /// </summary>
    private static bool EditorMessage(string platform, out string path)
    {
        path = EditorUtility.SaveFolderPanel("Choose Location", PlayerPrefs.GetString(platform), string.Empty);
        return !string.IsNullOrEmpty(path);
    }

    /// <summary>
    /// When build succeeded, open file browser
    /// </summary>
    private static void OnBuildSucceeded(string platform, string path)
    {
        EditorUtility.RevealInFinder(path);
        PlayerPrefs.SetString(platform, path);
    }
    #endregion
}
#endif