Shader "Custom/CRTShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ScanlineSpeed ("Scanline Speed", Float) = 2.0
        _ScanlineIntensity ("Scanline Intensity", Range(0,1)) = 0.5
        _ScanlineCount ("Scanline Count", Float) = 400.0
        _Vignette ("Vignette Strength", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _ScanlineSpeed;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _Vignette;

            fixed4 frag (v2f_img i) : SV_Target
            {
                // Base texture
                fixed4 col = tex2D(_MainTex, i.uv);

                // Scanlines (horizontal stripes scrolling vertically)
                float scanline = sin((i.uv.y * _ScanlineCount) + (_Time.y * _ScanlineSpeed));
                scanline = (scanline * 0.5 + 0.5); // normalize to 0–1
                col.rgb *= (1.0 - scanline * _ScanlineIntensity);

                // Vignette (darken edges)
                float2 center = i.uv - 0.5;
                float vignette = 1.0 - _Vignette * length(center) * 2.0;
                col.rgb *= vignette;

                return col;
            }
            ENDCG
        }
    }
}
