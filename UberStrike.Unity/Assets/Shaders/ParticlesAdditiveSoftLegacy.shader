// Custom shader matching Unity 3.5's fixed-function "Particles/Additive (Soft)" behavior.
// BLEND: One OneMinusSrcColor (alpha NOT used in blending equation itself).
//
// Original 3.5 formula: output = texture * _TintColor * vertexColor * 2 (DOUBLE combiner)
// Alpha had NO effect on RGB output — Blend One OneMinusSrcColor ignores alpha entirely.
// ColorOverLifetime alpha still animates for bookkeeping but doesn't dim particles.
//
// We add a gentle edge fix (saturate * 4) to kill PSD import artifacts at texture borders
// (bright RGB at near-zero alpha) without clipping the soft smoke gradients that
// make fire/smoke particles look wispy. The original had no edge fix but also had
// no PSD import issues (Unity 3.5 gamma-space textures).

Shader "Particles/Additive (Soft) Legacy" {
    Properties {
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
    }

    Category {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend One OneMinusSrcColor
        ColorMask RGB
        Cull Off Lighting Off ZWrite Off

        SubShader {
            Pass {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0
                #pragma multi_compile_particles
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _TintColor;

                struct appdata_t {
                    float4 vertex : POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f {
                    float4 vertex : SV_POSITION;
                    fixed4 color : COLOR;
                    float2 texcoord : TEXCOORD0;
                    UNITY_FOG_COORDS(1)
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert (appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.color = v.color * _TintColor;
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                fixed4 frag (v2f i) : SV_Target
                {
                    // 2.0x multiplier matches Unity 3.5 fixed-function DOUBLE keyword.
                    // Original: output = texture * _TintColor * vertexColor * 2
                    fixed4 texel = tex2D(_MainTex, i.texcoord);
                    fixed4 col = 2.0f * i.color * texel;

                    // Gentle edge fix: kill PSD import artifacts (alpha < 25%)
                    // without clipping soft smoke/fire gradients.
                    // Original had no edge fix but 3.5 textures had no PSD artifacts.
                    col.rgb *= saturate(texel.a * 4.0);

                    // Vertex alpha fade: folds ColorOverLifetime alpha into brightness.
                    // Original 3.5 Additive (Soft) did NOT fold alpha into RGB — alpha
                    // was ignored by Blend One OneMinusSrcColor. But without this fold,
                    // particles pop in/out at full brightness with no fade transition.
                    // Use a gentle fold (saturate * 2) to keep smooth fade while
                    // staying closer to the original constant-brightness behavior.
                    col.rgb *= saturate(i.color.a * 2.0);

                    // Clamp to 1.0: original gamma pipeline clamped before blending.
                    // In linear pipeline, 2x can push values above 1.0 into HDR,
                    // making particles overbright vs the original. This restores the
                    // gamma-pipeline capping behavior.
                    col.rgb = min(col.rgb, 1.0);

                    UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0));
                    return col;
                }
                ENDCG
            }
        }
    }
}
