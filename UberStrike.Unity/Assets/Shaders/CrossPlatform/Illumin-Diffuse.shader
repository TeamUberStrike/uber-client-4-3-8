Shader "Cross Platform Shaders/Self-Illumin/Diffuse" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_Illum ("Illumin (A)", 2D) = "white" {}
		_EmissionLM ("Emission (Lightmapper)", Float) = 0
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
	
		CGPROGRAM
			#pragma exclude_renderers gles
			#pragma surface surf Lambert

			sampler2D _MainTex;
			sampler2D _Illum;
			fixed4 _Color;

			struct Input {
				float2 uv_MainTex;
				float2 uv_Illum;
			};

			void surf (Input IN, inout SurfaceOutput o) {
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				fixed4 c = tex * _Color;
				o.Albedo = c.rgb;
				o.Emission = c.rgb * tex2D(_Illum, IN.uv_Illum).a;
				o.Alpha = c.a;
			}
		ENDCG
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
	
		CGPROGRAM
			#pragma only_renderers gles
			#pragma surface surf Lambert noforwardadd

			sampler2D _MainTex;
			sampler2D _Illum;

			struct Input {
				half2 uv_MainTex;
				half2 uv_Illum;
			};

			void surf (Input IN, inout SurfaceOutput o) {
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = tex.rgb;
				o.Emission = tex.rgb * tex2D(_Illum, IN.uv_Illum).a;
				o.Alpha = tex.a;
			}
		ENDCG
	}
	FallBack "Cross Platform Shaders/Self-Illumin/VertexLit"
}
