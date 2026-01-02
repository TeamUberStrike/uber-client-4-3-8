Shader "Cross Platform Shaders/Diffuse Detail" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Detail ("Detail (RGB)", 2D) = "gray" {}
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 250
	
		CGPROGRAM
			#pragma surface surf Lambert
			#pragma exclude_renderers gles

			sampler2D _MainTex;
			sampler2D _Detail;
			fixed4 _Color;

			struct Input {
				float2 uv_MainTex;
				float2 uv_Detail;
			};

			void surf (Input IN, inout SurfaceOutput o) {
				fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
				c.rgb *= tex2D(_Detail,IN.uv_Detail).rgb*2;
				o.Albedo = c.rgb;
				o.Alpha = c.a;
			}
		ENDCG
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 150

		CGPROGRAM
		#pragma surface surf Lambert noforwardadd
		#pragma only_renderers gles

		sampler2D _MainTex;
		sampler2D _Detail;

		struct Input 
		{
			float2 uv_MainTex;
			float2 uv_Detail;
		};

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			fixed4 d = tex2D(_Detail, IN.uv_Detail);
			o.Albedo = c.rgb;
			o.Albedo *= d.rgb * 2;
			o.Alpha = c.a;
		}
		ENDCG
	}


	Fallback "Diffuse"
}
