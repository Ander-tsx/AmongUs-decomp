// The ship map sprite is authored with red room fills; the SpriteRenderer colour replaces that red.
Shader "Unlit/MapShader"
{
    Properties { [PerRendererData] _MainTex ("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color; return o; }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                float m = min(t.r, min(t.g, t.b));
                float3 rgb = m + (t.r - m) * i.color.rgb;
                fixed4 c = fixed4(rgb, t.a * i.color.a);
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
