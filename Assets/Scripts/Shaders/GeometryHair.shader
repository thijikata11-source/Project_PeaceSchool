Shader "Custom/HairStrandsURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 1
        _AnisoOffset ("Anisotropy Offset", Range(0, 1)) = 0.5
        _Alpha ("Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 tangent : TEXCOORD1;
                float3 normal : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _SpecularStrength;
            float _AnisoOffset;
            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.tangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed3 texColor = tex2D(_MainTex, i.uv).rgb * _Color.rgb;

                // Strand direction lighting
                float3 halfway = normalize(i.viewDir + normalize(_WorldSpaceLightPos0.xyz));
                float anisoDot = saturate(dot(i.tangent, halfway) + _AnisoOffset);
                float spec = pow(anisoDot, 40.0) * _SpecularStrength;

                fixed3 result = texColor + spec;
                return fixed4(result, _Alpha);
            }
            ENDCG
        }
    }
}
