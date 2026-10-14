Shader "Custom/LitSolidFade"
{
    Properties
    {
        _Color ("Tint Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Fade ("Fade Amount", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        LOD 200

        Cull Back
        ZWrite On

        CGPROGRAM
        #pragma surface surf Standard

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _Fade;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            // Instead of alpha blending, scale brightness
            o.Albedo = c.rgb * _Fade;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1; // always opaque
        }
        ENDCG
    }
    FallBack "Diffuse"
}
