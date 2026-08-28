Shader "LuckyLeprechaun/GlowLineShader"
{
    Properties
    {
        [Header(Base Settings)]
        _MainTex ("Energy Noise Mask (R)", 2D) = "white" {}
        [HDR] _GlowColor ("Outer Glow Color", Color) = (1, 0.4, 0, 1)
        [HDR] _CoreColor ("Inner Core Color", Color) = (1, 0.95, 0.7, 1)
        
        [Header(Glow Controls)]
        _GlowPower ("Glow Sharpness", Range(0.5, 5)) = 2.0
        _CoreThickness ("Core Line Width", Range(0.01, 0.5)) = 0.1
        _EnergyIntensity ("HDR Boost multiplier", Range(1, 10)) = 4.0

        [Header(Lightning Displacement)]
        _Speed ("Movement Speed", Range(1, 30)) = 12.0
        _Frequency ("Frequency Scale", Range(1, 20)) = 5.0
        _Amplitude ("Displacement Strength", Range(0.0, 0.5)) = 0.15
        
        [Header(Plasma Turbulence)]
        _TurbulenceSpeed ("Turbulence Speed", Vector) = (2.5, -1.5, 0, 0)
        _NoiseScale ("Noise Scale (X, Y)", Vector) = (4.0, 2.0, 0, 0)
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
        }
        LOD 200

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One // Аддитивное смешивание для сочного горения

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; 
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 noiseUV : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _GlowColor;
            float4 _CoreColor;
            float _GlowPower;
            float _CoreThickness;
            float _EnergyIntensity;
            
            float _Speed;
            float _Frequency;
            float _Amplitude;
            float4 _TurbulenceSpeed;
            float4 _NoiseScale;

            // AAA Сглаженный градиентный шум (Smooth Noise 1D) вместо ступенчатого floor()
            float hash(float n) { return frac(sin(n) * 43758.5453123); }
            
            float snoise(float x)
            {
                float p = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f); // Сглаживание Эрмита
                return lerp(hash(p), hash(p + 1.0), f);
            }

            // Фрактальный шум (FBM) из 3-х октав для симуляции хаотичной структуры молнии
            float fbm(float x)
            {
                float v = 0.0;
                float a = 0.5;
                float shift = 100.0;
                for (int i = 0; i < 3; ++i)
                {
                    v += a * snoise(x);
                    x = x * 2.0 + shift;
                    a *= 0.5;
                }
                return v;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                float time = _Time.y * _Speed;
                
                // Создаем маску затухания к краям по X, чтобы концы линии не отрывались от вершин
                float edgeMask = sin(v.uv.x * 3.14159);
                
                // Двухволновой расчет смещения геометрии с использованием FBM шума
                float noiseDisplacement = fbm(v.uv.x * _Frequency - time) * 2.0 - 1.0;
                float microCrackle = snoise(v.uv.x * (_Frequency * 3.14) + time * 2.0) * 0.4;
                
                // Плавное искривление по Y
                float finalDisplacement = (noiseDisplacement + microCrackle) * _Amplitude * edgeMask;
                v.vertex.y += finalDisplacement;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv; // Оставляем чистые UV для расчета расстояния до центра луча
                
                // Генерируем UV для бегущей внутренней плазмы текстуры
                o.noiseUV = TRANSFORM_TEX(v.uv, _MainTex);
                
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Базовое расстояние от центра линии (0.5) по вертикали
                float distanceToCenter = abs(i.uv.y - 0.5) * 2.0;
                
                // Читаем текстуру шума в два слоя, бегущих в разные стороны (Plasma Effect)
                float2 uvNoiseA = i.noiseUV * _NoiseScale.xy + _Time.yy * _TurbulenceSpeed.xy;
                float2 uvNoiseB = i.noiseUV * _NoiseScale.xy * 1.5 + _Time.yy * _TurbulenceSpeed.yx * 0.8;
                
                float textureNoise = tex2D(_MainTex, uvNoiseA).r * tex2D(_MainTex, uvNoiseB).r;
                
                // Внедряем текстурный шум прямо в расчет толщины, создавая "рваные" разряды внутри свечения
                float animatedDistance = distanceToCenter + (textureNoise - 0.25) * 0.4;
                
                // Внешнее мягкое свечение
                float glow = 1.0 - saturate(animatedDistance);
                glow = pow(glow, _GlowPower);
                
                // Внутреннее яркое ядро
                float core = 1.0 - saturate(distanceToCenter / _CoreThickness);
                core = pow(core, 4.0) * (textureNoise * 1.5 + 0.5);
                
                // Рандомный высокочастотный сцинтилляционный микро-флэш всего луча (живая энергия)
                float microFlash = snoise(_Time.y * (_Speed * 1.5)) * 0.15 + 0.85;
                
                // ИСПРАВЛЕНО: Применяем Vertex Color (i.color) динамически. 
                // Внешнее свечение берет чистый цвет линии из скрипта, а ядро остается ярким.
                float3 dynamicGlowColor = _GlowColor.rgb * i.color.rgb;
                float3 dynamicCoreColor = _CoreColor.rgb * lerp(i.color.rgb, float3(1,1,1), 0.5);
                
                // Лерпим модифицированные цвета
                float3 finalRGB = lerp(dynamicGlowColor, dynamicCoreColor, core);
                
                // Накачка яркости свечения за счет маски и параметров
                finalRGB *= (glow + core * 2.0) * _EnergyIntensity * microFlash;
                
                // Прозрачность с учетом Vertex Color альфы (из скрипта) и мягких краев по X
                float edgeFadeX = sin(i.uv.x * 3.14159);
                float finalAlpha = saturate(glow + core) * i.color.a * edgeFadeX;

                return float4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
}