// URP lit shader with screen-door (dither) fade. Driven by _Fade (1 = fully opaque, 0 = fully hidden)
// via a MaterialPropertyBlock from CameraOcclusionFader — no transparency, no overdraw, mobile-friendly.
Shader "RelicRaid/DitherFadeLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.5,0.5,0.55,1)
        _Fade ("Fade (1=visible, 0=hidden)", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float  _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 normalWS:TEXCOORD0; float2 uv:TEXCOORD1; float4 screenPos:TEXCOORD2; };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionHCS = p.positionCS;
                o.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                o.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                o.screenPos = ComputeScreenPos(p.positionCS);
                return o;
            }

            static const float _Bayer4x4[16] =
            {
                0.0/16, 8.0/16, 2.0/16, 10.0/16,
                12.0/16, 4.0/16, 14.0/16, 6.0/16,
                3.0/16, 11.0/16, 1.0/16, 9.0/16,
                15.0/16, 7.0/16, 13.0/16, 5.0/16
            };

            half4 frag (Varyings IN) : SV_Target
            {
                // Screen-door dither: clip pixels when _Fade is below the cell's threshold.
                float2 sp = (IN.screenPos.xy / max(IN.screenPos.w, 1e-5)) * _ScreenParams.xy;
                int ix = (int)fmod(sp.x, 4.0);
                int iy = (int)fmod(sp.y, 4.0);
                float threshold = _Bayer4x4[iy * 4 + ix];
                clip(_Fade - threshold);

                half4 baseC = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                Light mainLight = GetMainLight();
                float3 n = normalize(IN.normalWS);
                float ndl = saturate(dot(n, mainLight.direction));
                half3 ambient = SampleSH(n);
                half3 col = baseC.rgb * (mainLight.color * ndl + ambient);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
