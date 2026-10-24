// Single-pass post-processing effect that runs via OnRenderImage + Graphics.Blit.
// Adds saturation boost, contrast curve, and a luminance-gated pseudo-bloom so
// bright pixels visibly glow. Not Unity's Post Processing Stack — just enough
// for an "RTX on/off" feel without adding a package dependency.
Shader "UberStrike/PostProcessingRTX"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Saturation ("Saturation", Range(0, 2)) = 1.25
        _Contrast ("Contrast", Range(0.5, 2)) = 1.12
        _BloomThreshold ("Bloom Threshold", Range(0, 1)) = 0.65
        _BloomIntensity ("Bloom Intensity", Range(0, 4)) = 1.6
        _Vignette ("Vignette", Range(0, 1)) = 0.35
        _Warmth ("Warmth", Range(-0.2, 0.2)) = 0.04
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Saturation;
            float _Contrast;
            float _BloomThreshold;
            float _BloomIntensity;
            float _Vignette;
            float _Warmth;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // Saturation around luminance.
                float luma = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(float3(luma, luma, luma), col.rgb, _Saturation);

                // Contrast around 0.5.
                col.rgb = (col.rgb - 0.5) * _Contrast + 0.5;

                // Gentle warmth — shift the whole image a hair toward orange, cooler on the blue channel.
                col.r += _Warmth;
                col.b -= _Warmth * 0.5;

                // Luma-gated pseudo-bloom: bright pixels get boosted, dark pixels untouched.
                float lumaNew = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float bloom = saturate((lumaNew - _BloomThreshold) / max(1e-4, 1.0 - _BloomThreshold));
                col.rgb += col.rgb * bloom * _BloomIntensity * 0.5;

                // Vignette — soft dark corners for cinematic framing.
                float2 centred = i.uv - 0.5;
                float distSq = dot(centred, centred) * 4.0; // 0 at centre, ~2 at corners
                float vig = 1.0 - saturate(distSq * _Vignette);
                col.rgb *= vig;

                col.rgb = saturate(col.rgb);
                return col;
            }
            ENDCG
        }
    }
    FallBack Off
}
