Shader "Custom/LitCutOffSoftShadow4"
{
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _LightStrength ("Light Strength", Range(0, 1)) = 0.2
        _ShadowSoftness ("Shadow Softness", Range(0.1, 2)) = 1.0
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

CGPROGRAM
#pragma surface surf CustomSoftLambert addshadow

sampler2D _MainTex;
float _LightStrength;
float _ShadowSoftness;

#pragma lighting CustomSoftLambert exclude_path:prepass
inline half4 LightingCustomSoftLambert (SurfaceOutput s, half3 lightDir, half atten)
{
    #ifndef USING_DIRECTIONAL_LIGHT
    lightDir = normalize(lightDir);
    #endif

    // Softened dot product for smoother shading
    half NdotL = saturate(dot(s.Normal, lightDir));
    NdotL = pow(NdotL, _ShadowSoftness); // soften shadow edge

    // Darker lighting with adjustable strength
    half3 litColor = lerp(s.Albedo * 0.3, s.Albedo * NdotL, _LightStrength);

    half4 c;
    c.rgb = litColor * _LightColor0.rgb * (atten * 2);
    c.a = s.Alpha;
    clip(c.a - 0.5f);
    return c;
}

struct Input {
    float2 uv_MainTex : TEXCOORD0;
};

void surf (Input IN, inout SurfaceOutput o) {
    half4 c = tex2D(_MainTex, IN.uv_MainTex);
    o.Albedo = c.rgb;
    o.Alpha = c.a;
}
ENDCG

    }

    Fallback "Diffuse"
}
