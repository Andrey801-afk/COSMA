Shader "COSMA/EarthCloudLayerURP"
{
    Properties
    {
        _CloudTex ("Cloud Texture", 2D) = "white" {}
        _CloudColor ("Cloud Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _Opacity ("Opacity", Range(0.0, 1.0)) = 0.84
        _PanSpeed ("Pan Speed", Range(-0.2, 0.2)) = 0.005
        _Coverage ("Coverage", Range(0.0, 1.0)) = 0.18
        _Softness ("Softness", Range(0.01, 0.6)) = 0.22
        _RimPower ("Rim Power", Range(0.5, 10.0)) = 5.6
        _SunHighlight ("Sun Highlight", Range(0.0, 4.0)) = 1.42
        _NightVisibility ("Night Visibility", Range(0.0, 0.3)) = 0.03
        _TwilightBoost ("Twilight Boost", Range(0.0, 1.5)) = 0.2
        _DetailStrength ("Detail Strength", Range(0.0, 1.0)) = 0.24
        _ReliefStrength ("Relief Strength", Range(0.0, 4.0)) = 1.7
        _VortexStrength ("Vortex Strength", Range(0.0, 1.0)) = 0.0
        _VortexScale ("Vortex Scale", Range(0.4, 2.0)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+20"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "EarthCloudLayer"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_CloudTex);
            SAMPLER(sampler_CloudTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _CloudTex_ST;
                float4 _CloudColor;
                half _Opacity;
                half _PanSpeed;
                half _Coverage;
                half _Softness;
                half _RimPower;
                half _SunHighlight;
                half _NightVisibility;
                half _TwilightBoost;
                half _DetailStrength;
                half _ReliefStrength;
                half _VortexStrength;
                half _VortexScale;
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

            float2 RotateUv(float2 value, float angle)
            {
                float sine = sin(angle);
                float cosine = cos(angle);
                return float2(
                    value.x * cosine - value.y * sine,
                    value.x * sine + value.y * cosine);
            }

            float2 WrappedDelta(float2 uv, float2 center)
            {
                return frac(uv - center + 0.5) - 0.5;
            }

            float2 SwirlUv(float2 uv, float2 center, float radius, float spin, float strength)
            {
                float2 delta = WrappedDelta(uv, center);
                float2 stretchedDelta = delta;
                stretchedDelta.x *= 1.35;

                float distanceToCenter = length(stretchedDelta);
                float falloff = saturate(1.0 - distanceToCenter / radius);
                falloff = falloff * falloff * (3.0 - 2.0 * falloff);

                float2 rotatedDelta = RotateUv(stretchedDelta, spin * strength * falloff);
                rotatedDelta.x /= 1.35;

                return uv + (rotatedDelta - delta) * falloff;
            }

            float2 ApplyVortexField(float2 uv)
            {
                float scale = max(0.4, _VortexScale);
                float strength = _VortexStrength;

                uv = SwirlUv(uv, float2(0.26, 0.66), 0.27 * scale, 5.4, strength);
                uv = SwirlUv(uv, float2(0.58, 0.42), 0.23 * scale, -5.9, strength * 0.9);
                uv = SwirlUv(uv, float2(0.78, 0.72), 0.18 * scale, 4.8, strength * 0.76);

                return uv;
            }

            half VortexWisp(float2 uv, float2 center, float radius, float turns, float spin, float phase)
            {
                float2 delta = WrappedDelta(uv, center);
                delta.x *= 1.35;

                float distanceToCenter = length(delta);
                float inside = saturate(1.0 - distanceToCenter / radius);
                float angle = atan2(delta.y, delta.x);
                float spiral = sin(angle * 3.0 + distanceToCenter * turns + phase + _Time.y * _PanSpeed * spin);

                half armMask = smoothstep(0.12h, 0.92h, (half)spiral);
                half radialFade = (half)(inside * inside * (3.0 - 2.0 * inside));
                half eyeFade = smoothstep(0.025h, 0.08h, (half)distanceToCenter);

                return armMask * radialFade * eyeFade;
            }

            half BuildVortexWisps(float2 uv)
            {
                half wispA = VortexWisp(uv, float2(0.26, 0.66), 0.27 * _VortexScale, 34.0, 10.0, 0.4);
                half wispB = VortexWisp(uv, float2(0.58, 0.42), 0.23 * _VortexScale, 40.0, -8.5, 2.1);
                half wispC = VortexWisp(uv, float2(0.78, 0.72), 0.18 * _VortexScale, 36.0, 7.0, 4.3);

                return max(wispA, max(wispB, wispC)) * _VortexStrength;
            }

            half BuildLatitudeMoisture(float2 uv)
            {
                half equatorialBand = 1.0h - saturate(abs((half)uv.y - 0.50h) * 4.4h);
                half northFront = 1.0h - saturate(abs((half)uv.y - 0.68h) * 5.2h);
                half southFront = 1.0h - saturate(abs((half)uv.y - 0.31h) * 5.0h);
                half polarFade = smoothstep(0.04h, 0.16h, (half)uv.y) * (1.0h - smoothstep(0.82h, 0.96h, (half)uv.y));

                return saturate((0.72h + equatorialBand * 0.12h + northFront * 0.18h + southFront * 0.16h) * polarFade);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _CloudTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                half3 viewDirWS = normalize(input.viewDirWS);
                Light mainLight = GetMainLight();

                float2 cloudUv = input.uv + float2(_Time.y * _PanSpeed, _Time.y * _PanSpeed * 0.18);
                float2 softUv = input.uv + float2(-_Time.y * _PanSpeed * 0.16, _Time.y * _PanSpeed * 0.08);
                float2 breakUv = cloudUv + float2(0.0035, -0.0025);

                half cloudBase = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloudUv).r;
                half cloudSoft = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, softUv).r;
                half cloudBreak = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, breakUv).r;
                half cloudSample = saturate(
                    cloudBase * 0.82h +
                    cloudSoft * 0.22h +
                    (cloudBreak - 0.5h) * _DetailStrength * 0.12h);
                cloudSample = saturate(cloudSample * 1.18h);
                half cloudMask = smoothstep(_Coverage, saturate(_Coverage + _Softness), cloudSample);
                half cloudCore = smoothstep(saturate(_Coverage + _Softness * 0.6h), saturate(_Coverage + _Softness + 0.22h), cloudSample);

                float2 reliefOffset = float2(0.0024, 0.0017);
                half sampleEast = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloudUv + reliefOffset).r;
                half sampleNorth = SAMPLE_TEXTURE2D(_CloudTex, sampler_CloudTex, cloudUv + reliefOffset.yx).r;
                half relief = saturate(0.74h + (cloudSample - sampleEast) * _ReliefStrength + (cloudSample - sampleNorth) * (_ReliefStrength * 0.62h));

                half lightDot = dot(normalWS, mainLight.direction);
                half daylight = smoothstep(-0.08h, 0.28h, lightDot);
                half twilight = pow(saturate(1.0h - abs(lightDot)), 4.0h) * _TwilightBoost;
                half lighting = saturate(lerp(_NightVisibility, 1.0h, daylight) + twilight);
                half rim = pow(saturate(1.0h - dot(normalWS, viewDirWS)), max(0.05h, _RimPower));
                half sunFacing = pow(saturate(lightDot), 2.2h);
                half cloudDensity = saturate(cloudMask * (0.44h + cloudCore * 0.78h));
                half cloudSelfShadow = lerp(0.76h, 1.18h, relief) * lerp(0.82h, 1.05h, daylight);
                half3 baseClouds = _CloudColor.rgb * cloudDensity * lighting * cloudSelfShadow;
                half3 silverLining = _CloudColor.rgb * rim * sunFacing * _SunHighlight * cloudDensity;
                half alpha = saturate((cloudMask * 0.42h + cloudCore * 0.82h) * _Opacity + rim * sunFacing * 0.08h);

                return half4(baseClouds + silverLining, alpha);
            }
            ENDHLSL
        }
    }
}
