Shader "Cross Platform Shaders/TerrainSplat" 
{
	Properties 
	{
		_Control ("Control (RGBA)", 2D) = "red" {}
		_Splat0 ("Layer 0 (R)", 2D) = "white" {}
		_Splat1 ("Layer 1 (G)", 2D) = "white" {}
		_Splat2 ("Layer 2 (B)", 2D) = "white" {}
	}
	
	SubShader 
	{
		Tags 
		{
			"RenderType" = "Opaque"
		}

		CGPROGRAM
		#pragma surface surf Lambert

		struct Input 
		{
			float2 uv_Control : TEXCOORD0;
			float2 uv_Splat0 : TEXCOORD1;
			float2 uv_Splat1 : TEXCOORD2;
			float2 uv_Splat2 : TEXCOORD3;
		};

		sampler2D _Control;
		sampler2D _Splat0,_Splat1,_Splat2;

		void surf (Input IN, inout SurfaceOutput o) 
		{
			half4 splat_control = tex2D (_Control, IN.uv_Control);
			o.Albedo = splat_control.r * tex2D (_Splat0, IN.uv_Splat0).rgb 
						+ splat_control.g * tex2D (_Splat1, IN.uv_Splat1).rgb 
						+ splat_control.b * tex2D (_Splat2, IN.uv_Splat2).rgb;
		}

		ENDCG  
	}

	Fallback "Diffuse"
}