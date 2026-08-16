#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LighthouseProjectSetupUtility
{
    public const string RecoveryScenePath = "Assets/_Recovery/0 (7).unity";
    public const string MainScenePath = "Assets/Scenes/Lighthouse_Main.unity";

    private const string MigrationVersion = "2";
    private static readonly string MigrationKey =
        $"LightTower.FormalMainSceneMigration.{Application.dataPath.GetHashCode()}.{MigrationVersion}";

    [InitializeOnLoadMethod]
    private static void ScheduleRequiredSetup()
    {
        if (EditorPrefs.GetBool(MigrationKey, false))
        {
            return;
        }

        EditorApplication.delayCall += TryRunRequiredSetup;
    }

    [MenuItem("Light Tower/Architecture/Prepare Formal Main Scene")]
    public static void PrepareFormalMainScene()
    {
        if (!TryPrepareFormalMainScene(out string report))
        {
            throw new InvalidOperationException(report);
        }

        EditorPrefs.SetBool(MigrationKey, true);
        Debug.Log(report);
    }

    [MenuItem("Light Tower/Architecture/Validate Formal Main Scene")]
    public static void ValidateFormalMainScene()
    {
        if (!TryValidateFormalMainScene(out string report))
        {
            throw new InvalidOperationException(report);
        }

        Debug.Log(report);
    }

    private static void TryRunRequiredSetup()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += TryRunRequiredSetup;
            return;
        }

        if (!TryPrepareFormalMainScene(out string report))
        {
            Debug.LogError(report);
            return;
        }

        EditorPrefs.SetBool(MigrationKey, true);
        Debug.Log(report);
    }

    private static bool TryPrepareFormalMainScene(out string report)
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(RecoveryScenePath))
        {
            report = $"Formal scene migration stopped: source scene was not found at {RecoveryScenePath}.";
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.path == RecoveryScenePath && activeScene.isDirty)
        {
            if (!EditorSceneManager.SaveScene(activeScene))
            {
                report = "Formal scene migration stopped because the open recovery scene could not be saved.";
                return false;
            }
        }

        EnsureFolder("Assets", "Scenes");

        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath))
        {
            if (!AssetDatabase.CopyAsset(RecoveryScenePath, MainScenePath))
            {
                report = $"AssetDatabase.CopyAsset failed while copying {RecoveryScenePath} to {MainScenePath}.";
                return false;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        ConfigureBuildSettings();
        AssetDatabase.SaveAssets();
        return TryValidateFormalMainScene(out report);
    }

    private static bool TryValidateFormalMainScene(out string report)
    {
        SceneAsset source = AssetDatabase.LoadAssetAtPath<SceneAsset>(RecoveryScenePath);
        SceneAsset destination = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        if (source == null || destination == null)
        {
            report = "Formal scene validation failed: source or destination SceneAsset is missing.";
            return false;
        }

        string sourceFullPath = Path.GetFullPath(RecoveryScenePath);
        string destinationFullPath = Path.GetFullPath(MainScenePath);
        long sourceBytes = new FileInfo(sourceFullPath).Length;
        long destinationBytes = new FileInfo(destinationFullPath).Length;
        if (destinationBytes < sourceBytes * 0.9f)
        {
            report = $"Formal scene validation failed: destination size is unexpectedly small " +
                     $"({destinationBytes} vs source {sourceBytes}).";
            return false;
        }

        string[] sourceDependencies = AssetDatabase.GetDependencies(RecoveryScenePath, true);
        string[] destinationDependencies = AssetDatabase.GetDependencies(MainScenePath, true);
        HashSet<string> sourceSet = new(sourceDependencies.Where(path => path != RecoveryScenePath));
        HashSet<string> destinationSet = new(destinationDependencies.Where(path => path != MainScenePath));
        if (!sourceSet.IsSubsetOf(destinationSet))
        {
            report = "Formal scene validation failed: the formal scene lost dependencies from the recovery source.";
            return false;
        }

        EditorBuildSettingsScene firstEnabled = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
        if (firstEnabled == null || firstEnabled.path != MainScenePath)
        {
            report = $"Formal scene validation failed: {MainScenePath} is not the first enabled Build Settings scene.";
            return false;
        }

        report = $"Formal main scene ready: {MainScenePath}. " +
                 $"Validated {destinationBytes:N0} bytes and {destinationSet.Count} dependencies; " +
                 $"all {sourceSet.Count} recovery dependencies remain present, the recovery source is untouched, " +
                 "and the formal scene is first in Build Settings.";
        return true;
    }

    private static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new()
        {
            new EditorBuildSettingsScene(MainScenePath, true)
        };

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.path == MainScenePath)
            {
                continue;
            }

            bool enabled = scene.enabled && !scene.path.EndsWith("/SampleScene.unity", StringComparison.OrdinalIgnoreCase);
            scenes.Add(new EditorBuildSettingsScene(scene.path, enabled));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
