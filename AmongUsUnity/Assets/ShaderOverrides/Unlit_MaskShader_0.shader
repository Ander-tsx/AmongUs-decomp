Shader "Unlit/MaskShader"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _MaskLayer ("MaskLayer", Float) = 1
        _MaskComp ("Mask Comparison", Float) = 8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off ZWrite Off Blend One OneMinusSrcAlpha
        Stencil { Ref [_MaskLayer] Comp [_MaskComp] Pass Replace }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color;
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            v2f vert(appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; o.color = v.color * _Color; return o; }
            fixed4 frag(v2f i) : SV_Target { fixed4 c = tex2D(_MainTex, i.uv) * i.color; c.rgb *= c.a; return c; }
            ENDCG
        }
    }
}
