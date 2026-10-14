Shader "UI/VerticalWireCable"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Cable Properties driven by C# script
        _HandlePosNormalized ("Handle Position (0-1)", Range(0,1)) = 0.5
        _WiggleWeight ("Wiggle Weight (0-1)", Range(0,1)) = 1.0
        _WiggleAmount ("Wiggle Max Amplitude", Float) = 20.0
        _WiggleSpeed ("Wiggle Speed", Float) = 25.0
        _TimeOffset ("Time Offset", Float) = 0.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off Lighting Off ZWrite Off

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
                float2 texcoord  : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _HandlePosNormalized;
            float _WiggleWeight;
            float _WiggleAmount;
            float _WiggleSpeed;
            float _TimeOffset;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                // yProgress is 0 at the bottom anchor, 1 at the top anchor of the background
                float yProgress = IN.texcoord.y;

                // Create a curve that is anchored (0 distortion) at the very top (1) and very bottom (0)
                float cableAnchorConstraint = sin(yProgress * 3.14159265);

                // Create a wave ripple that peaks heaviest right near the handle position
                // It utilizes a Gaussian-like bell curve around the handle position
                float distanceToHandle = yProgress - _HandlePosNormalized;
                float handleInfluence = exp(-(distanceToHandle * distanceToHandle) * 16.0); 

                // Dynamic sine-wave wiggle calculation
                float dynamicWave = sin((yProgress * 8.0) - (_TimeOffset * _WiggleSpeed));
                
                // Combine everything into a horizontal (X-axis) vertex deformation offset
                // The distortion scales down completely as _WiggleWeight approaches 0 (at value 100)
                float horizontalOffset = dynamicWave * _WiggleAmount * _WiggleWeight * cableAnchorConstraint * (0.4 + handleInfluence * 0.6);

                IN.vertex.x += horizontalOffset;

                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, IN.texcoord) * IN.color;
                return col;
            }
            ENDCG
        }
    }
}
