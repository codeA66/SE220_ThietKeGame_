Shader "DeadCells/UI/Scroll Focus Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Captured frame", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            struct AppData { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 clip : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(AppData input)
            {
                Varyings output;
                output.clip = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                return output;
            }

            fixed4 frag(Varyings input) : SV_Target
            {
                float2 radius = abs(_MainTex_TexelSize.xy) * 5.0;
                fixed3 sum = tex2D(_MainTex, input.uv).rgb * 0.20;
                sum += tex2D(_MainTex, input.uv + float2(radius.x, 0)).rgb * 0.12;
                sum += tex2D(_MainTex, input.uv - float2(radius.x, 0)).rgb * 0.12;
                sum += tex2D(_MainTex, input.uv + float2(0, radius.y)).rgb * 0.12;
                sum += tex2D(_MainTex, input.uv - float2(0, radius.y)).rgb * 0.12;
                sum += tex2D(_MainTex, input.uv + radius).rgb * 0.08;
                sum += tex2D(_MainTex, input.uv - radius).rgb * 0.08;
                sum += tex2D(_MainTex, input.uv + float2(radius.x, -radius.y)).rgb * 0.08;
                sum += tex2D(_MainTex, input.uv + float2(-radius.x, radius.y)).rgb * 0.08;
                return fixed4(sum * fixed3(0.64, 0.72, 0.83), 1);
            }
            ENDCG
        }
    }
}
