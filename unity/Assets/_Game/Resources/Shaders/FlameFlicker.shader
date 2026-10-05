// A photographed flame that burns: the picture pulled about by two layers of noise climbing through it (the foot
// steady, the tips licking more), and its upper part eaten away and given back by a third, so no two moments are the
// same picture. Additive; the colour's alpha fades it. _Seed (per flame) puts each one at its own place in the noise.
Shader "IronNight/FlameFlicker"
{
    Properties
    {
        _BaseMap ("Flame", 2D) = "black" {}
        _BaseColor ("Colour", Color) = (1, 1, 1, 1)
        _Noise ("Noise", 2D) = "gray" {}
        _Seed ("Seed", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float2 raw : TEXCOORD1; };

            sampler2D _BaseMap, _Noise; float4 _BaseMap_ST, _BaseColor; float _Seed;

            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _BaseMap); o.raw = v.uv; return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float t = _Time.y, h = i.raw.y;
                float2 n1 = tex2D(_Noise, i.raw * float2(1.1, 0.8) + float2(_Seed, -t * 0.9)).rg - 0.5;
                float2 n2 = tex2D(_Noise, i.raw * float2(2.6, 2.0) + float2(-_Seed * 0.7, -t * 2.1)).rg - 0.5;
                float2 d = (n1 * 0.10 + n2 * 0.05) * (0.15 + h * 1.2);   // the foot holds, the tips lick
                d.y *= 0.6;
                float4 c = tex2D(_BaseMap, i.uv + d);
                float e = tex2D(_Noise, i.raw * float2(1.7, 1.3) + float2(_Seed * 1.3, -t * 1.5)).b;
                float keep = saturate((e + 0.95 - h * 1.0) * 2.5);   // the top torn away and given back
                float a = c.a * keep * _BaseColor.a;
                return half4(c.rgb * _BaseColor.rgb * a, 0);
            }
            ENDCG
        }
    }
    Fallback Off
}
