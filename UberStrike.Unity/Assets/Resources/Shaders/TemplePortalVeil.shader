// Swirling arched veil for the Temple of the Raven secret portal.
// Drawn on a quad that covers the SecretDoor opening; everything outside the
// pointed arch is transparent apart from a soft glow along the edge.
Shader "UberStrike/TemplePortalVeil"
{
    Properties
    {
        _DoorSize ("Door size (m)", Vector) = (2.96, 5.86, 0, 0)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend One OneMinusSrcAlpha
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

            float4 _DoorSize;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 size = _DoorSize.xy;
                float2 p = float2((i.uv.x - 0.5) * size.x, i.uv.y * size.y);
                float d = PortalArchSDF(p, size);

                // Parallax: the swirl lies behind the wall, so the opening reads as a
                // hole into another space instead of a flat plate. View ray in door axes:
                float3 V = normalize(i.worldPos - _WorldSpaceCameraPos);
                float3 lv = float3(dot(V, normalize(unity_ObjectToWorld._m00_m10_m20)),
                                   dot(V, normalize(unity_ObjectToWorld._m01_m11_m21)),
                                   dot(V, normalize(unity_ObjectToWorld._m02_m12_m22)));
                // Capped so steep side views don't smear the swirl into streaks.
                float2 slope = lv.xy / max(abs(lv.z), 0.5);
                float2 pDeep = p + slope * 1.2;   // swirl layer, 1.2 m in
                float2 pDeeper = p + slope * 2.5; // second layer + core, 2.5 m in
                // Where the ray, 0.4 m in, is already outside the arch it hits the
                // tunnel side wall: draw that dark so the opening has depth.
                float tunnel = smoothstep(-0.02, 0.08, PortalArchSDF(p + slope * 0.4, size));

                float t = _Time.y;
                float2 center = float2(0.0, size.y * 0.42);
                float2 v = (pDeep - center) / float2(1.0, 1.7);
                float r = length(v) / PortalArchHalfWidth(size);
                float ang = atan2(v.y, v.x);
                float2 v2 = (pDeeper - center) / float2(1.0, 1.7);
                float r2 = length(v2) / PortalArchHalfWidth(size);
                float ang2 = atan2(v2.y, v2.x);

                // The spiral arms all meet at the centre; fade them out there so the
                // pinch point doesn't show up as a bright stripe.
                float swirl = sin(ang * 3.0 + r * 7.0 - t * 1.6) * smoothstep(0.05, 0.35, r);
                float swirl2 = sin(ang2 * 5.0 - r2 * 11.0 + t * 1.1 + sin(t * 0.7) * 2.0) * smoothstep(0.05, 0.35, r2);
                float ripple = sin(pDeep.y * 3.1 - t * 1.3 + sin(pDeep.x * 2.3 + t * 0.6) * 1.5);
                float pattern = saturate(0.45 + 0.45 * swirl + 0.3 * swirl2 + 0.2 * ripple);

                float3 deep = float3(0.01, 0.02, 0.07);
                float3 blue = float3(0.08, 0.22, 0.62);
                float3 violet = float3(0.30, 0.10, 0.55);
                float3 bright = float3(0.55, 0.90, 1.00);

                float falloff = saturate(1.0 - r);
                float3 col = lerp(deep, blue, pattern * (0.25 + 0.75 * falloff));
                col = lerp(col, violet, 0.4 * saturate(swirl2 * 0.5 + 0.5) * falloff);
                col += bright * pow(saturate(swirl * 0.5 + 0.5), 6.0) * 0.35 * (0.3 + 0.7 * falloff); // swirling streaks
                col += bright * pow(saturate(1.0 - r2), 3.0) * (0.3 + 0.1 * sin(t * 2.3));
                col = lerp(col, deep * 0.5, tunnel * 0.85);

                // Glow outside the arch fades out before the quad's sides and top.
                float edge = min(min(p.x + size.x * 0.5, size.x * 0.5 - p.x), size.y - p.y);
                float edgeFade = smoothstep(0.0, 0.18, edge);

                float inside = smoothstep(0.03, -0.03, d);
                float rimPulse = 0.8 + 0.2 * sin(t * 3.0 + p.y * 1.5);
                float rim = exp(-abs(d) * 16.0) * rimPulse * edgeFade;
                float halo = exp(-max(d, 0.0) * 9.0) * (1.0 - inside) * 0.45 * edgeFade;

                // Premultiplied: colour is what the veil adds, alpha what it hides.
                float3 outCol = col * inside * 0.9 + bright * (rim * 1.4 + halo);
                float alpha = saturate(inside * 0.9 + rim * 0.3);
                return float4(outCol, alpha);
            }
            ENDCG
        }
    }
}
