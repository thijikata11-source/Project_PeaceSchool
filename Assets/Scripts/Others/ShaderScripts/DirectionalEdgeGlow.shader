Shader "UI/DirectionalEdgeGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Glow Properties
        _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        _GlowThickness ("Glow Thickness", Range(0.0, 0.5)) = 0.15
        _GlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 0.0 
        _GlowDirection ("Glow Direction (XY)", Vector) = (0, 0, 0, 0) // Passed by C#
        _GlowAngleSpread ("Glow Spread/Width", Range(0.0, 2.0)) = 1.0  // Adjusts how wide the arc is
        
        // Required for UI Canvas clipping/masking
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off ZTest [ZTest]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowThickness;
            float _GlowIntensity;
            float4 _GlowDirection;
            float _GlowAngleSpread;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample base texture
                fixed4 baseColor = tex2D(_MainTex, i.texcoord) * i.color;

                // Center the UV coordinates to (-0.5 to 0.5) to find center/edges
                float2 uvCenter = i.texcoord - float2(0.5, 0.5);
                float distFromCenter = length(uvCenter);

                // 1. Calculate the Edge Mask (Perfect for circular joysticks)
                // If using a square joystick, change this to the square border logic
                float edgeMask = smoothstep(0.5 - _GlowThickness, 0.5, distFromCenter);

                // 2. Calculate the Directional Mask
                // Normalize the pixel's direction from the center
                float2 pixelDir = normalize(uvCenter);
                float2 glowDir = normalize(_GlowDirection.xy);

                // Dot product determines how closely aligned the pixel is to the drag direction
                // Returns 1.0 if perfectly aligned, 0.0 if perpendicular, -1.0 if opposite
                float dirDot = dot(pixelDir, glowDir);
                
                // Shape the dot product into a smooth arc/spread
                float directionalMask = smoothstep(1.0 - _GlowAngleSpread, 1.0, dirDot);

                // Combine masks and dynamic intensity
                float finalGlowMask = edgeMask * directionalMask * _GlowIntensity;
                
                // Constrain glow to the visible shape of your sprite asset
                fixed4 glowEffect = _GlowColor * finalGlowMask;
                glowEffect.a *= baseColor.a;

                // Final Blend
                fixed4 finalColor = baseColor + glowEffect;
                finalColor.a = max(baseColor.a, glowEffect.a);

                return finalColor;
            }
            ENDCG
        }
    }
}
