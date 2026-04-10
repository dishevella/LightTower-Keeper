Shader "LightTower/Characters/ShadowFigure"
{
    Properties
    {
        [MainColor] _BodyColor("Body Color", Color) = (0.043, 0.063, 0.086, 1)
        _RimColor("Rim Color", Color) = (0.20, 0.30, 0.38, 1)
        _RimStrength("Rim Strength", Range(0, 1)) = 0.18
        _RimPower("Rim Power", Range(0.5, 8)) = 3.5
        _EmissionStrength("Emission Strength", Range(0, 1)) = 0.08
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _NoiseScale("Dissolve Noise Scale", Range(0.5, 32)) = 8
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BodyColor;
                half4 _RimColor;
                half _RimStrength;
                half _RimPower;
                half _EmissionStrength;
                half _Dissolve;
                half _NoiseScale;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float noise = Hash31(floor(input.positionWS * _NoiseScale));
                clip(noise - _Dissolve);

                half3 normalWS = SafeNormalize(input.normalWS);
                half3 viewDirWS = SafeNormalize(input.viewDirWS);

                half rim = 1.0h - saturate(dot(normalWS, viewDirWS));
                rim = pow(rim, max(_RimPower, 0.0001h));
                rim = saturate(rim * _RimStrength);

                half3 color = lerp(_BodyColor.rgb, _RimColor.rgb, rim);
                color += _RimColor.rgb * rim * _EmissionStrength;
                color = MixFog(color, input.fogFactor);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
