using System;
using UnityEngine;

public static class ShaderParser
{
    public static readonly string TestShaderString =
        @" Shader Alex/TerrainThreeLayers No Lit {
Properties {
	_Color (Main Color, Color) = (1,1,1,1)
	_BaseMap (BaseMap (RGB), 2D) = white {}
	_LightMap (LightMap (RGB), 2D) = white {}
	_Exposure (Exposure (0 ~ 5), Range(0, 2)) = 1
	_Bias (Bias between Base and Detail, Range(0, 1)) = 0.5

	_Control (Control 1 (RGB), 2D) = black {}

	_Splat0 (Layer 0 (R 1), 2D) = white {}
	_Splat1 (Layer 1 (G 1), 2D) = white {}
	_Splat2 (Layer 2 (B 1), 2D) = white {}
}

Category {

	SubShader {
		Pass {
CGPROGRAM
#pragma vertex LightmapSplatVertex
#pragma fragment LightmapSplatFragment
#pragma fragmentoption ARB_fog_exp2
#pragma fragmentoption ARB_precision_hint_fastest

#include UnityCG.cginc

// Six splatmaps
uniform float4 _Splat0_ST,_Splat1_ST,_Splat2_ST;

struct v2f {
	//float4 pos : POSITION;
	//float fog : FOGC;
	V2F_POS_FOG;
	
	// first uv for control, then 1 uv for every 2 splatmaps
	float4 uv[3] : TEXCOORD0;
	float4 color : COLOR;
};


uniform sampler2D _Control;
uniform sampler2D _Splat0,_Splat1,_Splat2;

void CalculateSplatUV (float2 baseUV, inout v2f o) 
{
	o.uv[0].xy = baseUV;
	
	o.uv[1].xy = TRANSFORM_TEX (baseUV, _Splat0);
	o.uv[1].zw = TRANSFORM_TEX (baseUV, _Splat1);
	o.uv[2].xy = TRANSFORM_TEX (baseUV, _Splat2);
}

half4 CalculateSplat (v2f i)
{
	half4 color = half4(0,0,0,0);

	half4 control = tex2D (_Control, i.uv[0].xy);
	
	color += control.r * tex2D (_Splat0, i.uv[1].xy);
	color += control.g * tex2D (_Splat1, i.uv[1].zw);
	color += control.b * tex2D (_Splat2, i.uv[2].xy);
	
	return color;	
}

uniform sampler2D _BaseMap;
uniform sampler2D _LightMap;
uniform float _Exposure;
uniform float _Bias;

uniform float4 _Color;

half4 LightmapSplatFragment (v2f i) : COLOR
{
	half4 col = CalculateSplat (i);
	half4 base = tex2D (_BaseMap, i.uv[0].xy);
	
	col = lerp(base, col, _Bias);
	col *= tex2D (_LightMap, i.uv[0].xy);
	col *= float4 (_Exposure,_Exposure,_Exposure, 0);
	
	col.a = _Color.a;
	
	return col;
}

v2f LightmapSplatVertex (appdata_base v)
{
	v2f o;
	
	PositionFog( v.vertex, o.pos, o.fog );
	CalculateSplatUV (v.texcoord, o);

	return o;
}

ENDCG
		}
		
 	}

 }
Fallback Off
}
";

    public static string GetProperties(Material mat)
    {
        string shaderstring = mat.shader.name;

        int idxstart = shaderstring.Substring(shaderstring.IndexOf("Properties")).IndexOf("{");
        int idxend = 0;

        int bracketcount = 0;
        for (int i = idxstart; i < shaderstring.Length; i++)
        {
            if (shaderstring[i] == '{') bracketcount++;
            else if (shaderstring[i] == '}')
            {
                if (bracketcount == 0)
                {
                    idxend = i;
                    break;
                }
                else
                {
                    bracketcount--;
                }
            }
        }

        shaderstring = shaderstring.Substring(idxstart, idxend - idxstart);

        return shaderstring;
    }
}

