#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LighthouseSceneStructureUtility
{
    private static readonly string[] CanonicalRootNames =
    {
        "Environment",
        "Lighthouse",
        "Gameplay",
        "StoryContent",
        "UI",
        "LightingAndAtmosphere",
        "DevelopmentOnly"
    };

    [MenuItem("Light Tower/Architecture/Create Safe Main Scene Roots")]
    public static void CreateSafeMainSceneRoots()
    {
        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        Scene scene = EditorSceneManager.OpenScene(
            LighthouseProjectSetupUtility.MainScenePath,
            OpenSceneMode.Single);

        try
        {
            int missingScriptsBefore = CountMissingScripts(scene);
            Dictionary<string, GameObject> roots = IndexRoots(scene);
            foreach (string rootName in CanonicalRootNames)
            {
                if (!roots.ContainsKey(rootName))
                {
                    GameObject root = new(rootName);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    roots.Add(rootName, root);
                }
            }

            GameObject storyRoot = roots["StoryContent"];
            for (int day = 0; day <= 4; day++)
            {
                EnsureDirectChild(storyRoot.transform, $"Day{day}");
            }

            roots["DevelopmentOnly"].tag = "EditorOnly";
            for (int i = 0; i < CanonicalRootNames.Length; i++)
            {
                roots[CanonicalRootNames[i]].transform.SetSiblingIndex(
                    Mathf.Max(0, scene.rootCount - CanonicalRootNames.Length + i));
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException("Unity could not save the structured formal main scene.");
            }

            int missingScripts = CountMissingScripts(scene);
            if (missingScripts > missingScriptsBefore)
            {
                throw new InvalidOperationException(
                    $"Scene structure introduced missing scripts ({missingScriptsBefore} -> {missingScripts}).");
            }

            Debug.Log(
                $"Safe scene roots are ready in {LighthouseProjectSetupUtility.MainScenePath}. " +
                $"No existing object was reparented; StoryContent contains Day0-Day4 placeholders; " +
                $"missing-script count remained {missingScripts}.");
        }
        finally
        {
            if (!Application.isBatchMode && previousSetup != null && previousSetup.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }

    private static Dictionary<string, GameObject> IndexRoots(Scene scene)
    {
        Dictionary<string, GameObject> result = new(StringComparer.Ordinal);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (!result.ContainsKey(root.name))
            {
                result.Add(root.name, root);
            }
        }

        return result;
    }

    private static void EnsureDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            if (string.Equals(parent.GetChild(i).name, name, StringComparison.Ordinal))
            {
                return;
            }
        }

        GameObject child = new(name);
        child.transform.SetParent(parent, false);
    }

    private static int CountMissingScripts(Scene scene)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform transform in transforms)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
            }
        }

        return count;
    }
}
#endif
