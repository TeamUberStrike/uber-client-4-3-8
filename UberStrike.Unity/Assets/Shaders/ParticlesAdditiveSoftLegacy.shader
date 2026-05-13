// Custom shader matching Unity 3.5's fixed-function "Particles/Additive (Soft)" behavior.
// BLEND: One OneMinusSrcColor (alpha NOT used in blending equation itself).
//
// Two paths:
//   Default:        2.0 * color * texel, edge fix, alpha fold, HDR clamp (surface impacts)
//   EXPLOSION_MODE: 2.0 * color * texel, HDR clamp only (cannon explosions)
//
// The original 3.5.5 fixed-function was: output = 2.0 * _TintColor * texel
// with NO edge fix and NO alpha fold. Surface impacts are small enough that the
// extra operations don't matter, but cannon explosions (large, overlapping) need
// the clean path to match the original visual.

Shader "Particles/Additive (Soft) Legacy" {
    Properties {
        [Gamma] _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _EdgeMul ("Edge Cleanup Multiplier", Range(0.5, 4.0)) = 2.0
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
                #pragma multi_compile _ EXPLOSION_MODE

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                float4 _MainTex_ST;
                fixed4 _TintColor;
                half _EdgeMul;

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
                    fixed4 texel = tex2D(_MainTex, i.texcoord);
                    fixed4 col = 2.0f * i.color * texel;
                #if defined(EXPLOSION_MODE)
                    // Soft edge cleanup: hide transparent quad edges without
                    // affecting visible particle shape. The original 3.5.5
                    // fixed-function didn't need this because the legacy
                    // ParticleRenderer handled edge blending differently.
                    // _EdgeMul tunable: 2.0=current, 1.5/1.0=preserves more starburst edges
                    col.rgb *= saturate(texel.a * _EdgeMul);
                #else
                    col.rgb *= saturate(texel.a * 4.0);      // edge fix (surface impacts)
                    col.rgb *= saturate(i.color.a * 2.0);     // alpha fold (surface impacts)
                #endif
                    col.rgb = min(col.rgb, 1.0);
                    UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0));
                    return col;
                }
                ENDCG
            }
        }
    }
}
