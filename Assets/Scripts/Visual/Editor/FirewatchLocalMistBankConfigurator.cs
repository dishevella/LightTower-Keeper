using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OccaSoftware.Buto.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FirewatchLocalMistBankConfigurator
{
    private const string TargetScenePath = "Assets/_Recovery/0 (7).unity";
    private const string ConfigPath = "Assets/Settings/FirewatchVisualStyleConfig.asset";
    private const string RequestPath = "FirewatchLocalMistBanks.request";
    private const string ResultPath = "Temp/FirewatchLocalMistBanks.result";
    private const string StyleRootName = "Firewatch Visual Style";
    private const string MistRootName = "Firewatch Local Mist Banks";
    private const string MistPrefix = "Firewatch Mist Bank - ";
    private const string VisibleMistPrefix = "Firewatch Visible Mist - ";
    private const string ShaftRootName = "Firewatch Light Shaft Zones";
    private const string ShaftPrefix = "Firewatch Light Shaft Zone - ";
    private const string CameraClearMaskName = "Firewatch Near Fog Exclusion";
    private const string LocalMistMaterialPath = "Assets/Settings/FirewatchLocalMist.mat";
    private const string LocalMistTexturePath =
        "Assets/Fog Particles/Texture/Smoke Sprite Sheet.png";
    private const string LocalMistShaderName = "LightTower/Firewatch/Local Mist";

    static FirewatchLocalMistBankConfigurator()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Light Tower/Visual/Apply Local Mist Banks Only")]
    public static void RequestConfigure()
    {
        File.WriteAllText(RequestPath, DateTime.Now.ToString("O"));
    }

    private static void Tick()
    {
        if (!File.Exists(RequestPath) || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded) return;

        File.Delete(RequestPath);
        try
        {
            Configure(scene);
        }
        catch (Exception exception)
        {
            WriteResult("FAILED\n" + exception);
            Debug.LogException(exception);
        }
    }

    private static void Configure(Scene scene)
    {
        bool preserveUnsavedScene = scene.isDirty;

        FirewatchVisualStyleConfig config =
            AssetDatabase.LoadAssetAtPath<FirewatchVisualStyleConfig>(ConfigPath);
        if (config == null)
            throw new FileNotFoundException("Firewatch visual config was not found.", ConfigPath);

        GameObject styleRoot = scene.GetRootGameObjects()
            .FirstOrDefault(candidate => candidate.name == StyleRootName);
        if (styleRoot == null)
            throw new InvalidOperationException("Firewatch Visual Style root was not found.");

        Transform mistRoot = styleRoot.transform.Find(MistRootName);
        if (mistRoot == null)
        {
            mistRoot = new GameObject(MistRootName).transform;
            mistRoot.SetParent(styleRoot.transform, false);
        }

        Transform shaftRoot = styleRoot.transform.Find(ShaftRootName);
        if (shaftRoot == null)
        {
            shaftRoot = new GameObject(ShaftRootName).transform;
            shaftRoot.SetParent(styleRoot.transform, false);
        }

        EnsureUniqueMistBankLabels(config.LocalMistBanks);
        FirewatchVisualStyleConfig.LocalMistBankSettings[] settings =
            config.EnableLocalMistBanks && config.LocalMistBanks != null
                ? config.LocalMistBanks.Where(candidate => candidate != null).ToArray()
                : Array.Empty<FirewatchVisualStyleConfig.LocalMistBankSettings>();
        FirewatchVisualStyleConfig.LocalLightShaftZoneSettings[] shaftSettings =
            config.EnableLocalLightShaftZones && config.LocalLightShaftZones != null
                ? config.LocalLightShaftZones
                    .Where(candidate => candidate != null && candidate.Enabled)
                    .ToArray()
                : Array.Empty<FirewatchVisualStyleConfig.LocalLightShaftZoneSettings>();
        UpgradeLegacyVisibleMistSettings(config, settings);
        Material localMistMaterial = config.EnableVisibleLocalMist
            ? EnsureLocalMistMaterial(config)
            : null;
        int existingNonGeneratedMasks = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<FogDensityMask>(true))
            .Count(mask => !mask.transform.IsChildOf(mistRoot) &&
                           !mask.transform.IsChildOf(shaftRoot));
        if (existingNonGeneratedMasks + settings.Length + shaftSettings.Length > 8)
            throw new InvalidOperationException(
                $"Buto supports at most 8 fog masks. Existing={existingNonGeneratedMasks}, " +
                $"mist banks={settings.Length}, light shafts={shaftSettings.Length}.");

        HashSet<string> expectedNames = new HashSet<string>();
        HashSet<string> expectedVisibleNames = new HashSet<string>();
        foreach (FirewatchVisualStyleConfig.LocalMistBankSettings bank in settings)
        {
            string objectName = MistPrefix + SanitizeName(bank.Label);
            expectedNames.Add(objectName);
            Transform bankTransform = mistRoot.Find(objectName);
            if (bankTransform == null)
            {
                bankTransform = new GameObject(objectName).transform;
                bankTransform.SetParent(mistRoot, false);
            }

            bankTransform.position = bank.Center;
            bankTransform.rotation = Quaternion.identity;
            bankTransform.localScale = Vector3.one * Mathf.Max(1f, bank.Radius);

            FogDensityMask mask = bankTransform.GetComponent<FogDensityMask>();
            if (mask == null) mask = bankTransform.gameObject.AddComponent<FogDensityMask>();
            mask.enabled = config.EnableLocalMistBanks;
            ConfigureFogMask(
                mask,
                FogDensityMask.PrimitiveShape.Sphere,
                Mathf.Max(1f, bank.DensityMultiplier),
                Mathf.Max(0f, bank.BlendDistance));
            EditorUtility.SetDirty(mask);
            EditorUtility.SetDirty(bankTransform);

            if (config.EnableVisibleLocalMist && bank.EnableVisibleLayer &&
                localMistMaterial != null)
            {
                string visibleName = VisibleMistPrefix + SanitizeName(bank.Label);
                expectedVisibleNames.Add(visibleName);
                Transform visibleTransform = mistRoot.Find(visibleName);
                if (visibleTransform == null)
                {
                    visibleTransform = new GameObject(visibleName).transform;
                    visibleTransform.SetParent(mistRoot, false);
                }

                FirewatchLocalMistLayer layer =
                    visibleTransform.GetComponent<FirewatchLocalMistLayer>();
                if (layer == null)
                    layer = visibleTransform.gameObject.AddComponent<FirewatchLocalMistLayer>();
                visibleTransform.gameObject.SetActive(true);
                layer.Configure(bank, config, localMistMaterial);
                EditorUtility.SetDirty(layer);
                EditorUtility.SetDirty(visibleTransform);
            }
        }

        Transform[] staleBanks = mistRoot.Cast<Transform>()
            .Where(candidate => candidate.name.StartsWith(MistPrefix, StringComparison.Ordinal) &&
                                !expectedNames.Contains(candidate.name))
            .ToArray();
        foreach (Transform stale in staleBanks)
            UnityEngine.Object.DestroyImmediate(stale.gameObject);

        Transform[] staleVisibleLayers = mistRoot.Cast<Transform>()
            .Where(candidate => candidate.name.StartsWith(VisibleMistPrefix, StringComparison.Ordinal) &&
                                !expectedVisibleNames.Contains(candidate.name))
            .ToArray();
        foreach (Transform stale in staleVisibleLayers)
            UnityEngine.Object.DestroyImmediate(stale.gameObject);

        HashSet<string> expectedShaftNames = new HashSet<string>();
        foreach (FirewatchVisualStyleConfig.LocalLightShaftZoneSettings zone in shaftSettings)
        {
            string objectName = ShaftPrefix + SanitizeName(zone.Label);
            expectedShaftNames.Add(objectName);
            Transform zoneTransform = shaftRoot.Find(objectName);
            if (zoneTransform == null)
            {
                zoneTransform = new GameObject(objectName).transform;
                zoneTransform.SetParent(shaftRoot, false);
            }

            zoneTransform.position = zone.Center;
            zoneTransform.rotation = Quaternion.Euler(zone.EulerAngles);
            zoneTransform.localScale = new Vector3(
                Mathf.Max(0.1f, Mathf.Abs(zone.Size.x)),
                Mathf.Max(0.1f, Mathf.Abs(zone.Size.y)),
                Mathf.Max(0.1f, Mathf.Abs(zone.Size.z)));

            FogDensityMask mask = zoneTransform.GetComponent<FogDensityMask>();
            if (mask == null) mask = zoneTransform.gameObject.AddComponent<FogDensityMask>();
            ConfigureFogMask(
                mask,
                FogDensityMask.PrimitiveShape.Box,
                Mathf.Max(1f, zone.DensityMultiplier),
                Mathf.Max(0f, zone.BlendDistance));
            mask.enabled = config.EnableLocalLightShaftZones;
            zoneTransform.gameObject.SetActive(config.EnableLocalLightShaftZones);
            EditorUtility.SetDirty(mask);
            EditorUtility.SetDirty(zoneTransform);
        }

        Transform[] staleShaftZones = shaftRoot.Cast<Transform>()
            .Where(candidate => candidate.name.StartsWith(ShaftPrefix, StringComparison.Ordinal) &&
                                !expectedShaftNames.Contains(candidate.name))
            .ToArray();
        foreach (Transform stale in staleShaftZones)
            UnityEngine.Object.DestroyImmediate(stale.gameObject);

        ConfigureCameraClearMasks(scene, config);

        EditorUtility.SetDirty(mistRoot);
        EditorUtility.SetDirty(shaftRoot);
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!preserveUnsavedScene && !EditorSceneManager.SaveScene(scene))
            throw new IOException("Unity could not save the local mist bank configuration.");

        WriteResult(
            "SUCCESS\n" +
            $"Scene={TargetScenePath}\n" +
            $"SceneSaved={!preserveUnsavedScene}\n" +
            $"MistBanks={settings.Length}\n" +
            $"LightShaftZones={shaftSettings.Length}\n" +
            string.Join("\n", settings.Select(bank =>
                $"{bank.Label}: Center={bank.Center}, Radius={bank.Radius:0.#}, " +
                $"Blend={bank.BlendDistance:0.#}, Density={bank.DensityMultiplier:0.##}, " +
                $"VisibleMist={config.EnableVisibleLocalMist && bank.EnableVisibleLayer}")));
    }

    private static void ConfigureCameraClearMasks(
        Scene scene,
        FirewatchVisualStyleConfig config)
    {
        FogDensityMask[] masks = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<FogDensityMask>(true))
            .ToArray();
        foreach (FogDensityMask mask in masks)
        {
            if (mask.gameObject.name != CameraClearMaskName ||
                mask.GetComponentInParent<Camera>(true) == null)
                continue;

            mask.transform.localPosition = Vector3.zero;
            mask.transform.localRotation = Quaternion.identity;
            mask.transform.localScale =
                Vector3.one * Mathf.Max(0.1f, config.NearFogClearRadius);
            ConfigureFogMask(
                mask,
                FogDensityMask.PrimitiveShape.Sphere,
                Mathf.Clamp(config.NearFogDensityMultiplier, 0f, 0.99f),
                Mathf.Max(0f, config.NearFogClearBlend));
            mask.enabled = config.EnableNearFogClearZone;
            mask.gameObject.SetActive(config.EnableNearFogClearZone);
            EditorUtility.SetDirty(mask);
            EditorUtility.SetDirty(mask.transform);
        }
    }

    private static void EnsureUniqueMistBankLabels(
        FirewatchVisualStyleConfig.LocalMistBankSettings[] banks)
    {
        if (banks == null) return;

        HashSet<string> labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < banks.Length; i++)
        {
            FirewatchVisualStyleConfig.LocalMistBankSettings bank = banks[i];
            if (bank == null) continue;

            string baseLabel = string.IsNullOrWhiteSpace(bank.Label)
                ? $"Mist Bank {i + 1}"
                : bank.Label.Trim();
            string candidate = baseLabel;
            int suffix = 1;
            while (!labels.Add(candidate))
            {
                suffix++;
                candidate = $"{baseLabel} {suffix}";
            }

            bank.Label = candidate;
        }
    }

    private static void ConfigureFogMask(
        FogDensityMask mask,
        FogDensityMask.PrimitiveShape shape,
        float densityMultiplier,
        float blendDistance)
    {
        mask.DensityMultiplier = densityMultiplier;
        SerializedObject serializedMask = new SerializedObject(mask);
        serializedMask.FindProperty("shape").enumValueIndex = (int)shape;
        serializedMask.FindProperty("mode").enumValueIndex =
            (int)FogDensityMask.BlendMode.Multiplicative;
        serializedMask.FindProperty("densityMultiplier").floatValue = densityMultiplier;
        serializedMask.FindProperty("blendDistance").floatValue = blendDistance;
        serializedMask.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material EnsureLocalMistMaterial(FirewatchVisualStyleConfig config)
    {
        Shader shader = Shader.Find(LocalMistShaderName);
        if (shader == null)
            throw new InvalidOperationException(
                $"Local mist shader '{LocalMistShaderName}' was not found.");

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(LocalMistTexturePath);
        if (texture == null)
            throw new FileNotFoundException("The imported fog particle texture was not found.", LocalMistTexturePath);

        Material material = AssetDatabase.LoadAssetAtPath<Material>(LocalMistMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "Firewatch Local Mist" };
            AssetDatabase.CreateAsset(material, LocalMistMaterialPath);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", texture);
        material.SetFloat("_SoftIntersectionDistance", config.LocalMistSoftIntersectionDistance);
        material.SetFloat("_SunScatter", config.LocalMistSunScatter);
        material.SetFloat("_Brightness", config.LocalMistBrightness);
        material.SetFloat("_OpacityMultiplier", config.VisibleMistDensity);
        material.SetFloat("_NearFadeStart", config.LocalMistNearFade.x);
        material.SetFloat("_NearFadeEnd", config.LocalMistNearFade.y);
        material.SetFloat("_FarFadeStart", config.LocalMistFarFade.x);
        material.SetFloat("_FarFadeEnd", config.LocalMistFarFade.y);
        material.renderQueue = 3020;
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);

        config.LocalMistMaterial = material;
        return material;
    }

    private static void UpgradeLegacyVisibleMistSettings(
        FirewatchVisualStyleConfig config,
        FirewatchVisualStyleConfig.LocalMistBankSettings[] settings)
    {
        foreach (FirewatchVisualStyleConfig.LocalMistBankSettings bank in settings)
        {
            // Preserve artist edits and migrate only the exact first-pass defaults.
            bool usesInitialVisibleMistDefaults = bank.VisibleParticleCount == 72 &&
                Mathf.Abs(bank.VisibleLayerOffset.y - 4f) < 0.01f &&
                Mathf.Abs(bank.VisibleLayerSize.y - 7f) < 0.01f &&
                Mathf.Abs(bank.VisibleParticleHeightRatio - 0.34f) < 0.01f;
            if (!usesInitialVisibleMistDefaults) continue;

            bank.VisibleLayerOffset = new Vector3(27f, 2.2f, 8f);
            bank.VisibleLayerSize = new Vector3(95f, 2.5f, 75f);
            bank.VisibleParticleCount = 96;
            bank.VisibleParticleSize = new Vector2(20f, 38f);
            bank.VisibleParticleHeightRatio = 0.16f;
            bank.VisibleParticleOpacity = 0.3f;
            bank.VisibleParticleLifetime = 55f;
            bank.VisibleDriftVelocity = new Vector3(0.06f, 0f, 0.02f);
            bank.VisibleTurbulence = 0.35f;
            bank.VisibleTurbulenceFrequency = 0.035f;
            bank.VisibleTurbulenceScrollSpeed = 0.018f;
            EditorUtility.SetDirty(config);
        }
    }

    private static string SanitizeName(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "Unnamed";
        foreach (char invalid in Path.GetInvalidFileNameChars())
            label = label.Replace(invalid, '_');
        return label.Trim();
    }

    private static void WriteResult(string result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, result);
    }
}
