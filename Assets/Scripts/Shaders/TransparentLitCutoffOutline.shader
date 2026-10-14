Shader "Custom/TransparentLitCutoffAlphaOutline"
{
    Properties
    {
        _MainTex ("Base (RGB) Alpha (A)", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.0, 5.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="TransparentCutout" }
        LOD 200

        // Main Pass (Lit Transparent Cutoff)
        CGPROGRAM
        #pragma surface surf Standard alpha:fade clip:_Cutoff

        sampler2D _MainTex;
        fixed4 _Color;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG

        // Outline Pass (texture-based edge detection)
        Pass
        {
            Name "AlphaOutline"
            Tags { "LightMode" = "Always" }
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Cutoff;
            float4 _OutlineColor;
            float _OutlineThickness;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float alpha = tex2D(_MainTex, i.uv).a;

                // Sample neighbors to detect edges
                float2 offset = _OutlineThickness * _MainTex_TexelSize.xy;
                float alphaLeft = tex2D(_MainTex, i.uv + float2(-offset.x, 0)).a;
                float alphaRight = tex2D(_MainTex, i.uv + float2(offset.x, 0)).a;
                float alphaUp = tex2D(_MainTex, i.uv + float2(0, offset.y)).a;
                float alphaDown = tex2D(_MainTex, i.uv + float2(0, -offset.y)).a;

                // Edge detection: outline where alpha crosses cutoff
                bool isInside = alpha > _Cutoff;
                bool neighborOutside = (alphaLeft <= _Cutoff || alphaRight <= _Cutoff || alphaUp <= _Cutoff || alphaDown <= _Cutoff);

                if (isInside && neighborOutside)
                    return _OutlineColor;
                else
                    return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
    FallBack "Transparent/Cutout/Diffuse"
}
