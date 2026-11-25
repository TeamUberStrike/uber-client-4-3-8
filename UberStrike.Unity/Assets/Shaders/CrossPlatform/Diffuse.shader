Shader "Cross Platform Shaders/Diffuse" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1) // Ignored in Mobile
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	
	// Desktop
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma exclude_renderers gles
		#pragma surface surf Lambert

		sampler2D _MainTex;
		fixed4 _Color;

		struct Input { float2 uv_MainTex; };

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}

	// Mobile
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 150

		CGPROGRAM
		#pragma only_renderers gles
		#pragma surface surf Lambert noforwardadd

		sampler2D _MainTex;

		struct Input { float2 uv_MainTex; };

		void surf (Input IN, inout SurfaceOutput o) 
		{
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	}

	Fallback Off
}