Shader "UI/Glass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Glass Tint", Color) = (0.55, 0.35, 0.2, 1)

        [Toggle(_BLUR_ON)] _UseBlur ("Liquid Glass (sample background)", Float) = 1
        [NoScaleOffset] _BgTex ("Background Texture", 2D) = "black" {}
        _BgTransform ("BG Transform (xy=scale, zw=offset)", Vector) = (1,1,0,0)
        [Toggle(_FLIP_BG_Y)] _FlipBgY ("Flip Background Y", Float) = 0

        _BlurSize ("Blur Size (px)", Range(0,100)) = 6
        _Distortion ("Liquid Distortion", Range(0,0.05)) = 0.012
        _FlowSpeed ("Flow Speed", Range(0,1)) = 0.15
        _EdgeRefraction ("Edge Lens Refraction", Range(0,0.05)) = 0.015
        _EdgeLensWidth ("Edge Lens Width", Range(0.02,0.4)) = 0.15

        _Opacity ("Master Opacity (fake mode)", Range(0,1)) = 0.55
        _TintAmount ("Tint Amount", Range(0,1)) = 0.45
        _GradientStrength ("Gradient Strength", Range(0,0.3)) = 0.06

        _BorderColor ("Edge Color", Color) = (1, 0.65, 0.25, 0.9)
        _BorderWidth ("Edge Width", Range(0.001,0.05)) = 0.006
        _HighlightIntensity ("Top Edge Intensity", Range(0,2)) = 0.7
        _HighlightWidth ("Top Edge Width", Range(0.01,0.5)) = 0.12

        _NoiseStrength ("Frost Strength", Range(0,0.3)) = 0.02
        _NoiseScale ("Noise Scale", Range(1,512)) = 8

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
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i),               Hash21(i + float2(1,0)), u.x),
                    lerp(Hash21(i + float2(0,1)), Hash21(i + float2(1,1)), u.x), u.y);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half shape = tex.a * IN.color.a;

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;

                float2 noiseUV = float2(screenUV.x * (_ScreenParams.x / _ScreenParams.y), screenUV.y);

                // «Текучий» шум: два слоя, дрейфуют во времени
                float t = _Time.y * _FlowSpeed;
                float nA = ValueNoise(noiseUV * _NoiseScale + float2(t, t * 0.7));
                float nB = ValueNoise(noiseUV * _NoiseScale * 1.3 + float2(-t * 0.6, t) + 5.3);

                // Геометрия краёв: тонкая линия + широкая «линза»
                float d = min(min(IN.uv.x, 1.0 - IN.uv.x), min(IN.uv.y, 1.0 - IN.uv.y));
                float edgeLine = 1.0 - smoothstep(0.0, _BorderWidth, d);
                float rim  = 1.0 - smoothstep(0.0, _EdgeLensWidth, d);
                float2 toCenter = normalize(float2(0.5, 0.5) - IN.uv + 1e-5);

                float3 col; half alpha;

                #if defined(_BLUR_ON)
                    float2 bgUV = screenUV * _BgTransform.xy + _BgTransform.zw;
                    #if defined(_FLIP_BG_Y)
                        bgUV.y = 1.0 - bgUV.y;
                    #endif

                    // Жидкое преломление + краевая линза
                    bgUV += (float2(nA, nB) - 0.5) * _Distortion;
                    bgUV += toCenter * rim * _EdgeRefraction;

                    // Неравномерный фрост-блюр, 9 тапов
                    float radius = (_BlurSize / _ScreenParams.y) * (0.75 + 0.5 * nA);
                    float3 acc = 0;
                    [unroll]
                    for (int i = 0; i < 9; i++)
                        acc += SAMPLE_TEXTURE2D(_BgTex, sampler_BgTex, bgUV + POISSON[i] * radius).rgb;
                    col = acc / 9.0;

                    // Тонированное стекло
                    col = lerp(col, col * _Color.rgb * 2.0, _TintAmount);
                    alpha = shape;
                #else
                    col = _Color.rgb * 0.25;
                    alpha = shape * _Opacity;
                #endif

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