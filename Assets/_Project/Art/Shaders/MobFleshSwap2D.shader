// Enemy sprites from Dead Cells: real colours, with pure magenta (255,0,255) marking "flesh" details
// (pustules, cloth, glowing bits). Magenta is replaced by _FleshColor (mob table column flesh2).
// The magenta -> flesh2 rule comes from a visual experiment, not from documented source.
// Unlit, SRP-batcher compatible; honours SpriteRenderer flip and colour; _Flash for hit feedback.
Shader "DeadCells/MobFleshSwap2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Sprite", 2D) = "white" {}
        _FleshColor ("Flesh Color (replaces magenta)", Color) = (1, 0, 1, 1)
        _Flash ("Flash (0-1, set per renderer)", Range(0, 1)) = 0
        _FlashColor ("Flash Color (set per renderer)", Color) = (1, 1, 1, 1)
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

            CBUFFER_START(UnityPerMaterial)
                half4 _FleshColor;
                half _Flash;
                half4 _FlashColor;
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
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv);
                clip(c.a - 0.001h);

                // Exact magenta survives sRGB->linear as (1, 0, 1).
                half isFlesh = step(0.99h, c.r) * step(c.g, 0.01h) * step(0.99h, c.b);
                c.rgb = lerp(c.rgb, _FleshColor.rgb, isFlesh);

                c *= input.color;
                c.rgb = lerp(c.rgb, _FlashColor.rgb, _Flash);
                return c;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
