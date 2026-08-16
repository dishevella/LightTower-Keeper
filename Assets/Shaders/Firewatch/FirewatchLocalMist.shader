Shader "LightTower/Firewatch/Local Mist"
{
    Properties
    {
        _BaseMap("Mist Texture", 2D) = "white" {}
        [HDR] _ShadowColor("Shadow Fog", Color) = (0.16, 0.32, 0.34, 1)
        [HDR] _LitColor("Lit Fog", Color) = (0.82, 0.56, 0.42, 1)
        _SoftIntersectionDistance("Soft Intersection", Float) = 4
        _SunScatter("Sun Scatter", Range(0, 1)) = 0.5
        _Brightness("Mist Brightness", Range(0, 3)) = 1.35
        _OpacityMultiplier("Visible Mist Density", Range(0, 3)) = 1
        _NearFadeStart("Near Fade Start", Float) = 8
        _NearFadeEnd("Near Fade End", Float) = 18
        _FarFadeStart("Far Fade Start", Float) = 120
        _FarFadeEnd("Far Fade End", Float) = 190
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+20"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FirewatchLocalMist"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.occasoftware.buto/Shaders/Resources/Buto.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _ShadowColor;
                half4 _LitColor;
                float _SoftIntersectionDistance;
                float _SunScatter;
                float _Brightness;
                float _OpacityMultiplier;
                float _NearFadeStart;
                float _NearFadeEnd;
                float _FarFadeStart;
                float _FarFadeEnd;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float particleEyeDepth = -TransformWorldToView(input.positionWS).z;
                float softFade = saturate(
                    (sceneEyeDepth - particleEyeDepth) / max(_SoftIntersectionDistance, 0.001));

                float cameraDistance = distance(GetCameraPositionWS(), input.positionWS);
                float nearFade = smoothstep(_NearFadeStart, max(_NearFadeEnd, _NearFadeStart + 0.001), cameraDistance);
                float farFade = 1.0 - smoothstep(_FarFadeStart, max(_FarFadeEnd, _FarFadeStart + 0.001), cameraDistance);

                half4 mistSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                Light mainLight = GetMainLight();
                float3 toCamera = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                float sunFacing = pow(saturate(dot(toCamera, mainLight.direction)), 4.0) * _SunScatter;
                half3 palette = lerp(_ShadowColor.rgb, _LitColor.rgb, sunFacing) * _Brightness;
                half3 localMistColor = mistSample.rgb * input.color.rgb * palette;
                half3 atmosphereColor = ButoFogBlend(screenUV, cameraDistance, localMistColor);

                half alpha = saturate(
                    mistSample.a * input.color.a * _OpacityMultiplier * softFade * nearFade * farFade);
                return half4(atmosphereColor, alpha);
            }
            ENDHLSL
        }
    }
}
