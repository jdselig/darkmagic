Shader "UI/DarkMagicTransition"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Cutoff ("Cutoff", Range(0,1)) = 0
        _Softness ("Softness", Range(0,0.5)) = 0.08
        _Mode ("Mode", Float) = 0 // 0=Diagonal, 1=PixelDissolve
        _PixelScale ("Pixel Scale", Float) = 120
        [HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [HideInInspector] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Cutoff;
            float _Softness;
            float _Mode;
            float _PixelScale;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(o.worldPos);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UI clipping support
                half4 col = i.color;
                col *= tex2D(_MainTex, i.uv);

                // Modes:
                // 0 Diagonal wipe (top-left to bottom-right)
                // 1 Pixel dissolve (random threshold on pixelated grid)

                float alphaMask = 0;

                if (_Mode < 0.5)
                {
                    // Diagonal wipe with aspect correction (consistent angle across aspect ratios)
                    float2 uv = i.uv;
                    float aspect = _ScreenParams.x / max(1.0, _ScreenParams.y);
                    uv.x *= aspect;

                    // Normalize so t stays in [0,1]
                    float t = (uv.x + uv.y) / (aspect + 1.0);

                    float edge0 = _Cutoff - _Softness;
                    float edge1 = _Cutoff + _Softness;
                    alphaMask = smoothstep(edge0, edge1, t);
                }
                else
                {
                    float scale = max(1.0, _PixelScale);
                    float2 p = floor(i.uv * scale) / scale;
                    float r = hash21(p);
                    alphaMask = step(r, _Cutoff);
                }

                col.a *= alphaMask;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                return col;
            }
            ENDCG
        }
    }
}