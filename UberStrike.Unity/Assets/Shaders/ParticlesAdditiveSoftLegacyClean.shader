// Replicates the original Unity 3.5 fixed-function "Particles/Additive (Soft)" shader.
//
// Original formula: output = texture * vertexColor * 2.0  (via DOUBLE combiner)
// Blend mode: One OneMinusSrcColor (soft additive — bright areas saturate, dark areas transparent)
//
// _TintColor added for linear pipeline brightness control. In the original gamma pipeline,
// the 2.0 DOUBLE gave correct brightness. In Unity 2022's linear pipeline with sRGBTexture=0
// (gamma-encoded textures), 2.0x is too bright. _TintColor scales the output:
//   (0.5,0.5,0.5,0.5) -> 2.0 * 0.5 = 1.0x brightness (good starting point for linear)
//   (1.0,1.0,1.0,0.5) -> 2.0 * 1.0 = 2.0x brightness (original gamma pipeline value)
// Alpha channel of _TintColor is unused (Blend One OneMinusSrcColor ignores alpha).
//
// Gentle edge fix: saturate(texel.a * 20) kills PSD import artifacts (alpha < 5%)
// without affecting explosion gradients. The type 2 shader's smoothstep(0, 0.5) was
// too aggressive — it dimmed everything below 50% alpha, cutting off soft glow edges.

Shader "Particles/Additive (Soft) Legacy Clean" {
    Properties {
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
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
                    o.color = v.color;
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                fixed4 frag (v2f i) : SV_Target
                {
                    // 2.0x multiplier matches Unity 3.5 fixed-function DOUBLE keyword.
                    // _TintColor scales the result for linear pipeline brightness control.
                    fixed4 texel = tex2D(_MainTex, i.texcoord);
                    fixed4 col = 2.0f * i.color * _TintColor * texel;

                    // Gentle edge fix: kill truly transparent PSD edges (alpha < 5%).
                    col.rgb *= saturate(texel.a * 20.0);

                    // Alpha fade: Blend One OneMinusSrcColor ignores alpha entirely —
                    // without this line, colorOverLifetime's alpha fade has ZERO effect
                    // and particles stay fully visible for their entire lifetime.
                    // In the original gamma pipeline this was acceptable (dim particles),
                    // but in linear, gamma-encoded textures are brighter so unfaded
                    // particles are glaringly visible (ring grows huge, blast never fades).
                    // Multiplying RGB by vertex alpha makes the fade actually work.
                    col.rgb *= i.color.a;

                    UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(0,0,0,0));
                    return col;
                }
                ENDCG
            }
        }
    }
}
