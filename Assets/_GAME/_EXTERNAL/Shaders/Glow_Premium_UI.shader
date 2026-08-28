Shader "UI/Glow_Premium"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        [HDR] _CoreColor ("Core Color (Inner)", Color) = (1, 1, 1, 1)
        _BoxSize ("Box Size (XY)", Vector) = (0.8, 0.8, 0, 0)
        _Roundness ("Corner Roundness", Range(0, 0.5)) = 0.2
        _GlowSpread ("Glow Spread", Range(0.01, 5.0)) = 0.5
        _GlowPower ("Glow Intensity", Range(0.1, 10.0)) = 2.0
        
        _EdgeSoftness ("Edge Anti-Aliasing", Range(0.001, 0.1)) = 0.01
        _VerticalGradient ("Vertical Gradient", Range(0, 1)) = 0.5
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.0
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.1
        
        // Настройки шума (Энергии/Плазмы)
        _NoiseScale ("Noise Scale", Range(1, 50)) = 15.0
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 1.0
        _NoiseAmount ("Noise Distortion Amount", Range(0, 0.5)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        
        // Аддитивный блендинг для свечения
        Blend SrcAlpha One

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float4 color    : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _GlowColor;
            float4 _CoreColor;
            float4 _BoxSize;
            float _Roundness;
            float _GlowSpread;
            float _GlowPower;
            float _EdgeSoftness;
            float _VerticalGradient;
            float _PulseSpeed;
            float _PulseAmount;
            
            // Переменные шума
            float _NoiseScale;
            float _NoiseSpeed;
            float _NoiseAmount;
            
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            // SDF функция
            float sdRoundBox(float2 p, float2 b, float r)
            {
                p = p - 0.5;
                b = b * 0.5;
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            // --- ФУНКЦИИ ШУМА ---
            // Хэш-функция для генерации псевдослучайных чисел
            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 2D Value Noise (Плавный шум)
            float noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                
                // Сглаживание (Hermite curve)
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            // -------------------

            fixed4 frag(v2f IN) : SV_Target
            {
                // Базовая пульсация
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                
                // 1. Базовое расстояние (для ровного внутреннего бокса)
                float baseDist = sdRoundBox(IN.texcoord, _BoxSize.xy, _Roundness);
                
                // 2. Генерируем движущийся шум
                // Умножаем UV на масштаб, прибавляем время * скорость
                float2 noiseUV = IN.texcoord * _NoiseScale + float2(_Time.y * _NoiseSpeed, _Time.y * _NoiseSpeed * 0.5);
                float n = noise(noiseUV); // от 0 до 1
                
                // 3. ИСКАЖАЕМ расстояние только для свечения!
                // Смещаем границу свечения вперед/назад в зависимости от шума
                float glowDist = baseDist - (n - 0.5) * _NoiseAmount;
                
                // Считаем свечение по ИСКАЖЕННОМУ расстоянию + добавляем мерцание интенсивности (flicker)
                float glow = exp(-max(glowDist, 0.0) * (10.0 / _GlowSpread)) * _GlowPower * pulse;
                glow *= (0.7 + n * 0.6); // Шум также делает свечение то ярче, то слабее
                
                // 4. Внутренний бокс (использует baseDist, поэтому остается ИДЕАЛЬНО РОВНЫМ)
                float boxShape = smoothstep(_EdgeSoftness, 0.0, baseDist); 
                
                // 5. Градиент для объема
                float gradient = lerp(1.0 - _VerticalGradient, 1.0 + _VerticalGradient, IN.texcoord.y);
                gradient = saturate(gradient);
                
                // 6. Смешивание цветов
                float3 finalColor = _GlowColor.rgb * glow + _CoreColor.rgb * boxShape * gradient;
                
                // 7. Альфа-канал
                float alpha = saturate(boxShape + glow);
                alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                alpha *= IN.color.a;

                return fixed4(finalColor * alpha, alpha);
            }
            ENDCG
        }
    }
}