using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OccaSoftware.Buto.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class FirewatchRenderingAudit
{
    private const string RequestPath = "FirewatchRenderingAudit.request";
    private const string ResultPath = "Temp/FirewatchRenderingAudit.txt";
    private static bool running;

    static FirewatchRenderingAudit()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Light Tower/Validation/Run Rendering Audit")]
    public static void RequestRun()
    {
        File.WriteAllText(RequestPath, DateTime.Now.ToString("O"));
    }

    private static void Tick()
    {
        if (running || !File.Exists(RequestPath) || EditorApplication.isCompiling ||
            EditorApplication.isUpdating)
            return;

        running = true;
        try
        {
            File.Delete(RequestPath);
            RunAudit();
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
            File.WriteAllText(ResultPath, exception.ToString());
            Debug.LogException(exception);
        }
        finally
        {
            running = false;
        }
    }

    private static void RunAudit()
    {
        StringBuilder report = new StringBuilder(32768);
        Scene activeScene = SceneManager.GetActiveScene();
        Camera camera = FindSceneObjects<Camera>()
            .FirstOrDefault(candidate => candidate.gameObject.name == "Camera" && candidate.enabled) ??
            FindSceneObjects<Camera>().FirstOrDefault(candidate => candidate.enabled);

        report.AppendLine("FIREWATCH RENDERING AUDIT");
        report.AppendLine($"Generated={DateTime.Now:O}");
        report.AppendLine($"Unity={Application.unityVersion}");
        report.AppendLine($"Scene={activeScene.name} ({activeScene.path}) Dirty={activeScene.isDirty}");
        report.AppendLine();

        AppendPipeline(report, camera);
        AppendVolumes(report);
        AppendLighting(report);
        AppendMaterials(report, camera);

        Directory.CreateDirectory(Path.GetDirectoryName(ResultPath));
        File.WriteAllText(ResultPath, report.ToString());
        Debug.Log($"Firewatch rendering audit written to {Path.GetFullPath(ResultPath)}");
    }

    private static void AppendPipeline(StringBuilder report, Camera camera)
    {
        report.AppendLine("[PIPELINE]");
        RenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline;
        report.AppendLine($"Asset={pipeline?.name ?? "None"} Path={AssetDatabase.GetAssetPath(pipeline)}");
        if (pipeline is UniversalRenderPipelineAsset urp)
        {
            report.AppendLine($"DepthTexture={urp.supportsCameraDepthTexture} OpaqueTexture={urp.supportsCameraOpaqueTexture} " +
                              $"HDR={urp.supportsHDR} MSAA={urp.msaaSampleCount} RenderScale={urp.renderScale:0.###}");
        }

        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(
            "Assets/Settings/PC_Renderer.asset");
        if (rendererData != null)
        {
            report.AppendLine($"Renderer={rendererData.name}");
            for (int i = 0; i < rendererData.rendererFeatures.Count; i++)
            {
                ScriptableRendererFeature feature = rendererData.rendererFeatures[i];
                string details = feature is ButoRenderFeature butoFeature
                    ? $" event={(int)butoFeature.settings.renderPassEvent} ({butoFeature.settings.renderPassEvent})"
                    : string.Empty;
                report.AppendLine($"Feature[{i}]={feature?.name ?? "Missing"} Active={feature != null && feature.isActive}{details}");
            }
        }

        if (camera != null)
        {
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            report.AppendLine($"Camera={HierarchyPath(camera.transform)} HDR={camera.allowHDR} MSAA={camera.allowMSAA} " +
                              $"Near={camera.nearClipPlane:0.###} Far={camera.farClipPlane:0.#}");
            report.AppendLine($"CameraDepth={data.requiresDepthTexture} CameraOpaque={data.requiresColorTexture} " +
                              $"Post={data.renderPostProcessing} RendererIndex={data.scriptableRenderer != null}");
        }
        report.AppendLine("TransparentOrder=Opaque -> Buto(450 BeforeRenderingTransparents) -> Transparent");
        report.AppendLine();
    }

    private static void AppendVolumes(StringBuilder report)
    {
        report.AppendLine("[VOLUMES]");
        foreach (Volume volume in FindSceneObjects<Volume>().OrderByDescending(candidate => candidate.priority))
        {
            VolumeProfile profile = volume.sharedProfile;
            report.AppendLine($"Volume={HierarchyPath(volume.transform)} Global={volume.isGlobal} Priority={volume.priority:0.##} " +
                              $"Weight={volume.weight:0.##} Profile={profile?.name ?? "None"}");
            if (profile == null) continue;

            if (profile.TryGet(out ButoVolumetricFog buto))
            {
                report.AppendLine($"  Buto Active={buto.active} Mode={buto.mode.value} Density={buto.fogDensity.value:0.###} " +
                                  $"Distance={buto.maxDistanceVolumetric.value:0.#} Base={buto.baseHeight.value:0.##} " +
                                  $"Reach={buto.attenuationBoundarySize.value:0.##}");
                report.AppendLine($"  Noise={buto.volumeNoise.value.noiseType} Tiling={buto.noiseTiling.value:0.##} " +
                                  $"Remap={buto.noiseMap.value} Wind={buto.noiseWindSpeed.value}");
                report.AppendLine($"  FogLight={buto.lightIntensity.value:0.##} LitDensity={buto.densityInLight.value:0.##} " +
                                  $"ShadowDensity={buto.densityInShadow.value:0.##} ColorInfluence={buto.colorInfluence.value:0.##}");
                report.AppendLine($"  LitColor={buto.litColor.value} ShadowColor={buto.shadowedColor.value}");
            }
            if (profile.TryGet(out ColorAdjustments color))
                report.AppendLine($"  Color Exposure={color.postExposure.value:0.##} Contrast={color.contrast.value:0.##} " +
                                  $"Saturation={color.saturation.value:0.##} Filter={color.colorFilter.value}");
            if (profile.TryGet(out Tonemapping tone))
                report.AppendLine($"  Tonemapping={tone.mode.value}");
            if (profile.TryGet(out Bloom bloom))
                report.AppendLine($"  Bloom Intensity={bloom.intensity.value:0.###} Threshold={bloom.threshold.value:0.###}");
        }
        report.AppendLine($"UnityNativeFog={RenderSettings.fog}");
        report.AppendLine();
    }

    private static void AppendLighting(StringBuilder report)
    {
        report.AppendLine("[LIGHTING]");
        report.AppendLine($"RenderSun={RenderSettings.sun?.name ?? "None"} AmbientMode={RenderSettings.ambientMode} " +
                          $"AmbientIntensity={RenderSettings.ambientIntensity:0.###} ReflectionIntensity={RenderSettings.reflectionIntensity:0.###}");
        foreach (Light light in FindSceneObjects<Light>()
                     .Where(candidate => candidate.type == LightType.Directional)
                     .OrderByDescending(candidate => candidate.intensity))
        {
            report.AppendLine($"Directional={HierarchyPath(light.transform)} Enabled={light.enabled} Active={light.gameObject.activeInHierarchy} " +
                              $"Color={light.color} Intensity={light.intensity:0.###} Shadows={light.shadows} " +
                              $"ShadowStrength={light.shadowStrength:0.##}");
        }
        foreach (ReflectionProbe probe in FindSceneObjects<ReflectionProbe>())
            report.AppendLine($"ReflectionProbe={HierarchyPath(probe.transform)} Mode={probe.mode} Intensity={probe.intensity:0.##} " +
                              $"BoxProjection={probe.boxProjection}");
        report.AppendLine();
    }

    private static void AppendMaterials(StringBuilder report, Camera camera)
    {
        Renderer[] renderers = FindSceneObjects<Renderer>();
        Plane[] planes = camera != null ? GeometryUtility.CalculateFrustumPlanes(camera) : null;

        Renderer[] waterRenderers = renderers
            .Where(renderer => RendererLooksLike(renderer, "water", "ocean", "sea", "river", "lake"))
            .OrderBy(renderer => camera != null ? Vector3.Distance(camera.transform.position, renderer.bounds.center) : 0f)
            .ToArray();
        report.AppendLine($"[WATER] Renderers={waterRenderers.Length}");
        AppendRendererMaterials(report, waterRenderers, camera, 40, true);
        report.AppendLine();

        Renderer[] rockRenderers = renderers
            .Where(renderer => RendererLooksLike(renderer, "rock", "stone", "cliff", "island", "boulder"))
            .Where(renderer => planes == null || GeometryUtility.TestPlanesAABB(planes, renderer.bounds))
            .OrderBy(renderer => camera != null ? Vector3.Distance(camera.transform.position, renderer.bounds.center) : 0f)
            .ToArray();
        report.AppendLine($"[VISIBLE ROCKS] Renderers={rockRenderers.Length}");
        AppendRendererMaterials(report, rockRenderers, camera, 80, false);
    }

    private static void AppendRendererMaterials(
        StringBuilder report,
        IEnumerable<Renderer> renderers,
        Camera camera,
        int limit,
        bool includeShaderProperties)
    {
        HashSet<Material> expandedMaterials = new HashSet<Material>();
        foreach (Renderer renderer in renderers.Take(limit))
        {
            float distance = camera != null
                ? Vector3.Distance(camera.transform.position, renderer.bounds.center)
                : 0f;
            report.AppendLine($"Renderer={HierarchyPath(renderer.transform)} Type={renderer.GetType().Name} " +
                              $"Distance={distance:0.#} Center={renderer.bounds.center} Size={renderer.bounds.size}");
            foreach (Material material in renderer.sharedMaterials.Where(candidate => candidate != null))
            {
                string shaderPath = AssetDatabase.GetAssetPath(material.shader);
                bool butoIntegrated = ShaderSourceContainsButo(shaderPath);
                report.AppendLine($"  Material={material.name} Path={AssetDatabase.GetAssetPath(material)}");
                report.AppendLine($"    Shader={material.shader.name} ShaderPath={shaderPath} Queue={material.renderQueue} " +
                                  $"RenderType={material.GetTag("RenderType", false, "")}");
                report.AppendLine($"    ButoIntegration={butoIntegrated} Surface={FloatProperty(material, "_Surface")} " +
                                  $"ZWrite={FloatProperty(material, "_ZWrite")} SrcBlend={FloatProperty(material, "_SrcBlend")} " +
                                  $"DstBlend={FloatProperty(material, "_DstBlend")} DepthOnlyPass={material.GetShaderPassEnabled("DepthOnly")}");
                report.AppendLine($"    Base={ColorProperty(material, "_BaseColor", "_Color")} Emission={ColorProperty(material, "_EmissionColor")} " +
                                  $"Metallic={FloatProperty(material, "_Metallic")} Smoothness={FloatProperty(material, "_Smoothness", "_Glossiness")} " +
                                  $"Specular={FloatProperty(material, "_Specular", "_SpecularHighlights", "_WaterSpecular")}");

                if (includeShaderProperties && expandedMaterials.Add(material))
                    AppendInterestingProperties(report, material);
            }
        }
    }

    private static void AppendInterestingProperties(StringBuilder report, Material material)
    {
        Shader shader = material.shader;
        int count = shader.GetPropertyCount();
        for (int i = 0; i < count; i++)
        {
            string property = shader.GetPropertyName(i);
            string lower = property.ToLowerInvariant();
            if (!lower.Contains("water") && !lower.Contains("foam") && !lower.Contains("color") &&
                !lower.Contains("specular") && !lower.Contains("smooth") && !lower.Contains("reflect") &&
                !lower.Contains("fresnel") && !lower.Contains("opacity") && !lower.Contains("alpha") &&
                !lower.Contains("surface") && !lower.Contains("zwrite"))
                continue;

            ShaderPropertyType type = shader.GetPropertyType(i);
            if (type == ShaderPropertyType.Color)
                report.AppendLine($"      {property}={material.GetColor(property)}");
            else if (type == ShaderPropertyType.Float || type == ShaderPropertyType.Range)
                report.AppendLine($"      {property}={material.GetFloat(property):0.####}");
        }
    }

    private static bool RendererLooksLike(Renderer renderer, params string[] terms)
    {
        string rendererName = renderer.gameObject.name.ToLowerInvariant();
        if (terms.Any(rendererName.Contains)) return true;
        return renderer.sharedMaterials.Any(material => material != null &&
            terms.Any(term => material.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0 ||
                              material.shader.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private static bool ShaderSourceContainsButo(string shaderPath)
    {
        if (string.IsNullOrEmpty(shaderPath)) return false;
        string fullPath = Path.GetFullPath(shaderPath);
        if (!File.Exists(fullPath)) return false;
        string source = File.ReadAllText(fullPath);
        return source.IndexOf("ButoFog", StringComparison.OrdinalIgnoreCase) >= 0 ||
               source.IndexOf("com.occasoftware.buto", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ColorProperty(Material material, params string[] names)
    {
        foreach (string property in names)
            if (material.HasProperty(property)) return material.GetColor(property).ToString();
        return "N/A";
    }

    private static string FloatProperty(Material material, params string[] names)
    {
        foreach (string property in names)
            if (material.HasProperty(property)) return material.GetFloat(property).ToString("0.####");
        return "N/A";
    }

    private static T[] FindSceneObjects<T>() where T : Component
    {
        return UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(component => component.gameObject.scene.IsValid())
            .ToArray();
    }

    private static string HierarchyPath(Transform transform)
    {
        List<string> names = new List<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }
}
