Shader "COSMA/EarthNightLightsURP"
{
    Properties
    {
        _NightMap ("Night Map", 2D) = "black" {}
        _LightColor ("Light Color", Color) = (1.0, 0.72, 0.42, 1.0)
        _Intensity ("Intensity", Range(0.0, 4.0)) = 0.0
        _NightFalloff ("Night Falloff", Range(0.5, 10.0)) = 3.4
        _RimBoost ("Rim Boost", Range(0.0, 2.0)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+10"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "EarthNightLights"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NightMap);
            SAMPLER(sampler_NightMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _NightMap_ST;
                float4 _LightColor;
                half _Intensity;
                half _NightFalloff;
                half _RimBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 viewDirWS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _NightMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                Light mainLight = GetMainLight();

                half4 nightTex = SAMPLE_TEXTURE2D(_NightMap, sampler_NightMap, input.uv);
                half rawMask = saturate(dot(nightTex.rgb, half3(0.2126h, 0.7152h, 0.0722h)));
                half texMask = rawMask * rawMask;
                half lightDot = dot(normalWS, mainLight.direction);
                half nightGate = smoothstep(0.08h, 0.52h, -lightDot);
                half nightSide = pow(nightGate, max(0.05h, _NightFalloff));
                half rim = pow(saturate(1.0h - dot(normalWS, viewDirWS)), 4.0h) * _RimBoost * nightSide;
                half intensity = saturate(texMask * nightSide + texMask * rim) * _Intensity;

                return half4(_LightColor.rgb * intensity, intensity);
            }
            ENDHLSL
        }
    }
}
