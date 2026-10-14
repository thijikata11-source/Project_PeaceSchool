Shader "Custom/BuiltIn/TransparentDoubleSidedOutlineFade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness (px)", Range(0,10)) = 2
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0.25

        _AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.5
        _Premultiply ("Premultiply Alpha", Float) = 1
        _MirrorBackface ("Mirror Backface UV", Float) = 1

        _Fade ("Global Fade", Range(0,1)) = 1   // NEW fade property
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Color;
            float4 _OutlineColor;
            float _OutlineThickness;
            float _OutlineSoftness;
            float _AlphaCutoff;
            float _Premultiply;
            float _MirrorBackface;
            float _Fade;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float SampleAlpha(float2 uv)
            {
                return tex2D(_MainTex, uv).a;
            }

            float OutlineAlpha(float2 uv, float thicknessPx)
            {
                float2 texel = float2(_MainTex_TexelSize.x, _MainTex_TexelSize.y);
                float2 d1 = texel * thicknessPx;
                float2 d2 = texel * (thicknessPx * 0.7071);

                float a = SampleAlpha(uv);
                a = max(a, SampleAlpha(uv + float2( d1.x,  0)));
                a = max(a, SampleAlpha(uv + float2(-d1.x,  0)));
                a = max(a, SampleAlpha(uv + float2( 0,    d1.y)));
                a = max(a, SampleAlpha(uv + float2( 0,   -d1.y)));
                a = max(a, SampleAlpha(uv + float2( d2.x,  d2.y)));
                a = max(a, SampleAlpha(uv + float2(-d2.x,  d2.y)));
                a = max(a, SampleAlpha(uv + float2( d2.x, -d2.y)));
                a = max(a, SampleAlpha(uv + float2(-d2.x, -d2.y)));
                return a;
            }

            float ComputeOutlineMask(float2 uv, float thicknessPx, float softness)
            {
                float aDilated = OutlineAlpha(uv, thicknessPx);
                float aCore = SampleAlpha(uv);

                float edge = saturate(aDilated - aCore);
                edge = smoothstep(_AlphaCutoff, 1.0, edge);

                float aDilatedSoft = OutlineAlpha(uv, thicknessPx + max(1.0, thicknessPx) * softness);
                float rim = saturate(aDilatedSoft - aCore);
                rim = smoothstep(_AlphaCutoff, 1.0, rim);

                return max(edge, rim * 0.7);
            }

            fixed4 frag (v2f i, float faceSign : VFACE) : SV_Target
            {
                float2 uv = i.uv;
                if (faceSign < 0 && _MirrorBackface > 0.5)
                {
                    uv.x = 1.0 - uv.x;
                }

                fixed4 tex = tex2D(_MainTex, uv);
                float baseAlpha = tex.a;

                float outlineMask = ComputeOutlineMask(uv, _OutlineThickness, _OutlineSoftness);
                float outlineAlpha = outlineMask * _OutlineColor.a;

                fixed4 fillCol = tex * _Color;
                float fillAlpha = baseAlpha * _Color.a;

                if (_Premultiply > 0.5)
                {
                    fillCol.rgb *= fillAlpha;
                    _OutlineColor.rgb *= outlineAlpha;
                }

                fixed4 outCol;
                outCol.rgb = _OutlineColor.rgb;
                outCol.a = outlineAlpha;

                outCol.rgb = lerp(outCol.rgb, fillCol.rgb, fillAlpha);
                outCol.a = max(outCol.a, fillAlpha);

                // Apply fade factor globally
                outCol.a *= _Fade;

                outCol.a = smoothstep(0.0, 1.0, outCol.a);
                return outCol;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Cutout/VertexLit"
}
