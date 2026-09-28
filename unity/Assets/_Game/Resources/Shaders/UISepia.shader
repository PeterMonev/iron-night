// The moment of the night's reel: the picture toned like a wartime newsreel (a warm sepia with a little of its own colour
// left), darker toward the edges. For a UI RawImage on the overlay canvas.
Shader "IronNight/UISepia"
{
    Properties
    {
        [PerRendererData] _MainTex ("Picture", 2D) = "white" {}
        _Sepia ("Sepia", Range(0, 1)) = 0.72
        _Vignette ("Vignette", Range(0, 2)) = 1.1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };

            sampler2D _MainTex; float4 _MainTex_ST; float _Sepia, _Vignette;

            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); o.color = v.color; return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float l = dot(c.rgb, float3(0.299, 0.587, 0.114));
                float3 tone = float3(l * 1.08 + 0.025, l * 0.93 + 0.015, l * 0.74);   // warm paper and silver
                c.rgb = lerp(c.rgb, tone, _Sepia);
                float2 d = i.uv - 0.5; c.rgb *= saturate(1.12 - dot(d, d) * _Vignette * 1.5);
                c.a = 1.0;
                return c * i.color;
            }
            ENDCG
        }
    }
    Fallback Off
}
