// Per pixel bumped refraction.
// Uses a normal map to distort the image behind, and
// an additional texture to tint the color.
// Updated for Unity 2022 compatibility.

Shader "HeatDistort_3.0" {
Properties {
	_BumpAmt  ("Distortion", Range (0,128)) = 10
	_MainTex ("Tint Color (RGB)", 2D) = "white" {}
	_BumpMap ("Normalmap", 2D) = "bump" {}
}

SubShader {
	Tags { "Queue"="Overlay" "RenderType"="Overlay" }

	Fog { Mode Off }
	Cull Off

	GrabPass {
		Name "BASE"
		Tags { "LightMode" = "Always" }
	}

	Pass {
		Name "BASE"
		Tags { "LightMode" = "Always" }
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"

		sampler2D _GrabTexture;
		float4 _GrabTexture_TexelSize;
		sampler2D _BumpMap;
		float4 _BumpMap_ST;
		sampler2D _MainTex;
		float4 _MainTex_ST;
		float _BumpAmt;

		struct appdata {
			float4 vertex : POSITION;
			float2 texcoord : TEXCOORD0;
		};

		struct v2f {
			float4 vertex : SV_POSITION;
			float4 uvgrab : TEXCOORD0;
			float2 uvbump : TEXCOORD1;
			float2 uvmain : TEXCOORD2;
		};

		v2f vert (appdata v)
		{
			v2f o;
			o.vertex = UnityObjectToClipPos(v.vertex);
			o.uvgrab = ComputeGrabScreenPos(o.vertex);
			o.uvbump = TRANSFORM_TEX(v.texcoord, _BumpMap);
			o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
			return o;
		}

		half4 frag (v2f i) : SV_Target
		{
			half2 bump = UnpackNormal(tex2D(_BumpMap, i.uvbump)).rg;
			float2 offset = bump * _BumpAmt * _GrabTexture_TexelSize.xy;
			i.uvgrab.xy += offset * i.uvgrab.z;

			half4 col = tex2Dproj(_GrabTexture, UNITY_PROJ_COORD(i.uvgrab));
			half4 tint = tex2D(_MainTex, i.uvmain);
			return col * tint;
		}
		ENDCG
	}
}

Fallback Off
}
