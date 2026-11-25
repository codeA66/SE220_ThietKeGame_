// World-space dual-layer counter-drifting 3-octave quintic FBM ground mist with soft edge feathering.
Shader "DeadCells/GroundMist2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Mist Mask Sprite", 2D) = "white" {}
        _MistColor ("Mist Color", Color) = (0.52, 0.80, 0.76, 0.62)
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
                float2 pA = input.worldXY * float2(0.18, 0.55) * (_NoiseScale * 0.28);
                float2 pB = input.worldXY * float2(0.31, 0.88) * (_NoiseScale * 0.28);
                float t = _Time.y * _ScrollSpeed * 4.0;
                float fbmA = ValueNoise2D(pA + float2(t, 0.0)) * 0.52
                           + ValueNoise2D(pA * 2.03 + float2(-t * 0.70, 1.73)) * 0.33
                           + ValueNoise2D(pA * 4.17 + float2(t * 0.42, 3.41)) * 0.15;
                float fbmB = ValueNoise2D(pB + float2(-t * 0.62, t * 0.14 + 4.11)) * 0.55
                           + ValueNoise2D(pB * 2.11 + float2(t * 0.38, -t * 0.09 + 2.37)) * 0.30
                           + ValueNoise2D(pB * 4.05 + float2(-t * 0.25, 5.19)) * 0.15;
                half fbm = (half)(fbmA * 0.62 + fbmB * 0.38);
                half density = lerp(0.45h, 1.0h, smoothstep(0.18h, 0.82h, fbm));

                half vBottom = smoothstep(0.0h, 0.16h, (half)input.uv.y);
                half vTop = 1.0h - smoothstep(0.24h, 1.0h, (half)input.uv.y);
                half vFade = vBottom * pow(max(vTop, 0.0001h), 1.25h);
                // World-space constant-width edge feathering (works even when SpriteRenderers are dynamically batched)
                float duv = max(abs(ddx(input.uv.x)), 1e-6);
                float stripWidth = clamp(abs(ddx(input.worldXY.x)) / duv, 4.0, 200.0);
                float edgeSpanW = 3.6 + (float)fbm * 0.4;
                float distL = input.uv.x * stripWidth;
                float distR = (1.0 - input.uv.x) * stripWidth;
                half hLeft = (half)smoothstep(0.0, edgeSpanW, distL);
                half hRight = (half)smoothstep(0.0, edgeSpanW, distR);
                // Complementary pow(0.72) keeps alpha-over brightness flat across 4u interior strip overlaps and hits exact 0 at edges
                half hEdge = hLeft * hRight;
                half hFade = hEdge > 0.0001h ? pow(hEdge, 0.72h) : 0.0h;

                half4 col = _MistColor * input.color;
                col.a = saturate(col.a * density * vFade * hFade * tex.a);
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
