// Unlit HDR pulsing emissive sprite shader for torches, glowing mushrooms, scrolls, and exits.
// Leaves RGB unclamped above 1.0 so URP Bloom responds cleanly to _EmissionColor.
Shader "DeadCells/EmissiveHdr2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Sprite", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (2, 1.2, 0.4, 1)
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.15
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
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

        Blend SrcAlpha [_DstBlend]
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
                half4 _EmissionColor;
                float _PulseSpeed;
                half _PulseAmount;
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
                clip(tex.a - 0.001h);

                half pulse = max(0.0h, 1.0h + (half)sin(_Time.y * _PulseSpeed) * _PulseAmount);
                half3 rgb = tex.rgb * _EmissionColor.rgb * pulse * input.color.rgb;
                half alpha = tex.a * _EmissionColor.a * input.color.a;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
