Shader "Custom/ParallaxWithCamera"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _HeightMap ("Height Map", 2D) = "black" {}
        _ParallaxStrength ("Parallax Strength", Range(0.0, 0.1)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _HeightMap;
        float _ParallaxStrength;

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir; // camera view direction
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample height map
            float height = tex2D(_HeightMap, IN.uv_MainTex).r;

            // Offset UVs based on view direction and height
            float2 parallaxOffset = IN.viewDir.xy * (height * _ParallaxStrength);
            float2 uv = IN.uv_MainTex + parallaxOffset;

            // Sample base texture with parallax UVs
            fixed4 c = tex2D(_MainTex, uv);
            o.Albedo = c.rgb;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
