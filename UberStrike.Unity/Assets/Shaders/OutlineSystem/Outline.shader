Shader "Cmune/Outline/Outline" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Outline_Size ("Outline Size", Range( 0, 0.020 )) = 0.010 
		_Outline_Color ("Outline Color", COLOR) = (1,1,1,1)
	}
	SubShader {
		Tags { "Outline"="Blue" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
