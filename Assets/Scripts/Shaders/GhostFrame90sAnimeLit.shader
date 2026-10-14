Shader "Custom/GhostFrame90sAnimeLit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _RimColor ("Rim Light Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(1,10)) = 4
        _GrainTex ("Grain Texture", 2D) = "white" {}
        _GrainStrength ("Grain Strength", Range(0,1)) = 0.2
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Cull Off // Double-sided!

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 viewDir : TEXCOORD2;
                float4 vertex : SV_POSITION;
                float2 screenUV : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _GrainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _RimColor;
            float _RimPower;
            float _GrainStrength;
            float _ShadowThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                o.screenUV = o.vertex.xy / o.vertex.w * 0.5 + 0.5; // for grain
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float NdotL = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                float shadow = step(_ShadowThreshold, NdotL);

                float3 baseColor = tex2D(_MainTex, i.uv).rgb * _Color.rgb;
                float3 shaded = baseColor * shadow;

                // Rim lighting
                float rim = pow(1.0 - saturate(dot(normal, i.viewDir)), _RimPower);
                shaded += rim * _RimColor.rgb;

                // Grain overlay
                float grain = tex2D(_GrainTex, i.screenUV).r;
                shaded = lerp(shaded, shaded * grain, _GrainStrength);

                return float4(shaded, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
