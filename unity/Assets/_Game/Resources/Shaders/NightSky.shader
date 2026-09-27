// The night sky over the battlefield, drawn from nothing but the direction: haze along the horizon deepening to the
// zenith, three layers of stars and the Milky Way turning slowly round the pole through the night, the moon where its
// light comes from with a halo, clouds by the weather that hide the stars and catch the moonlight, and a low warm glow
// along parts of the horizon where the front is burning. Crisp at any resolution: no textures.
Shader "IronNight/NightSky"
{
    Properties
    {
        _Zenith ("Zenith", Color) = (0.006, 0.01, 0.025, 1)
        _Horizon ("Horizon (the fog colour)", Color) = (0.03, 0.045, 0.07, 1)
        _Glow ("The front's glow", Color) = (0.09, 0.04, 0.018, 1)
        _CloudColor ("Clouds", Color) = (0.03, 0.035, 0.05, 1)
        _MoonDir ("Toward the moon", Vector) = (0.46, 0.79, -0.4, 0)
        _Stars ("Stars", Range(0, 1)) = 1
        _Clouds ("Cloud cover", Range(0, 1)) = 0.12
        _Moon ("Moon", Range(0, 1)) = 1
        _Turn ("Turn of the sky (radians)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _Glow, _CloudColor;
                float4 _MoonDir;
                half _Stars, _Clouds, _Moon;
                float _Turn;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            float Hash13(float3 p) { p = frac(p * 0.1031); p += dot(p, p.zyx + 31.32); return frac((p.x + p.y) * p.z); }
            float3 Hash33(float3 p) { p = frac(p * float3(0.1031, 0.1030, 0.0973)); p += dot(p, p.yxz + 33.33); return frac((p.xxy + p.yxx) * p.zyx); }

            float Noise(float3 p)
            {
                float3 i = floor(p), f = frac(p); f = f * f * (3.0 - 2.0 * f);
                float a = lerp(lerp(Hash13(i), Hash13(i + float3(1, 0, 0)), f.x), lerp(Hash13(i + float3(0, 1, 0)), Hash13(i + float3(1, 1, 0)), f.x), f.y);
                float b = lerp(lerp(Hash13(i + float3(0, 0, 1)), Hash13(i + float3(1, 0, 1)), f.x), lerp(Hash13(i + float3(0, 1, 1)), Hash13(i + float3(1, 1, 1)), f.x), f.y);
                return lerp(a, b, f.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0, amp = 0.5;
                for (int k = 0; k < 4; k++) { sum += amp * Noise(p); p = p * 2.03 + 17.1; amp *= 0.5; }
                return sum;
            }

            // one layer of stars: the sky cut into cells, a star in some of them, set off the middle, each with its own
            // brightness, tint and twinkle; never thinner than a pixel, a small one keeping its light rather than its size
            float3 StarLayer(float3 d, float scale, float density, float size)
            {
                float3 p = d * scale, cell = floor(p), f = frac(p);
                float h = Hash13(cell);
                if (h < 1.0 - density) return float3(0, 0, 0);
                float3 at = 0.25 + 0.5 * Hash33(cell + 7.13);
                float bright = pow(frac(h * 91.7), 4.0);
                float px = max(length(fwidth(p)) * 1.2, 1e-4);
                float want = size * (0.45 + bright), r = max(want, px);
                float glint = saturate(1.0 - length(f - at) / r); glint *= glint;
                float twinkle = 0.7 + 0.3 * sin(_Time.y * (1.5 + 4.0 * frac(h * 13.1)) + h * 60.0);
                float3 tint = lerp(float3(0.72, 0.8, 1.0), float3(1.0, 0.85, 0.68), frac(h * 7.31));
                return tint * glint * (0.35 + 2.4 * bright) * twinkle * saturate(want / r);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float up = d.y;

                // the dome: the haze along the horizon (the fog's own colour, so the land fades into it) deepening upward
                float3 col = lerp(_Horizon.rgb, _Zenith.rgb, smoothstep(0.0, 0.55, up));

                // the front: a low warm glow along parts of the horizon, where the fires are
                float around = atan2(d.x, d.z);
                float fires = smoothstep(0.38, 0.8, Fbm(float3(around * 1.6, 0.0, 3.1)));
                col += _Glow.rgb * exp(-max(up, 0.0) * 14.0) * fires;
                col += float3(0.012, 0.018, 0.032) * exp(-max(up, 0.0) * 6.0);   // airglow: the sky low down is never quite black
                if (up < -0.02) return half4(col, 1);

                // the stars turn round the pole as the night goes: forty-nine degrees up, due north
                float3 pole = float3(0.0, 0.7547, 0.6561);
                float c = cos(_Turn), sn = sin(_Turn);
                float3 sd = d * c + cross(pole, d) * sn + pole * dot(pole, d) * (1.0 - c);

                // the Milky Way: a soft band across the sky, mottled, and more stars in it
                float q = dot(sd, normalize(float3(0.8, 0.3, 0.1))) / 0.22;   // across the northern sky, where the opening shot looks
                float band = exp(-q * q);
                float milk = band * (0.35 + 0.65 * Fbm(sd * 6.0));
                float3 stars = StarLayer(sd, 150.0, 0.16, 0.11) * 1.5 + StarLayer(sd, 60.0, 0.05, 0.10) * 2.2 + StarLayer(sd, 240.0, 0.45 * band, 0.12) * 1.1;
                float low = smoothstep(0.0, 0.18, up);

                // clouds drifting over it all, thicker the more the cover: they hide the stars and catch the moonlight
                float2 uv = d.xz / (up + 0.12) * 0.9 + float2(_Time.y * 0.006, _Time.y * 0.002);
                float cover = smoothstep(0.62 - 0.45 * _Clouds, 0.95 - 0.25 * _Clouds, Fbm(float3(uv * 1.4, 0.0))) * saturate(_Clouds * 1.6);

                // the moon: a disc with its seas and a halo round it
                float3 toward = normalize(_MoonDir.xyz);
                float m = dot(d, toward);
                float disc = smoothstep(0.99955, 0.99975, m);
                float halo = pow(saturate(m), 60.0) * 0.12 + pow(saturate(m), 900.0) * 0.35;
                float3 moonlight = float3(0.95, 0.93, 0.86);

                col += float3(0.26, 0.3, 0.4) * milk * 0.22 * _Stars * low;
                col += stars * _Stars * low * (1.0 - cover);
                col += moonlight * halo * _Moon * (1.0 - cover * 0.6);
                col = lerp(col, _CloudColor.rgb + moonlight * halo * 0.6 * _Moon, cover * 0.92);
                if (disc > 0.0)
                {
                    float seas = 0.78 + 0.22 * Fbm((d - toward) * 400.0 + 3.0);
                    col += moonlight * disc * seas * _Moon * 1.6 * (1.0 - cover * 0.85);
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
