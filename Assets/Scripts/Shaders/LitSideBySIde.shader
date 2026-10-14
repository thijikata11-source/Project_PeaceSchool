Shader "Custom/SideBySideTransparentLitDoubleSided"
{
    Properties
    {
        _MainTex ("Left Texture", 2D) = "white" {}
        _SecondTex ("Right Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
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
            sampler2D _SecondTex;
            float4 _Color;
            float _Cutoff;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Flip normals if backface
                float3 normal = normalize(i.worldNormal);
                if (dot(normal, i.viewDir) < 0)
                    normal = -normal;

                // Simple Lambert lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diff = saturate(dot(normal, lightDir));
                fixed4 texColor;

                if (i.uv.x < 0.5)
                {
                    float2 leftUV = float2(i.uv.x * 2.0, i.uv.y);
                    texColor = tex2D(_MainTex, leftUV);
                }
                else
                {
                    float2 rightUV = float2((i.uv.x - 0.5) * 2.0, i.uv.y);
                    texColor = tex2D(_SecondTex, rightUV);
                }

                texColor *= _Color;
                texColor.rgb *= diff;

                clip(texColor.a - _Cutoff);
                return texColor;
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/Diffuse"
}
