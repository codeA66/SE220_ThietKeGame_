Shader "DeadCells/UI/Scroll Focus Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured frame", 2D) = "black" {}
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
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = (half4)input.color;
                return output;
            }

            half3 SampleClamped(float2 uv, float2 minUv, float2 maxUv)
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, clamp(uv, minUv, maxUv)).rgb;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 texel = abs(_MainTex_TexelSize.xy);
                float2 radius = texel * 5.0;
                float2 minUv = texel * 0.5;
                float2 maxUv = max(minUv, 1.0 - minUv);

                half3 sum = SampleClamped(input.uv, minUv, maxUv) * 0.20h;
                sum += SampleClamped(input.uv + float2(radius.x, 0.0), minUv, maxUv) * 0.12h;
                sum += SampleClamped(input.uv - float2(radius.x, 0.0), minUv, maxUv) * 0.12h;
                sum += SampleClamped(input.uv + float2(0.0, radius.y), minUv, maxUv) * 0.12h;
                sum += SampleClamped(input.uv - float2(0.0, radius.y), minUv, maxUv) * 0.12h;
                sum += SampleClamped(input.uv + radius, minUv, maxUv) * 0.08h;
                sum += SampleClamped(input.uv - radius, minUv, maxUv) * 0.08h;
                sum += SampleClamped(input.uv + float2(radius.x, -radius.y), minUv, maxUv) * 0.08h;
                sum += SampleClamped(input.uv + float2(-radius.x, radius.y), minUv, maxUv) * 0.08h;
                return half4(sum * half3(0.64h, 0.72h, 0.83h) * input.color.rgb, input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
