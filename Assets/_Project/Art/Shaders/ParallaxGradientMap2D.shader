// Grayscale-to-4-stop-gradient mapper for Dead Cells-style parallax background layers.
// Samples _MainTex luminance, evaluates a piecewise-linear 4-stop ramp (_ColorDeep ->
// _ColorMidDark -> _ColorMidLight -> _ColorHighlight), and optionally blends URP 2D lighting.
Shader "DeadCells/ParallaxGradientMap2D"
{
    Properties
    {
        [MainTexture][NoScaleOffset] _MainTex ("Grayscale Sprite", 2D) = "white" {}
        _ColorDeep ("Deep Color (0.0)", Color) = (0.027, 0.051, 0.067, 1)
        _ColorMidDark ("Mid Dark Color (0.333)", Color) = (0.059, 0.110, 0.118, 1)
        _ColorMidLight ("Mid Light Color (0.666)", Color) = (0.106, 0.188, 0.173, 1)
        _ColorHighlight ("Highlight Color (1.0)", Color) = (0.180, 0.290, 0.259, 1)
        _ScrollSpeed ("Scroll Speed (UV/s)", Float) = 0
        _LitBlend ("Lit Blend", Range(0, 1)) = 0
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
            SAMPLER(sampler_point_repeat);
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorDeep;
                half4 _ColorMidDark;
                half4 _ColorMidLight;
                half4 _ColorHighlight;
                float _ScrollSpeed;
                half _LitBlend;
            CBUFFER_END

            half3 EvaluateGradient(half lum)
            {
                const half stop1 = 1.0h / 3.0h;
                const half stop2 = 2.0h / 3.0h;

                half t0 = saturate(lum / stop1);
                half t1 = saturate((lum - stop1) / (stop2 - stop1));
                half t2 = saturate((lum - stop2) / (1.0h - stop2));

                half3 c01 = lerp(_ColorDeep.rgb, _ColorMidDark.rgb, t0);
                half3 c12 = lerp(_ColorMidDark.rgb, _ColorMidLight.rgb, t1);
                half3 c23 = lerp(_ColorMidLight.rgb, _ColorHighlight.rgb, t2);

                half w1 = step(stop1, lum);
                half w2 = step(stop2, lum);
                half3 rgb = lerp(c01, c12, w1);
                return lerp(rgb, c23, w2);
            }

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
                float2 scrolledUv = input.uv + float2(frac(_ScrollSpeed * _Time.y), 0.0);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_point_repeat, scrolledUv);
                clip(tex.a - 0.001h);

                // Convert sampled linear grayscale luminance back to perceptual sRGB space so
                // 4-stop gradient stops distribute cleanly across [0.12..0.78] under Linear/ACES.
                half linearLum = saturate(dot(tex.rgb, half3(0.299h, 0.587h, 0.114h)));
                half lum = pow(max(linearLum, 1e-4h), 1.0h / 2.2h);
                half3 gradRgb = EvaluateGradient(lum) * input.color.rgb;
                half alpha = tex.a * input.color.a;

                if (_LitBlend <= 0.001h)
                    return half4(gradRgb, alpha);

                half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, scrolledUv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, scrolledUv));

                SurfaceData2D surfaceData;
                InputData2D inputData;
                InitializeSurfaceData(gradRgb, alpha, mask, normalTS, surfaceData);
                InitializeInputData(scrolledUv, input.lightingUV, inputData);

                half4 lit = CombinedShapeLightShared(surfaceData, inputData);
                half3 finalRgb = lerp(gradRgb, lit.rgb, _LitBlend);
                return half4(finalRgb, alpha);
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

            SAMPLER(sampler_point_repeat);

            // Identical layout across passes for 2D SRP Batcher compatibility; no _ST properties.
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorDeep;
                half4 _ColorMidDark;
                half4 _ColorMidLight;
                half4 _ColorHighlight;
                float _ScrollSpeed;
                half _LitBlend;
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

                float2 scrolledUv = input.uv + float2(frac(_ScrollSpeed * _Time.y), 0.0);
                half4 mainTex = input.color * SAMPLE_TEXTURE2D(_MainTex, sampler_point_repeat, scrolledUv);
                clip(mainTex.a - 0.001h);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, scrolledUv));
                return NormalsRenderingShared(mainTex, normalTS, input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
