Shader "Custom/LitCutOffShadow3"
{
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _LightStrength ("Light Strength", Range(0, 1)) = 0.3
    }

    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

CGPROGRAM
#pragma surface surf CustomLambert addshadow

sampler2D _MainTex;
float _LightStrength;

#pragma lighting CustomLambert exclude_path:prepass
inline half4 LightingCustomLambert (SurfaceOutput s, half3 lightDir, half atten)
{
    #ifndef USING_DIRECTIONAL_LIGHT
    lightDir = normalize(lightDir);
    #endif

    half NdotL = max(0, dot(s.Normal, lightDir));
    half3 litColor = lerp(s.Albedo, s.Albedo * NdotL, _LightStrength);

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
