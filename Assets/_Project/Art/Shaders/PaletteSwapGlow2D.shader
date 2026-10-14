// Palette swap (same index rule as PaletteSwap2D) plus a pulsing 1-texel outline glow and a flash.
// Used by small effect sprites cut from the hero (the spawn-intro head) so they read on dark backgrounds.
// The sprite texture needs a 1-texel transparent border for the outline to show on every side.
Shader "DeadCells/PaletteSwapGlow2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Index Sprite", 2D) = "white" {}
        [NoScaleOffset] _PaletteTex ("Palette (256 x N)", 2D) = "white" {}
        _PaletteRow ("Palette Row", Float) = 1
        _PaletteRows ("Palette Row Count", Float) = 4
        [HDR] _OutlineColor ("Outline Color", Color) = (0.85, 0.95, 1, 1)
        _PulseSpeed ("Outline Pulse (Hz)", Float) = 3
        _PulseMin ("Outline Pulse Min Alpha", Range(0, 1)) = 0.45
        _Flash ("Flash (0-1, set per renderer)", Range(0, 1)) = 0
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_point_clamp);
            TEXTURE2D(_PaletteTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _PaletteRow;
                float _PaletteRows;
                half4 _OutlineColor;
                float _PulseSpeed;
                half _PulseMin;
                half _Flash;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 index = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv);
                half pulse = lerp(_PulseMin, 1.0h, 0.5h + 0.5h * sin(_Time.y * _PulseSpeed * 6.2831853));

                if (index.a < 0.001h)
                {
                    // Outline: an empty texel next to an opaque one (4-neighbourhood).
                    float2 t = _MainTex_TexelSize.xy;
                    half n = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv + float2(t.x, 0)).a
                           + SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv - float2(t.x, 0)).a
                           + SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv + float2(0, t.y)).a
                           + SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv - float2(0, t.y)).a;
                    clip(n - 0.001h);
                    half4 o = _OutlineColor;
                    o.a *= pulse * input.color.a;
                    return o;
                }

                float u = (round(index.r * 255.0) + 0.5) / 256.0;
                float v = (_PaletteRow + 0.5) / _PaletteRows;
                half4 color = SAMPLE_TEXTURE2D(_PaletteTex, sampler_point_clamp, float2(u, v));
                color.a = index.a;
                color *= input.color;
                color.rgb = lerp(color.rgb, half3(1, 1, 1), _Flash);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
