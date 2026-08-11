using System;
using MK.Toon;
using OccaSoftware.Buto.Runtime;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[CreateAssetMenu(
    menuName = "Light Tower/Visual/Firewatch Visual Style",
    fileName = "FirewatchVisualStyleConfig")]
public class FirewatchVisualStyleConfig : ScriptableObject
{
    public event Action PreviewChanged;

    [Serializable]
    public class AtmosphereKey
    {
        public string Label;
        [Range(0f, 24f)] public float Hour;
        [MinValue(0f)] public float FogDensity = 2f;
        [MinValue(10f)] public float MaxDistance = 450f;
        [ColorUsage(false, true)] public Color LitFogColor = Color.white;
        [ColorUsage(false, true)] public Color ShadowFogColor = Color.gray;
        [ColorUsage(false, true)] public Color TowardSunColor = Color.white;
        [ColorUsage(false, true)] public Color AwayFromSunColor = Color.gray;
        [ColorUsage(false, true)] public Color FoliageLitTint = Color.white;
        [ColorUsage(false, true)] public Color FoliageShadowTint = Color.gray;
        [ColorUsage(false, true)] public Color DistanceFogColor = Color.gray;
    }

    [TabGroup("Setup"), ReadOnly] public int ConfigurationRevision;
    [TabGroup("Setup"), Required, AssetsOnly] public VolumeProfile TargetVolumeProfile;
    [TabGroup("Setup"), AssetsOnly] public Material PlayerToonMaterial;

    [TabGroup("Look Dev")] public bool LockLookDevelopmentTime = true;
    [TabGroup("Look Dev"), Range(17f, 18.5f)] public float LookDevelopmentHour = 17.75f;
    [TabGroup("Look Dev"), MinValue(1f)] public float NearPaletteDistance = 85f;
    [TabGroup("Look Dev"), MinValue(1f)] public float MidPaletteDistance = 260f;
    [TabGroup("Look Dev"), MinValue(1f)] public float FarPaletteDistance = 620f;
    [TabGroup("Look Dev"), ColorUsage(false, false)] public Color NearPaletteTint =
        new Color(0.86f, 0.83f, 0.67f, 1f);
    [TabGroup("Look Dev"), ColorUsage(false, false)] public Color MidPaletteTint =
        new Color(0.68f, 0.65f, 0.62f, 1f);
    [TabGroup("Look Dev"), ColorUsage(false, false)] public Color FarPaletteTint =
        new Color(0.54f, 0.58f, 0.76f, 1f);

    [TabGroup("Lighting"), ColorUsage(false, true)] public Color MainSunColor =
        new Color(1f, 0.52f, 0.28f, 1f);
    [TabGroup("Lighting"), MinValue(0f)] public float MainSunIntensity = 1.35f;
    [TabGroup("Lighting"), Range(0f, 1f)] public float MainSunShadowStrength = 0.78f;
    [TabGroup("Lighting"), ColorUsage(false, true)] public Color FillLightColor =
        new Color(0.2f, 0.32f, 0.52f, 1f);
    [TabGroup("Lighting"), MinValue(0f)] public float FillLightIntensity = 0.08f;
    [TabGroup("Lighting"), ColorUsage(false, false)] public Color AmbientSkyColor =
        new Color(0.58f, 0.43f, 0.48f, 1f);
    [TabGroup("Lighting"), ColorUsage(false, false)] public Color AmbientEquatorColor =
        new Color(0.38f, 0.34f, 0.4f, 1f);
    [TabGroup("Lighting"), ColorUsage(false, false)] public Color AmbientGroundColor =
        new Color(0.16f, 0.2f, 0.24f, 1f);
    [TabGroup("Lighting"), Range(0f, 2f)] public float AmbientIntensity = 0.72f;
    [TabGroup("Lighting"), Range(0f, 1f)]
    [Tooltip("Controls sky and reflection-probe contribution. Lower values keep distant sunlit surfaces from washing out.")]
    public float ReflectionIntensity = 0.5f;

    [TabGroup("Buto Fog")]
    [InfoBox("Overall controls affect every time key. Atmosphere Keys still define the time-of-day shape.")]
    public bool EnableButo = true;
    [TabGroup("Buto Fog"), Title("Volume Quality")]
    public OccaSoftware.Buto.Runtime.QualityLevel FogQuality =
        OccaSoftware.Buto.Runtime.QualityLevel.High;
    [TabGroup("Buto Fog"), LabelText("Froxel Pixel Size"), Range(4, 16)]
    [Tooltip("Lower values produce cleaner light shafts at a higher GPU cost.")]
    public int FogGridPixelSize = 8;
    [TabGroup("Buto Fog"), LabelText("Depth Samples"), Range(32, 240)]
    public int FogDepthSamples = 164;
    [TabGroup("Buto Fog"), LabelText("Lighting Temporal Blend"), Range(0f, 1f)]
    public float FogTemporalLighting = 0.063f;
    [TabGroup("Buto Fog"), LabelText("Media Temporal Blend"), Range(0f, 1f)]
    public float FogTemporalMedia = 0.077f;

    [TabGroup("Buto Fog"), Title("Body And Coverage")]
    [LabelText("Body Density Multiplier"), Range(0f, 20f)]
    public float FogDensityMultiplier = 0.68f;
    [TabGroup("Buto Fog"), LabelText("Volumetric Range Multiplier"), Range(0.1f, 2f)]
    public float FogDistanceMultiplier = 0.58f;
    [TabGroup("Buto Fog"), LabelText("Fallback Max Distance"), MinValue(10f)]
    public float MaxVolumetricDistance = 520f;
    [TabGroup("Buto Fog"), LabelText("Fallback Fog Density"), MinValue(0f)]
    public float FogDensity = 2.25f;
    [TabGroup("Buto Fog"), Title("Lighting And Height")]
    [Range(-1f, 1f)] public float Anisotropy = 0.45f;
    [TabGroup("Buto Fog"), MinValue(0f)] public float LightIntensity = 0.8f;
    [TabGroup("Buto Fog"), MinValue(0f)] public float DensityInLight = 0.68f;
    [TabGroup("Buto Fog"), MinValue(0f)] public float DensityInShadow = 0.82f;
    [TabGroup("Buto Fog")] public float BaseHeight = 22f;
    [TabGroup("Buto Fog"), MinValue(1f)] public float HeightFalloff = 24f;
    [TabGroup("Buto Fog"), LabelText("Enable Camera Fog Zone")]
    public bool EnableNearFogClearZone = true;
    [TabGroup("Buto Fog"), LabelText("Camera Fog Density Multiplier"), Range(0f, 5f)]
    [Tooltip("Multiplies Buto density around each camera. 0 clears fog, 1 keeps global density, and values above 1 create a denser local fog body.")]
    public float NearFogDensityMultiplier = 5f;
    [TabGroup("Buto Fog"), LabelText("Camera Fog Radius"), MinValue(0f), SuffixLabel("m")]
    public float NearFogClearRadius = 20f;
    [TabGroup("Buto Fog"), LabelText("Camera Fog Blend"), MinValue(0f), SuffixLabel("m")]
    public float NearFogClearBlend = 28f;
    [TabGroup("Buto Fog"), ColorUsage(false, true)] public Color EmissionFogColor =
        new Color(0.0015f, 0.0005f, 0.0002f, 1f);
    [TabGroup("Buto Fog"), Range(0f, 1f)] public float FogColorInfluence = 0.16f;
    [TabGroup("Buto Fog")] public float DirectionalRatio = 1f;

    [TabGroup("Buto Fog"), Title("Distance Color Ramp")]
    [InfoBox("The generated three-row Buto ramp changes shadow, lit, and emissive fog color over distance.")]
    public bool UseGeneratedFogColorRamp = true;
    [TabGroup("Buto Fog"), GradientUsage(true)] public Gradient FogDistanceColorRamp =
        CreateDefaultFogDistanceColorRamp();
    [TabGroup("Buto Fog"), LabelText("Shadow Ramp Brightness"), Range(0f, 2f)]
    public float FogShadowRampBrightness = 0.58f;
    [TabGroup("Buto Fog"), LabelText("Lit Ramp Brightness"), Range(0f, 2f)]
    public float FogLitRampBrightness = 0.78f;
    [TabGroup("Buto Fog"), LabelText("Emission Ramp Brightness"), Range(0f, 1f)]
    public float FogEmissionRampBrightness = 0.02f;
    [TabGroup("Buto Fog"), AssetsOnly, PreviewField(64), LabelText("Generated Ramp Texture")]
    public Texture2D FogColorRampTexture;

    [TabGroup("Buto Fog"), Title("Three-Dimensional Noise")]
    public VolumeNoise.NoiseType FogNoiseType = VolumeNoise.NoiseType.PerlinWorley;
    [TabGroup("Buto Fog")] public VolumeNoise.NoiseQuality FogNoiseQuality =
        VolumeNoise.NoiseQuality.High;
    [TabGroup("Buto Fog"), LabelText("Noise Detail Frequency"), Range(1, 64)]
    public int FogNoiseFrequency = 6;
    [TabGroup("Buto Fog"), LabelText("Generated Noise Octaves"), Range(1, 4)]
    public int FogNoiseGeneratedOctaves = 4;
    [TabGroup("Buto Fog"), LabelText("Generated Noise Lacunarity"), Range(1, 8)]
    public int FogNoiseGeneratedLacunarity = 2;
    [TabGroup("Buto Fog"), LabelText("Generated Noise Gain"), Range(0f, 1f)]
    public float FogNoiseGeneratedGain = 0.42f;
    [TabGroup("Buto Fog")] public int FogNoiseSeed = 17;
    [TabGroup("Buto Fog")] public bool InvertFogNoise;
    [TabGroup("Buto Fog"), LabelText("Sampled Noise Octaves"), Range(1, 3)]
    public int FogNoiseSamplingOctaves = 2;
    [TabGroup("Buto Fog"), LabelText("Sampled Noise Lacunarity"), Range(1f, 8f)]
    public float FogNoiseSamplingLacunarity = 2f;
    [TabGroup("Buto Fog"), LabelText("Sampled Noise Gain"), Range(0f, 1f)]
    public float FogNoiseSamplingGain = 0.25f;
    [TabGroup("Buto Fog"), LabelText("Fog Patch Scale"), MinValue(0f)]
    public float NoiseTiling = 60f;
    [TabGroup("Buto Fog")] public Vector3 NoiseWindSpeed = new Vector3(0.08f, 0f, 0.025f);
    [TabGroup("Buto Fog")] public Vector2 NoiseRemap = new Vector2(0.25f, 0.9f);

    [TabGroup("Buto Fog"), ListDrawerSettings(DefaultExpandedState = false)]
    public AtmosphereKey[] AtmosphereKeys = CreateDefaultAtmosphereKeys();

    [TabGroup("Buto Fog"), ShowInInspector, ReadOnly, LabelText("Current Evaluated Density")]
    private float CurrentEvaluatedFogDensity =>
        EvaluateAtmosphere(LookDevelopmentHour).FogDensity * FogDensityMultiplier;

    [TabGroup("Buto Fog"), ShowInInspector, ReadOnly, LabelText("Current Evaluated Distance")]
    private float CurrentEvaluatedFogDistance =>
        EvaluateAtmosphere(LookDevelopmentHour).MaxDistance * FogDistanceMultiplier;

    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Environment Preset")]
    private string CurrentEnvironmentPreset => "Firewatch Sunset / Golden Hour";
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Fog Density")]
    private float DebugFogDensity => CurrentEvaluatedFogDensity;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Fog Range")]
    [SuffixLabel("m")]
    private float DebugFogRange => CurrentEvaluatedFogDistance;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Lit Surface Retained At 50m")]
    [SuffixLabel("%")]
    private float DebugLitSurfaceRetentionAt50m =>
        Mathf.Exp(-CurrentEvaluatedFogDensity * 0.01f * DensityInLight * 50f) * 100f;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, PreviewField(64), LabelText("Color Ramp")]
    private Texture2D DebugColorRamp => FogColorRampTexture;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Volumetric Light")]
    private float DebugVolumetricLighting => LightIntensity;
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Noise Strength")]
    private float DebugNoiseStrength => 1f - Mathf.Clamp01(NoiseRemap.y - NoiseRemap.x);
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Local Fog Influence")]
    private string DebugLocalFogInfluence => EnableNearFogClearZone
        ? $"Each camera: {NearFogDensityMultiplier:F1}x density inside {NearFogClearRadius:F0}m, {NearFogClearBlend:F0}m blend"
        : "None";
    [TabGroup("Runtime Debug"), ShowInInspector, ReadOnly, LabelText("Unity Native Fog")]
    private string DebugUnityFog => RenderSettings.fog ? "Enabled (conflict)" : "Disabled";

    [TabGroup("Buto Fog"), Button("Apply Fog Preview", ButtonSizes.Medium)]
    private void ApplyFogPreview()
    {
        ApplyVolumeProfile();
        NotifyPreviewChanged();
#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    [TabGroup("Foliage"), Range(2, 3)] public int FoliageLightBands = 3;
    [TabGroup("Foliage"), Range(0f, 0.49f)] public float FoliageBandSoftness = 0.08f;
    [TabGroup("Foliage"), ColorUsage(false, true)] public Color FoliageBacklightColor =
        new Color(1f, 0.42f, 0.16f, 1f);
    [TabGroup("Foliage"), Range(0f, 2f)] public float FoliageBacklightStrength = 0.42f;
    [TabGroup("Foliage"), ColorUsage(false, false)] public Color FoliageBottomTint =
        new Color(0.58f, 0.52f, 0.32f, 1f);
    [TabGroup("Foliage"), ColorUsage(false, false)] public Color FoliageTopTint =
        new Color(0.92f, 0.76f, 0.42f, 1f);
    [TabGroup("Foliage")] public float FoliageGradientBaseHeight = -8f;
    [TabGroup("Foliage"), MinValue(0.1f)] public float FoliageGradientHeight = 110f;
    [TabGroup("Foliage"), Range(0f, 0.5f)] public float FoliageColorVariation = 0.14f;
    [TabGroup("Foliage"), Range(0f, 2f)] public float FoliageWindStrength = 0.44f;
    [TabGroup("Foliage"), Range(0f, 4f)] public float FoliageWindSpeed = 0.72f;
    [TabGroup("Foliage"), Range(0f, 2f)] public float FoliageGustStrength = 0.34f;
    [TabGroup("Foliage"), MinValue(0.001f)] public float FoliageGustScale = 0.018f;
    [TabGroup("Foliage")] public Vector2 FoliageWindDirection = new Vector2(0.86f, 0.5f);
    [TabGroup("Foliage"), Range(0f, 1f)] public float FoliageAlphaCutoff = 0.4f;

    [TabGroup("Environment Toon"), Range(2, 5)] public int EnvironmentLightBands = 3;
    [TabGroup("Environment Toon"), Range(0f, 1f)] public float EnvironmentLightBandsScale = 0.62f;
    [TabGroup("Environment Toon"), Range(0f, 1f)] public float EnvironmentLightThreshold = 0.48f;
    [TabGroup("Environment Toon"), Range(0f, 1f)] public float EnvironmentDiffuseSmoothness = 0.08f;
    [TabGroup("Environment Toon"), Range(0f, 1f)] public float EnvironmentDiffuseThreshold = 0.4f;
    [TabGroup("Environment Toon"), ColorUsage(false, false)] public Color TrunkTint =
        new Color(0.72f, 0.48f, 0.34f, 1f);
    [TabGroup("Environment Toon"), ColorUsage(false, false)] public Color RockTint =
        new Color(0.82f, 0.68f, 0.58f, 1f);
    [TabGroup("Environment Toon"), ColorUsage(false, false)] public Color BuildingTint =
        new Color(0.92f, 0.72f, 0.5f, 1f);

    [TabGroup("Distant Vista")] public bool GenerateDistantVista = true;
    [TabGroup("Distant Vista"), AssetsOnly] public GameObject BackgroundTreesA;
    [TabGroup("Distant Vista"), AssetsOnly] public GameObject BackgroundTreesB;
    [TabGroup("Distant Vista"), AssetsOnly] public GameObject NearMountainA;
    [TabGroup("Distant Vista"), AssetsOnly] public GameObject NearMountainB;
    [TabGroup("Distant Vista"), AssetsOnly] public GameObject FarMountainA;
    [TabGroup("Distant Vista"), AssetsOnly] public GameObject FarMountainB;
    [TabGroup("Distant Vista"), Range(30f, 240f)] public float VistaArc = 125f;
    [TabGroup("Distant Vista"), Range(3, 12)] public int MidTreeCount = 7;
    [TabGroup("Distant Vista"), MinValue(20f)] public float MidTreeRadius = 145f;
    [TabGroup("Distant Vista"), MinValue(5f)] public float MidTreeTargetHeight = 34f;
    [TabGroup("Distant Vista")] public float MidTreeVerticalOffset = -4f;
    [TabGroup("Distant Vista"), Range(2, 8)] public int NearMountainCount = 4;
    [TabGroup("Distant Vista"), MinValue(50f)] public float NearMountainRadius = 390f;
    [TabGroup("Distant Vista"), MinValue(20f)] public float NearMountainTargetHeight = 170f;
    [TabGroup("Distant Vista")] public float NearMountainVerticalOffset = -26f;
    [TabGroup("Distant Vista"), Range(2, 8)] public int FarMountainCount = 4;
    [TabGroup("Distant Vista"), MinValue(100f)] public float FarMountainRadius = 780f;
    [TabGroup("Distant Vista"), MinValue(20f)] public float FarMountainTargetHeight = 270f;
    [TabGroup("Distant Vista")] public float FarMountainVerticalOffset = -52f;

    [TabGroup("Color Grade")] public TonemappingMode ToneMapper = TonemappingMode.ACES;
    [TabGroup("Color Grade"), Range(-5f, 5f)] public float Exposure = -0.15f;
    [TabGroup("Color Grade"), Range(-100f, 100f)] public float Contrast = 16f;
    [TabGroup("Color Grade"), Range(-100f, 100f)] public float Saturation = 10f;
    [TabGroup("Color Grade"), ColorUsage(false, true)] public Color ColorFilter =
        new Color(1f, 0.9f, 0.78f, 1f);
    [TabGroup("Color Grade"), Range(-100f, 100f)] public float Temperature = 20f;
    [TabGroup("Color Grade"), Range(-100f, 100f)] public float Tint = 5f;
    [TabGroup("Color Grade"), ColorUsage(false, false)] public Color SplitShadowColor =
        new Color(0.28f, 0.34f, 0.48f, 1f);
    [TabGroup("Color Grade"), ColorUsage(false, false)] public Color SplitHighlightColor =
        new Color(0.76f, 0.5f, 0.3f, 1f);
    [TabGroup("Color Grade"), Range(-100f, 100f)] public float SplitBalance = 8f;
    [TabGroup("Color Grade"), Range(0f, 1f)] public float BloomIntensity = 0.06f;
    [TabGroup("Color Grade"), MinValue(0f)] public float BloomThreshold = 1.25f;
    [TabGroup("Color Grade"), Range(0f, 1f)] public float VignetteIntensity = 0.05f;

    [TabGroup("MK Toon"), Range(2, 12)] public int LightBands = 4;
    [TabGroup("MK Toon"), Range(0f, 1f)] public float LightBandsScale = 0.58f;
    [TabGroup("MK Toon"), Range(0f, 1f)] public float LightThreshold = 0.46f;
    [TabGroup("MK Toon"), Range(0f, 1f)] public float DiffuseSmoothness = 0.12f;
    [TabGroup("MK Toon"), Range(0f, 1f)] public float DiffuseThreshold = 0.38f;
    [TabGroup("MK Toon"), Range(0f, 1f)] public float MaterialSmoothness = 0.08f;
    [TabGroup("MK Toon"), MinValue(0f)] public float ToonContrast = 1.08f;
    [TabGroup("MK Toon"), MinValue(0f)] public float ToonSaturation = 1.08f;
    [TabGroup("MK Toon"), MinValue(0f)] public float ToonBrightness = 1.02f;

    [Button("Apply Visual Style", ButtonSizes.Large)]
    public void ApplyVisualStyle()
    {
        ApplyVolumeProfile();
        ApplyPlayerToonMaterial();
        ApplyFoliageGlobals(LookDevelopmentHour);
        NotifyPreviewChanged();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
    }

    public bool EnsureDefaults()
    {
        bool changed = false;
        if (AtmosphereKeys == null || AtmosphereKeys.Length == 0)
        {
            AtmosphereKeys = CreateDefaultAtmosphereKeys();
            changed = true;
        }

        if (MidPaletteDistance <= NearPaletteDistance)
        {
            MidPaletteDistance = NearPaletteDistance + 1f;
            changed = true;
        }

        if (FarPaletteDistance <= MidPaletteDistance)
        {
            FarPaletteDistance = MidPaletteDistance + 1f;
            changed = true;
        }

        if (FogDistanceColorRamp == null || FogDistanceColorRamp.colorKeys.Length == 0)
        {
            FogDistanceColorRamp = CreateDefaultFogDistanceColorRamp();
            changed = true;
        }

        Vector2 validatedNoiseRemap = GetValidatedNoiseRemap();
        if (NoiseRemap != validatedNoiseRemap)
        {
            NoiseRemap = validatedNoiseRemap;
            changed = true;
        }

        return changed;
    }

    public void ApplyVolumeProfile()
    {
        if (TargetVolumeProfile == null) return;

#if UNITY_EDITOR
        EnsureFogColorRampTextureAsset();
#endif

        ApplyTimeOfDayToProfile(TargetVolumeProfile, LookDevelopmentHour);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(TargetVolumeProfile);
        color.active = true;
        Set(color.postExposure, Exposure);
        Set(color.contrast, Contrast);
        Set(color.saturation, Saturation);
        Set(color.colorFilter, ColorFilter);

        WhiteBalance whiteBalance = GetOrAdd<WhiteBalance>(TargetVolumeProfile);
        whiteBalance.active = true;
        Set(whiteBalance.temperature, Temperature);
        Set(whiteBalance.tint, Tint);

        SplitToning splitToning = GetOrAdd<SplitToning>(TargetVolumeProfile);
        splitToning.active = true;
        Set(splitToning.shadows, SplitShadowColor);
        Set(splitToning.highlights, SplitHighlightColor);
        Set(splitToning.balance, SplitBalance);

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(TargetVolumeProfile);
        tonemapping.active = true;
        Set(tonemapping.mode, ToneMapper);

        Bloom bloom = GetOrAdd<Bloom>(TargetVolumeProfile);
        bloom.active = BloomIntensity > 0f;
        Set(bloom.intensity, BloomIntensity);
        Set(bloom.threshold, BloomThreshold);
        Set(bloom.scatter, 0.5f);

        Vignette vignette = GetOrAdd<Vignette>(TargetVolumeProfile);
        vignette.active = VignetteIntensity > 0f;
        Set(vignette.intensity, VignetteIntensity);
        Set(vignette.smoothness, 0.45f);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(TargetVolumeProfile);
#endif
    }

    public void ApplyTimeOfDayToProfile(VolumeProfile profile, float hour)
    {
        if (profile == null) return;

        AtmosphereSample sample = EvaluateAtmosphere(hour);
        ButoVolumetricFog buto = GetOrAdd<ButoVolumetricFog>(profile);
        buto.active = EnableButo;
        Set(buto.mode, EnableButo ? VolumetricFogMode.On : VolumetricFogMode.Off);
        Set(buto.qualityLevel, FogQuality);
        Set(buto.gridPixelSize, Mathf.Clamp(FogGridPixelSize, 4, 16));
        Set(buto.gridSizeZ, Mathf.Clamp(FogDepthSamples, 32, 240));
        Set(buto.temporalAALighting, FogTemporalLighting);
        Set(buto.temporalAAMedia, FogTemporalMedia);
        Set(buto.maxDistanceVolumetric,
            sample.MaxDistance * Mathf.Max(0.01f, FogDistanceMultiplier));
        Set(buto.fogDensity,
            sample.FogDensity * Mathf.Max(0f, FogDensityMultiplier));
        Set(buto.anisotropy, Anisotropy);
        Set(buto.lightIntensity, LightIntensity);
        Set(buto.densityInLight, DensityInLight);
        Set(buto.densityInShadow, DensityInShadow);
        Set(buto.baseHeight, BaseHeight);
        Set(buto.attenuationBoundarySize, HeightFalloff);
        Set(buto.litColor, sample.LitFogColor);
        Set(buto.shadowedColor, sample.ShadowFogColor);
        Set(buto.emitColor, EmissionFogColor);
        Set(buto.colorRamp, FogColorRampTexture);
        Set(buto.colorRampId, 0f);
        Set(buto.colorInfluence, FogColorInfluence);
        Set(buto.directionalForward, sample.TowardSunColor);
        Set(buto.directionalBack, sample.AwayFromSunColor);
        Set(buto.directionalRatio, DirectionalRatio);
        Set(buto.octaves, Mathf.Clamp(FogNoiseSamplingOctaves, 1, 3));
        Set(buto.lacunarity, FogNoiseSamplingLacunarity);
        Set(buto.gain, FogNoiseSamplingGain);
        Set(buto.noiseTiling, NoiseTiling);
        Set(buto.noiseWindSpeed, NoiseWindSpeed);
        Set(buto.noiseMap, GetValidatedNoiseRemap());
        ApplyVolumeNoise(buto);

        ApplyFoliageGlobals(hour);
        RenderSettings.fog = false;
    }

    private void ApplyVolumeNoise(ButoVolumetricFog buto)
    {
        VolumeNoise noise = buto.volumeNoise.value ?? new VolumeNoise();
        bool recreateTexture = noise.noiseType != FogNoiseType ||
                               noise.noiseQuality != FogNoiseQuality;
        bool changed = recreateTexture ||
                       noise.frequency != FogNoiseFrequency ||
                       noise.octaves != FogNoiseGeneratedOctaves ||
                       noise.lacunarity != FogNoiseGeneratedLacunarity ||
                       !Mathf.Approximately(noise.gain, FogNoiseGeneratedGain) ||
                       noise.seed != FogNoiseSeed ||
                       noise.invert != InvertFogNoise;

        if (recreateTexture)
            noise.Release();

        noise.noiseType = FogNoiseType;
        noise.noiseQuality = FogNoiseQuality;
        noise.frequency = Mathf.Clamp(FogNoiseFrequency, 1, 64);
        noise.octaves = Mathf.Clamp(FogNoiseGeneratedOctaves, 1, 4);
        noise.lacunarity = Mathf.Clamp(FogNoiseGeneratedLacunarity, 1, 8);
        noise.gain = Mathf.Clamp01(FogNoiseGeneratedGain);
        noise.seed = FogNoiseSeed;
        noise.invert = InvertFogNoise;
        if (changed)
            noise.SetDirty();

        Set(buto.volumeNoise, noise);
    }

    private Vector2 GetValidatedNoiseRemap()
    {
        float minimum = Mathf.Clamp01(NoiseRemap.x);
        float maximum = Mathf.Clamp01(NoiseRemap.y);
        if (maximum < minimum + 0.01f)
            maximum = Mathf.Min(1f, minimum + 0.01f);
        if (maximum <= minimum)
            minimum = Mathf.Max(0f, maximum - 0.01f);
        return new Vector2(minimum, maximum);
    }

    private static Gradient CreateDefaultFogDistanceColorRamp()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.3f, 0.34f, 0.25f), 0f),
                new GradientColorKey(new Color(0.38f, 0.33f, 0.25f), 0.1f),
                new GradientColorKey(new Color(0.74f, 0.42f, 0.27f), 0.3f),
                new GradientColorKey(new Color(0.46f, 0.39f, 0.58f), 0.6f),
                new GradientColorKey(new Color(0.44f, 0.53f, 0.66f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            });
        return gradient;
    }

    private void NotifyPreviewChanged()
    {
        PreviewChanged?.Invoke();
    }

#if UNITY_EDITOR
    private const string FogRampAssetPath = "Assets/Settings/FirewatchDuskFogColorRamp.png";
    private static bool updatingFogRampTexture;

    private void EnsureFogColorRampTextureAsset()
    {
        if (!UseGeneratedFogColorRamp || updatingFogRampTexture) return;

        updatingFogRampTexture = true;
        try
        {
            const int width = 256;
            const int height = 3;
            Texture2D generated = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                name = "Firewatch Dusk Fog Color Ramp",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                Color distanceColor = FogDistanceColorRamp.Evaluate(t);
                Color shadowColor = Color.Lerp(
                    distanceColor,
                    new Color(0.3f, 0.4f, 0.68f, 1f),
                    0.32f) * FogShadowRampBrightness;
                Color litColor = distanceColor * FogLitRampBrightness;
                Color emissionColor = distanceColor * FogEmissionRampBrightness;
                shadowColor.a = 1f;
                litColor.a = 1f;
                emissionColor.a = 1f;
                generated.SetPixel(x, 0, shadowColor);
                generated.SetPixel(x, 1, litColor);
                generated.SetPixel(x, 2, emissionColor);
            }

            generated.Apply(false, false);
            byte[] png = generated.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(generated);

            string absolutePath = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                FogRampAssetPath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(absolutePath));
            System.IO.File.WriteAllBytes(absolutePath, png);
            UnityEditor.AssetDatabase.ImportAsset(
                FogRampAssetPath,
                UnityEditor.ImportAssetOptions.ForceSynchronousImport |
                UnityEditor.ImportAssetOptions.ForceUpdate);

            UnityEditor.TextureImporter importer =
                UnityEditor.AssetImporter.GetAtPath(FogRampAssetPath) as UnityEditor.TextureImporter;
            if (importer != null)
            {
                bool importerChanged = importer.wrapMode != TextureWrapMode.Clamp ||
                                       importer.filterMode != FilterMode.Bilinear ||
                                       importer.mipmapEnabled ||
                                       importer.textureCompression !=
                                       UnityEditor.TextureImporterCompression.Uncompressed ||
                                       !importer.sRGBTexture;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                importer.sRGBTexture = true;
                if (importerChanged)
                    importer.SaveAndReimport();
            }

            Texture2D ramp = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(FogRampAssetPath);
            if (FogColorRampTexture != ramp)
            {
                FogColorRampTexture = ramp;
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
        finally
        {
            updatingFogRampTexture = false;
        }
    }

    private void OnValidate()
    {
        EnsureDefaults();

        if (UnityEditor.EditorApplication.isPlaying)
        {
            NotifyPreviewChanged();
            return;
        }

        if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess()) return;
        UnityEditor.EditorApplication.delayCall -= ApplyEditorPreview;
        UnityEditor.EditorApplication.delayCall += ApplyEditorPreview;
    }

    private void ApplyEditorPreview()
    {
        if (this == null || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

        ApplyVolumeProfile();
        ApplyFoliageGlobals(LookDevelopmentHour);
        if (TargetVolumeProfile != null)
            UnityEditor.EditorUtility.SetDirty(TargetVolumeProfile);
        UnityEditor.SceneView.RepaintAll();
        NotifyPreviewChanged();
    }
#endif

    public void ApplyFoliageGlobals(float hour)
    {
        AtmosphereSample sample = EvaluateAtmosphere(hour);
        Vector2 direction = FoliageWindDirection.sqrMagnitude > 0.0001f
            ? FoliageWindDirection.normalized
            : Vector2.right;

        Shader.SetGlobalFloat("_FWFoliageBands", Mathf.Clamp(FoliageLightBands, 2, 3));
        Shader.SetGlobalFloat("_FWBandSoftness", FoliageBandSoftness);
        Shader.SetGlobalColor("_FWLitTint", sample.FoliageLitTint);
        Shader.SetGlobalColor("_FWShadowTint", sample.FoliageShadowTint);
        Shader.SetGlobalColor("_FWBacklightColor", FoliageBacklightColor);
        Shader.SetGlobalFloat("_FWBacklightStrength", FoliageBacklightStrength);
        Shader.SetGlobalColor("_FWBottomTint", FoliageBottomTint);
        Shader.SetGlobalColor("_FWTopTint", FoliageTopTint);
        Shader.SetGlobalFloat("_FWGradientBaseHeight", FoliageGradientBaseHeight);
        Shader.SetGlobalFloat("_FWGradientHeight", Mathf.Max(0.1f, FoliageGradientHeight));
        Shader.SetGlobalFloat("_FWColorVariation", FoliageColorVariation);
        Shader.SetGlobalFloat("_FWWindStrength", FoliageWindStrength);
        Shader.SetGlobalFloat("_FWWindSpeed", FoliageWindSpeed);
        Shader.SetGlobalFloat("_FWGustStrength", FoliageGustStrength);
        Shader.SetGlobalFloat("_FWGustScale", FoliageGustScale);
        Shader.SetGlobalVector("_FWWindDirection", new Vector4(direction.x, 0f, direction.y, 0f));
        Shader.SetGlobalColor("_FWDistanceFogColor", sample.DistanceFogColor);
        Shader.SetGlobalFloat("_FWFogStart", NearPaletteDistance);
        Shader.SetGlobalFloat("_FWFogEnd", Mathf.Max(NearPaletteDistance + 1f, FarPaletteDistance));
    }

    public void ApplyPlayerToonMaterial()
    {
        if (PlayerToonMaterial == null) return;

        Properties.light.SetValue(PlayerToonMaterial, MK.Toon.Light.Banded);
        Properties.lightBands.SetValue(PlayerToonMaterial, LightBands);
        Properties.lightBandsScale.SetValue(PlayerToonMaterial, LightBandsScale);
        Properties.lightThreshold.SetValue(PlayerToonMaterial, LightThreshold);
        Properties.diffuseSmoothness.SetValue(PlayerToonMaterial, DiffuseSmoothness);
        Properties.diffuseThresholdOffset.SetValue(PlayerToonMaterial, DiffuseThreshold);
        Properties.smoothness.SetValue(PlayerToonMaterial, MaterialSmoothness);
        Properties.specularColor.SetValue(PlayerToonMaterial, new Color(0.08f, 0.07f, 0.06f, 1f));
        Properties.colorGrading.SetValue(PlayerToonMaterial, ColorGrading.FinalOutput);
        Properties.contrast.SetValue(PlayerToonMaterial, ToonContrast);
        Properties.saturation.SetValue(PlayerToonMaterial, ToonSaturation);
        Properties.brightness.SetValue(PlayerToonMaterial, ToonBrightness);
        Properties.UpdateSystemProperties(PlayerToonMaterial);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(PlayerToonMaterial);
#endif
    }

    public void ApplyEnvironmentToonMaterial(Material material, Color albedoColor, bool distant)
    {
        if (material == null) return;

        Properties.albedoColor.SetValue(material, albedoColor);
        Properties.light.SetValue(material, MK.Toon.Light.Banded);
        Properties.lightBands.SetValue(material, distant ? 2 : EnvironmentLightBands);
        Properties.lightBandsScale.SetValue(material, EnvironmentLightBandsScale);
        Properties.lightThreshold.SetValue(material, EnvironmentLightThreshold);
        Properties.diffuseSmoothness.SetValue(
            material,
            distant ? 0.02f : EnvironmentDiffuseSmoothness);
        Properties.diffuseThresholdOffset.SetValue(material, EnvironmentDiffuseThreshold);
        Properties.smoothness.SetValue(material, 0f);
        Properties.specularColor.SetValue(material, Color.black);
        Properties.colorGrading.SetValue(material, ColorGrading.FinalOutput);
        Properties.contrast.SetValue(material, distant ? 0.9f : 1.03f);
        Properties.saturation.SetValue(material, distant ? 0.72f : 0.95f);
        Properties.brightness.SetValue(material, distant ? 1.08f : 1f);
        Properties.UpdateSystemProperties(material);
        material.enableInstancing = true;
    }

    private AtmosphereSample EvaluateAtmosphere(float hour)
    {
        if (AtmosphereKeys == null || AtmosphereKeys.Length == 0)
        {
            return new AtmosphereSample
            {
                FogDensity = FogDensity,
                MaxDistance = MaxVolumetricDistance,
                LitFogColor = new Color(0.95f, 0.58f, 0.3f, 1f),
                ShadowFogColor = new Color(0.16f, 0.24f, 0.3f, 1f),
                TowardSunColor = new Color(1f, 0.5f, 0.2f, 1f),
                AwayFromSunColor = new Color(0.18f, 0.31f, 0.42f, 1f),
                FoliageLitTint = new Color(1f, 0.72f, 0.4f, 1f),
                FoliageShadowTint = new Color(0.24f, 0.34f, 0.42f, 1f),
                DistanceFogColor = new Color(0.5f, 0.48f, 0.66f, 1f)
            };
        }

        float wrappedHour = TimeSystem.WrapHour(hour);
        AtmosphereKey previous = null;
        AtmosphereKey next = null;
        float previousDistance = float.MaxValue;
        float nextDistance = float.MaxValue;

        for (int i = 0; i < AtmosphereKeys.Length; i++)
        {
            AtmosphereKey key = AtmosphereKeys[i];
            if (key == null) continue;

            float backward = Mathf.Repeat(wrappedHour - key.Hour, 24f);
            float forward = Mathf.Repeat(key.Hour - wrappedHour, 24f);
            if (backward < previousDistance)
            {
                previousDistance = backward;
                previous = key;
            }

            if (forward < nextDistance)
            {
                nextDistance = forward;
                next = key;
            }
        }

        if (previous == null && next == null)
            return default;
        if (previous == null) previous = next;
        if (next == null) next = previous;
        if (previous == next)
            return AtmosphereSample.From(previous);

        float duration = Mathf.Repeat(next.Hour - previous.Hour, 24f);
        float elapsed = Mathf.Repeat(wrappedHour - previous.Hour, 24f);
        float t = duration > 0.001f ? Mathf.Clamp01(elapsed / duration) : 0f;
        return AtmosphereSample.Lerp(previous, next, t);
    }

    private static AtmosphereKey[] CreateDefaultAtmosphereKeys()
    {
        return new[]
        {
            new AtmosphereKey
            {
                Label = "Dawn",
                Hour = 6f,
                FogDensity = 2.1f,
                MaxDistance = 430f,
                LitFogColor = new Color(0.92f, 0.61f, 0.42f, 1f),
                ShadowFogColor = new Color(0.22f, 0.32f, 0.42f, 1f),
                TowardSunColor = new Color(1f, 0.56f, 0.3f, 1f),
                AwayFromSunColor = new Color(0.28f, 0.4f, 0.52f, 1f),
                FoliageLitTint = new Color(1f, 0.76f, 0.46f, 1f),
                FoliageShadowTint = new Color(0.28f, 0.4f, 0.43f, 1f),
                DistanceFogColor = new Color(0.58f, 0.52f, 0.62f, 1f)
            },
            new AtmosphereKey
            {
                Label = "Afternoon",
                Hour = 14f,
                FogDensity = 1.45f,
                MaxDistance = 520f,
                LitFogColor = new Color(0.9f, 0.73f, 0.52f, 1f),
                ShadowFogColor = new Color(0.27f, 0.38f, 0.45f, 1f),
                TowardSunColor = new Color(1f, 0.75f, 0.45f, 1f),
                AwayFromSunColor = new Color(0.34f, 0.48f, 0.58f, 1f),
                FoliageLitTint = new Color(1f, 0.82f, 0.48f, 1f),
                FoliageShadowTint = new Color(0.3f, 0.4f, 0.4f, 1f),
                DistanceFogColor = new Color(0.56f, 0.58f, 0.67f, 1f)
            },
            new AtmosphereKey
            {
                Label = "Dusk",
                Hour = 18f,
                FogDensity = 2.25f,
                MaxDistance = 520f,
                LitFogColor = new Color(0.96f, 0.48f, 0.25f, 1f),
                ShadowFogColor = new Color(0.2f, 0.22f, 0.36f, 1f),
                TowardSunColor = new Color(1f, 0.38f, 0.12f, 1f),
                AwayFromSunColor = new Color(0.3f, 0.3f, 0.5f, 1f),
                FoliageLitTint = new Color(1f, 0.66f, 0.32f, 1f),
                FoliageShadowTint = new Color(0.22f, 0.3f, 0.4f, 1f),
                DistanceFogColor = new Color(0.52f, 0.43f, 0.61f, 1f)
            },
            new AtmosphereKey
            {
                Label = "Night",
                Hour = 22f,
                FogDensity = 1.75f,
                MaxDistance = 360f,
                LitFogColor = new Color(0.25f, 0.34f, 0.52f, 1f),
                ShadowFogColor = new Color(0.06f, 0.1f, 0.18f, 1f),
                TowardSunColor = new Color(0.34f, 0.38f, 0.58f, 1f),
                AwayFromSunColor = new Color(0.08f, 0.12f, 0.22f, 1f),
                FoliageLitTint = new Color(0.45f, 0.52f, 0.68f, 1f),
                FoliageShadowTint = new Color(0.1f, 0.16f, 0.24f, 1f),
                DistanceFogColor = new Color(0.2f, 0.25f, 0.38f, 1f)
            }
        };
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        if (profile.TryGet(out T component)) return component;
        component = profile.Add<T>(true);
#if UNITY_EDITOR
        if (UnityEditor.AssetDatabase.Contains(profile) && !UnityEditor.AssetDatabase.Contains(component))
        {
            component.name = typeof(T).Name;
            UnityEditor.AssetDatabase.AddObjectToAsset(component, profile);
        }
#endif
        return component;
    }

    private static void Set<T>(VolumeParameter<T> parameter, T value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private struct AtmosphereSample
    {
        public float FogDensity;
        public float MaxDistance;
        public Color LitFogColor;
        public Color ShadowFogColor;
        public Color TowardSunColor;
        public Color AwayFromSunColor;
        public Color FoliageLitTint;
        public Color FoliageShadowTint;
        public Color DistanceFogColor;

        public static AtmosphereSample From(AtmosphereKey key)
        {
            return new AtmosphereSample
            {
                FogDensity = key.FogDensity,
                MaxDistance = key.MaxDistance,
                LitFogColor = key.LitFogColor,
                ShadowFogColor = key.ShadowFogColor,
                TowardSunColor = key.TowardSunColor,
                AwayFromSunColor = key.AwayFromSunColor,
                FoliageLitTint = key.FoliageLitTint,
                FoliageShadowTint = key.FoliageShadowTint,
                DistanceFogColor = key.DistanceFogColor
            };
        }

        public static AtmosphereSample Lerp(AtmosphereKey from, AtmosphereKey to, float t)
        {
            return new AtmosphereSample
            {
                FogDensity = Mathf.Lerp(from.FogDensity, to.FogDensity, t),
                MaxDistance = Mathf.Lerp(from.MaxDistance, to.MaxDistance, t),
                LitFogColor = Color.Lerp(from.LitFogColor, to.LitFogColor, t),
                ShadowFogColor = Color.Lerp(from.ShadowFogColor, to.ShadowFogColor, t),
                TowardSunColor = Color.Lerp(from.TowardSunColor, to.TowardSunColor, t),
                AwayFromSunColor = Color.Lerp(from.AwayFromSunColor, to.AwayFromSunColor, t),
                FoliageLitTint = Color.Lerp(from.FoliageLitTint, to.FoliageLitTint, t),
                FoliageShadowTint = Color.Lerp(from.FoliageShadowTint, to.FoliageShadowTint, t),
                DistanceFogColor = Color.Lerp(from.DistanceFogColor, to.DistanceFogColor, t)
            };
        }
    }
}
