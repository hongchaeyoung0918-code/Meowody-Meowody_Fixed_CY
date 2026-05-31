// ──────────────────────────────────────────────────────────────────
// SpriteColorKeeper.shader
// GRIS 스타일 개별 컬러 복원용 스프라이트 셰이더
//
// 동작:
//   - Stencil=1 기록 → 글로벌 그레이스케일 패스가 이 픽셀을 건너뜀
//   - _Saturation (0~1) 으로 개별 채도 제어
//     0 = 흑백 (스프라이트 자체가 그레이스케일 렌더링)
//     1 = 풀컬러
//   - clip() 으로 투명 픽셀은 스텐실도 기록하지 않음 (픽셀 정확도)
//
// ColorKeeper.cs 가 MaterialPropertyBlock 으로 _Saturation 을 갱신
// ──────────────────────────────────────────────────────────────────
Shader "Custom/SpriteColorKeeper"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        _AlphaClip ("Alpha Clip Threshold", Range(0, 1)) = 0.01
        _Saturation ("Saturation", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "SpriteColorKeeper"

            // Stencil=200 기록: GrayscaleStencilBlit이 이 픽셀을 건너뜀
            // Ref=200: URP 2D 렌더러(Ref 1~16)와 충돌 방지
            Stencil
            {
                Ref 200
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFragColorKeeper
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _AlphaClip;
            float _Saturation;

            fixed4 SpriteFragColorKeeper(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // 투명 픽셀 제거: discard → 스텐실도 기록 안 함 (픽셀 정확도 보장)
                clip(c.a - _AlphaClip);

                // GRIS 스타일 개별 채도 적용
                // _Saturation=0: 흑백, _Saturation=1: 풀컬러
                float gray = dot(c.rgb, float3(0.2126, 0.7152, 0.0722));
                c.rgb = lerp(float3(gray, gray, gray), c.rgb, _Saturation);

                // Premultiplied alpha (Blend One OneMinusSrcAlpha 와 짝)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
