Shader "LuckyLeprechaun/UI_TextureScroll_AAA"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Управление из C#
        _ScrollY ("Scroll Speed Y", Float) = 0.0
        
        // Настройки AAA эффектов (можно подкрутить в инспекторе материала)
        _BlurStrength ("Blur Strength", Range(0, 0.05)) = 0.02
        _CylinderBending ("Cylinder Bend Intensity", Range(0, 1)) = 0.3
        _ChromaticAberration ("Chromatic Aberration", Range(0, 0.03)) = 0.01
        
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Write Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_INRECT
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _ClipRect;
            
            float _ScrollY;
            float _BlurStrength;
            float _CylinderBending;
            float _ChromaticAberration;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Считаем текущую скорость (интенсивность эффектов зависит от неё)
                float currentSpeed = abs(_ScrollY);
                
                // --- 1. ЭФФЕКТ ЦИЛИНДРА (3D Bending) ---
                // Искривляем UV.y, чтобы создать иллюзию объема барабана
                float uvYNormalized = i.uv.y * 2.0 - 1.0; // перевод в диапазон [-1, 1]
                float bend = asin(uvYNormalized * _CylinderBending) / _CylinderBending;
                float finalUV_Y = (bend + 1.0) * 0.5;
                
                // Базовый скроллинг во времени
                float2 baseUV = float2(i.uv.x, finalUV_Y + _ScrollY * _Time.y);

                // --- 2. ДИНАМИЧЕСКИЙ MOTION BLUR И ХРОМАТИЧЕСКАЯ АБЕРРАЦИЯ ---
                // Сила размытия и искажения цвета масштабируется от скорости барабана
                float blurFactor = _BlurStrength * currentSpeed;
                float chromFactor = _ChromaticAberration * currentSpeed;

                // Для AAA Motion Blur нам нужно сделать несколько выборок (Samples) текстуры со сдвигом
                fixed4 col = fixed4(0, 0, 0, 0);
                
                // Делаем 5 умных выборок со смещением по вертикали + расщепляем каналы (RGB) для аберрации
                float offsets[5] = {-2.0, -1.0, 0.0, 1.0, 2.0};
                float weights[5] = {0.1, 0.25, 0.3, 0.25, 0.1}; // Гауссово распределение весов для мягкости
                
                for (int s = 0; s < 5; s++)
                {
                    float yOffset = offsets[s] * blurFactor * 0.1;
                    float chromOffset = offsets[s] * chromFactor * 0.05;
                    
                    // Расщепляем каналы: Красный берем чуть выше, Синий чуть ниже, Зеленый по центру
                    float r = tex2D(_MainTex, baseUV + float2(0.0, yOffset + chromOffset)).r;
                    float g = tex2D(_MainTex, baseUV + float2(0.0, yOffset)).g;
                    float b = tex2D(_MainTex, baseUV + float2(0.0, yOffset - chromOffset)).b;
                    float a = tex2D(_MainTex, baseUV + float2(0.0, yOffset)).a;
                    
                    col += fixed4(r, g, b, a) * weights[s];
                }

                col *= i.color;

                // Загрузка UI-клиппинга (ScrollRect / Mask)
                // #ifdef UNITY_UI_CLIP_INRECT
                // col.a *= UnityGet2DClippedAlpha(i.worldPosition.xy, _ClipRect);
                // #endif

                return col;
            }
            ENDCG
        }
    }
}