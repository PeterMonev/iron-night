// The night through the hangar's door: a picture that lights itself (the hangar's lamps do not reach outside), a
// little darker than it was painted so it sits behind the room, its lamps and its moon lifted past the bloom's
// threshold so they glow.
Shader "IronNight/Backdrop"
{
    Properties
    {
        _MainTex ("Picture", 2D) = "black" {}
        _Gain ("Gain", Float) = 0.85
        _Glow ("Glow of the brightest lights", Float) = 1.6
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex; float4 _MainTex_ST; float _Gain, _Glow;

            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb * _Gain;
                c += max(c - 0.55, 0) * _Glow;   // the lamps and the moon, past one: half, not fixed, keeps them
                return half4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
