Shader "Custom/Dark90sAnime"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Base Color", Color) = (1,1,1,1)
        _ShadowThreshold ("Shadow Threshold", Range(0,1)) = 0.5
        _RimColor ("Rim Light Color", Color) = (0.8, 0.8, 1, 1)
        _RimPower ("Rim Power", Range(1,10)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _ShadowThreshold;
            float4 _RimColor;
            float _RimPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float NdotL = saturate(dot(normal, _WorldSpaceLightPos0.xyz));
                float3 baseColor = tex2D(_MainTex, i.uv).rgb * _Color.rgb;

                // Cel shading: hard threshold
                float shadow = step(_ShadowThreshold, NdotL);
                float3 shaded = baseColor * shadow;

                // Rim lighting
                float rim = pow(1.0 - saturate(dot(normal, i.viewDir)), _RimPower);
                shaded += rim * _RimColor.rgb;

                return float4(shaded, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Diffuse"
}
