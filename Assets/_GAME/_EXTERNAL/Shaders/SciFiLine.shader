Shader "Custom/SciFiLine"
{
    Properties
    {
        [HDR] _BaseColor ("Base Color", Color) = (1.0, 0.4, 0.0, 1.0) // Оранжевый
        [HDR] _CoreColor ("Core Color", Color) = (1.0, 1.0, 0.6, 1.0) // Желто-белый центр
        _PulseSpeed ("Pulse Speed", Float) = 2.0
        _PulseFrequency ("Pulse Frequency", Float) = 5.0
        _PulseSharpness ("Pulse Sharpness", Range(0.01, 10)) = 3.0
        _EdgeSoftness ("Edge Softness", Range(0.01, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
        }
        
        // Отключаем запись в буфер глубины (чтобы прозрачные объекты не перекрывали друг друга неправильно)
        ZWrite Off
        Cull Off
        Fog { Mode Off }

        // Аддитивный блендинг — идеален для огня, энергии и свечения
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Поддержка Vertex Colors из LineRenderer
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _BaseColor;
            float4 _CoreColor;
            float _PulseSpeed;
            float _PulseFrequency;
            float _PulseSharpness;
            float _EdgeSoftness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Формируем свечение поперек линии (по оси V)
                // i.uv.y равен 0 на краях и 1 в центре (стандартный LineRenderer)
                float v = i.uv.y;
                
                // Мягкое затухание к краям
                float edgeGlow = smoothstep(0.0, _EdgeSoftness, v) * smoothstep(1.0, 1.0 - _EdgeSoftness, v);
                // Острый яркий центр
                float core = smoothstep(0.4, 0.5, v) * smoothstep(0.6, 0.5, v);
                
                // 2. Формируем бегущий импульс вдоль линии (по оси U)
                // Время умножаем на скорость
                float flow = i.uv.x * _PulseFrequency - _Time.y * _PulseSpeed;
                // Делаем импульс острым с помощью pow
                float pulse = pow(abs(sin(flow)), _PulseSharpness);
                
                // 3. Смешиваем цвета
                // Базовый цвет линии + яркий центр
                float3 color = lerp(_BaseColor.rgb, _CoreColor.rgb, core);
                // Добавляем цвет импульса (он делает линию ярче в местах "пульса")
                color += _CoreColor.rgb * pulse;
                
                // 4. Итоговая прозрачность
                // Учитываем мягкие края, центр, импульс и цвет вершин LineRenderer
                float alpha = (edgeGlow + core * 2.0 + pulse * 0.5) * i.color.a;
                
                return fixed4(color, alpha);
            }
            ENDCG
        }
    }
}