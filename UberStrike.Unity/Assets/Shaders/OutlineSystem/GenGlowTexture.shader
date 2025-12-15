// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Cmune/Outline/GenGlowTexture" 
{
	CGINCLUDE

    #include "UnityCG.cginc"

	struct appdata 
	{
		float4 vertex : POSITION;
		float3 normal : NORMAL;
	};

	struct v2f 
	{
		float4 pos : POSITION;
		float4 color : COLOR;
	};

	uniform float _Outline_Size;
	uniform half4 _Outline_Color;

	v2f vert_enlarge(appdata v) 
	{
		v2f o;
		o.pos = UnityObjectToClipPos(v.vertex);

		float3 norm   = mul ((float3x3)UNITY_MATRIX_IT_MV, v.normal);
		float2 offset = TransformViewToProjection(norm.xy);

		o.pos.xy += offset * o.pos.z * _Outline_Size;
		o.color = _Outline_Color;
		if ( _Outline_Size == 0.0 )
		{
			o.color.a = 0.0f;
		}
		else
		{
			o.color.a = 1.0f;
		}
		return o;
	}

//	v2f vert_normal(appdata v)
//	{
//		v2f o;
//		o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
//		o.color = float4(1.0, 1.0, 1.0, 0.0);
//		return o;
//	}

	half4 frag(v2f i) :COLOR
	{
		return i.color;
	}

	ENDCG

	SubShader 
	{
		Tags { "RenderType"="Opaque" "Outline"="Blue" }
		Pass 
		{
			Fog { Mode Off }		
			Cull Back 
			CGPROGRAM
			#pragma vertex vert_enlarge
			#pragma fragment frag
			ENDCG
		}
//		Pass 
//		{
//			Fog { Mode Off }
//			Cull Back
//			ZTest Off
//			ZWrite Off 
//			CGPROGRAM
//			#pragma vertex vert_normal
//			#pragma fragment frag
//			ENDCG
//		}
	}
}
