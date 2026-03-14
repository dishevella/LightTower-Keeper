Shader "Custom/AlignedGames/StandardWithWindCutout"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB) Alpha (A)", 2D) = "white" {}

        _Metallic("Metallic", Range(0,1)) = 0.0
        _Glossiness("Smoothness", Range(0,1)) = 0.5

        _BumpMap("Normal Map", 2D) = "bump" {}
        _NormalIntensity("Normal Intensity", Range(0,2)) = 1.0

        _OcclusionMap("Occlusion Map", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0

        _EmissionMap("Emission Map", 2D) = "black" {}
        _EmissionColor("Emission Color", Color) = (0,0,0,0)

        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _WindStrength("Wind Strength", Float) = 0.5
        _WindSpeed("Wind Speed", Float) = 1.0
        _WindScale("Wind Scale", Float) = 1.0
    }

        SubShader
        {
            Tags { "RenderType" = "TransparentCutout" "Queue" = "AlphaTest" }
            LOD 300
            ZWrite On
            Cull Back

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows alpha:clip vertex:vert addshadow
            #pragma target 3.0

            sampler2D _MainTex;
            sampler2D _BumpMap;
            sampler2D _OcclusionMap;
            sampler2D _EmissionMap;

            fixed4 _Color;
            fixed4 _EmissionColor;
            half _Glossiness;
            half _Metallic;
            float _Cutoff;

            float _NormalIntensity;
            float _OcclusionStrength;

            float _WindStrength;
            float _WindSpeed;
            float _WindScale;

            struct Input
            {
                float2 uv_MainTex;
                float2 uv_BumpMap;
                float2 uv_OcclusionMap;
                float2 uv_EmissionMap;
            };

            void vert(inout appdata_full v)
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                float sway = sin(_Time.y * _WindSpeed + worldPos.x * _WindScale) *
                             cos(_Time.y * _WindSpeed + worldPos.z * _WindScale);

                float heightFactor = saturate(v.vertex.y);
                float3 offset = float3(sway, 0, sway) * _WindStrength * heightFactor;

                v.vertex.xyz += offset;
            }

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                fixed4 col = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                clip(col.a - _Cutoff);

                o.Albedo = col.rgb;
                o.Alpha = col.a;
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;

                fixed3 unpackedNormal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
                o.Normal = lerp(float3(0, 0, 1), unpackedNormal, _NormalIntensity);

                float rawAO = tex2D(_OcclusionMap, IN.uv_OcclusionMap).r;
                o.Occlusion = lerp(1.0, rawAO, _OcclusionStrength);

                o.Emission = tex2D(_EmissionMap, IN.uv_EmissionMap).rgb * _EmissionColor.rgb;
            }
            ENDCG
        }

            FallBack "Transparent/Cutout/VertexLit"
}
