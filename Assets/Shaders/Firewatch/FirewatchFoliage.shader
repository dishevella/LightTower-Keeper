Shader "Light Tower/Firewatch/Foliage"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _LeafTex("Leaf Map", 2D) = "white" {}
        _TrunkTex("Trunk Map", 2D) = "white" {}
        _LeafColor("Leaf Color", Color) = (1, 1, 1, 1)
        _TrunkColor("Trunk Color", Color) = (1, 1, 1, 1)
        [Toggle] _UseSplitMaps("Use Leaf And Trunk Maps", Float) = 0
        _PaletteTint("Palette Tint", Color) = (1, 1, 1, 1)
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.45
        _DistanceFogInfluence("Distance Fog Influence", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "UniversalMaterialType" = "Lit"
        }
        LOD 300
        Cull Off
        ZWrite On

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_LeafTex);
        SAMPLER(sampler_LeafTex);
        TEXTURE2D(_TrunkTex);
        SAMPLER(sampler_TrunkTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _LeafTex_ST;
            float4 _TrunkTex_ST;
            half4 _BaseColor;
            half4 _LeafColor;
            half4 _TrunkColor;
            half4 _PaletteTint;
            half _UseSplitMaps;
            half _Cutoff;
            half _DistanceFogInfluence;
        CBUFFER_END

        half _FWFoliageBands;
        half _FWBandSoftness;
        half4 _FWLitTint;
        half4 _FWShadowTint;
        half4 _FWBacklightColor;
        half _FWBacklightStrength;
        half4 _FWBottomTint;
        half4 _FWTopTint;
        float _FWGradientBaseHeight;
        float _FWGradientHeight;
        half _FWColorVariation;
        half _FWWindStrength;
        half _FWWindSpeed;
        half _FWGustStrength;
        half _FWGustScale;
        float4 _FWWindDirection;
        half4 _FWDistanceFogColor;
        float _FWFogStart;
        float _FWFogEnd;

        struct FoliageAttributes
        {
            float4 positionOS : POSITION;
            half3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            half4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        float FirewatchHash(float3 value)
        {
            return frac(sin(dot(value, float3(12.9898, 78.233, 37.719))) * 43758.5453);
        }

        float3 ApplyFirewatchWind(float3 positionOS, half4 vertexColor)
        {
            float3 positionWS = TransformObjectToWorld(positionOS);
            float2 direction = _FWWindDirection.xz;
            direction *= rsqrt(max(dot(direction, direction), 0.0001));

            float phase = dot(positionWS.xz, direction) * max(_FWGustScale, 0.001)
                + _Time.y * max(_FWWindSpeed, 0.01);
            float broadWave = sin(phase) * 0.65 + sin(phase * 0.43 + 1.7) * 0.35;
            float gust = pow(saturate(sin(phase * 0.19 + 1.4) * 0.5 + 0.5), 2.0)
                * _FWGustStrength;
            float flutter = sin(_Time.y * _FWWindSpeed * 2.7
                + dot(positionWS.xz, float2(0.37, 0.61)) * 1.9) * vertexColor.g * 0.22;

            // PNB vegetation uses red for branch sway and green for leaf flutter.
            float windMask = saturate(vertexColor.r);
            float displacement = (broadWave * _FWWindStrength + gust + flutter * _FWWindStrength)
                * windMask;
            positionWS.xz += direction * displacement;
            positionWS.y += abs(displacement) * 0.04 * windMask;
            return TransformWorldToObject(positionWS);
        }

        half4 SampleFirewatchFoliage(float2 uv, half4 vertexColor)
        {
            half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap,
                uv * _BaseMap_ST.xy + _BaseMap_ST.zw) * _BaseColor;
            half4 leafSample = SAMPLE_TEXTURE2D(_LeafTex, sampler_LeafTex,
                uv * _LeafTex_ST.xy + _LeafTex_ST.zw) * _LeafColor;
            half4 trunkSample = SAMPLE_TEXTURE2D(_TrunkTex, sampler_TrunkTex,
                uv * _TrunkTex_ST.xy + _TrunkTex_ST.zw) * _TrunkColor;
            half leafMask = step(0.5h, vertexColor.b);
            half4 splitSample = lerp(trunkSample, leafSample, leafMask);
            return lerp(baseSample, splitSample, step(0.5h, _UseSplitMaps));
        }

        half QuantizeFirewatchLight(half lightAmount)
        {
            half stepCount = max(1.0h, round(_FWFoliageBands) - 1.0h);
            half scaled = saturate(lightAmount) * stepCount;
            half lowerBand = floor(scaled);
            half blend = smoothstep(0.5h - _FWBandSoftness, 0.5h + _FWBandSoftness,
                frac(scaled));
            return saturate((lowerBand + blend) / stepCount);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageForwardVertex
            #pragma fragment FoliageForwardFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            struct FoliageForwardVaryings
            {
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 color : COLOR;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            FoliageForwardVaryings FoliageForwardVertex(FoliageAttributes input)
            {
                FoliageForwardVaryings output = (FoliageForwardVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = ApplyFirewatchWind(input.positionOS.xyz, input.color);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                output.uv = input.uv;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.positionCS = positionInputs.positionCS;
                return output;
            }

            half4 FoliageForwardFragment(
                FoliageForwardVaryings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif

                half4 surface = SampleFirewatchFoliage(input.uv, input.color);
                clip(surface.a - _Cutoff);

                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                half3 normalWS = SafeNormalize(input.normalWS) * faceSign;
                half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half directLight = saturate(dot(normalWS, mainLight.direction));
                directLight *= mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half bandedLight = QuantizeFirewatchLight(directLight);

                half heightBlend = saturate((input.positionWS.y - _FWGradientBaseHeight)
                    / max(_FWGradientHeight, 0.01));
                half3 heightTint = lerp(_FWBottomTint.rgb, _FWTopTint.rgb, heightBlend);
                float3 objectPosition = GetObjectToWorldMatrix()._m03_m13_m23;
                half variation = lerp(1.0h - _FWColorVariation, 1.0h + _FWColorVariation,
                    FirewatchHash(objectPosition));
                half3 albedo = surface.rgb * _PaletteTint.rgb * heightTint * variation;

                half3 boundedMainLight = min(mainLight.color, half3(1.25h, 1.25h, 1.25h));
                half3 litTone = _FWLitTint.rgb * boundedMainLight;
                half3 tone = lerp(_FWShadowTint.rgb, litTone, bandedLight);
                half3 ambient = SampleSH(normalWS) * 0.22h;
                half3 color = albedo * (tone + ambient);

                half backFacingLight = saturate(-dot(normalWS, mainLight.direction) * 0.5h + 0.5h);
                half viewBacklight = pow(saturate(dot(-mainLight.direction, viewDirectionWS)), 3.0h);
                half backlight = backFacingLight * viewBacklight * mainLight.shadowAttenuation
                    * _FWBacklightStrength;
                color += albedo * _FWBacklightColor.rgb * backlight;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(additionalLightCount)
                        Light fillLight = GetAdditionalLight(lightIndex, input.positionWS);
                        half fill = saturate(dot(normalWS, fillLight.direction));
                        fill *= fillLight.distanceAttenuation * fillLight.shadowAttenuation;
                        color += albedo * fillLight.color * fill * 0.35h;
                    LIGHT_LOOP_END
                #endif

                float cameraDistance = distance(_WorldSpaceCameraPos, input.positionWS);
                half distanceFog = smoothstep(_FWFogStart, max(_FWFogStart + 0.01, _FWFogEnd),
                    cameraDistance) * _DistanceFogInfluence;
                color = lerp(color, _FWDistanceFogColor.rgb, saturate(distanceFog));
                return half4(color, surface.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageShadowVertex
            #pragma fragment FoliageShadowFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            float3 _LightDirection;
            float3 _LightPosition;

            struct FoliageShadowVaryings
            {
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            FoliageShadowVaryings FoliageShadowVertex(FoliageAttributes input)
            {
                FoliageShadowVaryings output = (FoliageShadowVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float3 positionOS = ApplyFirewatchWind(input.positionOS.xyz, input.color);
                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                output.uv = input.uv;
                output.color = input.color;
                output.positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                output.positionCS = ApplyShadowClamping(output.positionCS);
                return output;
            }

            half4 FoliageShadowFragment(FoliageShadowVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif
                half alpha = SampleFirewatchFoliage(input.uv, input.color).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageDepthVertex
            #pragma fragment FoliageDepthFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            struct FoliageDepthVaryings
            {
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            FoliageDepthVaryings FoliageDepthVertex(FoliageAttributes input)
            {
                FoliageDepthVaryings output = (FoliageDepthVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionOS = ApplyFirewatchWind(input.positionOS.xyz, input.color);
                output.uv = input.uv;
                output.color = input.color;
                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 FoliageDepthFragment(FoliageDepthVaryings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif
                half alpha = SampleFirewatchFoliage(input.uv, input.color).a;
                clip(alpha - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex FoliageDepthNormalsVertex
            #pragma fragment FoliageDepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct FoliageDepthNormalsVaryings
            {
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 color : COLOR;
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            FoliageDepthNormalsVaryings FoliageDepthNormalsVertex(FoliageAttributes input)
            {
                FoliageDepthNormalsVaryings output = (FoliageDepthNormalsVaryings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float3 positionOS = ApplyFirewatchWind(input.positionOS.xyz, input.color);
                output.uv = input.uv;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 FoliageDepthNormalsFragment(
                FoliageDepthNormalsVaryings input,
                FRONT_FACE_TYPE frontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                #if defined(LOD_FADE_CROSSFADE)
                    LODFadeCrossFade(input.positionCS);
                #endif
                half alpha = SampleFirewatchFoliage(input.uv, input.color).a;
                clip(alpha - _Cutoff);

                half faceSign = IS_FRONT_VFACE(frontFace, 1.0h, -1.0h);
                half3 normalWS = NormalizeNormalPerPixel(input.normalWS) * faceSign;
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                    half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                    return half4(packedNormalWS, 0.0h);
                #else
                    return half4(normalWS, 0.0h);
                #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
