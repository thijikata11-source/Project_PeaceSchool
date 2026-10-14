Shader "Custom/HandPaintedFlatShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _LightDir ("Light Direction", Vector) = (0, 0, 1, 0)
        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.3
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _LightDir;
            float _SpecularStrength;
            float4 _SpecularColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

                // Normalize and invert light direction
                float3 lightDir = normalize(_LightDir.xyz);
                float ndotl = max(0, dot(i.worldNormal, lightDir));
                
                // Basic lighting: no ambient, just direct and spec
                float spec = pow(ndotl, 32) * _SpecularStrength;
                fixed4 specular = spec * _SpecularColor;

                return texColor + specular;
            }
            ENDCG
        }
    }
}
