// Upgrade NOTE: replaced 'PositionFog()' with multiply of UNITY_MATRIX_MVP by position
// Upgrade NOTE: replaced 'V2F_POS_FOG' with 'float4 pos : SV_POSITION'

Shader "Cmune/Sky System Fallback" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	SubShader {
		Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"

struct v2f
{
	float4 pos : SV_POSITION;
	float4 uv0 : TEXCOORD0;
};

float4 _MainTex_ST;

v2f vert(appdata_base v)
{
	v2f o;
	o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
	
	o.uv0.xy = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
	
	return o;
}

sampler2D _MainTex;

half4 frag(v2f i) : COLOR
{
	float4 col;
	
	// only day texture
	col = tex2D(_MainTex, i.uv0.xy);
	
	return col;
}

ENDCG
		}
	}
	
	SubShader {
		Pass {
			Lighting Off
			
			SetTexture [_MainTex] {
				ConstantColor [_Color]
				Combine texture * constant
			}
		}
	}
	
	FallBack "Diffuse"
}
