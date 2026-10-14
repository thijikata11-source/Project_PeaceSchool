Shader "Custom/BuiltInStylizedCharacter"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.05)) = 0.015
        _SkyboxLightColor ("Skybox Light Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(1.0, 10.0)) = 5.0
        _RimSharpness ("Rim Sharpness", Range(2.0, 40.0)) = 20.0
        _AmbientStrength ("Ambient Light", Range(0, 1)) = 0.2
        _LightStrength ("Light Boost", Range(0.1, 2)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert fullforwardshadows

        sampler2D _BaseMap;
        float4 _BaseColor;
        float4 _SkyboxLightColor;
        float _RimPower;
        float _RimSharpness;
        float _AmbientStrength;
        float _LightStrength;

        struct Input
        {
            float2 uv_BaseMap;
            float3 viewDir;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            fixed4 tex = tex2D(_BaseMap, IN.uv_BaseMap) * _BaseColor;
            o.Albedo = tex.rgb;
            o.Alpha = tex.a;

            // Rim lighting
            float rim = saturate(1.0 - dot(normalize(IN.viewDir), o.Normal));
            rim = pow(rim, _RimSharpness);
            o.Emission = _SkyboxLightColor.rgb * rim * _RimPower;

            // Ambient + direct light boost
            o.Albedo *= (_AmbientStrength + _LightStrength);
        }
        ENDCG

        // Outline Pass
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _OutlineColor;
            float _OutlineWidth;

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
                v2f o;
                float3 norm = normalize(v.normal);
                v.vertex.xyz += norm * _OutlineWidth;
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
