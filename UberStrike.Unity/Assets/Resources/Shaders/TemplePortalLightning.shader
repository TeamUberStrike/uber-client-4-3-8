// Additive particle shader (like Legacy Shaders/Particles/Additive) that fades
// out everything outside the Temple of the Raven portal arch, so the portal
// lightning never spills past the doorway.
Shader "UberStrike/TemplePortalLightning"
{
    Properties
    {
        _TintColor ("Tint Color", Color) = (0.5, 0.5, 0.5, 0.5)
        _MainTex ("Particle Texture", 2D) = "white" {}
        _DoorSize ("Door size (m), z = wall plane in door-local z", Vector) = (2.96, 5.86, 0.4, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha One
        ColorMask RGB
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "TemplePortalArch.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            float4 _DoorSize;
            float4x4 _DoorWorldToLocal; // SecretDoor transform.worldToLocalMatrix

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Follow the view ray onto the wall plane, so the mask matches what the
                // player sees even though the lightning floats in front of the wall.
                // Door-local unit cube (-0.5..0.5) -> metres from the floor centre.
                float3 l = mul(_DoorWorldToLocal, float4(i.worldPos, 1.0)).xyz;
                float3 cam = mul(_DoorWorldToLocal, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                float3 ray = l - cam;
                float t = (_DoorSize.z - cam.z) / (abs(ray.z) > 1e-4 ? ray.z : 1e-4);
                l = cam + ray * t;
                float2 p = float2(l.x * _DoorSize.x, (l.y + 0.5) * _DoorSize.y);
                float mask = smoothstep(0.0, -0.35, PortalArchSDF(p, _DoorSize.xy));

                fixed4 c = 2.0 * i.color * _TintColor * tex2D(_MainTex, i.uv);
                c.a *= mask;
                return c;
            }
            ENDCG
        }
    }
}
