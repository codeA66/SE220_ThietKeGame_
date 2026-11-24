// Lit-hybrid palette swap for Dead Cells-style index sprites.
// Preserves exact 256-entry palette row lookup with point clamp sampling, blends URP 2D
// lighting with a self-lit floor (_SelfLitWeight), and applies _Flash after lighting.
Shader "DeadCells/PaletteSwapLit2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Index Sprite", 2D) = "white" {}
        [NoScaleOffset] _PaletteTex ("Palette (256 x N)", 2D) = "white" {}
        _PaletteRow ("Palette Row", Float) = 1
        _PaletteRows ("Palette Row Count", Float) = 4
        _SelfLitWeight ("Self-Lit Weight", Range(0, 1)) = 0.45
        _Flash ("Flash (0-1, set per renderer)", Range(0, 1)) = 0
        [NoScaleOffset] _MaskTex ("Mask", 2D) = "white" {}
        [NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
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
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

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
                half2  lightingUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_point_clamp);
            TEXTURE2D(_PaletteTex);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // Identical layout across passes for 2D SRP Batcher compatibility; no _ST properties.
            CBUFFER_START(UnityPerMaterial)
                float _PaletteRow;
                float _PaletteRows;
                half _SelfLitWeight;
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
                output.lightingUV = half2(ComputeScreenPos(output.positionCS / output.positionCS.w).xy);
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
                half4 albedo = SAMPLE_TEXTURE2D(_PaletteTex, sampler_point_clamp, float2(u, v));
                albedo.a = index.a;
                albedo *= input.color;

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(albedo.rgb, albedo.a, mask, normalTS, surfaceData);
                InitializeInputData(input.uv, input.lightingUV, inputData);

                half4 lit = CombinedShapeLightShared(surfaceData, inputData);
                half3 finalRgb = lerp(lit.rgb, albedo.rgb, _SelfLitWeight);
                finalRgb = lerp(finalRgb, half3(1, 1, 1), _Flash);
                return half4(finalRgb, albedo.a);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "NormalsRendering" }

            HLSLPROGRAM
            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                COMMON_2D_NORMALS_INPUTS
                float4 color : COLOR;
            };

            struct Varyings
            {
                COMMON_2D_NORMALS_OUTPUTS
                half4 color : COLOR;
            };

            // Normals2DCommon.hlsl declares _MainTex, sampler_MainTex, _NormalMap, sampler_NormalMap.
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Normals2DCommon.hlsl"

            SAMPLER(sampler_point_clamp);

            CBUFFER_START(UnityPerMaterial)
                float _PaletteRow;
                float _PaletteRows;
                half _SelfLitWeight;
                half _Flash;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonNormalsVertex(input);
                output.color = input.color * unity_SpriteColor;
                return output;
            }

            half4 NormalsRenderingFragment(Varyings input) : SV_Target
            {
                SetUpSpriteInstanceProperties();

                half4 mainTex = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_point_clamp, input.uv);
                clip(mainTex.a - 0.001h);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.uv));
                return NormalsRenderingShared(mainTex, normalTS, input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
