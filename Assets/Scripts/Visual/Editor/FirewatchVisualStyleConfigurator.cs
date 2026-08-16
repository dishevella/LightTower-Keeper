using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MK.Toon;
using OccaSoftware.Buto.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Light = UnityEngine.Light;
using RenderQueue = UnityEngine.Rendering.RenderQueue;

public static class FirewatchVisualStyleConfigurator
{
    public const string ConfigAssetPath = "Assets/Settings/FirewatchVisualStyleConfig.asset";
    public const string VolumeProfilePath = "Assets/Settings/FirewatchVisualStyleProfile.asset";
    public const string PlayerToonMaterialPath = "Assets/Materials/Firewatch/Player_Firewatch_MKToon.mat";

    private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
    private const string TargetScenePath = MobilityProLocomotionConfigurator.TargetScenePath;
    private const string FoliageShaderName = "Light Tower/Firewatch/Foliage";
    private const string EnvironmentVariantRoot = "Assets/Materials/Firewatch/Environment";
    private const string VistaRootName = "Firewatch Distant Vista";
    private const string NearFogExclusionName = "Firewatch Near Fog Exclusion";
    private const string AutoSessionKey = "LightTower.FirewatchVisualStyleConfigurator.v7";
    private const int ConfigurationRevision = 7;
    private const string WaterSourceMaterialPath =
        "Assets/PolygonNatureBiomes/PNB_Tropical_Jungle/Materials/Water_Ocean_Day.mat";
    private const string ButoWaterShaderPath =
        "Assets/PolygonNatureBiomes/PNB_Core/Shaders/Water.shadergraph";

    private const string BackgroundTreesAPath =
        "Assets/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_Background_Trees_01.prefab";
    private const string BackgroundTreesBPath =
        "Assets/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_Background_Trees_02.prefab";
    private const string NearMountainAPath =
        "Assets/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_MountainRange_02.prefab";
    private const string NearMountainBPath =
        "Assets/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_MountainRange_03.prefab";
    private const string FarMountainAPath =
        "Assets/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_MountainRange_03.prefab";
    private const string FarMountainBPath =
        "Assets/PolygonNatureBiomes/PNB_Alpine_Mountain/Prefabs/SM_Env_MountainRange_04.prefab";

    private static readonly string[] BaseTextureProperties =
    {
        "_BaseMap", "_MainTex", "_MainTexture", "_AlbedoMap", "_TextureSample1",
        "_TextureSample0", "_LeafTex", "_TunkTex", "_TrunkTex"
    };

    private static readonly string[] BaseColorProperties =
    {
        "_BaseColor", "_Color", "_ColorTint", "_Tint", "_LeafBaseColour", "_LeafColor"
    };

    private static bool waitingForEditorIdle;
    private static int sceneViewPreviewAttempts;

    [InitializeOnLoadMethod]
    private static void InitializeAtmospherePreview()
    {
        sceneViewPreviewAttempts = 0;
        EditorApplication.update -= EnableSceneViewAtmospherePreviewWhenReady;
        EditorApplication.update += EnableSceneViewAtmospherePreviewWhenReady;
    }

    [MenuItem("Tools/Light Tower/Visual/Apply Firewatch Visual Style")]
    public static void ConfigureFromMenu()
    {
        Configure();
    }

    [MenuItem("Tools/Light Tower/Visual/Select Firewatch Visual Style Config")]
    public static void SelectConfiguration()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<FirewatchVisualStyleConfig>(ConfigAssetPath);
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    [MenuItem("Tools/Light Tower/Visual/Restore Fog And Post Processing Only")]
    public static void RestoreAtmosphereOnlyFromMenu()
    {
        RestoreAtmosphereOnly();
    }

    public static void RestoreAtmosphereOnly()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before restoring the atmosphere.");

        FirewatchVisualStyleConfig config =
            AssetDatabase.LoadAssetAtPath<FirewatchVisualStyleConfig>(ConfigAssetPath);
        if (config == null)
            throw new FileNotFoundException("The Firewatch visual style config was not found.", ConfigAssetPath);

        VolumeProfile profile = LoadOrCreateVolumeProfile();
        RemoveMissingVolumeComponents(profile);
        config.TargetVolumeProfile = profile;
        config.EnsureDefaults();
        config.ApplyVolumeProfile();
        bool butoFeatureAdded = EnsureButoRendererFeature();

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene targetScene = SceneManager.GetSceneByPath(TargetScenePath);
        bool openedForRestore = !targetScene.IsValid() || !targetScene.isLoaded;
        if (openedForRestore)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(targetScene);

        try
        {
            AtmosphereResult atmosphere = ConfigureAtmosphereOnly(targetScene, config);
            EditorUtility.SetDirty(config);
            EditorSceneManager.MarkSceneDirty(targetScene);
            if (!EditorSceneManager.SaveScene(targetScene))
                throw new InvalidOperationException("Unity could not save the restored atmosphere.");

            int remainingVariantSlots = CountVariantMaterialReferences(targetScene);
            int remainingPlayerToonSlots = CountMaterialReferences(targetScene,
                AssetDatabase.LoadAssetAtPath<Material>(PlayerToonMaterialPath));
            bool vistaStillExists = targetScene.GetRootGameObjects()
                .Any(root => root.name == VistaRootName);
            ScriptableRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(PcRendererPath);
            ButoRenderFeature butoFeature = rendererData?.rendererFeatures
                .OfType<ButoRenderFeature>()
                .FirstOrDefault();
            bool butoProfileActive = profile.TryGet(out ButoVolumetricFog buto) &&
                                     buto.active && buto.IsActive();
            bool butoRendererActive = butoFeature != null && butoFeature.isActive;

            if (!atmosphere.GlobalVolumeAssigned || !atmosphere.ControllerAssigned ||
                !atmosphere.AllCamerasPostProcessing || !atmosphere.AllCamerasDepthTexture ||
                RenderSettings.fog || !butoProfileActive || !butoRendererActive ||
                remainingVariantSlots != 0 || remainingPlayerToonSlots != 0 || vistaStillExists)
            {
                throw new InvalidOperationException(
                    "Atmosphere validation failed without applying material or layout changes.");
            }

            AssetDatabase.SaveAssets();
            EnableSceneViewAtmospherePreview();
            WriteAtmosphereResult(
                "SUCCESS\n" +
                $"Scene={TargetScenePath}\n" +
                $"ButoRendererActive={butoRendererActive}\n" +
                $"ButoRendererFeatureAdded={butoFeatureAdded}\n" +
                $"ButoProfileActive={butoProfileActive}\n" +
                $"ButoFogDensity={buto.fogDensity.value:F3}\n" +
                $"GlobalVolume={atmosphere.GlobalVolumeAssigned}\n" +
                $"AtmosphereController={atmosphere.ControllerAssigned}\n" +
                $"AtmosphereCameraCount={atmosphere.CameraCount}\n" +
                $"AllCamerasPostProcessing={atmosphere.AllCamerasPostProcessing}\n" +
                $"AllCamerasDepthTexture={atmosphere.AllCamerasDepthTexture}\n" +
                $"LookDevelopmentLocked={atmosphere.LookDevelopmentLocked}\n" +
                $"LookDevelopmentHour={config.LookDevelopmentHour:F2}\n" +
                $"UnityFog={RenderSettings.fog}\n" +
                $"UnityFogDensity={RenderSettings.fogDensity:F4}\n" +
                $"RemainingVariantSlots={remainingVariantSlots}\n" +
                $"RemainingPlayerToonSlots={remainingPlayerToonSlots}\n" +
                $"VistaStillExists={vistaStillExists}");
        }
        catch (Exception exception)
        {
            WriteAtmosphereResult("FAILED\n" + exception);
            throw;
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (openedForRestore && targetScene.IsValid() && targetScene.isLoaded)
                EditorSceneManager.CloseScene(targetScene, true);
        }
    }

    [MenuItem("Tools/Light Tower/Visual/Restore Original Materials And Map Layout")]
    public static void RestoreOriginalMaterialsAndLayoutFromMenu()
    {
        RestoreOriginalMaterialsAndLayout();
    }

    public static void RestoreOriginalMaterialsAndLayout()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before restoring original scene materials.");

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene targetScene = SceneManager.GetSceneByPath(TargetScenePath);
        bool openedForRestore = !targetScene.IsValid() || !targetScene.isLoaded;
        if (openedForRestore)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);

        try
        {
            int restoredMaterialSlots = RestoreVariantMaterialReferences(targetScene);
            bool playerMaterialRestored = RestorePlayerMaterial(targetScene);
            int removedVistaObjects = RemoveGeneratedVista(targetScene);

            FirewatchVisualStyleConfig config =
                AssetDatabase.LoadAssetAtPath<FirewatchVisualStyleConfig>(ConfigAssetPath);
            if (config != null && config.GenerateDistantVista)
            {
                config.GenerateDistantVista = false;
                EditorUtility.SetDirty(config);
            }

            EditorSceneManager.MarkSceneDirty(targetScene);
            if (!EditorSceneManager.SaveScene(targetScene))
                throw new InvalidOperationException("Unity could not save the restored recover scene 58.");

            int remainingVariantSlots = CountVariantMaterialReferences(targetScene);
            int remainingPlayerToonSlots = CountMaterialReferences(targetScene,
                AssetDatabase.LoadAssetAtPath<Material>(PlayerToonMaterialPath));
            bool vistaStillExists = targetScene.GetRootGameObjects()
                .Any(root => root.name == VistaRootName);
            if (remainingVariantSlots != 0 || remainingPlayerToonSlots != 0 || vistaStillExists)
            {
                throw new InvalidOperationException(
                    $"Restore validation failed. RemainingVariants={remainingVariantSlots}, " +
                    $"RemainingPlayerToonSlots={remainingPlayerToonSlots}, " +
                    $"VistaStillExists={vistaStillExists}.");
            }

            AssetDatabase.SaveAssets();
            WriteRollbackResult(
                "SUCCESS\n" +
                $"Scene={TargetScenePath}\n" +
                $"RestoredMaterialSlots={restoredMaterialSlots}\n" +
                $"PlayerMaterialRestored={playerMaterialRestored}\n" +
                $"RemovedVistaObjects={removedVistaObjects}\n" +
                $"RemainingVariantSlots={remainingVariantSlots}\n" +
                $"RemainingPlayerToonSlots={remainingPlayerToonSlots}\n" +
                $"VistaStillExists={vistaStillExists}\n" +
                "AutomaticVisualReapply=False");
        }
        catch (Exception exception)
        {
            WriteRollbackResult("FAILED\n" + exception);
            throw;
        }
        finally
        {
            if (openedForRestore && targetScene.IsValid() && targetScene.isLoaded)
                EditorSceneManager.CloseScene(targetScene, true);
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
        }
    }

    private static int RestoreVariantMaterialReferences(Scene scene)
    {
        int restoredSlots = 0;
        Renderer[] renderers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .ToArray();

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
            {
                Material original = ResolveSourceMaterial(materials[i]);
                if (original == null || original == materials[i]) continue;
                materials[i] = original;
                restoredSlots++;
                changed = true;
            }

            if (!changed) continue;
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
        }

        return restoredSlots;
    }

    private static bool RestorePlayerMaterial(Scene scene)
    {
        Material playerToonMaterial =
            AssetDatabase.LoadAssetAtPath<Material>(PlayerToonMaterialPath);
        if (playerToonMaterial == null) return false;

        PlayerController player = FindInScene<PlayerController>(scene,
            candidate => candidate.gameObject.name == "Player") ?? FindInScene<PlayerController>(scene);
        if (player == null) return false;

        SkinnedMeshRenderer playerRenderer = player.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .FirstOrDefault(renderer => renderer.sharedMaterials.Contains(playerToonMaterial));
        if (playerRenderer == null) return false;

        SkinnedMeshRenderer sourceRenderer =
            PrefabUtility.GetCorrespondingObjectFromSource(playerRenderer) as SkinnedMeshRenderer;
        if (sourceRenderer == null)
            sourceRenderer = PrefabUtility.GetCorrespondingObjectFromOriginalSource(playerRenderer)
                as SkinnedMeshRenderer;
        if (sourceRenderer == null) return false;

        Material[] materials = playerRenderer.sharedMaterials;
        Material[] sourceMaterials = sourceRenderer.sharedMaterials;
        bool changed = false;
        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] != playerToonMaterial || i >= sourceMaterials.Length ||
                sourceMaterials[i] == null)
                continue;
            materials[i] = sourceMaterials[i];
            changed = true;
        }

        if (!changed) return false;
        playerRenderer.sharedMaterials = materials;
        PrefabUtility.RecordPrefabInstancePropertyModifications(playerRenderer);
        EditorUtility.SetDirty(playerRenderer);
        return true;
    }

    private static int RemoveGeneratedVista(Scene scene)
    {
        GameObject vistaRoot = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == VistaRootName);
        if (vistaRoot == null) return 0;

        int objectCount = vistaRoot.GetComponentsInChildren<Transform>(true).Length;
        UnityEngine.Object.DestroyImmediate(vistaRoot);
        return objectCount;
    }

    private static int CountVariantMaterialReferences(Scene scene)
    {
        int count = 0;
        Renderer[] renderers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .ToArray();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material != null && AssetDatabase.GetLabels(material).Contains("FirewatchVariant"))
                    count++;
            }
        }

        return count;
    }

    private static int CountMaterialReferences(Scene scene, Material target)
    {
        if (target == null) return 0;

        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Sum(renderer => renderer.sharedMaterials.Count(material => material == target));
    }

    public static void Configure()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WaitForEditorIdle();
            return;
        }

        try
        {
            FirewatchVisualStyleConfig config = LoadOrCreateConfig(out bool configCreated);
            VolumeProfile profile = LoadOrCreateVolumeProfile();
            RemoveMissingVolumeComponents(profile);
            config.TargetVolumeProfile = profile;

            bool configChanged = config.EnsureDefaults();
            configChanged |= AssignDefaultVistaPrefabs(config);
            if (config.ConfigurationRevision < 6)
            {
                ApplyRevisionSixPalette(config);
                configChanged = true;
            }
            if (config.ConfigurationRevision < 7)
            {
                config.EnableLocalLightShaftZones = true;
                config.NearFogDensityMultiplier = 0.2f;
                config.VisibleMistDensity = 1.35f;
                configChanged = true;
            }
            if (configCreated || config.ConfigurationRevision < ConfigurationRevision)
            {
                config.ConfigurationRevision = ConfigurationRevision;
                configChanged = true;
            }

            config.ApplyVisualStyle();
            bool butoFeatureAdded = EnsureButoRendererFeature();
            SceneResult sceneResult = ConfigureTargetScene(config);
            if (configChanged) EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            SessionState.SetBool(AutoSessionKey, true);

            string[] profileComponents = profile.components
                .Where(component => component != null)
                .Select(component => component.GetType().Name)
                .ToArray();
            string butoVersion = UnityEditor.PackageManager.PackageInfo
                .FindForAssetPath("Packages/com.occasoftware.buto")?.version ?? "Unknown";

            WriteResult(
                "SUCCESS\n" +
                $"Scene={TargetScenePath}\n" +
                $"ButoPackageVersion={butoVersion}\n" +
                $"ButoRendererFeature={sceneResult.ButoRendererFeaturePresent}\n" +
                $"ButoRendererFeatureAdded={butoFeatureAdded}\n" +
                $"ButoTimeLinked={sceneResult.AtmosphereControllerAssigned}\n" +
                $"ProfileComponentCount={profileComponents.Length}\n" +
                $"ProfileComponents={string.Join(",", profileComponents)}\n" +
                $"GlobalVolume={sceneResult.GlobalVolumeAssigned}\n" +
                $"CameraPostProcessing={sceneResult.CameraPostProcessingEnabled}\n" +
                $"AtmosphereCameraCount={sceneResult.CameraCount}\n" +
                $"CameraFarClip={sceneResult.CameraFarClip:F1}\n" +
                $"LookDevelopmentLocked={sceneResult.LookDevelopmentLocked}\n" +
                $"LookDevelopmentHour={config.LookDevelopmentHour:F2}\n" +
                $"MainSun={sceneResult.MainSunName}\n" +
                $"CoolFillLights={sceneResult.FillLightCount}\n" +
                $"FoliageVariants={sceneResult.Materials.FoliageVariantCount}\n" +
                $"EnvironmentToonVariants={sceneResult.Materials.ToonVariantCount}\n" +
                $"RendererMaterialAssignments={sceneResult.Materials.RendererAssignmentCount}\n" +
                $"VistaObjects={sceneResult.VistaObjectCount}\n" +
                $"PlayerMKToon={sceneResult.PlayerToonAssigned}\n" +
                $"PlayerShader={sceneResult.PlayerShader}\n" +
                $"PlayerRenderer={sceneResult.PlayerRendererName}");
        }
        catch (Exception exception)
        {
            SessionState.SetBool(AutoSessionKey, false);
            WriteResult("FAILED\n" + exception);
            Debug.LogException(exception);
        }
    }

    private static void TryAutoConfigure()
    {
        if (AssetDatabase.IsAssetImportWorkerProcess()) return;
        if (SessionState.GetBool(AutoSessionKey, false)) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
        {
            WaitForEditorIdle();
            return;
        }

        Configure();
    }

    private static void EnableSceneViewAtmospherePreview()
    {
        int enabledViews = 0;
        foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
        {
            if (sceneView.sceneViewState == null) continue;
            sceneView.sceneViewState.SetAllEnabled(true);
            sceneView.Repaint();
            if (sceneView.sceneViewState.fogEnabled && sceneView.sceneViewState.imageEffectsEnabled)
                enabledViews++;
        }

        if (enabledViews > 0)
            WriteSceneViewPreviewResult($"SUCCESS\nEnabledSceneViews={enabledViews}");
    }

    private static void EnableSceneViewAtmospherePreviewWhenReady()
    {
        sceneViewPreviewAttempts++;
        if (SceneView.sceneViews.Count == 0 && sceneViewPreviewAttempts < 300) return;

        EditorApplication.update -= EnableSceneViewAtmospherePreviewWhenReady;
        EnableSceneViewAtmospherePreview();
    }

    private static void WaitForEditorIdle()
    {
        if (waitingForEditorIdle) return;
        waitingForEditorIdle = true;
        EditorApplication.update += ConfigureWhenIdle;
    }

    private static void ConfigureWhenIdle()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlaying ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.update -= ConfigureWhenIdle;
        waitingForEditorIdle = false;
        TryAutoConfigure();
    }

    private static FirewatchVisualStyleConfig LoadOrCreateConfig(out bool created)
    {
        FirewatchVisualStyleConfig config =
            AssetDatabase.LoadAssetAtPath<FirewatchVisualStyleConfig>(ConfigAssetPath);
        created = config == null;
        if (!created) return config;

        EnsureFolder("Assets/Settings");
        config = ScriptableObject.CreateInstance<FirewatchVisualStyleConfig>();
        AssetDatabase.CreateAsset(config, ConfigAssetPath);
        return config;
    }

    private static bool AssignDefaultVistaPrefabs(FirewatchVisualStyleConfig config)
    {
        bool changed = false;
        changed |= AssignIfMissing(ref config.BackgroundTreesA, BackgroundTreesAPath);
        changed |= AssignIfMissing(ref config.BackgroundTreesB, BackgroundTreesBPath);
        changed |= AssignIfMissing(ref config.NearMountainA, NearMountainAPath);
        changed |= AssignIfMissing(ref config.NearMountainB, NearMountainBPath);
        changed |= AssignIfMissing(ref config.FarMountainA, FarMountainAPath);
        changed |= AssignIfMissing(ref config.FarMountainB, FarMountainBPath);

        if (config.WaterSourceMaterial == null)
        {
            config.WaterSourceMaterial =
                AssetDatabase.LoadAssetAtPath<Material>(WaterSourceMaterialPath);
            if (config.WaterSourceMaterial == null)
                throw new FileNotFoundException(
                    "The PNB source water material was not found.",
                    WaterSourceMaterialPath);
            changed = true;
        }

        Shader butoWaterShader = AssetDatabase.LoadAssetAtPath<Shader>(ButoWaterShaderPath);
        if (butoWaterShader == null)
            throw new FileNotFoundException(
                "The Buto-integrated original PNB water Shader Graph was not found.",
                ButoWaterShaderPath);
        if (config.ButoWaterShader != butoWaterShader)
        {
            config.ButoWaterShader = butoWaterShader;
            changed = true;
        }

        return changed;
    }

    private static void ApplyRevisionSixPalette(FirewatchVisualStyleConfig config)
    {
        config.MainSunColor = new Color(1f, 0.72f, 0.52f, 1f);
        config.MainSunIntensity = 1.25f;
        config.MainSunShadowStrength = 0.82f;
        config.FillLightColor = new Color(0.22f, 0.34f, 0.56f, 1f);
        config.FillLightIntensity = 0.08f;
        config.AmbientSkyColor = new Color(0.42f, 0.44f, 0.55f, 1f);
        config.AmbientEquatorColor = new Color(0.3f, 0.33f, 0.42f, 1f);
        config.AmbientGroundColor = new Color(0.12f, 0.16f, 0.22f, 1f);
        config.AmbientIntensity = 0.68f;
        config.ReflectionIntensity = 0.42f;
        config.MidPaletteTint = new Color(0.64f, 0.61f, 0.59f, 1f);
        config.FarPaletteTint = new Color(0.38f, 0.43f, 0.64f, 1f);

        config.FogDensityMultiplier = 0.92f;
        config.FogDistanceMultiplier = 0.58f;
        config.Anisotropy = 0.45f;
        config.LightIntensity = 0.82f;
        config.DensityInLight = 0.52f;
        config.DensityInShadow = 0.74f;
        config.HeightFalloff = 10f;
        config.EmissionFogColor = new Color(0.006f, 0.018f, 0.016f, 1f);
        config.FogColorInfluence = 0.38f;
        config.DirectionalRatio = 1f;
        config.NearFogDensityMultiplier = 0.2f;
        config.NearFogClearRadius = 12f;
        config.NearFogClearBlend = 18f;
        config.FogShadowRampBrightness = 0.72f;
        config.FogLitRampBrightness = 0.78f;
        config.FogEmissionRampBrightness = 0.035f;
        config.FogNoiseFrequency = 4;
        config.FogNoiseGeneratedOctaves = 3;
        config.FogNoiseGeneratedGain = 0.32f;
        config.FogNoiseSamplingGain = 0.18f;
        config.NoiseTiling = 110f;
        config.NoiseWindSpeed = new Vector3(0.035f, 0f, 0.012f);
        config.NoiseRemap = new Vector2(0.36f, 0.68f);

        config.ToneMapper = TonemappingMode.ACES;
        config.Exposure = -0.15f;
        config.Contrast = 9f;
        config.Saturation = -6f;
        config.ColorFilter = new Color(1f, 0.98f, 0.94f, 1f);
        config.Temperature = 5f;
        config.Tint = 0f;
        config.SplitShadowColor = new Color(0.3f, 0.35f, 0.52f, 1f);
        config.SplitHighlightColor = new Color(0.74f, 0.56f, 0.4f, 1f);
        config.SplitBalance = -8f;
        config.BloomIntensity = 0.06f;
        config.BloomThreshold = 1.25f;
        config.FoliageBacklightColor = new Color(1f, 0.58f, 0.3f, 1f);
        config.FoliageBacklightStrength = 0.2f;

        if (config.AtmosphereKeys == null) return;
        foreach (FirewatchVisualStyleConfig.AtmosphereKey key in config.AtmosphereKeys)
        {
            if (key == null) continue;
            if (string.Equals(key.Label, "Dusk", StringComparison.OrdinalIgnoreCase) ||
                Mathf.Abs(key.Hour - 18f) < 0.1f)
            {
                key.LitFogColor = new Color(0.82f, 0.56f, 0.42f, 1f);
                key.ShadowFogColor = new Color(0.16f, 0.32f, 0.34f, 1f);
                key.TowardSunColor = new Color(0.95f, 0.58f, 0.32f, 1f);
                key.AwayFromSunColor = new Color(0.2f, 0.36f, 0.44f, 1f);
                key.FoliageLitTint = new Color(0.92f, 0.72f, 0.48f, 1f);
                key.FoliageShadowTint = new Color(0.22f, 0.31f, 0.44f, 1f);
                key.DistanceFogColor = config.FarPaletteTint;
            }
            else if (string.Equals(key.Label, "Afternoon", StringComparison.OrdinalIgnoreCase) ||
                     Mathf.Abs(key.Hour - 14f) < 0.1f)
            {
                key.LitFogColor = new Color(0.82f, 0.67f, 0.52f, 1f);
                key.ShadowFogColor = new Color(0.24f, 0.38f, 0.38f, 1f);
                key.TowardSunColor = new Color(0.96f, 0.72f, 0.48f, 1f);
                key.AwayFromSunColor = new Color(0.3f, 0.44f, 0.5f, 1f);
                key.FoliageLitTint = new Color(0.95f, 0.78f, 0.5f, 1f);
                key.FoliageShadowTint = new Color(0.27f, 0.38f, 0.46f, 1f);
                key.DistanceFogColor = new Color(0.45f, 0.49f, 0.65f, 1f);
            }
        }
    }

    private static bool AssignIfMissing(ref GameObject field, string assetPath)
    {
        if (field != null) return false;
        field = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (field == null)
            throw new FileNotFoundException("A distant-vista prefab was not found.", assetPath);
        return true;
    }

    private static VolumeProfile LoadOrCreateVolumeProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (profile != null) return profile;

        EnsureFolder("Assets/Settings");
        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "FirewatchVisualStyleProfile";
        AssetDatabase.CreateAsset(profile, VolumeProfilePath);
        return profile;
    }

    private static void RemoveMissingVolumeComponents(VolumeProfile profile)
    {
        if (profile == null || profile.components == null) return;
        if (profile.components.RemoveAll(component => component == null) > 0)
            EditorUtility.SetDirty(profile);
    }

    private static bool EnsureButoRendererFeature()
    {
        ScriptableRendererData rendererData =
            AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(PcRendererPath);
        if (rendererData == null)
            throw new FileNotFoundException("PC URP Renderer asset was not found.", PcRendererPath);

        bool added = false;
        ButoRenderFeature butoFeature = rendererData.rendererFeatures
            .OfType<ButoRenderFeature>()
            .FirstOrDefault();
        if (butoFeature == null)
        {
            butoFeature = ScriptableObject.CreateInstance<ButoRenderFeature>();
            butoFeature.name = "Buto Volumetric Fog";
            butoFeature.settings.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            AssetDatabase.AddObjectToAsset(butoFeature, rendererData);
            rendererData.rendererFeatures.Add(butoFeature);
            butoFeature.Create();
            EditorUtility.SetDirty(butoFeature);
            added = true;
        }

        butoFeature.SetActive(true);
        butoFeature.settings.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        EditorUtility.SetDirty(butoFeature);

        SynchronizeRendererFeatureMap(rendererData);
        EditorUtility.SetDirty(rendererData);
        return added;
    }

    private static void SynchronizeRendererFeatureMap(ScriptableRendererData rendererData)
    {
        SerializedObject serializedRenderer = new SerializedObject(rendererData);
        SerializedProperty features = serializedRenderer.FindProperty("m_RendererFeatures");
        SerializedProperty featureMap = serializedRenderer.FindProperty("m_RendererFeatureMap");
        if (features == null || featureMap == null) return;

        featureMap.arraySize = features.arraySize;
        for (int i = 0; i < features.arraySize; i++)
        {
            UnityEngine.Object feature = features.GetArrayElementAtIndex(i).objectReferenceValue;
            long localId = 0;
            if (feature != null)
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out localId);
            featureMap.GetArrayElementAtIndex(i).longValue = localId;
        }

        serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AtmosphereResult ConfigureAtmosphereOnly(
        Scene targetScene,
        FirewatchVisualStyleConfig config)
    {
        PlayerController player = FindInScene<PlayerController>(targetScene,
            candidate => candidate.gameObject.name == "Player") ?? FindInScene<PlayerController>(targetScene);
        if (player == null)
            throw new InvalidOperationException("No PlayerController was found in recover scene 58.");

        GameObject styleObject = targetScene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "Firewatch Visual Style");
        if (styleObject == null)
        {
            styleObject = new GameObject("Firewatch Visual Style");
            SceneManager.MoveGameObjectToScene(styleObject, targetScene);
        }

        Volume volume = styleObject.GetComponent<Volume>();
        if (volume == null) volume = styleObject.AddComponent<Volume>();
        volume.enabled = true;
        volume.isGlobal = true;
        volume.priority = 50f;
        volume.weight = 1f;
        volume.sharedProfile = config.TargetVolumeProfile;

        FirewatchAtmosphereController controller =
            styleObject.GetComponent<FirewatchAtmosphereController>();
        if (controller == null) controller = styleObject.AddComponent<FirewatchAtmosphereController>();
        controller.enabled = true;
        controller.Configure(config, volume);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(volume);
        ConfigureNearFogExclusions(targetScene, config);

        TimeOfDayDriver timeDriver = FindInScene<TimeOfDayDriver>(targetScene);
        if (timeDriver != null)
        {
            timeDriver.ConfigureLookDevelopmentTime(
                config.LockLookDevelopmentTime,
                config.LookDevelopmentHour);
            EditorUtility.SetDirty(timeDriver);
        }

        Camera playerCamera = player.CameraRootTransform != null
            ? player.CameraRootTransform.GetComponentInChildren<Camera>(true)
            : player.GetComponentInChildren<Camera>(true);
        if (playerCamera == null)
            throw new InvalidOperationException("The Player camera was not found.");

        CameraAtmosphereResult cameras = ConfigureAtmosphereCameras(
            targetScene,
            styleObject,
            config);

        TimeOfDayEnvironmentController environment =
            FindInScene<TimeOfDayEnvironmentController>(targetScene);
        ConfigureLighting(targetScene, environment, config, out _);
        config.ApplyFoliageGlobals(config.LookDevelopmentHour);

        return new AtmosphereResult
        {
            GlobalVolumeAssigned = volume.enabled && volume.isGlobal &&
                                   volume.weight > 0f &&
                                   volume.sharedProfile == config.TargetVolumeProfile,
            ControllerAssigned = controller.enabled && controller.Config == config &&
                                 controller.TargetVolume == volume,
            CameraCount = cameras.CameraCount,
            AllCamerasPostProcessing = cameras.AllPostProcessing,
            AllCamerasDepthTexture = cameras.AllDepthTexture,
            LookDevelopmentLocked = timeDriver != null && config.LockLookDevelopmentTime
        };
    }

    private static CameraAtmosphereResult ConfigureAtmosphereCameras(
        Scene scene,
        GameObject volumeObject,
        FirewatchVisualStyleConfig config)
    {
        Camera[] cameras = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .ToArray();

        foreach (Camera camera in cameras)
        {
            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
            cameraData.requiresDepthTexture = true;

            LayerMask volumeMask = cameraData.volumeLayerMask;
            volumeMask.value |= 1 << volumeObject.layer;
            cameraData.volumeLayerMask = volumeMask;

            camera.farClipPlane = Mathf.Max(
                camera.farClipPlane,
                config.MaxVolumetricDistance * 2f);
            EditorUtility.SetDirty(cameraData);
            EditorUtility.SetDirty(camera);
        }

        return new CameraAtmosphereResult
        {
            CameraCount = cameras.Length,
            AllPostProcessing = cameras.Length > 0 && cameras.All(camera =>
                camera.GetUniversalAdditionalCameraData().renderPostProcessing),
            AllDepthTexture = cameras.Length > 0 && cameras.All(camera =>
                camera.GetUniversalAdditionalCameraData().requiresDepthTexture)
        };
    }

    private static void ConfigureNearFogExclusions(
        Scene scene,
        FirewatchVisualStyleConfig config)
    {
        Camera[] cameras = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Camera>(true))
            .OrderByDescending(camera => camera.enabled && camera.gameObject.activeInHierarchy)
            .Take(8)
            .ToArray();

        foreach (Camera camera in cameras)
            ConfigureNearFogExclusion(camera.transform, config);

        Transform[] legacyZones = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Where(candidate => candidate.name == NearFogExclusionName &&
                                candidate.GetComponentInParent<Camera>() == null)
            .ToArray();

        foreach (Transform legacyZone in legacyZones)
            UnityEngine.Object.DestroyImmediate(legacyZone.gameObject);
    }

    private static void ConfigureNearFogExclusion(
        Transform cameraAnchor,
        FirewatchVisualStyleConfig config)
    {
        Transform clearZone = cameraAnchor.Cast<Transform>()
            .FirstOrDefault(candidate => candidate.name == NearFogExclusionName);
        if (clearZone == null)
        {
            clearZone = new GameObject(NearFogExclusionName).transform;
            clearZone.SetParent(cameraAnchor, false);
        }

        clearZone.gameObject.SetActive(config.EnableNearFogClearZone);
        clearZone.localPosition = Vector3.zero;
        clearZone.localRotation = Quaternion.identity;
        clearZone.localScale = Vector3.one * Mathf.Max(0.01f, config.NearFogClearRadius);

        FogDensityMask mask = clearZone.GetComponent<FogDensityMask>();
        if (mask == null) mask = clearZone.gameObject.AddComponent<FogDensityMask>();
        mask.enabled = config.EnableNearFogClearZone;
        mask.DensityMultiplier = config.NearFogDensityMultiplier;

        SerializedObject serializedMask = new SerializedObject(mask);
        SerializedProperty shape = serializedMask.FindProperty("shape");
        SerializedProperty mode = serializedMask.FindProperty("mode");
        SerializedProperty density = serializedMask.FindProperty("densityMultiplier");
        SerializedProperty blend = serializedMask.FindProperty("blendDistance");
        if (shape != null) shape.enumValueIndex = (int)FogDensityMask.PrimitiveShape.Sphere;
        if (mode != null) mode.enumValueIndex = (int)FogDensityMask.BlendMode.Multiplicative;
        if (density != null) density.floatValue = config.NearFogDensityMultiplier;
        if (blend != null) blend.floatValue = Mathf.Max(0f, config.NearFogClearBlend);
        serializedMask.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(mask);
        EditorUtility.SetDirty(clearZone);
    }

    private static SceneResult ConfigureTargetScene(FirewatchVisualStyleConfig config)
    {
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene targetScene = SceneManager.GetSceneByPath(TargetScenePath);
        bool openedForConfiguration = !targetScene.IsValid() || !targetScene.isLoaded;
        if (openedForConfiguration)
            targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(targetScene);

        try
        {
            PlayerController player = FindInScene<PlayerController>(targetScene,
                candidate => candidate.gameObject.name == "Player") ?? FindInScene<PlayerController>(targetScene);
            if (player == null)
                throw new InvalidOperationException("No PlayerController was found in recover scene 58.");

            GameObject styleObject = targetScene.GetRootGameObjects()
                .FirstOrDefault(root => root.name == "Firewatch Visual Style");
            if (styleObject == null)
            {
                styleObject = new GameObject("Firewatch Visual Style");
                SceneManager.MoveGameObjectToScene(styleObject, targetScene);
            }

            Volume volume = styleObject.GetComponent<Volume>();
            if (volume == null) volume = styleObject.AddComponent<Volume>();
            volume.enabled = true;
            volume.isGlobal = true;
            volume.priority = 50f;
            volume.weight = 1f;
            volume.sharedProfile = config.TargetVolumeProfile;

            FirewatchAtmosphereController atmosphere =
                styleObject.GetComponent<FirewatchAtmosphereController>();
            if (atmosphere == null) atmosphere = styleObject.AddComponent<FirewatchAtmosphereController>();
            atmosphere.Configure(config, volume);
            EditorUtility.SetDirty(atmosphere);
            EditorUtility.SetDirty(volume);
            ConfigureNearFogExclusions(targetScene, config);

            TimeOfDayDriver timeDriver = FindInScene<TimeOfDayDriver>(targetScene);
            if (timeDriver != null)
            {
                timeDriver.ConfigureLookDevelopmentTime(
                    config.LockLookDevelopmentTime,
                    config.LookDevelopmentHour);
                EditorUtility.SetDirty(timeDriver);
            }

            Camera playerCamera = player.CameraRootTransform != null
                ? player.CameraRootTransform.GetComponentInChildren<Camera>(true)
                : player.GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
                throw new InvalidOperationException("The Player camera was not found.");

            CharacterAnimancerController animancer = player.GetComponent<CharacterAnimancerController>();
            playerCamera.nearClipPlane = animancer?.Config?.Camera != null
                ? animancer.Config.Camera.CameraNearClip
                : 0.05f;
            playerCamera.farClipPlane = Mathf.Max(
                playerCamera.farClipPlane,
                config.FarMountainRadius * 1.5f);
            EditorUtility.SetDirty(playerCamera);
            CameraAtmosphereResult cameras = ConfigureAtmosphereCameras(
                targetScene,
                styleObject,
                config);

            SkinnedMeshRenderer playerRenderer = player.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(renderer =>
                    renderer.enabled &&
                    renderer.gameObject.activeInHierarchy &&
                    renderer.gameObject.name.StartsWith("Chr_", StringComparison.Ordinal));
            if (playerRenderer == null)
                throw new InvalidOperationException("The active Player character renderer was not found.");

            Material toonMaterial = EnsurePlayerToonMaterial(playerRenderer.sharedMaterial);
            config.PlayerToonMaterial = toonMaterial;
            config.ApplyPlayerToonMaterial();
            Material[] playerMaterials = playerRenderer.sharedMaterials;
            if (playerMaterials.Length == 0) playerMaterials = new[] { toonMaterial };
            else playerMaterials[0] = toonMaterial;
            playerRenderer.sharedMaterials = playerMaterials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(playerRenderer);
            EditorUtility.SetDirty(playerRenderer);

            TimeOfDayEnvironmentController environment =
                FindInScene<TimeOfDayEnvironmentController>(targetScene);
            Light mainSun = ConfigureLighting(targetScene, environment, config, out int fillCount);
            int vistaObjectCount = config.GenerateDistantVista
                ? GenerateDistantVista(targetScene, player.transform, playerCamera.transform, config)
                : 0;
            MaterialResult materialResult = ConfigureEnvironmentMaterials(
                targetScene,
                player.transform,
                config);

            EditorUtility.SetDirty(config);
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.MarkSceneDirty(targetScene);
                if (!EditorSceneManager.SaveScene(targetScene))
                    throw new InvalidOperationException(
                        "Unity could not save the visual style changes to recover scene 58.");
            }

            ScriptableRendererData rendererData =
                AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(PcRendererPath);
            return new SceneResult
            {
                ButoRendererFeaturePresent = rendererData != null &&
                    rendererData.rendererFeatures.Any(feature => feature is ButoRenderFeature),
                GlobalVolumeAssigned = volume.sharedProfile == config.TargetVolumeProfile,
                AtmosphereControllerAssigned = atmosphere.Config == config && atmosphere.TargetVolume == volume,
                CameraPostProcessingEnabled = cameras.AllPostProcessing,
                CameraCount = cameras.CameraCount,
                CameraFarClip = playerCamera.farClipPlane,
                LookDevelopmentLocked = timeDriver != null && config.LockLookDevelopmentTime,
                MainSunName = mainSun != null ? mainSun.gameObject.name : "Missing",
                FillLightCount = fillCount,
                Materials = materialResult,
                VistaObjectCount = vistaObjectCount,
                PlayerToonAssigned = playerRenderer.sharedMaterial == toonMaterial &&
                    toonMaterial.shader != null &&
                    toonMaterial.shader.name.StartsWith("MK/Toon/", StringComparison.Ordinal),
                PlayerShader = toonMaterial.shader != null ? toonMaterial.shader.name : "Missing",
                PlayerRendererName = playerRenderer.gameObject.name
            };
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);
            if (openedForConfiguration && targetScene.IsValid() && targetScene.isLoaded)
                EditorSceneManager.CloseScene(targetScene, true);
        }
    }

    private static T FindInScene<T>(Scene scene, Func<T, bool> predicate = null) where T : Component
    {
        IEnumerable<T> components = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true));
        return predicate == null ? components.FirstOrDefault() : components.FirstOrDefault(predicate);
    }

    private static Light ConfigureLighting(
        Scene scene,
        TimeOfDayEnvironmentController environment,
        FirewatchVisualStyleConfig config,
        out int fillCount)
    {
        Light mainSun = null;
        if (environment != null)
        {
            SerializedObject serializedEnvironment = new SerializedObject(environment);
            SerializedProperty mainLightProperty =
                serializedEnvironment.FindProperty("mainDirectionalLight");
            mainSun = mainLightProperty?.objectReferenceValue as Light;
            ConfigureTimeOfDayProfiles(serializedEnvironment, config);
            serializedEnvironment.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(environment);
        }

        Light[] sceneLights = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Light>(true))
            .ToArray();
        Light[] directionalLights = sceneLights
            .Where(light => light.type == LightType.Directional)
            .ToArray();
        if (mainSun == null)
        {
            mainSun = directionalLights
                .OrderByDescending(light => light.shadows != LightShadows.None)
                .ThenByDescending(light => light.intensity)
                .FirstOrDefault();
        }

        if (mainSun == null)
            throw new InvalidOperationException("No directional light was found for the main sun.");

        mainSun.enabled = true;
        mainSun.color = config.MainSunColor;
        mainSun.intensity = config.MainSunIntensity;
        mainSun.shadows = LightShadows.Soft;
        mainSun.shadowStrength = config.MainSunShadowStrength;
        mainSun.renderMode = LightRenderMode.ForcePixel;
        RenderSettings.sun = mainSun;
        EditorUtility.SetDirty(mainSun);

        fillCount = 0;
        foreach (Light light in directionalLights)
        {
            if (light == mainSun) continue;
            light.color = config.FillLightColor;
            light.intensity = Mathf.Min(light.intensity, config.FillLightIntensity);
            light.shadows = LightShadows.None;
            light.shadowStrength = 0f;
            light.bounceIntensity = Mathf.Min(light.bounceIntensity, 0.25f);
            EditorUtility.SetDirty(light);
            fillCount++;
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = config.AmbientSkyColor;
        RenderSettings.ambientEquatorColor = config.AmbientEquatorColor;
        RenderSettings.ambientGroundColor = config.AmbientGroundColor;
        RenderSettings.ambientIntensity = config.AmbientIntensity;
        RenderSettings.reflectionIntensity = config.ReflectionIntensity;
        RenderSettings.fog = false;
        RenderSettings.fogDensity = 0f;
        return mainSun;
    }

    private static void ConfigureTimeOfDayProfiles(
        SerializedObject serializedEnvironment,
        FirewatchVisualStyleConfig config)
    {
        SerializedProperty profiles = serializedEnvironment.FindProperty("profiles");
        if (profiles == null || !profiles.isArray) return;

        for (int i = 0; i < profiles.arraySize; i++)
        {
            SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
            SerializedProperty period = profile.FindPropertyRelative("period");
            if (period == null) continue;

            TimeOfDayPeriod profilePeriod = (TimeOfDayPeriod)period.enumValueIndex;
            SetRelative(profile, "fogDensity", 0f);
            if (profilePeriod != TimeOfDayPeriod.Afternoon && profilePeriod != TimeOfDayPeriod.Dusk)
                continue;

            bool dusk = profilePeriod == TimeOfDayPeriod.Dusk;
            Color sunColor = dusk
                ? config.MainSunColor
                : Color.Lerp(Color.white, config.MainSunColor, 0.58f);
            float sunIntensity = dusk
                ? config.MainSunIntensity
                : config.MainSunIntensity * 1.08f;
            Color fogColor = dusk
                ? config.FarPaletteTint
                : Color.Lerp(config.MidPaletteTint, config.FarPaletteTint, 0.62f);

            SetRelative(profile, "directionalColor", sunColor);
            SetRelative(profile, "directionalIntensity", sunIntensity);
            SetRelative(profile, "shadowStrength", config.MainSunShadowStrength);
            SetRelative(profile, "fogColor", fogColor);
            SetRelative(profile, "ambientSkyColor",
                dusk ? config.AmbientSkyColor : Color.Lerp(config.AmbientSkyColor, Color.white, 0.12f));
            SetRelative(profile, "ambientEquatorColor", config.AmbientEquatorColor);
            SetRelative(profile, "ambientGroundColor", config.AmbientGroundColor);
            SetRelative(profile, "ambientIntensity",
                dusk ? config.AmbientIntensity : config.AmbientIntensity * 1.08f);
            SetRelative(
                profile,
                "reflectionIntensity",
                dusk
                    ? config.ReflectionIntensity
                    : Mathf.Min(1f, config.ReflectionIntensity + 0.08f));
            SetRelative(profile, "skyTint",
                dusk ? new Color(1f, 0.52f, 0.34f, 1f) : new Color(1f, 0.78f, 0.58f, 1f));
            SetRelative(profile, "skyboxExposure", dusk ? 0.78f : 0.96f);
        }
    }

    private static void SetRelative(SerializedProperty parent, string name, Color value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null) property.colorValue = value;
    }

    private static void SetRelative(SerializedProperty parent, string name, float value)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null) property.floatValue = value;
    }

    private static int GenerateDistantVista(
        Scene scene,
        Transform player,
        Transform cameraTransform,
        FirewatchVisualStyleConfig config)
    {
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == VistaRootName);
        if (root == null)
        {
            root = new GameObject(VistaRootName);
            SceneManager.MoveGameObjectToScene(root, scene);
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);

        Vector3 anchor = player.position;
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.ProjectOnPlane(player.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

        int count = 0;
        count += CreateVistaLayer(
            scene,
            root.transform,
            "Midground Tree Silhouette",
            new[] { config.BackgroundTreesA, config.BackgroundTreesB },
            config.MidTreeCount,
            config.MidTreeRadius,
            config.MidTreeTargetHeight,
            config.MidTreeVerticalOffset,
            config.VistaArc,
            anchor,
            forward);
        count += CreateVistaLayer(
            scene,
            root.transform,
            "Near Mountain Layer",
            new[] { config.NearMountainA, config.NearMountainB },
            config.NearMountainCount,
            config.NearMountainRadius,
            config.NearMountainTargetHeight,
            config.NearMountainVerticalOffset,
            config.VistaArc * 1.08f,
            anchor,
            forward);
        count += CreateVistaLayer(
            scene,
            root.transform,
            "Far Mountain Layer",
            new[] { config.FarMountainA, config.FarMountainB },
            config.FarMountainCount,
            config.FarMountainRadius,
            config.FarMountainTargetHeight,
            config.FarMountainVerticalOffset,
            config.VistaArc * 1.16f,
            anchor,
            forward);

        EditorUtility.SetDirty(root);
        return count;
    }

    private static int CreateVistaLayer(
        Scene scene,
        Transform parent,
        string layerName,
        GameObject[] prefabs,
        int count,
        float radius,
        float targetHeight,
        float verticalOffset,
        float arc,
        Vector3 anchor,
        Vector3 forward)
    {
        GameObject[] validPrefabs = prefabs.Where(prefab => prefab != null).ToArray();
        if (validPrefabs.Length == 0 || count <= 0) return 0;

        GameObject layerRoot = new GameObject(layerName);
        SceneManager.MoveGameObjectToScene(layerRoot, scene);
        layerRoot.transform.SetParent(parent, true);

        int created = 0;
        for (int i = 0; i < count; i++)
        {
            GameObject prefab = validPrefabs[i % validPrefabs.Length];
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null) continue;

            instance.name = $"{layerName} {i + 1:00}";
            instance.transform.SetParent(layerRoot.transform, true);
            float normalized = count == 1 ? 0.5f : i / (float)(count - 1);
            float angle = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, normalized);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            float radiusVariation = 1f + ((i % 3) - 1) * 0.035f;
            Vector3 desiredPosition = anchor + direction * radius * radiusVariation;
            Vector3 towardPlayer = Vector3.ProjectOnPlane(anchor - desiredPosition, Vector3.up).normalized;
            if (towardPlayer.sqrMagnitude > 0.001f)
                instance.transform.rotation = Quaternion.LookRotation(towardPlayer, Vector3.up);

            instance.transform.position = desiredPosition;
            if (TryGetRendererBounds(instance, out Bounds initialBounds) && initialBounds.size.y > 0.01f)
            {
                float heightVariation = 0.92f + (i % 4) * 0.055f;
                float scale = targetHeight * heightVariation / initialBounds.size.y;
                instance.transform.localScale *= scale;
            }

            if (TryGetRendererBounds(instance, out Bounds scaledBounds))
            {
                float desiredBottom = anchor.y + verticalOffset;
                instance.transform.position += Vector3.up * (desiredBottom - scaledBounds.min.y);
            }

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
                EditorUtility.SetDirty(collider);
            }

            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                EditorUtility.SetDirty(renderer);
            }

            created++;
        }

        return created;
    }

    private static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true)
            .Where(renderer => renderer.enabled)
            .ToArray();
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static MaterialResult ConfigureEnvironmentMaterials(
        Scene scene,
        Transform player,
        FirewatchVisualStyleConfig config)
    {
        Shader foliageShader = Shader.Find(FoliageShaderName);
        if (foliageShader == null)
            throw new InvalidOperationException($"The foliage shader '{FoliageShaderName}' was not found.");
        Shader toonShader = Shader.Find("MK/Toon/URP/Standard/Simple");
        if (toonShader == null)
            throw new InvalidOperationException("MK Toon URP Simple shader was not found.");

        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Materials/Firewatch");
        EnsureFolder(EnvironmentVariantRoot);

        MaterialResult result = default;
        HashSet<Material> foliageVariants = new HashSet<Material>();
        HashSet<Material> toonVariants = new HashSet<Material>();
        Renderer[] renderers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .ToArray();

        foreach (Renderer renderer in renderers)
        {
            if (!CanRestyleRenderer(renderer, player)) continue;
            Material[] materials = renderer.sharedMaterials;
            bool rendererChanged = false;

            for (int i = 0; i < materials.Length; i++)
            {
                Material current = materials[i];
                Material source = ResolveSourceMaterial(current);
                MaterialCategory category = ClassifyMaterial(renderer, source);
                if (category == MaterialCategory.Skip) continue;

                PaletteBand band = GetPaletteBand(renderer.bounds.center, player.position, config);
                Material variant;
                if (category == MaterialCategory.Foliage)
                {
                    variant = EnsureFoliageVariant(source, band, foliageShader, config);
                    if (variant != null) foliageVariants.Add(variant);
                }
                else
                {
                    variant = EnsureToonVariant(source, category, band, toonShader, config);
                    if (variant != null) toonVariants.Add(variant);
                }

                if (variant == null || current == variant) continue;
                materials[i] = variant;
                rendererChanged = true;
            }

            if (!rendererChanged) continue;
            renderer.sharedMaterials = materials;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            result.RendererAssignmentCount++;
        }

        result.FoliageVariantCount = foliageVariants.Count;
        result.ToonVariantCount = toonVariants.Count;
        return result;
    }

    private static bool CanRestyleRenderer(Renderer renderer, Transform player)
    {
        if (renderer == null || renderer.sharedMaterials == null) return false;
        if (renderer.transform.IsChildOf(player)) return false;
        if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer ||
            renderer is SpriteRenderer || renderer is SkinnedMeshRenderer)
            return false;
        return true;
    }

    private static MaterialCategory ClassifyMaterial(Renderer renderer, Material material)
    {
        if (material == null || material.shader == null) return MaterialCategory.Skip;

        string materialPath = AssetDatabase.GetAssetPath(material).Replace('\\', '/').ToLowerInvariant();
        string materialText = (material.name + " " + material.shader.name + " " + materialPath).ToLowerInvariant();
        string rendererText = GetTransformPath(renderer.transform).ToLowerInvariant();
        string text = materialText + " " + rendererText;

        if (ContainsAny(text,
                "water", "river", "ocean", "lake", "glass", "window", "sky", "cloud",
                "particle", "trail", "smoke", "fire", "flame", "fog", "vfx", "decal",
                "character", "player", "skin", "eye", "hair"))
            return MaterialCategory.Skip;

        if (ContainsAny(materialText, "trunk", "tunk", "bark", "stump", "log") ||
            ContainsAny(text, "tree_trunk", "tree trunk"))
            return MaterialCategory.Trunk;

        bool foliage = ContainsAny(materialText,
                "leaf", "leaves", "foliage", "grass", "plant", "fern", "bush", "shrub",
                "flower", "reed", "clover", "weed", "vine", "moss", "vegetation",
                "/plants/", "tree_card", "tree card", "background_tree", "background tree") ||
            ContainsAny(rendererText,
                "leaf", "leaves", "foliage", "grass", "plant", "fern", "bush", "shrub",
                "flower", "reed", "clover", "weed", "vine", "background tree") ||
            (ContainsAny(rendererText, "tree", "pine", "spruce", "palm") &&
             !ContainsAny(materialText, "wood", "bark", "trunk", "tunk"));
        if (foliage) return MaterialCategory.Foliage;

        if (ContainsAny(text, "rock", "stone", "cliff", "boulder", "mountain", "canyon"))
            return MaterialCategory.Rock;

        if (ContainsAny(materialText, "wood", "timber") && ContainsAny(rendererText, "tree", "log", "stump"))
            return MaterialCategory.Trunk;

        if (ContainsAny(text,
                "building", "cabin", "house", "tower", "lookout", "lighthouse", "bridge",
                "wall", "roof", "door", "stairs", "railing", "platform", "structure"))
            return MaterialCategory.Building;

        return MaterialCategory.Skip;
    }

    private static bool ContainsAny(string text, params string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            if (text.Contains(tokens[i])) return true;
        }

        return false;
    }

    private static string GetTransformPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }

    private static PaletteBand GetPaletteBand(
        Vector3 worldPosition,
        Vector3 playerPosition,
        FirewatchVisualStyleConfig config)
    {
        float distance = Vector3.Distance(worldPosition, playerPosition);
        if (distance <= config.NearPaletteDistance) return PaletteBand.Near;
        if (distance <= config.MidPaletteDistance) return PaletteBand.Mid;
        return PaletteBand.Far;
    }

    private static Material EnsureFoliageVariant(
        Material source,
        PaletteBand band,
        Shader shader,
        FirewatchVisualStyleConfig config)
    {
        if (!TryGetSourceIdentity(source, out string guid, out long localId)) return null;
        string variantPath = GetVariantPath(source, guid, localId, "Foliage", band);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(variantPath);
        if (material == null)
        {
            material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(variantPath);
            AssetDatabase.CreateAsset(material, variantPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture baseTexture = TryGetTexture(source, BaseTextureProperties, out string baseProperty);
        SetTextureWithTransform(material, "_BaseMap", baseTexture, source, baseProperty);
        Color baseColor = TryGetColor(source, BaseColorProperties, Color.white);
        material.SetColor("_BaseColor", baseColor);

        Texture leafTexture = TryGetTexture(source, new[] { "_LeafTex" }, out string leafProperty);
        Texture trunkTexture = TryGetTexture(source, new[] { "_TunkTex", "_TrunkTex" }, out string trunkProperty);
        bool useSplitMaps = leafTexture != null && (source.HasProperty("_LeafTex") || trunkTexture != null);
        SetTextureWithTransform(material, "_LeafTex", leafTexture ?? baseTexture, source,
            leafTexture != null ? leafProperty : baseProperty);
        SetTextureWithTransform(material, "_TrunkTex", trunkTexture ?? baseTexture, source,
            trunkTexture != null ? trunkProperty : baseProperty);
        material.SetColor("_LeafColor", TryGetColor(source,
            new[] { "_LeafBaseColour", "_LeafColor", "_BaseColor", "_Color" }, baseColor));
        material.SetColor("_TrunkColor", TryGetColor(source,
            new[] { "_TrunkBaseColour", "_TrunkColor", "_BaseColor", "_Color" }, baseColor));
        material.SetFloat("_UseSplitMaps", useSplitMaps ? 1f : 0f);
        material.SetColor("_PaletteTint", GetPaletteTint(config, band));
        material.SetFloat("_Cutoff", Mathf.Clamp01(TryGetFloat(source,
            new[] { "_AlphaClip", "_Cutoff", "_AlphaCutoff" }, config.FoliageAlphaCutoff)));
        material.SetFloat("_DistanceFogInfluence", band == PaletteBand.Near ? 0.88f : 1f);
        material.renderQueue = (int)RenderQueue.AlphaTest;
        material.enableInstancing = true;
        SetVariantLabels(material, guid, localId, "Foliage", band);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material EnsureToonVariant(
        Material source,
        MaterialCategory category,
        PaletteBand band,
        Shader shader,
        FirewatchVisualStyleConfig config)
    {
        if (!TryGetSourceIdentity(source, out string guid, out long localId)) return null;
        string variantPath = GetVariantPath(source, guid, localId, category.ToString(), band);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(variantPath);
        if (material == null)
        {
            material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(variantPath);
            AssetDatabase.CreateAsset(material, variantPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture albedo = TryGetTexture(source, BaseTextureProperties, out string textureProperty);
        Color sourceColor = TryGetColor(source, BaseColorProperties, Color.white);
        Color categoryTint = category == MaterialCategory.Trunk
            ? config.TrunkTint
            : category == MaterialCategory.Rock
                ? config.RockTint
                : config.BuildingTint;
        Color paletteTint = GetPaletteTint(config, band);
        Color styleTint = MultiplyColor(categoryTint, paletteTint);
        Color finalColor = MultiplyColor(sourceColor, Color.Lerp(Color.white, styleTint, 0.58f));
        if (band == PaletteBand.Mid) finalColor = Desaturate(finalColor, 0.28f);
        if (band == PaletteBand.Far)
            finalColor = Color.Lerp(config.FarPaletteTint, config.AmbientGroundColor, 0.1f);

        Properties.albedoMap.SetValue(material, band == PaletteBand.Far ? null : albedo);
        if (band != PaletteBand.Far && !string.IsNullOrEmpty(textureProperty) &&
            material.HasProperty("_AlbedoMap"))
        {
            material.SetTextureScale("_AlbedoMap", source.GetTextureScale(textureProperty));
            material.SetTextureOffset("_AlbedoMap", source.GetTextureOffset(textureProperty));
        }

        config.ApplyEnvironmentToonMaterial(material, finalColor, band != PaletteBand.Near);
        if (band == PaletteBand.Far && material.HasProperty("_RenderFace"))
            material.SetFloat("_RenderFace", (float)RenderFace.DoubleSided);
        material.renderQueue = (int)RenderQueue.Geometry;
        SetVariantLabels(material, guid, localId, category.ToString(), band);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static string GetVariantPath(
        Material source,
        string guid,
        long localId,
        string category,
        PaletteBand band)
    {
        string safeName = SanitizeFileName(source.name);
        string shortGuid = guid.Length > 10 ? guid.Substring(0, 10) : guid;
        return $"{EnvironmentVariantRoot}/{category}_{band}_{safeName}_{shortGuid}_{localId}.mat";
    }

    private static string SanitizeFileName(string value)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        char[] result = value.Select(character => invalid.Contains(character) ? '_' : character).ToArray();
        string safe = new string(result).Replace(' ', '_');
        return string.IsNullOrWhiteSpace(safe) ? "Material" : safe;
    }

    private static void SetVariantLabels(
        Material material,
        string guid,
        long localId,
        string category,
        PaletteBand band)
    {
        AssetDatabase.SetLabels(material, new[]
        {
            "FirewatchVariant",
            "FirewatchSourceGuid_" + guid,
            "FirewatchSourceLocalId_" + localId,
            "FirewatchCategory_" + category,
            "FirewatchBand_" + band
        });
    }

    private static Material ResolveSourceMaterial(Material material)
    {
        if (material == null) return null;
        string[] labels = AssetDatabase.GetLabels(material);
        string guidLabel = labels.FirstOrDefault(label => label.StartsWith(
            "FirewatchSourceGuid_", StringComparison.Ordinal));
        string localIdLabel = labels.FirstOrDefault(label => label.StartsWith(
            "FirewatchSourceLocalId_", StringComparison.Ordinal));
        if (guidLabel == null || localIdLabel == null) return material;

        string guid = guidLabel.Substring("FirewatchSourceGuid_".Length);
        if (!long.TryParse(localIdLabel.Substring("FirewatchSourceLocalId_".Length), out long localId))
            return material;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return material;

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Material>()
            .FirstOrDefault(candidate =>
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long candidateId) &&
                candidateId == localId) ?? material;
    }

    private static bool TryGetSourceIdentity(Material source, out string guid, out long localId)
    {
        guid = null;
        localId = 0;
        if (source == null) return false;
        return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out guid, out localId) &&
            !string.IsNullOrEmpty(guid);
    }

    private static Texture TryGetTexture(Material source, string[] propertyNames, out string propertyName)
    {
        propertyName = null;
        if (source == null) return null;
        foreach (string candidate in propertyNames)
        {
            if (!source.HasProperty(candidate)) continue;
            Texture texture = source.GetTexture(candidate);
            if (texture == null) continue;
            propertyName = candidate;
            return texture;
        }

        return null;
    }

    private static void SetTextureWithTransform(
        Material target,
        string targetProperty,
        Texture texture,
        Material source,
        string sourceProperty)
    {
        target.SetTexture(targetProperty, texture);
        if (source == null || string.IsNullOrEmpty(sourceProperty) || !source.HasProperty(sourceProperty))
            return;
        target.SetTextureScale(targetProperty, source.GetTextureScale(sourceProperty));
        target.SetTextureOffset(targetProperty, source.GetTextureOffset(sourceProperty));
    }

    private static Color TryGetColor(Material source, string[] propertyNames, Color fallback)
    {
        if (source == null) return fallback;
        foreach (string propertyName in propertyNames)
        {
            if (source.HasProperty(propertyName)) return source.GetColor(propertyName);
        }

        return fallback;
    }

    private static float TryGetFloat(Material source, string[] propertyNames, float fallback)
    {
        if (source == null) return fallback;
        foreach (string propertyName in propertyNames)
        {
            if (source.HasProperty(propertyName)) return source.GetFloat(propertyName);
        }

        return fallback;
    }

    private static Color GetPaletteTint(FirewatchVisualStyleConfig config, PaletteBand band)
    {
        if (band == PaletteBand.Near) return config.NearPaletteTint;
        if (band == PaletteBand.Mid) return Desaturate(config.MidPaletteTint, 0.32f);
        return config.FarPaletteTint;
    }

    private static Color MultiplyColor(Color a, Color b)
    {
        return new Color(a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a);
    }

    private static Color Desaturate(Color color, float amount)
    {
        float luminance = color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
        return Color.Lerp(color, new Color(luminance, luminance, luminance, color.a), amount);
    }

    private static Material EnsurePlayerToonMaterial(Material sourceMaterial)
    {
        Material toonMaterial = AssetDatabase.LoadAssetAtPath<Material>(PlayerToonMaterialPath);
        if (toonMaterial != null) return toonMaterial;

        Shader shader = Shader.Find("MK/Toon/URP/Standard/Simple");
        if (shader == null)
            throw new InvalidOperationException("MK Toon URP Simple shader was not found.");

        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Materials/Firewatch");
        toonMaterial = new Material(shader) { name = "Player_Firewatch_MKToon" };
        if (sourceMaterial != null)
        {
            Texture albedo = TryGetTexture(sourceMaterial, BaseTextureProperties, out string textureProperty);
            Color albedoColor = TryGetColor(sourceMaterial, BaseColorProperties, Color.white);
            Properties.albedoMap.SetValue(toonMaterial, albedo);
            Properties.albedoColor.SetValue(toonMaterial, albedoColor);
            if (!string.IsNullOrEmpty(textureProperty) && toonMaterial.HasProperty("_AlbedoMap"))
            {
                toonMaterial.SetTextureScale("_AlbedoMap", sourceMaterial.GetTextureScale(textureProperty));
                toonMaterial.SetTextureOffset("_AlbedoMap", sourceMaterial.GetTextureOffset(textureProperty));
            }
        }

        Properties.UpdateSystemProperties(toonMaterial);
        AssetDatabase.CreateAsset(toonMaterial, PlayerToonMaterialPath);
        return toonMaterial;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath)) return;
        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string name = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    private static void WriteResult(string result)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "FirewatchVisualStyle.result");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, result);
    }

    private static void WriteRollbackResult(string result)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "FirewatchVisualRollback.result");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, result);
    }

    private static void WriteAtmosphereResult(string result)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "FirewatchAtmosphereRestore.result");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, result);
    }

    private static void WriteSceneViewPreviewResult(string result)
    {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "Temp", "FirewatchSceneViewPreview.result");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, result);
    }

    private enum MaterialCategory
    {
        Skip,
        Foliage,
        Trunk,
        Rock,
        Building
    }

    private enum PaletteBand
    {
        Near,
        Mid,
        Far
    }

    private struct MaterialResult
    {
        public int FoliageVariantCount;
        public int ToonVariantCount;
        public int RendererAssignmentCount;
    }

    private struct AtmosphereResult
    {
        public bool GlobalVolumeAssigned;
        public bool ControllerAssigned;
        public int CameraCount;
        public bool AllCamerasPostProcessing;
        public bool AllCamerasDepthTexture;
        public bool LookDevelopmentLocked;
    }

    private struct CameraAtmosphereResult
    {
        public int CameraCount;
        public bool AllPostProcessing;
        public bool AllDepthTexture;
    }

    private struct SceneResult
    {
        public bool ButoRendererFeaturePresent;
        public bool GlobalVolumeAssigned;
        public bool AtmosphereControllerAssigned;
        public bool CameraPostProcessingEnabled;
        public int CameraCount;
        public float CameraFarClip;
        public bool LookDevelopmentLocked;
        public string MainSunName;
        public int FillLightCount;
        public MaterialResult Materials;
        public int VistaObjectCount;
        public bool PlayerToonAssigned;
        public string PlayerShader;
        public string PlayerRendererName;
    }
}
