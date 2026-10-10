// Palette swap for Dead Cells-style index sprites.
// The sprite's red channel (0-255) is an index into a 256-wide palette texture; _PaletteRow picks
// which palette row to use. Sprite textures must be imported with sRGB OFF so the index is exact.
// Unlit: one extra point-sampled texture fetch per pixel, no lighting.
Shader "DeadCells/PaletteSwap2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Index Sprite", 2D) = "white" {}
        [NoScaleOffset] _PaletteTex ("Palette (256 x N)", 2D) = "white" {}
        _PaletteRow ("Palette Row", Float) = 1
        _PaletteRows ("Palette Row Count", Float) = 4
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

            // Core2D provides SetUpSpriteInstanceProperties, UnityFlipSprite (SpriteRenderer.flipX/Y)
            // and unity_SpriteColor (SpriteRenderer.color), matching URP's Sprite-Unlit-Default.
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

            // No _MainTex_ST: sprites never tile/offset, and _ST disables the 2D SRP Batcher.
            CBUFFER_START(UnityPerMaterial)
                float _PaletteRow;
                float _PaletteRows;
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
                clip(index.a - 0.001h);

                // Center of the texel: (round(r * 255) + 0.5) / 256
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
