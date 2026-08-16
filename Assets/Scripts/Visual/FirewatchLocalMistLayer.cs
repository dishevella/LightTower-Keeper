using UnityEngine;

[DisallowMultipleComponent]
public sealed class FirewatchLocalMistLayer : MonoBehaviour
{
    private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
    private static readonly int LitColorId = Shader.PropertyToID("_LitColor");
    private static readonly int SoftIntersectionId = Shader.PropertyToID("_SoftIntersectionDistance");
    private static readonly int SunScatterId = Shader.PropertyToID("_SunScatter");
    private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
    private static readonly int OpacityMultiplierId = Shader.PropertyToID("_OpacityMultiplier");
    private static readonly int NearFadeStartId = Shader.PropertyToID("_NearFadeStart");
    private static readonly int NearFadeEndId = Shader.PropertyToID("_NearFadeEnd");
    private static readonly int FarFadeStartId = Shader.PropertyToID("_FarFadeStart");
    private static readonly int FarFadeEndId = Shader.PropertyToID("_FarFadeEnd");

    [SerializeField] private string bankLabel;
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private ParticleSystemRenderer particleRenderer;

    private MaterialPropertyBlock propertyBlock;

    public string BankLabel => bankLabel;
    public ParticleSystem Particles => particles;
    public ParticleSystemRenderer ParticleRenderer => particleRenderer;

    public void Configure(
        FirewatchVisualStyleConfig.LocalMistBankSettings bank,
        FirewatchVisualStyleConfig config,
        Material material)
    {
        bankLabel = bank.Label;
        if (particles == null) particles = GetComponent<ParticleSystem>();
        if (particles == null) particles = gameObject.AddComponent<ParticleSystem>();
        if (particleRenderer == null) particleRenderer = GetComponent<ParticleSystemRenderer>();

        transform.position = bank.Center + bank.VisibleLayerOffset;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        main.maxParticles = Mathf.Max(1, bank.VisibleParticleCount);
        main.duration = Mathf.Max(5f, bank.VisibleParticleLifetime);
        main.startLifetime = new ParticleSystem.MinMaxCurve(
            bank.VisibleParticleLifetime * 0.8f,
            bank.VisibleParticleLifetime * 1.2f);
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(
            bank.VisibleParticleSize.x,
            bank.VisibleParticleSize.y);
        main.startSizeY = new ParticleSystem.MinMaxCurve(
            bank.VisibleParticleSize.x * bank.VisibleParticleHeightRatio,
            bank.VisibleParticleSize.y * bank.VisibleParticleHeightRatio);
        main.startSizeZ = 1f;
        // These particles are deliberately wide and shallow. Large roll angles turn
        // them into vertical streaks instead of a low-lying mist bank.
        main.startRotation = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        main.startColor = new Color(1f, 1f, 1f, bank.VisibleParticleOpacity);

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = bank.VisibleParticleCount /
                                Mathf.Max(1f, bank.VisibleParticleLifetime);

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = bank.VisibleLayerSize;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = bank.VisibleDriftVelocity.x;
        velocity.y = bank.VisibleDriftVelocity.y;
        velocity.z = bank.VisibleDriftVelocity.z;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.separateAxes = false;
        noise.strength = bank.VisibleTurbulence;
        noise.frequency = bank.VisibleTurbulenceFrequency;
        noise.scrollSpeed = bank.VisibleTurbulenceScrollSpeed;
        noise.damping = true;
        noise.octaveCount = 2;
        noise.octaveMultiplier = 0.5f;
        noise.octaveScale = 2f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient alphaEnvelope = new Gradient();
        alphaEnvelope.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.18f),
                new GradientAlphaKey(1f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = alphaEnvelope;

        ParticleSystem.TextureSheetAnimationModule textureSheet = particles.textureSheetAnimation;
        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.numTilesX = 2;
        textureSheet.numTilesY = 2;
        textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheet.frameOverTime = 0f;
        textureSheet.startFrame = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        textureSheet.cycleCount = 1;

        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.alignment = ParticleSystemRenderSpace.View;
        particleRenderer.allowRoll = false;
        particleRenderer.enableGPUInstancing = true;
        particleRenderer.sharedMaterial = material;

        ApplyPalette(config, config.LookDevelopmentHour);
    }

    public void ApplyPalette(FirewatchVisualStyleConfig config, float hour)
    {
        if (config == null || particleRenderer == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        particleRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ShadowColorId, config.EvaluateShadowFogColor(hour));
        propertyBlock.SetColor(LitColorId, config.EvaluateLitFogColor(hour));
        propertyBlock.SetFloat(SoftIntersectionId, config.LocalMistSoftIntersectionDistance);
        propertyBlock.SetFloat(SunScatterId, config.LocalMistSunScatter);
        propertyBlock.SetFloat(BrightnessId, config.LocalMistBrightness);
        propertyBlock.SetFloat(OpacityMultiplierId, config.VisibleMistDensity);
        propertyBlock.SetFloat(NearFadeStartId, config.LocalMistNearFade.x);
        propertyBlock.SetFloat(NearFadeEndId, config.LocalMistNearFade.y);
        propertyBlock.SetFloat(FarFadeStartId, config.LocalMistFarFade.x);
        propertyBlock.SetFloat(FarFadeEndId, config.LocalMistFarFade.y);
        particleRenderer.SetPropertyBlock(propertyBlock);
    }
}
