Shader "Custom/StyleofMe_AnimePixelOutline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Outline
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.02)) = 0.005

        // Rim
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(1.0, 10.0)) = 4.0
        _RimIntensity ("Rim Intensity", Range(0.0, 5.0)) = 1.0

        // Shadows
        _ShadowThreshold ("Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _ShadowSoftness ("Shadow Softness", Range(0.0, 0.5)) = 0.2
        _ShadowColor ("Shadow Color", Color) = (0.5, 0.5, 0.5, 1)

        // Pixelation
        _PixelSize ("Pixel Size", Float) = 8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off

        CGPROGRAM
        #pragma surface surf CustomLighting fullforwardshadows
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _RimColor;
        float _RimPower;
        float _RimIntensity;
        float _ShadowThreshold;
        float _ShadowSoftness;
        fixed4 _ShadowColor;
        float _PixelSize;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Pixelation step
            float2 pixelUV = floor(IN.uv_MainTex * _PixelSize) / _PixelSize;

            fixed4 tex = tex2D(_MainTex, pixelUV) * _Color;
            o.Albedo = tex.rgb;
            o.Alpha = tex.a;
        }

        // Custom anime-style lighting
        inline half4 LightingCustomLighting(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);

            // Soft shadow transition
            half shadowStep = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, NdotL);

            // Blend shadow color with lit color
            half3 litColor = s.Albedo * _LightColor0.rgb;
            half3 shadowTint = lerp(_ShadowColor.rgb, litColor, shadowStep);
            half3 diffuse = shadowTint * atten;

            // Rim light
            half rim = 1.0 - saturate(dot(normalize(viewDir), s.Normal));
            half rimSharp = pow(rim, _RimPower);
            half3 rimLight = _RimColor.rgb * rimSharp * _RimIntensity;

            return half4(diffuse + rimLight, s.Alpha);
        }
        ENDCG

        // Outline pass
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * _OutlineWidth;
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
