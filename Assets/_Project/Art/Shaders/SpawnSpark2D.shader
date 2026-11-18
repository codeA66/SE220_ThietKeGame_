// Additive pixel spark for particle systems (spawn-intro head trail and merge burst).
// Each particle quad is drawn as a hard square core of _CoreSize texels inside a soft square halo,
// quantised to _Pixels steps so it matches the game's pixel art. Colour and fade come from the
// particle's vertex colour (Color over Lifetime); _Intensity boosts brightness. No texture fetch.
Shader "DeadCells/SpawnSpark2D"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 4)) = 1.5
        _Pixels ("Pixels Across Quad", Float) = 6
        _CoreSize ("Core Size (0-1)", Range(0, 1)) = 0.34
        _HaloStrength ("Halo Strength", Range(0, 1)) = 0.35
        _CoreWhiten ("Core Whiten", Range(0, 1)) = 0.6
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

        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
                float _Pixels;
                half _CoreSize;
                half _HaloStrength;
                half _CoreWhiten;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Snap to a pixel grid, then measure the square (Chebyshev) distance from the centre.
                float2 p = (floor(input.uv * _Pixels) + 0.5) / _Pixels - 0.5;
                half d = max(abs(p.x), abs(p.y)) * 2.0h;

                half core = step(d, _CoreSize);
                half halo = saturate(1.0h - d) * _HaloStrength;
                half a = saturate(core + halo) * input.color.a;
                clip(a - 0.001h);

                half3 rgb = lerp(input.color.rgb, half3(1, 1, 1), core * _CoreWhiten) * _Intensity;
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
