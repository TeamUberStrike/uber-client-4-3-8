Shader "Cmune/Outline/Composite" 
{
	Properties 
	{
		_MainTex ("", RECT) = "white" {}
		_GlowTex ("", RECT) = "white" {}
		_MaskTex ("", RECT) = "white" {}
	}

	CGINCLUDE

	#include "UnityCG.cginc"

	struct v2f
	{
		float4 pos : POSITION;
		float2 uv : TEXCOORD0;
	};
	
	uniform sampler2D _MainTex;
	uniform sampler2D _GlowTex;
	uniform sampler2D _MaskTex;
	uniform half4 _GlobalOutlineColor;
	uniform float _IsUseGlobalColor;

	v2f vert( appdata_img v )
	{
		v2f o;
		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
		o.uv = v.texcoord.xy;
		return o;
	}	  

	half4 frag_glow( v2f i ) : COLOR
	{
		return tex2D(_GlowTex, i.uv);
	}

	half4 frag_mask( v2f i ) : COLOR
	{
		return tex2D(_MaskTex, i.uv);
	}

	half4 frag (v2f i) : COLOR
	{
		half4 original = tex2D(_MainTex, i.uv);
	
		half4 glow = tex2D(_GlowTex, i.uv);
		half4 mask = tex2D(_MaskTex, i.uv);
		float t = 1 - glow.a;
		if ( mask.a == 1.0 )
		{
			t = 1.0;
		}
	
		half4 outlineColor;
		if ( _IsUseGlobalColor == 1.0 )
		{
			outlineColor = _GlobalOutlineColor;
		}
		else
		{
			outlineColor = glow;
		}
		half4 color = lerp(outlineColor, original, t);
		return color;
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
			#pragma fragment frag
			//#pragma fragment frag_glow
			//#pragma fragment frag_mask
			ENDCG
		}
	}

	Fallback off
}
