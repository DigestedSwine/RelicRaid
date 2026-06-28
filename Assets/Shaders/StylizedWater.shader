// Stylized translucent water: gentle vertex waves, fresnel-brighter edges, a subtle scrolling sparkle.
// Cheap and unlit-ish (mobile-friendly). Used on the creek ribbon + the water-edge plane.
Shader "RelicRaid/StylizedWater"
{
    Properties
    {
        _DeepColor ("Deep Color", Color) = (0.05,0.20,0.34,1)
        _ShallowColor ("Shallow Color", Color) = (0.28,0.56,0.70,1)
        _Alpha ("Alpha", Range(0,1)) = 0.78
        _WaveAmp ("Wave Amplitude", Float) = 0.06
        _WaveFreq ("Wave Frequency", Float) = 0.6
        _WaveSpeed ("Wave Speed", Float) = 1.2
        _SparkleScale ("Sparkle Scale", Float) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _DeepColor, _ShallowColor;
                float _Alpha, _WaveAmp, _WaveFreq, _WaveSpeed, _SparkleScale;
            CBUFFER_END

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings   { float4 positionHCS:SV_POSITION; float3 worldPos:TEXCOORD0; float3 worldNormal:TEXCOORD1; };

            Varyings vert (Attributes IN)
            {
                Varyings o;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);
                float t = _Time.y * _WaveSpeed;
                wp.y += sin(wp.x * _WaveFreq + t) * _WaveAmp + cos(wp.z * _WaveFreq * 1.3 + t * 0.8) * _WaveAmp;
                o.worldPos = wp;
                o.positionHCS = TransformWorldToHClip(wp);
                o.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 vdir = normalize(GetWorldSpaceViewDir(IN.worldPos));
                float3 n = normalize(IN.worldNormal);
                float fres = pow(1.0 - saturate(dot(n, vdir)), 3.0);
                half3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, fres);

                float a = saturate(_Alpha + fres * 0.2);
                return half4(col, a);
            }
            ENDHLSL
        }
    }
}
