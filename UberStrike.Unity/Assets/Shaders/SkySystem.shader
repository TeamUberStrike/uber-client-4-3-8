	// Upgrade NOTE: replaced 'PositionFog()' with multiply of UNITY_MATRIX_MVP by position
// Upgrade NOTE: replaced 'V2F_POS_FOG' with 'float4 pos : SV_POSITION'

Shader "Cmune/Sky System" {
	Properties {
		_DaySkyColor ("Day Sky Color", Color) = (1,1,1,1)
		_HorizonColor ("Horizon Color", Color) = (1,1,1,1)
		_DayTex ("Day Sky", 2D) = "white" {}
		_DayCloudTex ("Day Cloud", 2D) = "white" {}
		_NightTex ("Night Sky", 2D) = "blue" {}
		_NightCloudTex ("Night Cloud", 2D) = "white" {}
		_NightClouds ("Night Clouds", Range(0, 1)) = 0
		
		_SunsetTex ("Sunset Cloud", 2D) = "white" {}
		
		_DayNightCycle ("Day Night Range", Range(0, 1)) = 0
		_SunsetVisibility ("Sunset Range", Range(0, 1)) = 0
		_SunSetColor ("Sunset Color", Color) = (1,1,1,1)
		
		_SunsetOffset ("Sunset Offset", Float) = 0
	}
	SubShader {
		Pass {
				Tags { "IgnoreProjector"="True" "RenderType"="Opaque" }
				Lighting Off
				Cull Back
				Zwrite Off

				//Fog { Mode Off }

CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f
{
	float4 pos : SV_POSITION;
	float4 uv0 : TEXCOORD0;
	float4 uv1 : TEXCOORD1;
	float4 uv2 : TEXCOORD2;
};

float4 _DayTex_ST;
float4 _NightTex_ST;
float4 _DayCloudTex_ST;
float4 _NightCloudTex_ST;
float4 _SunsetTex_ST;
float4 _SunSetColor;

float _SunsetOffset;
float _NightClouds;

v2f vert(appdata_base v)
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	
	o.uv0.xy = TRANSFORM_TEX(v.texcoord.xy, _NightTex);
	o.uv0.zw = TRANSFORM_TEX(v.texcoord.xy, _DayTex);
	o.uv1.xy = TRANSFORM_TEX(v.texcoord.xy, _DayCloudTex);
	o.uv1.zw = TRANSFORM_TEX(v.texcoord.xy, _NightCloudTex);
	o.uv2.xy = TRANSFORM_TEX(v.texcoord.xy, _SunsetTex);
	
	o.uv2.y =  abs(_SunsetOffset - o.uv2.y);
	
	return o;
}

sampler2D _DayTex;
sampler2D _NightTex;
sampler2D _DayCloudTex;
sampler2D _NightCloudTex;
sampler2D _SunsetTex;

float4 _DaySkyColor;
float4 _HorizonColor;

float _DayNightCycle;
float _SunsetVisibility;

half4 frag(v2f i) : COLOR
{
	float4 tmp, col, mask;
	
	// blend night sky and day sky
	col = tex2D(_NightTex, i.uv0.xy);
	mask = tex2D(_DayTex, i.uv0.zw);
	col.rgb = lerp(col.rgb, mask.rgb + _DaySkyColor.rgb, mask.a * _DayNightCycle);
	
	// blend in day cloud
	tmp = tex2D(_DayCloudTex, i.uv1.xy);
	col.rgb = lerp(col.rgb, tmp.rgb, tmp.a * _DayNightCycle);
	col.rgb = lerp(col.rgb, mask.rgb + _HorizonColor.rgb, _HorizonColor.a * mask.r);
	
	// blend in night cloud
	tmp = tex2D(_NightCloudTex, i.uv1.zw);
	col.rgb = lerp(col.rgb, tmp.rgb, _NightClouds * tmp.a * (1 - _DayNightCycle));
	col.rgb = lerp(col.rgb, mask.rgb * _HorizonColor.rgb, _HorizonColor.a * mask.r);
	
	// blend in sunset color
	tmp = tex2D(_SunsetTex, i.uv2.xy);
	col.rgb = lerp(col.rgb, tmp.rgb * _SunSetColor.rgb, tmp.a * _SunsetVisibility);

	return col;
}

ENDCG
		}
	}
}
