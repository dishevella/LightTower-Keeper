using System;
using System.IO;
using System.Linq;
using OccaSoftware.Buto.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FirewatchPreButoGlassConfigurator
{
    private const string TargetScenePath = "Assets/_Recovery/0 (7).unity";
    private const string ConfigPath = "Assets/Settings/FirewatchVisualStyleConfig.asset";
    private const string PcRendererPath = "Assets/Settings/PC_Renderer.asset";
    private const string TagManagerPath = "ProjectSettings/TagManager.asset";
    private const string RequestPath = "FirewatchPreButoGlass.request";
    private const string ResultPath = "Temp/FirewatchPreButoGlass.result";
    private const string FeatureName = "Firewatch Pre-Buto Lighthouse Glass";
    private const string LayerName = "PreButoGlass";
    private const int PreferredLayer = 27;

    static FirewatchPreButoGlassConfigurator()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Light Tower/Visual/Apply Lighthouse Glass Pre-Buto Pass")]
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

        int layer = EnsureLayer();
        config.EnablePreButoLighthouseGlass = true;
        config.LighthouseGlassObjectName = "LH_WindowGlass.mo";
        config.PreButoGlassLayerName = LayerName;
        config.PreButoGlassLayer = layer;
        EditorUtility.SetDirty(config);

        UniversalRendererData rendererData =
            AssetDatabase.LoadAssetAtPath<UniversalRendererData>(PcRendererPath);
        if (rendererData == null)
            throw new FileNotFoundException("PC URP Renderer asset was not found.", PcRendererPath);

        RenderObjects feature = EnsureRendererFeature(rendererData, layer);
        LayerMask transparentMask = rendererData.transparentLayerMask;
        transparentMask.value &= ~(1 << layer);
        rendererData.transparentLayerMask = transparentMask;
        rendererData.SetDirty();
        EditorUtility.SetDirty(rendererData);

        Renderer[] glassRenderers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Where(candidate => candidate.gameObject.name == config.LighthouseGlassObjectName)
            .ToArray();
        if (glassRenderers.Length == 0)
            throw new InvalidOperationException(
                $"No renderer named '{config.LighthouseGlassObjectName}' was found in recover (7).");

        int urpLitMaterialCount = 0;
        foreach (Renderer glassRenderer in glassRenderers)
        {
            glassRenderer.gameObject.layer = layer;
            urpLitMaterialCount += glassRenderer.sharedMaterials.Count(material =>
                material != null && material.shader != null &&
                material.shader.name == "Universal Render Pipeline/Lit");
            EditorUtility.SetDirty(glassRenderer.gameObject);
            EditorUtility.SetDirty(glassRenderer);
        }

        if (urpLitMaterialCount == 0)
            throw new InvalidOperationException(
                "The lighthouse glass no longer uses the expected Universal Render Pipeline/Lit shader.");

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!preserveUnsavedScene && !EditorSceneManager.SaveScene(scene))
            throw new IOException("Unity could not save the pre-Buto glass configuration.");

        SceneView.RepaintAll();
        WriteResult(
            "SUCCESS\n" +
            $"Scene={TargetScenePath}\n" +
            $"SceneSaved={!preserveUnsavedScene}\n" +
            $"GlassRenderers={glassRenderers.Length}\n" +
            $"URPLitMaterials={urpLitMaterialCount}\n" +
            $"Layer={LayerName} ({layer})\n" +
            $"PassEvent={feature.settings.Event}\n" +
            $"ButoEvent={RenderPassEvent.BeforeRenderingTransparents}\n" +
            $"DefaultTransparentLayerIncluded={(rendererData.transparentLayerMask.value & (1 << layer)) != 0}");
    }

    private static int EnsureLayer()
    {
        UnityEngine.Object tagManagerAsset = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath)
            .FirstOrDefault();
        if (tagManagerAsset == null)
            throw new FileNotFoundException("Unity TagManager asset was not found.", TagManagerPath);

        SerializedObject tagManager = new SerializedObject(tagManagerAsset);
        SerializedProperty layers = tagManager.FindProperty("layers");
        if (layers == null || layers.arraySize < 32)
            throw new InvalidOperationException("Unity layer table is unavailable or incomplete.");

        for (int i = 6; i < 32; i++)
        {
            if (layers.GetArrayElementAtIndex(i).stringValue == LayerName)
                return i;
        }

        int layer = PreferredLayer;
        if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(layer).stringValue))
        {
            layer = Enumerable.Range(6, 26)
                .Reverse()
                .FirstOrDefault(index =>
                    string.IsNullOrEmpty(layers.GetArrayElementAtIndex(index).stringValue));
        }

        if (layer < 6)
            throw new InvalidOperationException("No free user layer is available for pre-Buto glass.");

        layers.GetArrayElementAtIndex(layer).stringValue = LayerName;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
        return layer;
    }

    private static RenderObjects EnsureRendererFeature(
        UniversalRendererData rendererData,
        int layer)
    {
        RenderObjects feature = rendererData.rendererFeatures
            .OfType<RenderObjects>()
            .FirstOrDefault(candidate => candidate.name == FeatureName);
        if (feature == null)
        {
            feature = ScriptableObject.CreateInstance<RenderObjects>();
            feature.name = FeatureName;
            AssetDatabase.AddObjectToAsset(feature, rendererData);
        }

        feature.settings.passTag = FeatureName;
        feature.settings.Event = RenderPassEvent.AfterRenderingSkybox;
        feature.settings.filterSettings.RenderQueueType = RenderQueueType.Transparent;
        feature.settings.filterSettings.LayerMask = 1 << layer;
        feature.settings.filterSettings.PassNames = null;
        feature.settings.overrideMode = RenderObjects.RenderObjectsSettings.OverrideMaterialMode.None;
        feature.settings.overrideMaterial = null;
        feature.settings.overrideShader = null;
        feature.settings.overrideDepthState = false;
        feature.settings.cameraSettings.overrideCamera = false;
        feature.SetActive(true);
        feature.Create();
        EditorUtility.SetDirty(feature);

        rendererData.rendererFeatures.Remove(feature);
        int butoIndex = rendererData.rendererFeatures.FindIndex(candidate =>
            candidate is ButoRenderFeature);
        rendererData.rendererFeatures.Insert(
            butoIndex >= 0 ? butoIndex : rendererData.rendererFeatures.Count,
            feature);
        SynchronizeRendererFeatureMap(rendererData);

        ButoRenderFeature buto = rendererData.rendererFeatures
            .OfType<ButoRenderFeature>()
            .FirstOrDefault();
        if (buto == null)
            throw new InvalidOperationException("Buto Renderer Feature is missing from PC_Renderer.");
        if ((int)feature.settings.Event >= (int)buto.settings.renderPassEvent)
            throw new InvalidOperationException("The lighthouse glass pass is not scheduled before Buto.");

        return feature;
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

    private static void WriteResult(string text)
    {
        string directory = Path.GetDirectoryName(ResultPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllText(ResultPath, text);
    }
}
