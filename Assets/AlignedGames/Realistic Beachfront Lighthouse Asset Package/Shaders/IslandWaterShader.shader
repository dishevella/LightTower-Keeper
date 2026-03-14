Shader "AlignedGames/Custom/IslandWaterShader"
{
    Properties
    {
        _WaterColor("Water Color", Color) = (0.0, 0.5, 1.0, 1.0)
        _FoamColor("Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _ShoreFoamColor("Shore Foam Color", Color) = (1.0, 1.0, 1.0, 1.0)

        _WaveCenter("Wave Center (World)", Vector) = (0, 0, 0, 0)
        _WaveAmplitude("Wave Amplitude", Float) = 0.3
        _WaveFrequency("Wave Frequency", Float) = 6.0
        _WaveSpeed("Wave Speed", Float) = 2.0

        _ShoreWaveAmplitude("Shore Wave Amplitude", Float) = 0.5
        _ShoreWaveFrequency("Shore Wave Frequency", Float) = 12.0
        _ShoreWaveDistance("Shore Influence Distance", Float) = 5.0

        _FoamDistance("Foam Radius", Float) = 20.0
        _FoamIntensity("Foam Intensity", Float) = 1.5
        _ShoreFoamIntensity("Shore Foam Intensity", Float) = 3.0
        _ShoreFoamDistance("Shore Foam Distance", Float) = 5.0

        _FoamTex("Foam Texture", 2D) = "white" {}
        _FoamScrollSpeed("Foam Scroll Speed", Vector) = (0.05, 0.05, 0, 0)
        _FoamFadeSpeed("Foam Fade Speed", Float) = 1.0
        _NoiseScale("Foam Texture Scale", Float) = 1.0

        _WaveNormal("Wave Normal Map", 2D) = "bump" {}
        _NormalScrollSpeed("Normal Scroll Speed", Vector) = (0.05, 0.03, 0, 0)

        _Glossiness("Smoothness", Range(0,1)) = 0.8
        _Metallic("Metallic", Range(0,1)) = 0.0
    }

        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 300

            CGINCLUDE
            #include "UnityCG.cginc"

            float4 _WaveCenter;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;

            float _ShoreWaveAmplitude;
            float _ShoreWaveFrequency;
            float _ShoreWaveDistance;

            float _FoamDistance;
            float _FoamIntensity;
            float _ShoreFoamIntensity;
            float _ShoreFoamDistance;

            float _NoiseScale;
            float _FoamFadeSpeed;
            float4 _FoamScrollSpeed;

            sampler2D _FoamTex;
            sampler2D _WaveNormal;
            float4 _NormalScrollSpeed;

            float4 _WaterColor;
            float4 _FoamColor;
            float4 _ShoreFoamColor;

            float _Glossiness;
            float _Metallic;
            ENDCG

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows vertex:vert

            struct Input
            {
                float3 worldPos;
                float2 uv_WaveNormal;
                float2 uv_FoamTex;
            };

            void vert(inout appdata_full v)
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 toCenter = worldPos.xz - _WaveCenter.xz;
                float dist = length(toCenter);
                float t = _Time.y;

                float radialWave = sin(dist * _WaveFrequency - t * _WaveSpeed)
                                   * _WaveAmplitude / (1.0 + dist);

                float shoreFade = saturate(1.0 - dist / _ShoreWaveDistance);
                float shoreWave = sin(dist * _ShoreWaveFrequency - t * _WaveSpeed * 1.5)
                                  * _ShoreWaveAmplitude * shoreFade;

                float foamNoise = tex2Dlod(_FoamTex, float4(worldPos.xz * _NoiseScale, 0, 0)).r * 0.1;

                v.vertex.y += radialWave + shoreWave + foamNoise;
            }

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                float3 worldPos = IN.worldPos;
                float2 toCenter = worldPos.xz - _WaveCenter.xz;
                float dist = length(toCenter);

                float2 foamUV = IN.uv_FoamTex * _NoiseScale + _Time.y * _FoamScrollSpeed.xy;
                float foamTex = tex2D(_FoamTex, foamUV).r;

                float foamFade = 0.5 + 0.5 * sin(_Time.y * _FoamFadeSpeed);
                foamTex *= foamFade;

                float baseFoam = saturate(1.0 - dist / _FoamDistance) * foamTex * _FoamIntensity;
                float shoreFoam = saturate(1.0 - dist / _ShoreFoamDistance) * foamTex * _ShoreFoamIntensity;

                float3 foamLayer = lerp(_WaterColor.rgb, _FoamColor.rgb, baseFoam);
                float3 shoreLayer = lerp(foamLayer, _ShoreFoamColor.rgb, shoreFoam);

                o.Albedo = shoreLayer;

                float2 scrollUV = IN.uv_WaveNormal + _Time.y * _NormalScrollSpeed.xy;
                o.Normal = UnpackNormal(tex2D(_WaveNormal, scrollUV));

                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
                o.Alpha = 1.0;
            }
            ENDCG
        }

            FallBack "Diffuse"
}
