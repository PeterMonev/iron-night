// What the night leaves on a tank, drawn over it as a second pass that multiplies what is already lit (so the moon,
// the flares and the fires light it as they light the paint): mud or dust up from the tracks, thick at the bottom and
// thinning up the hull; snow settled on whatever faces up; soot of hits in blotches and bright scratches where shells
// glanced off. All in the tank's own frame (_Root), so it rides with the tank and does not slide as it turns.
Shader "IronNight/Grime"
{
    Properties
    {
        _Noise ("Noise", 2D) = "gray" {}
        _Height ("Tank height", Float) = 3
        _Mud ("Mud", Float) = 0
        _MudTint ("Mud factor (1 = none, below darkens, above lightens: dust)", Color) = (0.6, 0.5, 0.38, 1)
        _Snow ("Snow", Float) = 0
        _Soot ("Soot and scratches", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" }
        Pass
        {
            Blend DstColor SrcColor   // 2 x source x what is there: 0.5 leaves it as it is
            ZWrite Off
            ZTest LEqual
            Offset -1, -1
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 local : TEXCOORD0; float3 up : TEXCOORD1; };

            sampler2D _Noise; float4x4 _Root; float _Height, _Mud, _Snow, _Soot; float4 _MudTint;

            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                float3 w = mul(unity_ObjectToWorld, v.vertex).xyz; o.local = mul(_Root, float4(w, 1)).xyz;
                o.up = mul((float3x3)_Root, UnityObjectToWorldNormal(v.normal));
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 p = i.local; float h = saturate(p.y / max(_Height, 0.5));
                float n1 = tex2D(_Noise, p.xz * 0.35 + p.y * 0.21).r, n2 = tex2D(_Noise, p.xz * 1.4 + p.yy * 0.9).g, n3 = tex2D(_Noise, p.zy * 0.7 + p.x * 0.33).b;
                float3 f = 1;
                // mud or dust: up from the tracks
                float mud = saturate((0.55 - h) * 2.6 + (n1 - 0.5) * 1.6) * _Mud;
                f = lerp(f, _MudTint.rgb, mud);
                // snow on what faces up, a little caked low down too
                float up = saturate(normalize(i.up).y * 1.6 - 0.55);
                float snow = saturate(saturate(up * (0.55 + n2) - 0.2) + saturate((0.22 - h) * 3) * 0.5) * _Snow;
                f = lerp(f, float3(1.75, 1.8, 1.9), snow);
                // soot of the hits in blotches, and the bright scrapes of glancing rounds
                float soot = saturate((n3 - (1 - _Soot * 0.55)) * 5);
                f = lerp(f, float3(0.28, 0.26, 0.24), soot);
                float scrape = saturate((n2 - 0.92) * 22) * saturate(_Soot * 2);
                f = lerp(f, float3(1.55, 1.5, 1.45), scrape * 0.7);
                return half4(f * 0.5, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
