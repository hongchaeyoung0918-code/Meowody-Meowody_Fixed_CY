Shader "Custom/SpriteSaturation"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0, 2)) = 0.0
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment SpriteFragSaturation
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _Saturation;

            fixed4 SpriteFragSaturation(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // 채도 조절: luminance 기반 그레이스케일과 원본 컬러 사이를 보간
                // 표준 luminance 가중치 (Rec. 709)
                float gray = dot(c.rgb, float3(0.2126, 0.7152, 0.0722));
                c.rgb = lerp(float3(gray, gray, gray), c.rgb, _Saturation);

                // 핵심: 알파를 RGB에 곱해 premultiplied alpha로 출력
                // (Blend One OneMinusSrcAlpha와 짝을 이룸)
                c.rgb *= c.a;

                return c;
            }
        ENDCG
        }
    }
}
