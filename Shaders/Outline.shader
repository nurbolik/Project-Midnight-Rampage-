Shader "Custom/Outline" 
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _ExtrudeColor("Extrude Color", Color) = (0,0,0,0.5)
        _ExtrudeDirection("Extrude Dir", Vector) = (-1,1,0,0) // Default top-left
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _ExtrudeColor;
            float2 _ExtrudeDirection;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Only process transparent pixels
                if(col.a > 0) return col;

                // Calculate 1-pixel offset in specified direction
                float2 extrudeUV = i.uv + (_ExtrudeDirection * _MainTex_TexelSize.xy);
                
                // Sample extruded pixel
                fixed4 extrudeCol = tex2D(_MainTex, extrudeUV);
                
                // Apply extrude color only if neighbor is opaque
                return (extrudeCol.a > 0) ? _ExtrudeColor : col;
            }
            ENDCG
        }
    }
}