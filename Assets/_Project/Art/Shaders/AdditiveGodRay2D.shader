// Additive diagonal god-ray light shaft shader with animated dust shimmer and vertical fade.
Shader "DeadCells/AdditiveGodRay2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Shaft Texture", 2D) = "white" {}
        _Color ("Warm Shaft Tint", Color) = (1.0, 0.92, 0.68, 0.26)
        _ScrollSpeed ("Scroll Speed", Float) = 0.14
        _ShimmerStrength ("Shimmer Strength", Range(0, 1)) = 0.20
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

        Blend SrcAlpha One
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
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _ScrollSpeed;
                half _ShimmerStrength;
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
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half shimmer = max(0.0h, 0.85h + _ShimmerStrength * (half)sin(input.uv.x * 18.0 + input.uv.y * 6.0 - _Time.y * _ScrollSpeed * 6.283185));
                half vFade = smoothstep(0.0h, 0.18h, (half)input.uv.y) * (1.0h - smoothstep(0.88h, 1.0h, (half)input.uv.y));

                half4 col = tex * _Color * input.color;
                col.rgb *= shimmer;
                col.a *= vFade;
                return col;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
