// The field: four photographic ground textures in world space, blended by the vertex colour of the ground grid
// (plough, pasture, mown, stubble), each with a normal map for the moonlight, and a low-frequency variation map so
// no two acres look alike. Lit like URP Lit (moon shadows, the flare light, fog), without the parts we never use.
Shader "IronNight/Ground"
{
    Properties
    {
        _Tex0 ("Plough", 2D) = "white" {}   _Nrm0 ("Plough normal", 2D) = "bump" {}
        _Tex1 ("Pasture", 2D) = "white" {}  _Nrm1 ("Pasture normal", 2D) = "bump" {}
        _Tex2 ("Mown", 2D) = "white" {}     _Nrm2 ("Mown normal", 2D) = "bump" {}
        _Tex3 ("Stubble", 2D) = "white" {}  _Nrm3 ("Stubble normal", 2D) = "bump" {}
        _Variation ("Variation (world space, mid grey = none)", 2D) = "gray" {}
        _Tiling ("Metres per repeat", Float) = 13.3333
        _VariationTiling ("Variation metres per repeat", Float) = 300
        _VariationStrength ("Variation strength", Range(0, 1)) = 0.5
        _BumpScale ("Normal strength", Float) = 1
        _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Tex0); SAMPLER(sampler_Tex0); TEXTURE2D(_Nrm0); SAMPLER(sampler_Nrm0);
            TEXTURE2D(_Tex1); SAMPLER(sampler_Tex1); TEXTURE2D(_Nrm1); SAMPLER(sampler_Nrm1);
            TEXTURE2D(_Tex2); SAMPLER(sampler_Tex2); TEXTURE2D(_Nrm2); SAMPLER(sampler_Nrm2);
            TEXTURE2D(_Tex3); SAMPLER(sampler_Tex3); TEXTURE2D(_Nrm3); SAMPLER(sampler_Nrm3);
            TEXTURE2D(_Variation); SAMPLER(sampler_Variation);
            CBUFFER_START(UnityPerMaterial)
            float _Tiling, _VariationTiling, _VariationStrength, _BumpScale, _Smoothness; half4 _Tint;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half4 weights : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o; VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS; o.weights = v.color; o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float2 uv = i.positionWS.xz / _Tiling;
                half4 w = i.weights; w /= max(1e-4, w.x + w.y + w.z + w.w);
                half3 alb = 0; half3 nts = 0;
                // a pixel inside a field needs one texture, a pixel on a boundary two: skip the rest
                UNITY_BRANCH if (w.x > 0.004) { alb += w.x * SAMPLE_TEXTURE2D(_Tex0, sampler_Tex0, uv).rgb; nts += w.x * UnpackNormalScale(SAMPLE_TEXTURE2D(_Nrm0, sampler_Nrm0, uv), _BumpScale); }
                UNITY_BRANCH if (w.y > 0.004) { alb += w.y * SAMPLE_TEXTURE2D(_Tex1, sampler_Tex1, uv).rgb; nts += w.y * UnpackNormalScale(SAMPLE_TEXTURE2D(_Nrm1, sampler_Nrm1, uv), _BumpScale); }
                UNITY_BRANCH if (w.z > 0.004) { alb += w.z * SAMPLE_TEXTURE2D(_Tex2, sampler_Tex2, uv).rgb; nts += w.z * UnpackNormalScale(SAMPLE_TEXTURE2D(_Nrm2, sampler_Nrm2, uv), _BumpScale); }
                UNITY_BRANCH if (w.w > 0.004) { alb += w.w * SAMPLE_TEXTURE2D(_Tex3, sampler_Tex3, uv).rgb; nts += w.w * UnpackNormalScale(SAMPLE_TEXTURE2D(_Nrm3, sampler_Nrm3, uv), _BumpScale); }
                half3 var = SAMPLE_TEXTURE2D(_Variation, sampler_Variation, i.positionWS.xz / _VariationTiling).rgb;
                alb *= lerp(half3(1, 1, 1), var * 2.0, _VariationStrength) * _Tint.rgb;
                // the ground is flat: tangent +X, bitangent +Z, normal +Y
                half3 n = normalize(half3(nts.x, max(0.2, nts.z), nts.y));

                InputData d = (InputData)0;
                d.positionWS = i.positionWS; d.normalWS = n; d.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord = TransformWorldToShadowCoord(i.positionWS); d.fogCoord = i.fogFactor;
                d.bakedGI = SampleSH(n); d.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS); d.shadowMask = half4(1, 1, 1, 1);
                SurfaceData s = (SurfaceData)0;
                s.albedo = alb; s.alpha = 1; s.smoothness = _Smoothness; s.occlusion = 1; s.normalTS = half3(0, 0, 1);
                half4 c = UniversalFragmentPBR(d, s); c.rgb = MixFog(c.rgb, i.fogFactor); c.a = 1;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
