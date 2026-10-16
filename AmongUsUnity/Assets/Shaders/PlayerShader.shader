// Player colouring. The original sprite sheets are authored with channel masks:
//   R = body colour, B = shade/back colour, G = visor colour, white = highlights kept as is.
Shader "AmongUsLan/PlayerShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _BackColor ("Shadow Color", Color) = (0.48, 0.03, 0.22, 1)
        _BodyColor ("Body Color", Color) = (0.77, 0.07, 0.07, 1)
        _VisorColor ("Visor Color", Color) = (0.58, 0.79, 0.86, 1)
        _Outline ("Outline", Range(0, 1)) = 0
        _OutlineColor ("OutlineColor", Color) = (1,1,1,1)
        _Desaturate ("Desaturate", Range(0, 1)) = 0
        _MaskLayer ("Mask Layer", Float) = 1
        _MaskComp ("Mask Comp", Float) = 8
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
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _BackColor, _BodyColor, _VisorColor, _OutlineColor;
            float _Outline, _Desaturate;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                float m = min(t.r, min(t.g, t.b));
                float3 rgb = m + (t.r - m) * _BodyColor.rgb + (t.g - m) * _VisorColor.rgb + (t.b - m) * _BackColor.rgb;
                float g = dot(rgb, float3(0.299, 0.587, 0.114));
                rgb = lerp(rgb, g.xxx, _Desaturate);
                fixed4 c = fixed4(rgb, t.a) * i.color;
                if (_Outline > 0.5)
                {
                    float2 d = _MainTex_TexelSize.xy * 2;
                    float a = max(max(tex2D(_MainTex, i.uv + float2(d.x, 0)).a, tex2D(_MainTex, i.uv - float2(d.x, 0)).a),
                                  max(tex2D(_MainTex, i.uv + float2(0, d.y)).a, tex2D(_MainTex, i.uv - float2(0, d.y)).a));
                    float edge = t.a < 0.1 ? a : 0;
                    c = lerp(c, fixed4(_OutlineColor.rgb, 1), saturate(edge * 2));
                }
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
