// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Cmune/Outline/GaussianBlur" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	struct v2f
	{
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	uniform sampler2D _MainTex;
	uniform float _TexWidth;
	uniform float _TexHeight;

	v2f vert( appdata_img v )
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	}	  

	half4 honrizontalFrag (v2f i) : COLOR
	{
		half4 sum = half4(0.0);
		half blurSize = 1.0 / _TexWidth;

		sum += tex2D(_MainTex, half2(i.uv.x - 4.0*blurSize, i.uv.y)) * 0.05;
		sum += tex2D(_MainTex, half2(i.uv.x - 3.0*blurSize, i.uv.y)) * 0.09;
		sum += tex2D(_MainTex, half2(i.uv.x - 2.0*blurSize, i.uv.y)) * 0.12;
		sum += tex2D(_MainTex, half2(i.uv.x - blurSize, i.uv.y)) * 0.15;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y)) * 0.16;
		sum += tex2D(_MainTex, half2(i.uv.x + blurSize, i.uv.y)) * 0.15;
		sum += tex2D(_MainTex, half2(i.uv.x + 2.0*blurSize, i.uv.y)) * 0.12;
		sum += tex2D(_MainTex, half2(i.uv.x + 3.0*blurSize, i.uv.y)) * 0.09;
		sum += tex2D(_MainTex, half2(i.uv.x + 4.0*blurSize, i.uv.y)) * 0.05;

		return sum;
	}

	half4 verticalFrag (v2f i) : COLOR
	{
		half4 sum = half4(0.0);
		half blurSize = 1.0 / _TexHeight;

		// blur in y (vertical)
		// take nine samples, with the distance blurSize between them
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y - 4.0*blurSize)) * 0.05;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y - 3.0*blurSize)) * 0.09;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y - 2.0*blurSize)) * 0.12;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y - blurSize)) * 0.15;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y)) * 0.16;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y + blurSize)) * 0.15;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y + 2.0*blurSize)) * 0.12;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y + 3.0*blurSize)) * 0.09;
		sum += tex2D(_MainTex, half2(i.uv.x, i.uv.y + 4.0*blurSize)) * 0.05;

		return sum;
	}

	ENDCG

	SubShader 
	{
		Pass 
		{
			ZTest Always 
			Cull Off 
			ZWrite Off 
			Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment honrizontalFrag
			ENDCG
		}

		Pass 
		{
			ZTest Always 
			Cull Off 
			ZWrite Off 
			Fog { Mode off }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment verticalFrag
			ENDCG
		}
	}

	Fallback off
}
