Shader "UI/Glass_V2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Glass Tint", Color) = (0.55, 0.35, 0.2, 1)

        [Toggle(_BLUR_ON)] _UseBlur ("Liquid Glass (sample background)", Float) = 1
        [NoScaleOffset] _BgTex ("Background Texture", 2D) = "black" {}
        _BgTransform ("BG Transform (xy=scale, zw=offset)", Vector) = (1,1,0,0)
        [Toggle(_FLIP_BG_Y)] _FlipBgY ("Flip Background Y", Float) = 0

        _BlurSize ("Blur Size", Range(0, 20)) = 0.006
        _Distortion ("Liquid Distortion", Range(0,0.05)) = 0.012
        _FlowSpeed ("Flow Speed", Range(0,1)) = 0.15
        _EdgeRefraction ("Edge Lens Refraction", Range(0,0.05)) = 0.015
        _EdgeLensWidth ("Edge Lens Width", Range(0.02,0.4)) = 0.15

        _BevelWidth ("Bevel Width", Range(0.01,0.3)) = 0.1
        _BevelStrength ("Bevel Strength", Range(0,2)) = 1.0
        _ChromAberration ("Chromatic Aberration", Range(0, 0.005)) = 0.0015
        _SpecularIntensity ("Specular Intensity", Range(0, 3)) = 0.8
        _SpecularPower ("Specular Sharpness", Range(1, 128)) = 32.0

        _Opacity ("Master Opacity (fake mode)", Range(0,1)) = 0.55
        _TintAmount ("Tint Amount", Range(0,1)) = 0.45
        _GradientStrength ("Gradient Strength", Range(0,0.3)) = 0.06

        _BorderColor ("Edge Color", Color) = (1, 0.65, 0.25, 0.9)
        _BorderWidth ("Edge Width", Range(0.001,0.05)) = 0.006
        _HighlightIntensity ("Top Edge Intensity", Range(0,2)) = 0.7
        _HighlightWidth ("Top Edge Width", Range(0.01,0.5)) = 0.12

        _NoiseStrength ("Frost Strength", Range(0,0.3)) = 0.02
        _NoiseScale ("Noise Scale", Range(1,512)) = 12

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
                "CanUseSpriteAtlas"="True" "PreviewType"="Plane" }

        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp]
                  ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }

        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Glass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _BLUR_ON
            #pragma shader_feature_local _FLIP_BG_Y

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float4 screenPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_BgTex);   SAMPLER(sampler_BgTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; float4 _Color;
                float4 _BgTransform;
                float _UseBlur; float _FlipBgY;
                float _BlurSize; float _Distortion; float _FlowSpeed;
                float _EdgeRefraction; float _EdgeLensWidth;
                float _BevelWidth; float _BevelStrength;
                float _ChromAberration; float _SpecularIntensity; float _SpecularPower;
                float _Opacity; float _TintAmount; float _GradientStrength;
                float4 _BorderColor; float _BorderWidth;
                float _HighlightIntensity; float _HighlightWidth;
                float _NoiseStrength; float _NoiseScale;
            CBUFFER_END

            static const float2 POISSON[9] = {
                float2( 0.0,  0.0),
                float2( 0.52, 0.28), float2(-0.35, 0.61),
                float2(-0.62,-0.42), float2( 0.68,-0.51),
                float2( 0.22, 0.75), float2(-0.75, 0.18),
                float2( 0.15,-0.72), float2(-0.18,-0.15)
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.screenPos = ComputeScreenPos(OUT.positionCS);
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            float2 CalculateNoiseGradient(float2 uv)
            {
                // Маленький шаг.
                // Подбирается относительно масштаба шума.
                float e = 0.01;

                float t = _Time.y * _FlowSpeed;

                float hL = ValueNoise(
                    (uv - float2(e, 0)) * _NoiseScale +
                    float2(t, t * 0.7)
                );

                float hR = ValueNoise(
                    (uv + float2(e, 0)) * _NoiseScale +
                    float2(t, t * 0.7)
                );

                float hD = ValueNoise(
                    (uv - float2(0, e)) * _NoiseScale +
                    float2(t, t * 0.7)
                );

                float hU = ValueNoise(
                    (uv + float2(0, e)) * _NoiseScale +
                    float2(t, t * 0.7)
                );

                float dx = (hR - hL) / (2.0 * e);
                float dy = (hU - hD) / (2.0 * e);

                return float2(dx, dy);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half shape = tex.a * IN.color.a;

                // Экранные координаты с учетом соотношения сторон
                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 noiseUV = float2(screenUV.x * aspect, screenUV.y);

                // «Текучий» шум: два слоя, дрейфуют во времени
                float t = _Time.y * _FlowSpeed;
                float nA = ValueNoise(noiseUV * _NoiseScale + float2(t, t * 0.7));
                float nB = ValueNoise(noiseUV * _NoiseScale * 1.3 + float2(-t * 0.6, t) + 5.3);
                
                // 1. Карта высот шума для генерации нормалей
                float noiseHeight = (nA + nB) * 0.5;

                float2 gradient = CalculateNoiseGradient(noiseUV);

                float3 glassNormal = normalize(float3(
                    -gradient.x * 0.03,
                    -gradient.y * 0.03,
                    1.0
                ));

                // Геометрия краёв: тонкая линия + широкая «линза»
                float d = min(min(IN.uv.x, 1.0 - IN.uv.x), min(IN.uv.y, 1.0 - IN.uv.y));
                float edgeLine = 1.0 - smoothstep(0.0, _BorderWidth, d);
                float rim  = 1.0 - smoothstep(0.0, _EdgeLensWidth, d);
                float2 toCenter = normalize(float2(0.5, 0.5) - IN.uv + 1e-5);

                // 2. Эффект тиснения (Bevel)
                float bevelHeight = 1.0 - smoothstep(0.0, _BevelWidth, d);
                float3 bevelNormal = normalize(float3(
                    -ddx(bevelHeight) * 50.0, 
                    -ddy(bevelHeight) * 50.0, 
                    1.0
                ));

                float3 col; half alpha;

                #if defined(_BLUR_ON)
                    float2 bgUV = screenUV * _BgTransform.xy + _BgTransform.zw;
                    
                    // 3. Преломление через нормаль шума + краевая линза
                    bgUV += glassNormal.xy * _Distortion;
                    bgUV += toCenter * rim * _EdgeRefraction;

                    #if defined(_FLIP_BG_Y)
                        bgUV.y = 1.0 - bgUV.y;
                    #endif

                    // Неравномерный фрост-блюр с хроматической аберрацией (9 тапов)
                    float radius = _BlurSize * (0.75 + 0.5 * nA);
                    float2 aspectOffset = float2(1.0 / aspect, 1.0); // Коррекция кругового блюра
                    
                    float3 acc = 0;
                    [unroll]
                    for (int i = 0; i < 9; i++)
                    {
                        float2 offset = POISSON[i] * radius * aspectOffset;
                        // Дешевая хроматическая аберрация: смещаем R и B каналы
                        acc.r += SAMPLE_TEXTURE2D(_BgTex, sampler_BgTex, bgUV + offset + float2(_ChromAberration, 0.0)).r;
                        acc.g += SAMPLE_TEXTURE2D(_BgTex, sampler_BgTex, bgUV + offset).g;
                        acc.b += SAMPLE_TEXTURE2D(_BgTex, sampler_BgTex, bgUV + offset - float2(_ChromAberration, 0.0)).b;
                    }
                    col = acc / 9.0;

                    col = lerp(col, col * _Color.rgb * 2.0, _TintAmount);
                    alpha = shape;
                #else
                    col = _Color.rgb * 0.25;
                    alpha = shape * _Opacity;
                #endif

                // 4. Освещение: Диффузный свет для тиснения
                float3 lightDir = normalize(float3(-0.5, 0.7, 0.8)); // Свет падает сверху-слева
                float bevelLight = saturate(dot(bevelNormal, lightDir));
                col += bevelLight * bevelHeight * _BevelStrength * 0.5; // Подсветка скоса
                col -= (1.0 - bevelLight) * bevelHeight * _BevelStrength * 0.2; // Тень на противоположном скосе

                // 5. Освещение: Фальшивый блик (Specular) от микрорельефа (Glass Normal)
                float spec = pow(saturate(dot(glassNormal, lightDir)), _SpecularPower);
                spec *= rim * 2.0; // Усиливаем блик ближе к краям, где преломление заметнее
                col += spec * _SpecularIntensity * 0.5;

                // Свет и матовость
                col += (IN.uv.y - 0.5) * _GradientStrength;
                col += (nA - 0.5) * _NoiseStrength;

                // Стекло продаётся краями
                col += edgeLine * _BorderColor.rgb * _BorderColor.a;
                col += rim  * _BorderColor.rgb * _BorderColor.a * 0.25;
                col += smoothstep(1.0 - _HighlightWidth, 1.0, IN.uv.y)
                       * _HighlightIntensity * _BorderColor.rgb * _BorderColor.a * 0.5;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}