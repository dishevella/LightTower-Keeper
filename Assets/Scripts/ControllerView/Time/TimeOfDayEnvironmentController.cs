using System;
using UnityEngine;
using UnityEngine.Rendering;

public class TimeOfDayEnvironmentController : ControllerAbstract
{
    [Serializable]
    public class TimeOfDayProfile
    {
        public TimeOfDayPeriod period;
        [Range(0f, 24f)] public float anchorHour = 6f;
        public Material skyboxMaterial;
        public Color skyTint = Color.white;
        [Range(0f, 8f)] public float skyboxExposure = 1f;
        [Range(0f, 360f)] public float skyboxRotation = 0f;
        public Color directionalColor = Color.white;
        [Range(0f, 8f)] public float directionalIntensity = 1f;
        [Range(0f, 1f)] public float shadowStrength = 1f;
        public Vector3 directionalEulerAngles = new Vector3(50f, -30f, 0f);
        public Color fogColor = Color.gray;
        [Range(0f, 0.1f)] public float fogDensity = 0.01f;
        public Color ambientSkyColor = Color.gray;
        public Color ambientEquatorColor = Color.gray;
        public Color ambientGroundColor = Color.gray;
        [Range(0f, 2f)] public float ambientIntensity = 1f;
        [Range(0f, 2f)] public float reflectionIntensity = 1f;
    }

    [Serializable]
    public class TimeReactiveGroup
    {
        public string label;
        public TimeOfDayPeriod[] activePeriods;
        public GameObject[] objectsToEnable;
        public GameObject[] objectsToDisable;
    }

    [Header("Lighting")]
    [SerializeField] private Light mainDirectionalLight;
    [SerializeField] private bool forceTrilightAmbient = true;

    [Header("Profiles")]
    [SerializeField] private TimeOfDayProfile[] profiles;

    [Header("Reactive Groups")]
    [SerializeField] private TimeReactiveGroup[] reactiveGroups;

    private TimeOfDayModel timeModel;
    private TimeOfDayProfile[] sortedProfiles;
    private Material runtimeSkyboxMaterial;
    private Material currentSkyboxSource;

    private void Start()
    {
        timeModel = this.GetModel<TimeOfDayModel>();
        CacheProfiles();

        if (timeModel == null) return;

        timeModel.CurrentHour.OnValueChanged += HandleHourChanged;
        timeModel.CurrentPeriod.OnValueChanged += HandlePeriodChanged;

        ApplyForHour(timeModel.CurrentHour.Value);
        ApplyReactiveGroups(timeModel.CurrentPeriod.Value);
    }

    private void OnDestroy()
    {
        if (timeModel != null)
        {
            timeModel.CurrentHour.OnValueChanged -= HandleHourChanged;
            timeModel.CurrentPeriod.OnValueChanged -= HandlePeriodChanged;
        }

        if (runtimeSkyboxMaterial != null)
        {
            Destroy(runtimeSkyboxMaterial);
        }
    }

    private void HandleHourChanged(float hour)
    {
        ApplyForHour(hour);
    }

    private void HandlePeriodChanged(TimeOfDayPeriod period)
    {
        ApplyReactiveGroups(period);
    }

    private void ApplyForHour(float hour)
    {
        if (!TryGetBlendProfiles(hour, out TimeOfDayProfile from, out TimeOfDayProfile to, out float t))
        {
            return;
        }

        ApplySkybox(from, to, t);
        ApplyDirectionalLight(from, to, t);
        ApplyRenderSettings(from, to, t);
    }

    private void ApplySkybox(TimeOfDayProfile from, TimeOfDayProfile to, float t)
    {
        Material sourceMaterial = t < 0.5f
            ? (from.skyboxMaterial != null ? from.skyboxMaterial : to.skyboxMaterial)
            : (to.skyboxMaterial != null ? to.skyboxMaterial : from.skyboxMaterial);

        if (sourceMaterial == null) return;

        if (runtimeSkyboxMaterial == null || currentSkyboxSource != sourceMaterial)
        {
            if (runtimeSkyboxMaterial != null)
            {
                Destroy(runtimeSkyboxMaterial);
            }

            runtimeSkyboxMaterial = new Material(sourceMaterial);
            currentSkyboxSource = sourceMaterial;
            RenderSettings.skybox = runtimeSkyboxMaterial;
        }

        if (runtimeSkyboxMaterial.HasProperty("_Exposure"))
        {
            runtimeSkyboxMaterial.SetFloat("_Exposure", Mathf.Lerp(from.skyboxExposure, to.skyboxExposure, t));
        }

        if (runtimeSkyboxMaterial.HasProperty("_Rotation"))
        {
            runtimeSkyboxMaterial.SetFloat("_Rotation", Mathf.LerpAngle(from.skyboxRotation, to.skyboxRotation, t));
        }

        if (runtimeSkyboxMaterial.HasProperty("_Tint"))
        {
            runtimeSkyboxMaterial.SetColor("_Tint", Color.Lerp(from.skyTint, to.skyTint, t));
        }
    }

    private void ApplyDirectionalLight(TimeOfDayProfile from, TimeOfDayProfile to, float t)
    {
        if (mainDirectionalLight == null) return;

        mainDirectionalLight.color = Color.Lerp(from.directionalColor, to.directionalColor, t);
        mainDirectionalLight.intensity = Mathf.Lerp(from.directionalIntensity, to.directionalIntensity, t);
        mainDirectionalLight.shadowStrength = Mathf.Lerp(from.shadowStrength, to.shadowStrength, t);

        Vector3 euler = new Vector3(
            Mathf.LerpAngle(from.directionalEulerAngles.x, to.directionalEulerAngles.x, t),
            Mathf.LerpAngle(from.directionalEulerAngles.y, to.directionalEulerAngles.y, t),
            Mathf.LerpAngle(from.directionalEulerAngles.z, to.directionalEulerAngles.z, t));

        mainDirectionalLight.transform.rotation = Quaternion.Euler(euler);
    }

    private void ApplyRenderSettings(TimeOfDayProfile from, TimeOfDayProfile to, float t)
    {
        if (forceTrilightAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
        }

        RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
        RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
        RenderSettings.ambientSkyColor = Color.Lerp(from.ambientSkyColor, to.ambientSkyColor, t);
        RenderSettings.ambientEquatorColor = Color.Lerp(from.ambientEquatorColor, to.ambientEquatorColor, t);
        RenderSettings.ambientGroundColor = Color.Lerp(from.ambientGroundColor, to.ambientGroundColor, t);
        RenderSettings.ambientIntensity = Mathf.Lerp(from.ambientIntensity, to.ambientIntensity, t);
        RenderSettings.reflectionIntensity = Mathf.Lerp(from.reflectionIntensity, to.reflectionIntensity, t);
    }

    private void ApplyReactiveGroups(TimeOfDayPeriod period)
    {
        if (reactiveGroups == null) return;

        for (int i = 0; i < reactiveGroups.Length; i++)
        {
            TimeReactiveGroup group = reactiveGroups[i];
            if (group == null) continue;

            bool isActive = ContainsPeriod(group.activePeriods, period);
            SetObjectsActive(group.objectsToEnable, isActive);
            SetObjectsActive(group.objectsToDisable, !isActive);
        }
    }

    private bool TryGetBlendProfiles(float hour, out TimeOfDayProfile from, out TimeOfDayProfile to, out float t)
    {
        from = null;
        to = null;
        t = 0f;

        if (sortedProfiles == null || sortedProfiles.Length == 0)
        {
            return false;
        }

        if (sortedProfiles.Length == 1)
        {
            from = sortedProfiles[0];
            to = sortedProfiles[0];
            return true;
        }

        float wrappedHour = TimeSystem.WrapHour(hour);

        for (int i = 0; i < sortedProfiles.Length; i++)
        {
            TimeOfDayProfile current = sortedProfiles[i];
            TimeOfDayProfile next = sortedProfiles[(i + 1) % sortedProfiles.Length];

            float startHour = current.anchorHour;
            float endHour = next.anchorHour;
            float testHour = wrappedHour;

            if (i == sortedProfiles.Length - 1)
            {
                endHour += 24f;

                if (testHour < startHour)
                {
                    testHour += 24f;
                }
            }

            if (testHour >= startHour && testHour < endHour)
            {
                from = current;
                to = next;
                t = Mathf.InverseLerp(startHour, endHour, testHour);
                return true;
            }
        }

        from = sortedProfiles[0];
        to = sortedProfiles[0];
        return true;
    }

    private void CacheProfiles()
    {
        if (profiles == null || profiles.Length == 0)
        {
            sortedProfiles = Array.Empty<TimeOfDayProfile>();
            return;
        }

        int validCount = 0;
        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] != null)
            {
                validCount++;
            }
        }

        sortedProfiles = new TimeOfDayProfile[validCount];
        int index = 0;
        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] == null) continue;
            sortedProfiles[index++] = profiles[i];
        }

        Array.Sort(sortedProfiles, (a, b) => a.anchorHour.CompareTo(b.anchorHour));
    }

    private bool ContainsPeriod(TimeOfDayPeriod[] periods, TimeOfDayPeriod target)
    {
        if (periods == null || periods.Length == 0) return false;

        for (int i = 0; i < periods.Length; i++)
        {
            if (periods[i] == target)
            {
                return true;
            }
        }

        return false;
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }
}
