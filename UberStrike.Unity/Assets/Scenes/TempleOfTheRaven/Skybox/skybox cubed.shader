// RenderFX/Skybox Cubed — restored from DummyShaderTextExporter stub.
// Cubemap skybox shader. Samples a single CUBE texture with a tint.
// Used by TempleOfTheRaven (SkyboxV2 material).
Shader "RenderFX/Skybox Cubed" {
Properties {
    _Tint ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
    _Tex ("Cubemap", CUBE) = "white" {}
}
SubShader {
    Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
    Cull Off
    ZWrite Off

    Pass {
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 2.5

        #include "UnityCG.cginc"

        samplerCUBE _Tex;
        half4 _Tex_HDR;
        half4 _Tint;

        struct appdata {
            float4 vertex : POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float3 texcoord : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        v2f vert(appdata v) {
            v2f o;
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = v.vertex.xyz;
            return o;
        }

        fixed4 frag(v2f i) : SV_Target {
            half4 tex = texCUBE(_Tex, i.texcoord);
            half3 col = tex.rgb * _Tint.rgb * 2.0;
            return half4(col, 1.0);
        }
        ENDCG
    }
}
Fallback Off
}
