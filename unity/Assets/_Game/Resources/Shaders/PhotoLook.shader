// Photo mode's looks, laid on the picture the camera renders while a photo is framed: on the screen through a RawImage
// and into the saved photo through Graphics.Blit, so the two are the same. _Look 0 leaves the night as it is; 1 is a
// 1944 print (the old sepia matrix, a little more contrast, a warm dark edge, a coarse grain); 2 is black and white
// (harder contrast, a black edge, a finer grain). The grain is a hash of the pixel and _Seed in cells of _Grain pixels,
// so a picture twice the screen's size keeps grain of the same size to the eye. Graded in gamma, as prints were, after
// a lift of the middle tones, as a longer exposure gives: the night is darker than a print of it should be.
Shader "IronNight/PhotoLook"
{
    Properties
    {
        [PerRendererData] _MainTex ("Picture", 2D) = "white" {}
        _Look ("Look", Float) = 0
        _Seed ("Grain seed", Float) = 0
        _Grain ("Grain cell (pixels)", Float) = 1
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            sampler2D _MainTex; float4 _MainTex_ST; float4 _MainTex_TexelSize; float _Look, _Seed, _Grain;

            v2f vert(appdata v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = TRANSFORM_TEX(v.uv, _MainTex); return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(0.1031, 0.1030));
                p += dot(p, p.yx + 33.33);
                return frac((p.x + p.y) * p.x);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 c = tex2D(_MainTex, i.uv).rgb;
                if (_Look < 0.5) return fixed4(c, 1);
            #ifndef UNITY_COLORSPACE_GAMMA
                c = LinearToGammaSpace(c);
            #endif
                bool print = _Look < 1.5;
                c = pow(saturate(c), 0.78);
                float luma = dot(c, float3(0.299, 0.587, 0.114));
                // sepia: the classic matrix scaled so its middle keeps the brightness; black and white: the luma alone
                c = print ? float3(dot(c, float3(0.327, 0.639, 0.157)), dot(c, float3(0.290, 0.570, 0.140)), dot(c, float3(0.226, 0.444, 0.109))) : luma.xxx;
                c = saturate((c - 0.4) * (print ? 1.18 : 1.3) + 0.4 + (print ? 0.03 : 0.01));
                // the edge: darker toward the corners, warm brown on the print
                float2 d = i.uv - 0.5; float v = saturate(1.0 - dot(d, d) * (print ? 1.9 : 1.5));
                float3 edge = print ? float3(0.1, 0.06, 0.03) : float3(0.0, 0.0, 0.0);
                c = lerp(edge, c, pow(v, 1.6));
                // the grain, strongest in the middle tones
                float2 cell = floor(i.uv * _MainTex_TexelSize.zw / max(_Grain, 1.0));
                float n = Hash(cell + _Seed * float2(17.13, 31.71)) - 0.5;
                float mid = 1.0 - abs(luma * 2.0 - 1.0) * 0.6;
                c = saturate(c + n * (print ? 0.11 : 0.075) * mid);
            #ifndef UNITY_COLORSPACE_GAMMA
                c = GammaToLinearSpace(c);
            #endif
                return fixed4(c, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
