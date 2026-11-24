// Full URP 2D Lit shader with top-pinned sinusoidal vertex sway for hanging banners and vines.
// Both Universal2D and NormalsRendering apply identical sway and share the exact UnityPerMaterial CBUFFER.
Shader "DeadCells/VertexSway2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Diffuse", 2D) = "white" {}
        [NoScaleOffset] _MaskTex ("Mask", 2D) = "white" {}
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _SwayAmplitude ("Sway Amplitude", Float) = 0.09
        _SwayFrequency ("Sway Frequency", Float) = 1.6
        _SwayPhaseScale ("Sway Phase Scale", Float) = 0.7
        _UseWorldYPin ("Use World Y Pin (0=UV, 1=WorldY)", Float) = 0
        _PinTopWorldY ("Pin Top World Y", Float) = 0
        _SwaySpanY ("Sway Span Y", Float) = 1
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

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex LitVertex
            #pragma fragment LitFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_LIT_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Lit2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _SwayAmplitude;
                float _SwayFrequency;
                float _SwayPhaseScale;
                float _UseWorldYPin;
                float _PinTopWorldY;
                float _SwaySpanY;
            CBUFFER_END

            Varyings LitVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                float3 worldPos = TransformObjectToWorld(input.positionOS);
                float pinWeight = lerp(
                    1.0 - input.uv.y,
                    saturate((_PinTopWorldY - worldPos.y) / max(_SwaySpanY, 1e-4)),
                    step(0.5, _UseWorldYPin));
                input.positionOS.x += sin(_Time.y * _SwayFrequency + worldPos.x * _SwayPhaseScale) * _SwayAmplitude * pinWeight;

                Varyings o = CommonLitVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 LitFragment(Varyings input) : SV_Target
            {
                return CommonLitFragment(input, input.color);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            #pragma multi_compile_instancing
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _SwayAmplitude;
                float _SwayFrequency;
                float _SwayPhaseScale;
                float _UseWorldYPin;
                float _PinTopWorldY;
                float _SwaySpanY;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                float3 worldPos = TransformObjectToWorld(input.positionOS);
                float pinWeight = lerp(
                    1.0 - input.uv.y,
                    saturate((_PinTopWorldY - worldPos.y) / max(_SwaySpanY, 1e-4)),
                    step(0.5, _UseWorldYPin));
                input.positionOS.x += sin(_Time.y * _SwayFrequency + worldPos.x * _SwayPhaseScale) * _SwayAmplitude * pinWeight;

                Varyings o = CommonNormalsVertex(input);
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();
                return CommonNormalsFragment(input, input.color);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
