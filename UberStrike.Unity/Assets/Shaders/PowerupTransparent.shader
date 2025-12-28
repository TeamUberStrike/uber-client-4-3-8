Shader "Cmune/Health Vial (Transparent)" 
{
	Properties
	{
		_MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags {"IgnoreProjector"="True" "RenderType"="Transparent"}
		LOD 100
	
		ZTest LEqual Cull Off ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha 

		Pass
		{
			Lighting Off
			SetTexture [_MainTex] { combine texture } 
		}
	}
}
