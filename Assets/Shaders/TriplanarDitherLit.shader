// Triplanar (world-space) tiling + URP lighting + screen-door dither fade. Triplanar means textures map
// correctly onto arbitrarily-scaled cubes with NO UV work or stretching — ideal for grayblock geometry.
// Shares the _Fade convention with DitherFadeLit so CameraOcclusionFader fades these walls too.
Shader "RelicRaid/TriplanarDitherLit"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Tiling ("Tiling (repeats per meter)", Float) = 0.4
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
                float4 _BaseColor;
                float  _Tiling;
                float  _Fade;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 worldPos:TEXCOORD0; float3 worldNormal:TEXCOORD1; float4 screenPos:TEXCOORD2; };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                o.positionHCS = p.positionCS;
                o.worldPos = p.positionWS;
                o.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
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

            half3 SampleTriplanar (float3 wp, float3 blend, float tiling)
            {
                half3 cx = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.zy * tiling).rgb;
                half3 cy = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.xz * tiling).rgb;
                half3 cz = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, wp.xy * tiling).rgb;
                return cx * blend.x + cy * blend.y + cz * blend.z;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 sp = (IN.screenPos.xy / max(IN.screenPos.w, 1e-5)) * _ScreenParams.xy;
                int ix = (int)fmod(sp.x, 4.0);
                int iy = (int)fmod(sp.y, 4.0);
                clip(_Fade - _Bayer4x4[iy * 4 + ix]);

                float3 n = normalize(IN.worldNormal);
                float3 blend = abs(n);
                blend /= max(blend.x + blend.y + blend.z, 1e-5);
                half3 albedo = SampleTriplanar(IN.worldPos, blend, _Tiling) * _BaseColor.rgb;

                Light mainLight = GetMainLight();
                float ndl = saturate(dot(n, mainLight.direction));
                half3 ambient = SampleSH(n);
                half3 col = albedo * (mainLight.color * ndl + ambient);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
