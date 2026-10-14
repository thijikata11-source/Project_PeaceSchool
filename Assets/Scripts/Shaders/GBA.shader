Shader "Custom/GBA_CelShading_Advanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0,2)) = 1.0
        _HeightMap ("Height Map", 2D) = "black" {}
        _HeightStrength ("Height Strength", Range(0,0.1)) = 0.05
        _Color ("Color Tint", Color) = (1,1,1,1)
        _LightThreshold ("Light Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows

        sampler2D _MainTex;
        sampler2D _NormalMap;
        sampler2D _HeightMap;
        float _NormalStrength;
        float _HeightStrength;
        fixed4 _Color;
        float _LightThreshold;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
            float2 uv_HeightMap;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Sample and scale normal map
            float3 normalTex = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            o.Normal = normalize(lerp(o.Normal, normalTex, _NormalStrength));

            // Sample height map and perturb normal
            float height = tex2D(_HeightMap, IN.uv_HeightMap).r;
            float3 heightOffset = float3(0, 0, height * _HeightStrength);
            o.Normal = normalize(o.Normal + heightOffset);

            // Cel shading
            float lightIntensity = dot(o.Normal, normalize(_WorldSpaceLightPos0));
            float shade = lightIntensity > _LightThreshold ? 1.0 : 0.5;

            o.Albedo = tex.rgb * shade;
            o.Alpha = tex.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
