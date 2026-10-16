// Replacement for the exported dummy shader. Sprite shader with optional outline and additive tint,
// used for task/use highlights (_Outline, _OutlineColor) and flashes (_AddColor).
Shader "Sprites/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Outline ("Outline", Float) = 0
        _OutlineColor ("OutlineColor", Color) = (1,1,1,1)
        _OutlineSize ("Outline Size", Float) = 4
        _AddColor ("AddColor", Color) = (0,0,0,0)
        [Toggle(OUTLINE_FLIP)] _OutlineFlip ("Interior Outline", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local OUTLINE_FLIP
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _OutlineColor, _AddColor;
            float _Outline, _OutlineSize;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                c.rgb += _AddColor.rgb * _AddColor.a * c.a;
                if (_Outline > 0.5)
                {
                    float2 d = _MainTex_TexelSize.xy * _OutlineSize * 0.5;
                    float a = 0;
                    a = max(a, tex2D(_MainTex, i.uv + float2(d.x, 0)).a);
                    a = max(a, tex2D(_MainTex, i.uv - float2(d.x, 0)).a);
                    a = max(a, tex2D(_MainTex, i.uv + float2(0, d.y)).a);
                    a = max(a, tex2D(_MainTex, i.uv - float2(0, d.y)).a);
                    #ifdef OUTLINE_FLIP
                    float edge = (c.a > 0.1 && a < 0.9) ? 1 : 0;
                    #else
                    float edge = (tex2D(_MainTex, i.uv).a < 0.1) ? a : 0;
                    #endif
                    c = lerp(c, fixed4(_OutlineColor.rgb, 1) * _OutlineColor.a, saturate(edge * 2));
                    c.rgb *= c.a > 0 ? 1 : 0;
                    return c;
                }
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
