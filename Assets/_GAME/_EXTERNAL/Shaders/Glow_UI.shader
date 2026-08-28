Shader "UI/Glow"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (0, 1, 1, 1)
        _BoxSize ("Inner Box Size", Range(0, 1)) = 0.8
        _Roundness ("Corner Roundness", Range(0, 0.5)) = 0.2
        _GlowSpread ("Glow Spread", Range(0.01, 2.0)) = 0.5
        _GlowPower ("Glow Intensity", Range(0.1, 10.0)) = 2.0
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
        Blend SrcAlpha OneMinusSrcAlpha

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
            float _BoxSize;
            float _Roundness;
            float _GlowSpread;
            float _GlowPower;
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

            // SDF функция для прямоугольника с закругленными углами
            float sdRoundBox(float2 p, float2 b, float r)
            {
                // p - координаты UV (0 до 1), b - половина размера прямоугольника, r - радиус скругления
                // Смещаем координаты в центр (от -1 до 1)
                p = p * 2.0 - 1.0;
                b = b * 2.0 - 1.0;
                
                float2 q = abs(p) - b + r;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Вычисляем расстояние до фигуры
                float dist = sdRoundBox(IN.texcoord, float2(_BoxSize, _BoxSize), _Roundness);
                
                // Используем экспоненциальное затухание для мягкого свечения
                // exp(-dist * spread) дает очень красивый, нелинейный свет
                float glow = exp(-max(dist, 0.0) * (10.0 / _GlowSpread));
                
                // Внутри самой фигуры делаем цвет сплошным
                float boxShape = saturate(-dist * 50.0); 
                
                // Собираем финальный альфа-канал
                float alpha = saturate(boxShape + glow * _GlowPower);
                
                // Применяем обрезку по Rect Mask (чтобы панель резалась внутри Scroll View)
                alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                // Применяем прозрачность канваса (CrossFade, альфа группы и т.д.)
                alpha *= IN.color.a;

                // Микс между цветом свечения и цветом ядра
                float3 finalColor = _GlowColor.rgb * (glow * _GlowPower + boxShape);

                return fixed4(finalColor, alpha);
            }
            ENDCG
        }
    }
}