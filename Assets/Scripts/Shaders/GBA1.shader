Shader "Custom/GBA1_CelShading"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _LightThreshold ("Light Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        // Backface culling control:
        // Cull Back   = default (hide back faces)
        // Cull Front  = hide front faces (object invisible when facing camera)
        // Cull Off    = render both sides
        Cull Back

        CGPROGRAM
        #pragma surface surf Lambert

        sampler2D _MainTex;
        fixed4 _Color;
        float _LightThreshold;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_MainTex, IN.uv_MainTex) * _Color;

            // Cel shading by thresholding light
            float lightIntensity = dot(o.Normal, normalize(IN.viewDir));
            float shade = lightIntensity > _LightThreshold ? 1.0 : 0.5;

            o.Albedo = tex.rgb * shade;
            o.Alpha = tex.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
