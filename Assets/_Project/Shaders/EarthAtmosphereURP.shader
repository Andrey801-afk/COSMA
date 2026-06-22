Shader "COSMA/EarthAtmosphereURP"
{
    Properties
    {
        _AtmosphereColor ("Atmosphere Color", Color) = (0.36, 0.66, 1.0, 1.0)
        _SunTint ("Sun Tint", Color) = (0.96, 0.98, 1.0, 1.0)
        _NightTint ("Night Tint", Color) = (0.02, 0.06, 0.14, 1.0)
        _Intensity ("Intensity", Range(0.0, 4.0)) = 1.35
        _Alpha ("Alpha", Range(0.0, 2.0)) = 1.0
        _RimPower ("Rim Power", Range(0.5, 10.0)) = 4.8
        _SunBoost ("Sun Boost", Range(0.0, 4.0)) = 1.4
        _TerminatorBoost ("Terminator Boost", Range(0.0, 4.0)) = 1.8
        _NightBoost ("Night Boost", Range(0.0, 1.0)) = 0.08
        _NightPower ("Night Power", Range(0.5, 6.0)) = 2.4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+35"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "EarthAtmosphere"
            Tags { "LightMode" = "UniversalForward" }

            Blend One OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _AtmosphereColor;
                float4 _SunTint;
                float4 _NightTint;
                half _Intensity;
                half _Alpha;
                half _RimPower;
                half _SunBoost;
                half _TerminatorBoost;
                half _NightBoost;
                half _NightPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half3 normalWS : TEXCOORD0;
                half3 viewDirWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = SafeNormalize(input.normalWS);
                half3 viewDirWS = SafeNormalize(input.viewDirWS);
                Light mainLight = GetMainLight();
                half3 lightDirWS = SafeNormalize(mainLight.direction);
                half3 lightColor = max(mainLight.color, half3(0.35h, 0.35h, 0.35h));

                half ndv = saturate(dot(normalWS, viewDirWS));
                half rimBase = saturate(1.0h - ndv);
                half rim = pow(rimBase, max(0.05h, _RimPower));
                rim = saturate(rim * rim * 2.4h);

                // Hide the atmosphere on the front-facing center of the planet and keep it on the limb.
                half limbMask = smoothstep(0.08h, 0.42h, rimBase);
                rim *= limbMask;

                half lightDot = dot(normalWS, lightDirWS);
                half daySide = saturate(lightDot);
                half nightSide = saturate(-lightDot);
                half twilightBand = pow(saturate(1.0h - abs(lightDot)), 2.35h);
                half twilightScatter = rim * twilightBand * _Intensity * _TerminatorBoost;

                // Forward scattering gives the bright thin glow near the lit horizon.
                half forwardScatter = pow(saturate(dot(viewDirWS, lightDirWS) * 0.5h + 0.5h), 6.5h);
                half rayleighScatter = rim * _Intensity * (0.12h + daySide * 0.9h);
                half mieScatter = rim * _Intensity * forwardScatter * _SunBoost * (0.15h + daySide);
                half nightScatter = rim * _Intensity * pow(nightSide, max(0.05h, _NightPower)) * _NightBoost;

                half3 dayColor = _AtmosphereColor.rgb * rayleighScatter * lightColor;
                half3 sunColor = _SunTint.rgb * mieScatter * lightColor;
                half3 twilightColor = lerp(_SunTint.rgb, _AtmosphereColor.rgb, 0.65h) * twilightScatter * lightColor;
                half3 nightColor = _NightTint.rgb * nightScatter;

                half3 finalColor = dayColor + sunColor + twilightColor + nightColor;
                half alpha = saturate(
                    (rayleighScatter * 0.22h +
                     mieScatter * 0.16h +
                     twilightScatter * 0.55h +
                     nightScatter * 0.14h) * _Alpha);

                return half4(finalColor * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
