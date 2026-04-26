Shader "Custom/SimpleNoiseBackground"
{
    Properties
    {
        [Header(Colors)]
        _SkyBlue ("Sky Color", Color) = (0.529, 0.808, 0.922, 1)
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _MinBrightness ("Minimum Brightness", Range(0, 0.5)) = 0.05

        [Header(Movement)]
        _Speed ("Cloud Speed", Vector) = (-0.02, 0, 0, 0)
        _TimeScale ("Time Scale", Float) = 1.0

        [Header(Noise)]
        _Density ("Density", Range(0.1, 10)) = 2.0
        _DensitySpeed ("Density Speed", Float) = 0.025
        _Noise ("Noise Scale", Range(0.1, 10)) = 4.0
        _NoiseSpeed ("Noise Speed", Float) = 0.02
        _EdgeSharpness ("Edge Sharpness", Range(0.1, 5)) = 3.0
        _Intensity ("Intensity", Range(0.1, 5)) = 1.0

        [Header(Debug)]
        [Toggle] _ShowNoise ("Show Noise Only", Float) = 0
        [Toggle] _ShowDensity ("Show Density Only", Float) = 0
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            // Properties
            float4 _SkyBlue;
            float4 _CloudColor;
            float _MinBrightness;
            float2 _Speed;
            float _TimeScale;
            float _Density;
            float _DensitySpeed;
            float _Noise;
            float _NoiseSpeed;
            float _EdgeSharpness;
            float _Intensity;
            bool _ShowNoise;
            bool _ShowDensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // Improved random function
            float3 random3(float3 c) {
                float j = 4096.0 * sin(dot(c, float3(17.0, 59.4, 15.0)));
                float3 r;
                r.z = frac(512.0 * j);
                j *= .125;
                r.x = frac(512.0 * j);
                j *= .125;
                r.y = frac(512.0 * j);
                return r - 0.5;
            }

            // Simplex noise constants
            static const float F3 = 0.3333333;
            static const float G3 = 0.1666667;

            // 3D simplex noise
            float simplex3d(float3 p) {
                float3 s = floor(p + dot(p, float3(F3, F3, F3)));
                float3 x = p - s + dot(s, float3(G3, G3, G3));
                
                float3 e = step(float3(0,0,0), x - x.yzx);
                float3 i1 = e * (1.0 - e.zxy);
                float3 i2 = 1.0 - e.zxy * (1.0 - e);
                    
                float3 x1 = x - i1 + G3;
                float3 x2 = x - i2 + 2.0 * G3;
                float3 x3 = x - 1.0 + 3.0 * G3;
                
                float4 w, d;
                w.x = dot(x, x);
                w.y = dot(x1, x1);
                w.z = dot(x2, x2);
                w.w = dot(x3, x3);
                
                w = max(0.6 - w, 0.0);
                
                d.x = dot(random3(s), x);
                d.y = dot(random3(s + i1), x1);
                d.z = dot(random3(s + i2), x2);
                d.w = dot(random3(s + 1.0), x3);
                
                w *= w;
                w *= w;
                d *= w;
                
                return dot(d, float4(52.0, 52.0, 52.0, 52.0));
            }

            // Rotation matrices for fractal noise
            static const float3x3 rot1 = float3x3(-0.37, 0.36, 0.85, -0.14, -0.93, 0.34, 0.92, 0.01, 0.4);
            static const float3x3 rot2 = float3x3(-0.55, -0.39, 0.74, 0.33, -0.91, -0.24, 0.77, 0.12, 0.63);
            static const float3x3 rot3 = float3x3(-0.71, 0.52, -0.47, -0.08, -0.72, -0.68, -0.7, -0.45, 0.56);

            // Fractal noise generator
            float simplex3d_fractal(float3 m) {
                return 0.5333333 * simplex3d(mul(m, rot1))
                     + 0.2666667 * simplex3d(mul(2.0 * m, rot2))
                     + 0.1333333 * simplex3d(mul(4.0 * m, rot3))
                     + 0.0666667 * simplex3d(8.0 * m);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float time = _Time.y * _TimeScale;
                float epsilon = 0.001;

                // Cloud calculation
                float2 uvt = uv + _Speed * time;
                
                // Noise component
                float v_noise = 0.5 + 0.5 * simplex3d_fractal(float3(uvt, time * _NoiseSpeed) * _Noise);
                v_noise = saturate(v_noise);
                v_noise = 1.0 - pow(max(epsilon, 1.0 - v_noise), _Intensity);
                
                // Density component
                float density_mask = min(0.5 + simplex3d(float3(uvt, time * _DensitySpeed) * _Density) * _EdgeSharpness, 1.0);
                density_mask = max(density_mask, epsilon);
                
                // Combine results
                float v = v_noise * density_mask;
                v = saturate(v);
                v = lerp(_MinBrightness, 1.0, v); // Ensure minimum brightness
                v = smoothstep(0.0, 1.0, v); // Smooth transitions

                // Debug views
                if (_ShowNoise) return fixed4(v_noise.xxx, 1.0);
                if (_ShowDensity) return fixed4(density_mask.xxx, 1.0);
                
                // Final color
                return fixed4(lerp(_SkyBlue.rgb, _CloudColor.rgb, v), 1.0);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}