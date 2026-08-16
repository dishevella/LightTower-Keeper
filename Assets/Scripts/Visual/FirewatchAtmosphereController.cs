using System.Collections.Generic;
using System.Linq;
using OccaSoftware.Buto.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class FirewatchAtmosphereController : ControllerAbstract
{
    private const string LightShaftZonePrefix = "Firewatch Light Shaft Zone - ";

    private sealed class WaterRendererBinding
    {
        public Renderer Renderer;
        public Material[] OriginalMaterials;
    }

    private sealed class GlassLayerBinding
    {
        public GameObject GameObject;
        public int OriginalLayer;
    }

    [System.Serializable]
    private struct FogMaskSerializedSettings
    {
        public FogDensityMask.PrimitiveShape shape;
        public FogDensityMask.BlendMode mode;
        public float densityMultiplier;
        public float blendDistance;
    }

    private static readonly int ShallowColorId = Shader.PropertyToID("_Shallow_Color");
    private static readonly int WaterShallowColorId = Shader.PropertyToID("_Water_Shallow_Color");
    private static readonly int DeepColorId = Shader.PropertyToID("_Deep_Color");
    private static readonly int WaterDeepColorId = Shader.PropertyToID("_Water_Deep_Color");
    private static readonly int VeryDeepColorId = Shader.PropertyToID("_Very_Deep_Color");
    private static readonly int WaterVeryDeepColorId = Shader.PropertyToID("_Water_Very_Deep_Color");
    private static readonly int DistantColorId = Shader.PropertyToID("_Distant_Water_Color");
    private static readonly int WaterFarColorId = Shader.PropertyToID("_Water_Far_Color");
    private static readonly int FoamColorId = Shader.PropertyToID("_Foam_Color");
    private static readonly int OceanFoamColorId = Shader.PropertyToID("_Ocean_Wave_Foam_Color");
    private static readonly int ShoreFoamTintId = Shader.PropertyToID("_Shore_Foam_Color_Tint");
    private static readonly int ShoreWaveColorId = Shader.PropertyToID("_Shore_Wave_Color");
    private static readonly int ShoreWaveTintId = Shader.PropertyToID("_Shore_Wave_Color_Tint");
    private static readonly int SpecularColorId = Shader.PropertyToID("_Specular_Color");
    private static readonly int SpecColorId = Shader.PropertyToID("_SpecColor");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int DistantIntensityId = Shader.PropertyToID("_Distant_Water_Intensity");
    private static readonly int BaseOpacityId = Shader.PropertyToID("_Base_Opacity");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

    [SerializeField] private FirewatchVisualStyleConfig config;
    [SerializeField] private Volume targetVolume;

    private readonly Dictionary<Material, Material> runtimeWaterMaterials =
        new Dictionary<Material, Material>();
    private readonly List<WaterRendererBinding> waterRendererBindings =
        new List<WaterRendererBinding>();
    private readonly List<GlassLayerBinding> glassLayerBindings =
        new List<GlassLayerBinding>();
    private readonly List<FirewatchLocalMistLayer> localMistLayers =
        new List<FirewatchLocalMistLayer>();
    private readonly List<FogDensityMask> runtimeLightShaftMasks =
        new List<FogDensityMask>();

    private TimeOfDayModel timeModel;
    private VolumeProfile runtimeProfile;
    private Camera debugCamera;
    private Material debugWaterMaterial;

    public FirewatchVisualStyleConfig Config => config;
    public Volume TargetVolume => targetVolume;
    public float CurrentSunIntensityLimit { get; private set; }
    public float CurrentReflectionIntensityLimit { get; private set; }
    public int ConfiguredCameraFogMaskCount { get; private set; }
    public int ButoWaterRendererCount { get; private set; }
    public int ButoWaterMaterialCount => runtimeWaterMaterials.Count;
    public bool ButoWaterIntegrationActive => ButoWaterMaterialCount > 0;
    public int PreButoGlassRendererCount { get; private set; }
    public bool PreButoGlassIntegrationActive => PreButoGlassRendererCount > 0;
    public int LocalMistLayerCount => localMistLayers.Count;
    public int LocalLightShaftZoneCount { get; private set; }

    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Current Density")]
    public float CurrentFogDensity { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Base Height"), SuffixLabel("m")]
    public float CurrentFogBase { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Height Reach"), SuffixLabel("m")]
    public float CurrentFogReach { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("View Distance"), SuffixLabel("m")]
    public float CurrentFogViewDistance { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Noise Scale"), SuffixLabel("m")]
    public float CurrentFogNoiseScale { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Noise Wind")]
    public Vector3 CurrentFogWind { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Lit Color")]
    public Color CurrentFogLitColor { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Shadow Color")]
    public Color CurrentFogShadowColor { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), PreviewField(48), LabelText("Color Ramp")]
    public Texture CurrentFogColorRamp { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Visible Local Mist")]
    private string DebugLocalMist => config != null && config.EnableVisibleLocalMist
        ? $"Active ({LocalMistLayerCount} layer{(LocalMistLayerCount == 1 ? string.Empty : "s")})"
        : "Disabled";
    [ShowInInspector, ReadOnly, TitleGroup("Buto Debug"), LabelText("Local Light Shafts")]
    private string DebugLocalLightShafts => config != null && config.EnableLocalLightShaftZones
        ? $"Active ({LocalLightShaftZoneCount} zone{(LocalLightShaftZoneCount == 1 ? string.Empty : "s")})"
        : "Disabled";

    [ShowInInspector, ReadOnly, TitleGroup("Lighting Debug"), LabelText("Sun Color")]
    private Color DebugSunColor => RenderSettings.sun != null ? RenderSettings.sun.color : Color.black;
    [ShowInInspector, ReadOnly, TitleGroup("Lighting Debug"), LabelText("Sun Intensity")]
    private float DebugSunIntensity => RenderSettings.sun != null ? RenderSettings.sun.intensity : 0f;

    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("Buto Integration")]
    private string DebugWaterIntegration => ButoWaterIntegrationActive
        ? $"Active ({ButoWaterMaterialCount} materials / {ButoWaterRendererCount} renderers)"
        : "Inactive";
    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("Transparent Fog")]
    private bool DebugTransparentWaterFog => ButoWaterIntegrationActive;
    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("Fog Applied Once")]
    private bool DebugWaterFogAppliedOnce => ButoWaterIntegrationActive &&
        DebugWaterSurface == "Transparent" && DebugWaterZWrite == "Off";
    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("Shader")]
    private string DebugWaterShader => debugWaterMaterial != null && debugWaterMaterial.shader != null
        ? debugWaterMaterial.shader.name
        : "None";
    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("Surface Type")]
    private string DebugWaterSurface => debugWaterMaterial != null &&
        debugWaterMaterial.HasProperty(SurfaceId) && debugWaterMaterial.GetFloat(SurfaceId) >= 0.5f
            ? "Transparent"
            : "Opaque";
    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("ZWrite")]
    private string DebugWaterZWrite => debugWaterMaterial != null &&
        debugWaterMaterial.HasProperty(ZWriteId) && debugWaterMaterial.GetFloat(ZWriteId) > 0.5f
            ? "On"
            : "Off";
    [ShowInInspector, ReadOnly, TitleGroup("Water Debug"), LabelText("Render Queue")]
    private int DebugWaterRenderQueue => debugWaterMaterial != null ? debugWaterMaterial.renderQueue : -1;

    [ShowInInspector, ReadOnly, TitleGroup("Glass Debug"), LabelText("Pre-Buto Integration")]
    private string DebugPreButoGlassIntegration => PreButoGlassIntegrationActive
        ? $"Active ({PreButoGlassRendererCount} renderer)"
        : "Inactive";
    [ShowInInspector, ReadOnly, TitleGroup("Glass Debug"), LabelText("Layer")]
    private string DebugPreButoGlassLayer => config != null
        ? $"{config.PreButoGlassLayerName} ({config.PreButoGlassLayer})"
        : "None";

    [ShowInInspector, ReadOnly, TitleGroup("Post Debug"), LabelText("Camera HDR")]
    public bool CurrentCameraHdr { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Post Debug"), LabelText("Exposure")]
    public float CurrentExposure { get; private set; }
    [ShowInInspector, ReadOnly, TitleGroup("Post Debug"), LabelText("Tonemapping")]
    public string CurrentTonemapping { get; private set; } = "None";
    [ShowInInspector, ReadOnly, TitleGroup("Post Debug"), LabelText("Bloom")]
    public float CurrentBloom { get; private set; }

    public void Configure(FirewatchVisualStyleConfig visualConfig, Volume volume)
    {
        config = visualConfig;
        targetVolume = volume;
    }

    private void Start()
    {
        if (config == null || targetVolume == null) return;

        ApplyConfiguredCameraFogMasks();
        ConfigureLocalLightShaftZones();

        if (config.TargetVolumeProfile != null)
            targetVolume.sharedProfile = config.TargetVolumeProfile;

        runtimeProfile = targetVolume.profile;
        debugCamera = Camera.main;
        ConfigurePreButoLighthouseGlass();
        ConfigureWaterMaterials();
        CacheLocalMistLayers();
        timeModel = this.GetModel<TimeOfDayModel>();
        config.PreviewChanged += HandleConfigPreviewChanged;
        if (HasCurrentHour)
        {
            timeModel.CurrentHour.OnValueChanged += ApplyHour;
            ApplyHour(timeModel.CurrentHour.Value);
        }
        else
        {
            ApplyHour(config.LookDevelopmentHour);
        }
    }

    private void OnDestroy()
    {
        if (config != null)
            config.PreviewChanged -= HandleConfigPreviewChanged;

        if (HasCurrentHour)
            timeModel.CurrentHour.OnValueChanged -= ApplyHour;

        RestorePreButoLighthouseGlass();
        RestoreWaterMaterials();
        DestroyRuntimeLightShaftZones();
        localMistLayers.Clear();

        if (runtimeProfile != null)
            Destroy(runtimeProfile);
    }

    private void LateUpdate()
    {
        if (config == null) return;

        float hour = HasCurrentHour
            ? timeModel.CurrentHour.Value
            : config.LookDevelopmentHour;
        ApplyGoldenHourHighlightLimits(hour);
    }

    private void ApplyHour(float hour)
    {
        if (config == null) return;

        float atmosphereHour = config.LockLookDevelopmentTime
            ? config.LookDevelopmentHour
            : hour;

        if (runtimeProfile != null)
            config.ApplyTimeOfDayToProfile(runtimeProfile, atmosphereHour);
        else
            config.ApplyFoliageGlobals(atmosphereHour);

        ApplyWaterMaterialTuning(atmosphereHour);
        ApplyLocalMistPalette(atmosphereHour);
        ApplyGoldenHourHighlightLimits(atmosphereHour);
        RefreshRuntimeDebug();
    }

    private void HandleConfigPreviewChanged()
    {
        ApplyConfiguredCameraFogMasks();
        ConfigureLocalLightShaftZones();
        ConfigurePreButoLighthouseGlass();

        float hour = HasCurrentHour
            ? timeModel.CurrentHour.Value
            : config.LookDevelopmentHour;
        ApplyHour(hour);
    }

    private void ConfigurePreButoLighthouseGlass()
    {
        RestorePreButoLighthouseGlass();
        if (config == null || !config.EnablePreButoLighthouseGlass ||
            string.IsNullOrWhiteSpace(config.LighthouseGlassObjectName))
            return;

        int layer = LayerMask.NameToLayer(config.PreButoGlassLayerName);
        if (layer < 0)
            layer = config.PreButoGlassLayer;
        if (layer < 0 || layer > 31)
            return;

        Renderer[] renderers = gameObject.scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Where(candidate => candidate.gameObject.name == config.LighthouseGlassObjectName)
            .ToArray();
        foreach (Renderer renderer in renderers)
        {
            GameObject target = renderer.gameObject;
            glassLayerBindings.Add(new GlassLayerBinding
            {
                GameObject = target,
                OriginalLayer = target.layer
            });
            target.layer = layer;
            PreButoGlassRendererCount++;
        }
    }

    private void RestorePreButoLighthouseGlass()
    {
        foreach (GlassLayerBinding binding in glassLayerBindings)
        {
            if (binding.GameObject != null)
                binding.GameObject.layer = binding.OriginalLayer;
        }

        glassLayerBindings.Clear();
        PreButoGlassRendererCount = 0;
    }

    private void ApplyGoldenHourHighlightLimits(float hour)
    {
        float atmosphereHour = config.LockLookDevelopmentTime
            ? config.LookDevelopmentHour
            : TimeSystem.WrapHour(hour);
        bool isGoldenHour = atmosphereHour >= 16f && atmosphereHour <= 19.5f;
        if (!config.LockLookDevelopmentTime && !isGoldenHour) return;

        float duskBlend = Mathf.InverseLerp(17f, 18f, atmosphereHour);
        CurrentSunIntensityLimit =
            config.MainSunIntensity * Mathf.Lerp(1.08f, 1f, duskBlend);
        CurrentReflectionIntensityLimit =
            Mathf.Min(1f, config.ReflectionIntensity + Mathf.Lerp(0.08f, 0f, duskBlend));

        if (RenderSettings.sun != null)
        {
            RenderSettings.sun.color = config.MainSunColor;
            RenderSettings.sun.intensity = CurrentSunIntensityLimit;
        }

        RenderSettings.reflectionIntensity = CurrentReflectionIntensityLimit;
    }

    private void ConfigureWaterMaterials()
    {
        if (!config.EnableButoWaterIntegration || config.WaterSourceMaterial == null ||
            config.WaterSourceMaterial.shader == null || config.ButoWaterShader == null)
            return;

        Shader sourceShader = config.WaterSourceMaterial.shader;
        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.gameObject.scene != gameObject.scene)
                continue;

            Material[] originals = renderer.sharedMaterials;
            Material[] replacements = null;
            for (int i = 0; i < originals.Length; i++)
            {
                Material source = originals[i];
                if (source == null || source.shader != sourceShader)
                    continue;

                if (replacements == null)
                    replacements = (Material[])originals.Clone();

                if (!runtimeWaterMaterials.TryGetValue(source, out Material runtimeMaterial))
                {
                    runtimeMaterial = CreateRuntimeWaterMaterial(source);
                    runtimeWaterMaterials.Add(source, runtimeMaterial);
                }

                replacements[i] = runtimeMaterial;
            }

            if (replacements == null)
                continue;

            waterRendererBindings.Add(new WaterRendererBinding
            {
                Renderer = renderer,
                OriginalMaterials = originals
            });
            renderer.sharedMaterials = replacements;
            ButoWaterRendererCount++;
        }

        foreach (Material material in runtimeWaterMaterials.Values)
        {
            debugWaterMaterial = material;
            break;
        }
    }

    private Material CreateRuntimeWaterMaterial(Material source)
    {
        Material runtimeMaterial = new Material(config.ButoWaterShader)
        {
            name = source.name + " (Buto Runtime)",
            hideFlags = HideFlags.DontSave
        };
        runtimeMaterial.CopyPropertiesFromMaterial(source);
        runtimeMaterial.renderQueue = source.renderQueue;
        runtimeMaterial.enableInstancing = source.enableInstancing;
        runtimeMaterial.doubleSidedGI = source.doubleSidedGI;
        runtimeMaterial.globalIlluminationFlags = source.globalIlluminationFlags;
        return runtimeMaterial;
    }

    private void RestoreWaterMaterials()
    {
        foreach (WaterRendererBinding binding in waterRendererBindings)
        {
            if (binding.Renderer != null)
                binding.Renderer.sharedMaterials = binding.OriginalMaterials;
        }

        foreach (Material material in runtimeWaterMaterials.Values)
        {
            if (material != null)
                Destroy(material);
        }

        waterRendererBindings.Clear();
        runtimeWaterMaterials.Clear();
        ButoWaterRendererCount = 0;
        debugWaterMaterial = null;
    }

    private void ApplyWaterMaterialTuning(float hour)
    {
        if (runtimeWaterMaterials.Count == 0) return;

        float tintStrength = Mathf.Clamp01(config.WaterAtmosphereTintStrength);
        Color litFog = config.EvaluateLitFogColor(hour);
        Color distanceFog = config.EvaluateDistanceFogColor(hour);
        Color distantTint = Color.Lerp(config.WaterDistantTint, distanceFog, 0.45f);
        Color warmSpecular = Color.Lerp(config.MainSunColor, litFog, 0.25f) *
                             Mathf.Max(0f, config.WaterSunSpecularStrength);
        warmSpecular.a = 1f;

        foreach (KeyValuePair<Material, Material> pair in runtimeWaterMaterials)
        {
            Material source = pair.Key;
            Material runtime = pair.Value;

            SetTintedColor(runtime, source, ShallowColorId, config.WaterShallowTint, tintStrength);
            SetTintedColor(runtime, source, WaterShallowColorId, config.WaterShallowTint, tintStrength);
            SetTintedColor(runtime, source, DeepColorId, config.WaterDeepTint, tintStrength);
            SetTintedColor(runtime, source, WaterDeepColorId, config.WaterDeepTint, tintStrength);
            SetTintedColor(runtime, source, VeryDeepColorId, config.WaterVeryDeepTint, tintStrength);
            SetTintedColor(runtime, source, WaterVeryDeepColorId, config.WaterVeryDeepTint, tintStrength);
            SetTintedColor(runtime, source, DistantColorId, distantTint, tintStrength);
            SetTintedColor(runtime, source, WaterFarColorId, distantTint, tintStrength);
            SetTintedColor(runtime, source, FoamColorId, config.WaterFoamTint, tintStrength);
            SetTintedColor(runtime, source, OceanFoamColorId, config.WaterFoamTint, tintStrength);
            SetTintedColor(runtime, source, ShoreFoamTintId, config.WaterFoamTint, tintStrength);
            SetTintedColor(runtime, source, ShoreWaveColorId, config.WaterFoamTint, tintStrength);
            SetTintedColor(runtime, source, ShoreWaveTintId, config.WaterFoamTint, tintStrength);
            SetColorPreservingAlpha(runtime, source, SpecularColorId, warmSpecular);
            SetColorPreservingAlpha(runtime, source, SpecColorId, warmSpecular);
            SetMaximumFloat(runtime, source, SmoothnessId, config.WaterMaxSmoothness);
            SetMaximumFloat(runtime, source, DistantIntensityId, config.WaterDistantIntensity);
            SetMaximumFloat(runtime, source, BaseOpacityId, config.WaterMaxOpacity);
        }
    }

    private void CacheLocalMistLayers()
    {
        localMistLayers.Clear();
        FirewatchLocalMistLayer[] layers = FindObjectsByType<FirewatchLocalMistLayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (FirewatchLocalMistLayer layer in layers)
        {
            if (layer != null && layer.gameObject.scene == gameObject.scene)
                localMistLayers.Add(layer);
        }
    }

    private void ApplyLocalMistPalette(float hour)
    {
        foreach (FirewatchLocalMistLayer layer in localMistLayers)
        {
            if (layer != null)
                layer.ApplyPalette(config, hour);
        }
    }

    private static void SetTintedColor(
        Material runtime,
        Material source,
        int propertyId,
        Color target,
        float blend)
    {
        if (!runtime.HasProperty(propertyId) || !source.HasProperty(propertyId)) return;

        Color original = source.GetColor(propertyId);
        Color value = Color.Lerp(original, target, blend);
        value.a = original.a;
        runtime.SetColor(propertyId, value);
    }

    private static void SetColorPreservingAlpha(
        Material runtime,
        Material source,
        int propertyId,
        Color value)
    {
        if (!runtime.HasProperty(propertyId) || !source.HasProperty(propertyId)) return;

        value.a = source.GetColor(propertyId).a;
        runtime.SetColor(propertyId, value);
    }

    private static void SetMaximumFloat(
        Material runtime,
        Material source,
        int propertyId,
        float maximum)
    {
        if (!runtime.HasProperty(propertyId) || !source.HasProperty(propertyId)) return;
        runtime.SetFloat(propertyId, Mathf.Min(source.GetFloat(propertyId), maximum));
    }

    private void RefreshRuntimeDebug()
    {
        VolumeProfile profile = runtimeProfile != null
            ? runtimeProfile
            : targetVolume != null ? targetVolume.sharedProfile : null;
        if (profile == null) return;

        if (profile.TryGet(out ButoVolumetricFog buto))
        {
            CurrentFogDensity = buto.fogDensity.value;
            CurrentFogBase = buto.baseHeight.value;
            CurrentFogReach = buto.attenuationBoundarySize.value;
            CurrentFogViewDistance = buto.maxDistanceVolumetric.value;
            CurrentFogNoiseScale = buto.noiseTiling.value;
            CurrentFogWind = buto.noiseWindSpeed.value;
            CurrentFogLitColor = buto.litColor.value;
            CurrentFogShadowColor = buto.shadowedColor.value;
            CurrentFogColorRamp = buto.colorRamp.value;
        }

        if (profile.TryGet(out ColorAdjustments colorAdjustments))
            CurrentExposure = colorAdjustments.postExposure.value;
        if (profile.TryGet(out Tonemapping tonemapping))
            CurrentTonemapping = tonemapping.mode.value.ToString();
        if (profile.TryGet(out Bloom bloom))
            CurrentBloom = bloom.intensity.value;

        CurrentCameraHdr = debugCamera != null && debugCamera.allowHDR;
    }

    private void ApplyConfiguredCameraFogMasks()
    {
        ConfiguredCameraFogMaskCount = 0;
        FogDensityMask[] masks = FindObjectsByType<FogDensityMask>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (FogDensityMask mask in masks)
        {
            if (mask == null || mask.gameObject.name != "Firewatch Near Fog Exclusion")
                continue;
            if (mask.GetComponentInParent<Camera>(true) == null)
                continue;

            mask.DensityMultiplier = config.NearFogDensityMultiplier;
            mask.transform.localPosition = Vector3.zero;
            mask.transform.localScale =
                Vector3.one * Mathf.Max(0.01f, config.NearFogClearRadius);
            mask.enabled = config.EnableNearFogClearZone;
            mask.gameObject.SetActive(config.EnableNearFogClearZone);
            ConfiguredCameraFogMaskCount++;
        }
    }

    private void ConfigureLocalLightShaftZones()
    {
        LocalLightShaftZoneCount = 0;
        if (config == null) return;

        FogDensityMask[] allMasks = FindObjectsByType<FogDensityMask>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        int nonShaftMaskCount = 0;
        foreach (FogDensityMask existingMask in allMasks)
        {
            if (existingMask != null &&
                !existingMask.gameObject.name.StartsWith(LightShaftZonePrefix))
                nonShaftMaskCount++;
        }

        int maskBudget = Mathf.Max(0, 8 - nonShaftMaskCount);
        FirewatchVisualStyleConfig.LocalLightShaftZoneSettings[] settings =
            config.LocalLightShaftZones;
        int requestedCount = config.EnableLocalLightShaftZones && settings != null
            ? Mathf.Min(settings.Length, maskBudget)
            : 0;

        for (int index = 0; index < requestedCount; index++)
        {
            FirewatchVisualStyleConfig.LocalLightShaftZoneSettings zone = settings[index];
            if (zone == null || !zone.Enabled) continue;

            string zoneName = LightShaftZonePrefix +
                              (string.IsNullOrWhiteSpace(zone.Label) ? $"Zone {index + 1}" : zone.Label.Trim());
            FogDensityMask mask = null;
            foreach (FogDensityMask candidate in allMasks)
            {
                if (candidate != null && candidate.gameObject.name == zoneName &&
                    candidate.gameObject.scene == gameObject.scene)
                {
                    mask = candidate;
                    break;
                }
            }

            if (mask == null)
            {
                GameObject zoneObject = new GameObject(zoneName);
                zoneObject.transform.SetParent(transform, false);
                mask = zoneObject.AddComponent<FogDensityMask>();
                runtimeLightShaftMasks.Add(mask);
            }

            mask.transform.position = zone.Center;
            mask.transform.rotation = Quaternion.Euler(zone.EulerAngles);
            mask.transform.localScale = new Vector3(
                Mathf.Max(0.1f, Mathf.Abs(zone.Size.x)),
                Mathf.Max(0.1f, Mathf.Abs(zone.Size.y)),
                Mathf.Max(0.1f, Mathf.Abs(zone.Size.z)));
            ApplyFogMaskSettings(
                mask,
                FogDensityMask.PrimitiveShape.Box,
                FogDensityMask.BlendMode.Multiplicative,
                Mathf.Max(1f, zone.DensityMultiplier),
                Mathf.Max(0f, zone.BlendDistance));
            mask.gameObject.SetActive(true);
            mask.enabled = true;
            LocalLightShaftZoneCount++;
        }

        foreach (FogDensityMask existingMask in allMasks)
        {
            if (existingMask == null ||
                !existingMask.gameObject.name.StartsWith(LightShaftZonePrefix))
                continue;

            bool isConfigured = false;
            for (int index = 0; index < requestedCount; index++)
            {
                FirewatchVisualStyleConfig.LocalLightShaftZoneSettings zone = settings[index];
                if (zone == null || !zone.Enabled) continue;
                string expectedName = LightShaftZonePrefix +
                                      (string.IsNullOrWhiteSpace(zone.Label)
                                          ? $"Zone {index + 1}"
                                          : zone.Label.Trim());
                if (existingMask.gameObject.name == expectedName)
                {
                    isConfigured = true;
                    break;
                }
            }

            if (!isConfigured)
                existingMask.gameObject.SetActive(false);
        }
    }

    private static void ApplyFogMaskSettings(
        FogDensityMask mask,
        FogDensityMask.PrimitiveShape shape,
        FogDensityMask.BlendMode mode,
        float densityMultiplier,
        float blendDistance)
    {
        FogMaskSerializedSettings serializedSettings = new FogMaskSerializedSettings
        {
            shape = shape,
            mode = mode,
            densityMultiplier = densityMultiplier,
            blendDistance = blendDistance
        };
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(serializedSettings), mask);
    }

    private void DestroyRuntimeLightShaftZones()
    {
        foreach (FogDensityMask mask in runtimeLightShaftMasks)
        {
            if (mask != null)
                Destroy(mask.gameObject);
        }

        runtimeLightShaftMasks.Clear();
        LocalLightShaftZoneCount = 0;
    }

    private bool HasCurrentHour => timeModel != null && timeModel.CurrentHour != null;
}
