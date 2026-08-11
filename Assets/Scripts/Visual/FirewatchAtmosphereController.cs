using OccaSoftware.Buto.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class FirewatchAtmosphereController : ControllerAbstract
{
    [SerializeField] private FirewatchVisualStyleConfig config;
    [SerializeField] private Volume targetVolume;

    private TimeOfDayModel timeModel;
    private VolumeProfile runtimeProfile;

    public FirewatchVisualStyleConfig Config => config;
    public Volume TargetVolume => targetVolume;
    public float CurrentSunIntensityLimit { get; private set; }
    public float CurrentReflectionIntensityLimit { get; private set; }
    public int ConfiguredCameraFogMaskCount { get; private set; }

    public void Configure(FirewatchVisualStyleConfig visualConfig, Volume volume)
    {
        config = visualConfig;
        targetVolume = volume;
    }

    private void Start()
    {
        if (config == null || targetVolume == null) return;

        ApplyConfiguredCameraFogMasks();

        if (config.TargetVolumeProfile != null)
            targetVolume.sharedProfile = config.TargetVolumeProfile;

        runtimeProfile = targetVolume.profile;
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

        ApplyGoldenHourHighlightLimits(atmosphereHour);
    }

    private void HandleConfigPreviewChanged()
    {
        ApplyConfiguredCameraFogMasks();

        float hour = HasCurrentHour
            ? timeModel.CurrentHour.Value
            : config.LookDevelopmentHour;
        ApplyHour(hour);
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
            RenderSettings.sun.intensity = CurrentSunIntensityLimit;

        RenderSettings.reflectionIntensity = CurrentReflectionIntensityLimit;
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

    private bool HasCurrentHour => timeModel != null && timeModel.CurrentHour != null;
}
