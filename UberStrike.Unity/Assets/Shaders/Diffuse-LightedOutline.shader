// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Cmune/Diffuse Lighted Outline" {
	Properties {
		_Color ("Main Color", Color) = (0.5,0.5,0.5,1)
		_OutlineColor ("Outline Color", Color) = (0,0,0,1)
		_Outline ("Outline Width", Float) = .005
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

SubShader {

		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		//Tags { "RenderType" = "Opaque" }
		LOD 200
		
		Pass
		{
			Name "OUTLINE"
			Tags { "LightMode" = "Always" }

			Cull Off
			ZWrite Off

			CGPROGRAM
			#pragma fragment frag
			#pragma vertex vert
			#include "UnityCG.cginc"
				
			samplerCUBE _MainTex;

			uniform float _Outline;
			uniform float4 _OutlineColor;

			struct v2f 
			{
				float4 pos : POSITION;
				float4 color : COLOR;
				float fog : FOGC;
				float3 cubenormal : TEXCOORD0;
			};
				
			struct appdata 
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
		
			v2f vert(appdata v)
			{
				v2f o;
				
				o.pos = UnityObjectToClipPos(v.vertex);
				o.cubenormal = mul (UNITY_MATRIX_MV, float4(v.normal, 0)).xyz;
				
				// Don't calc pos.z - line thickness changes based on distance
				o.pos.xy += o.cubenormal.xy * _Outline * 3.0 * half2(UNITY_MATRIX_P[0][0], UNITY_MATRIX_P[1][1]);
				
				o.fog = o.pos.z;
				o.color = _OutlineColor;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				return half4(i.color.rgb, 0);
			}
			ENDCG
		}
		
		Cull Back
		ZWrite On

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		float4 _Color;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			float4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
		
	}
	
	Fallback "Diffuse"
}