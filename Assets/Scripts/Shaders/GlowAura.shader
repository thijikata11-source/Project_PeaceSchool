Shader "Custom/WeaponGlow"
{
    Properties
    {
        _GlowColor ("Glow Color", Color) = (0.3, 0.8, 1, 1)
        _GlowStrength ("Glow Strength", Range(0,10)) = 4
        _MainTex ("Base Texture", 2D) = "white" {}
        _NoiseTex ("Glow Pattern (Noise)", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Range(0, 5)) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        CGPROGRAM
        #pragma surface surf Lambert alpha

        sampler2D _MainTex;
        sampler2D _NoiseTex;
        float4 _GlowColor;
        float _GlowStrength;
        float _ScrollSpeed;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NoiseTex;
        };

        void surf (Input IN, inout SurfaceOutput o)
        {
            fixed4 baseCol = tex2D(_MainTex, IN.uv_MainTex);

            // Scrolling noise glow overlay
            float2 offsetUV = IN.uv_NoiseTex + float2(_Time.y * _ScrollSpeed, _Time.y * _ScrollSpeed * 0.5);
            fixed glowMask = tex2D(_NoiseTex, offsetUV).r;

            // Combine base and glow
            float glow = saturate(glowMask * _GlowStrength);
            o.Albedo = baseCol.rgb + glow * _GlowColor.rgb;
            o.Alpha = baseCol.a + glow * _GlowColor.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
