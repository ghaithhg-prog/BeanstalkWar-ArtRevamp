using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    [MenuItem("BeanstalkWar/Build Windows Standalone (.exe)")]
    public static void BuildWindows()
    {
        string projectRoot = Path.GetDirectoryName(Application.dataPath);
        string buildDir = Path.Combine(projectRoot, "Build");

        if (!Directory.Exists(buildDir))
        {
            Directory.CreateDirectory(buildDir);
        }

        string exePath = Path.Combine(buildDir, "BeanstalkWar.exe");

        // Collect all enabled scenes from EditorBuildSettings
        var scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
        {
            scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/i7tiyatBeanstalkWar.unity", true)
            };
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new string[] { "Assets/i7tiyatBeanstalkWar.unity" },
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        Debug.Log($"🚀 [BuildScript] Starting Standalone Windows Build -> {exePath}");

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✅ [BuildScript] Standalone Windows Build Succeeded! Total size: {summary.totalSize / (1024 * 1024)} MB");
        }
        else
        {
            Debug.LogError($"❌ [BuildScript] Build Failed with {summary.totalErrors} errors!");
        }
    }
}
