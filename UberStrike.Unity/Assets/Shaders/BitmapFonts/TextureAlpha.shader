Shader "Cmune/TextureAlpha" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color ("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags 
		{
			"Queue"="Transparent" 
			"RenderType"="Transparent" 
			"IgnoreProjector"="True" 
		}
		Lighting Off 
		Cull Off 
		ZTest Always 
		ZWrite Off 
		Fog 
		{ 
			Mode Off 
		}
		Blend SrcAlpha OneMinusSrcAlpha
		
		Pass 
		{
			CGPROGRAM
			#pragma exclude_renderers gles
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			sampler2D _MainTex;
			float4 _Color;

			//Unity-required vars
			float4 _MainTex_ST;

			struct v2f {
				float4 pos : SV_POSITION;
				float2 uv: TEXCOORD0;
			};

			v2f vert (appdata_base v)
			{
				v2f o;
				o.pos = mul (UNITY_MATRIX_MVP, v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				return o;
			}

			float4 frag(v2f i) : COLOR
			{
				float4 base = tex2D(_MainTex, i.uv);
				return _Color * base;
			}

			ENDCG
		}
	} 
	FallBack "Diffuse"
}
