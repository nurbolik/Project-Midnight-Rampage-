Shader "Custom/SpriteLitShadowCast"
{
    Properties
    {
        [MainColor] _Color ("Tint", Color) = (1,1,1,1)
        [MainTexture] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0,0,0,0.5)
        _ShadowPixelOffset ("Shadow Offset (pixels)", Vector) = (2, -2, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="Universal2D" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_ShapeLightTexture0);
            SAMPLER(sampler_ShapeLightTexture0);

            float4 _MainTex_ST;
            float4 _Color;
            float4 _ShadowColor;
            float4 _ShadowPixelOffset; // XY = pixel offsets, ZW unused

            // Texture size in pixels, set by script
            float2 _MainTex_TexelSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                o.screenPos = o.vertex;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Calculate precise UV offset in texture space based on pixel offset:
                float2 pixelOffsetUV = _ShadowPixelOffset.xy * _MainTex_TexelSize;

                // Sample shadow sprite with pixel-perfect offset
                half4 shadowTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv + pixelOffsetUV);
                half4 shadow = shadowTex * _ShadowColor;

                // Sample main sprite
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 mainColor = tex * i.color;

                // Sample 2D lightmap for lighting the sprite
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                screenUV = 0.5 * (float2(screenUV.x, -screenUV.y) + 1.0);
                half4 lightColor = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, screenUV);
                mainColor.rgb *= lightColor.rgb;

                // Composite shadow behind sprite
                return shadow * (1 - mainColor.a) + mainColor;
            }
            ENDHLSL
        }
    }
}
