Shader "Hidden/StencilMaskOnly"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            ColorMask 0 
            ZWrite Off 
            ZTest LEqual
            
            Stencil 
            { 
                Ref 5 
                Comp Always 
                Pass Replace 
            }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 vertex : SV_POSITION; };
            
            v2f vert(appdata v) 
            { 
                v2f o; 
                o.vertex = UnityObjectToClipPos(v.vertex); 
                return o; 
            }
            
            fixed4 frag(v2f i) : SV_Target 
            { 
                return fixed4(0,0,0,0); 
            }
            ENDCG
        }
    }
}
