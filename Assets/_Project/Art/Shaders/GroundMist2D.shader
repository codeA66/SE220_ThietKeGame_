// World-space 3-octave quintic FBM ground mist with soft alpha blending and quadratic vertical feathering.
Shader "DeadCells/GroundMist2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Mist Mask Sprite", 2D) = "white" {}
        _MistColor ("Mist Color", Color) = (0.38, 0.54, 0.52, 0.15)
        _ScrollSpeed ("Scroll Speed", Float) = 0.04
        _NoiseScale ("Noise Scale", Float) = 3.5
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                float2 worldXY    : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _MistColor;
                float _ScrollSpeed;
                float _NoiseScale;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                float3 posWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.worldXY = posWS.xy;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 p = input.worldXY * float2(0.22, 0.65) * (_NoiseScale * 0.28);
                float t = _Time.y * _ScrollSpeed * 4.0;
                float f1 = ValueNoise2D(p + float2(t, 0.0));
                float f2 = ValueNoise2D(p * 2.03 + float2(-t * 0.72, 1.73));
                float f3 = ValueNoise2D(p * 4.17 + float2(t * 0.45, 3.41));
                half fbm = (half)(f1 * 0.52 + f2 * 0.33 + f3 * 0.15);
                half noise = smoothstep(0.22h, 0.88h, fbm);

                half vBottom = smoothstep(0.0h, 0.38h, (half)input.uv.y);
                half vTop = 1.0h - smoothstep(0.32h, 1.0h, (half)input.uv.y);
                half vFade = vBottom * vTop * vTop;
                half hFade = smoothstep(0.0h, 0.10h, (half)input.uv.x) * (1.0h - smoothstep(0.90h, 1.0h, (half)input.uv.x));

                half4 col = _MistColor * input.color;
                col.a *= noise * vFade * hFade * tex.a;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
