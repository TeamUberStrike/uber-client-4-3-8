// Unlit alpha-blended shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "Cmune/UnlitTransparent" {
Properties {
	_Color ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha OneMinusSrcAlpha
	AlphaTest Greater .01
	ColorMask RGB
	Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
	BindChannels {
		Bind "Color", color
		Bind "Vertex", vertex
		Bind "TexCoord", texcoord
	}
	
	// ---- Fragment program cards
	SubShader {
		Pass {
		
			Program "vp" {
// Vertex combos: 2
//   opengl - ALU: 6 to 14
//   d3d9 - ALU: 6 to 14
SubProgram "opengl " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 5 [_MainTex_ST]
"!!ARBvp1.0
# 6 ALU
PARAM c[6] = { program.local[0],
		state.matrix.mvp,
		program.local[5] };
MOV result.color, vertex.color;
MAD result.texcoord[0].xy, vertex.texcoord[0], c[5], c[5].zwzw;
DP4 result.position.w, vertex.position, c[4];
DP4 result.position.z, vertex.position, c[3];
DP4 result.position.y, vertex.position, c[2];
DP4 result.position.x, vertex.position, c[1];
END
# 6 instructions, 0 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_mvp]
Vector 4 [_MainTex_ST]
"vs_2_0
; 6 ALU
dcl_position0 v0
dcl_color0 v1
dcl_texcoord0 v2
mov oD0, v1
mad oT0.xy, v2, c4, c4.zwzw
dp4 oPos.w, v0, c3
dp4 oPos.z, v0, c2
dp4 oPos.y, v0, c1
dp4 oPos.x, v0, c0
"
}

SubProgram "xbox360 " {
Keywords { "SOFTPARTICLES_OFF" }
Bind "vertex" Vertex
Bind "color" COLOR
Bind "texcoord" TexCoord0
Vector 4 [_MainTex_ST]
Matrix 0 [glstate_matrix_mvp]
"vs_360
backbbabaaaaabaeaaaaaajmaaaaaaaaaaaaaaceaaaaaaaaaaaaaamaaaaaaaaa
aaaaaaaaaaaaaajiaaaaaabmaaaaaailpppoadaaaaaaaaacaaaaaabmaaaaaaaa
aaaaaaieaaaaaaeeaaacaaaeaaabaaaaaaaaaafaaaaaaaaaaaaaaagaaaacaaaa
aaaeaaaaaaaaaaheaaaaaaaafpengbgjgofegfhifpfdfeaaaaabaaadaaabaaae
aaabaaaaaaaaaaaaghgmhdhegbhegffpgngbhehcgjhifpgnhghaaaklaaadaaad
aaaeaaaeaaabaaaaaaaaaaaahghdfpddfpdaaadccodacodbdbdgdcdgcodaaakl
aaaaaaaaaaaaaajmaabbaaadaaaaaaaaaaaaaaaaaaaabiecaaaaaaabaaaaaaad
aaaaaaacaaaaacjaaabaaaadaaaakaaeaadafaafaaaadafaaaabpbkaaaaabaal
aaaabaakhabfdaadaaaabcaamcaaaaaaaaaaeaagaaaabcaameaaaaaaaaaacaak
aaaaccaaaaaaaaaaafpidaaaaaaaagiiaaaaaaaaafpibaaaaaaaagiiaaaaaaaa
afpiaaaaaaaaapmiaaaaaaaamiapaaacaabliiaakbadadaamiapaaacaamgiiaa
kladacacmiapaaacaalbdejekladabacmiapiadoaagmaadekladaaacmiapiaab
aaaaaaaaocababaamiadiaaaaalalabkilaaaeaeaaaaaaaaaaaaaaaaaaaaaaaa
"
}

SubProgram "ps3 " {
Keywords { "SOFTPARTICLES_OFF" }
Matrix 256 [glstate_matrix_mvp]
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 467 [_MainTex_ST]
"sce_vp_rsx // 6 instructions using 1 registers
[Configuration]
8
0000000601090100
[Microcode]
96
401f9c6c0040030d8106c0836041ff84401f9c6c011d3808010400d740619f9c
401f9c6c01d0300d8106c0c360403f80401f9c6c01d0200d8106c0c360405f80
401f9c6c01d0100d8106c0c360409f80401f9c6c01d0000d8106c0c360411f81
"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;

varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;

uniform highp vec4 _MainTex_ST;
attribute vec4 _glesMultiTexCoord0;
attribute vec4 _glesColor;
attribute vec4 _glesVertex;
void main ()
{
  gl_Position = (gl_ModelViewProjectionMatrix * _glesVertex);
  xlv_COLOR = _glesColor;
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
}



#endif
#ifdef FRAGMENT

varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform lowp vec4 _Color;
uniform sampler2D _MainTex;
void main ()
{
  gl_FragData[0] = (((2.0 * xlv_COLOR) * _Color) * texture2D (_MainTex, xlv_TEXCOORD0));
}



#endif"
}

SubProgram "opengl " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 9 [_ProjectionParams]
Vector 10 [_MainTex_ST]
"!!ARBvp1.0
# 14 ALU
PARAM c[11] = { { 0.5 },
		state.matrix.modelview[0],
		state.matrix.mvp,
		program.local[9..10] };
TEMP R0;
TEMP R1;
DP4 R1.w, vertex.position, c[8];
DP4 R0.x, vertex.position, c[5];
MOV R0.w, R1;
DP4 R0.y, vertex.position, c[6];
MUL R1.xyz, R0.xyww, c[0].x;
MUL R1.y, R1, c[9].x;
DP4 R0.z, vertex.position, c[7];
MOV result.position, R0;
DP4 R0.x, vertex.position, c[3];
ADD result.texcoord[1].xy, R1, R1.z;
MOV result.color, vertex.color;
MAD result.texcoord[0].xy, vertex.texcoord[0], c[10], c[10].zwzw;
MOV result.texcoord[1].z, -R0.x;
MOV result.texcoord[1].w, R1;
END
# 14 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Matrix 0 [glstate_matrix_modelview0]
Matrix 4 [glstate_matrix_mvp]
Vector 8 [_ProjectionParams]
Vector 9 [_ScreenParams]
Vector 10 [_MainTex_ST]
"vs_2_0
; 14 ALU
def c11, 0.50000000, 0, 0, 0
dcl_position0 v0
dcl_color0 v1
dcl_texcoord0 v2
dp4 r1.w, v0, c7
dp4 r0.x, v0, c4
mov r0.w, r1
dp4 r0.y, v0, c5
mul r1.xyz, r0.xyww, c11.x
mul r1.y, r1, c8.x
dp4 r0.z, v0, c6
mov oPos, r0
dp4 r0.x, v0, c2
mad oT1.xy, r1.z, c9.zwzw, r1
mov oD0, v1
mad oT0.xy, v2, c10, c10.zwzw
mov oT1.z, -r0.x
mov oT1.w, r1
"
}

SubProgram "xbox360 " {
Keywords { "SOFTPARTICLES_ON" }
Bind "vertex" Vertex
Bind "color" COLOR
Bind "texcoord" TexCoord0
Vector 10 [_MainTex_ST]
Vector 8 [_ProjectionParams]
Vector 9 [_ScreenParams]
Matrix 4 [glstate_matrix_modelview0]
Matrix 0 [glstate_matrix_mvp]
"vs_360
backbbabaaaaableaaaaabeiaaaaaaaaaaaaaaceaaaaabdiaaaaabgaaaaaaaaa
aaaaaaaaaaaaabbaaaaaaabmaaaaabacpppoadaaaaaaaaafaaaaaabmaaaaaaaa
aaaaaaplaaaaaaiaaaacaaakaaabaaaaaaaaaaimaaaaaaaaaaaaaajmaaacaaai
aaabaaaaaaaaaaimaaaaaaaaaaaaaakoaaacaaajaaabaaaaaaaaaaimaaaaaaaa
aaaaaalmaaacaaaeaaaeaaaaaaaaaaniaaaaaaaaaaaaaaoiaaacaaaaaaaeaaaa
aaaaaaniaaaaaaaafpengbgjgofegfhifpfdfeaaaaabaaadaaabaaaeaaabaaaa
aaaaaaaafpfahcgpgkgfgdhegjgpgofagbhcgbgnhdaafpfdgdhcgfgfgofagbhc
gbgnhdaaghgmhdhegbhegffpgngbhehcgjhifpgngpgegfgmhggjgfhhdaaaklkl
aaadaaadaaaeaaaeaaabaaaaaaaaaaaaghgmhdhegbhegffpgngbhehcgjhifpgn
hghaaahghdfpddfpdaaadccodacodbdbdgdcdgcodaaaklklaaaaaaaaaaaaaaab
aaaaaaaaaaaaaaaaaaaaaabeaapmaabaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
aaaaaaeaaaaaabaiaacbaaaeaaaaaaaaaaaaaaaaaaaacigdaaaaaaabaaaaaaad
aaaaaaafaaaaacjaaabaaaadaaaakaaeaacafaafaaaadafaaaabpbfbaaaepcka
aaaababcaaaaaaapaaaaaabaaaaababeaaaababbaaaaaaaaaaaaaaaaaaaaaaaa
aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
aaaaaaaadpaaaaaaaaaaaaaaaaaaaaaaaaaaaaaahabfdaadaaaabcaamcaaaaaa
aaaafaagaaaabcaameaaaaaaaaaagaaleabbbcaaccaaaaaaafpidaaaaaaaagii
aaaaaaaaafpicaaaaaaaagiiaaaaaaaaafpibaaaaaaaapmiaaaaaaaamiapaaaa
aabliiaakbadadaamiapaaaaaamgaaiikladacaamiapaaaaaalbdedekladabaa
miapaaaeaagmnajekladaaaamiapiadoaananaaaocaeaeaamiabaaaaaamgmgaa
kbadagaamiabaaaaaalbmggmkladafaamiaiaaaaaagmmggmkladaeaamiahaaaa
aamagmaakbaeppaabeiaiaabaaaaaamgocaaaaaemiaeiaabafblmgblkladahaa
miapiaacaaaaaaaaocacacaamiadiaaaaalalabkilabakakkiiaaaaaaaaaaaeb
mcaaaaaimiadiaabaamgbkbiklaaajaaaaaaaaaaaaaaaaaaaaaaaaaa"
}

SubProgram "ps3 " {
Keywords { "SOFTPARTICLES_ON" }
Matrix 256 [glstate_matrix_modelview0]
Matrix 260 [glstate_matrix_mvp]
Bind "vertex" Vertex
Bind "color" Color
Bind "texcoord" TexCoord0
Vector 467 [_ProjectionParams]
Vector 466 [_MainTex_ST]
"sce_vp_rsx // 13 instructions using 2 registers
[Configuration]
8
0000000d01090200
[Defaults]
1
465 1
3f000000
[Microcode]
208
401f9c6c0040030d8106c0836041ff84401f9c6c011d2808010400d740619f9c
00001c6c01d0600d8106c0c360405ffc00001c6c01d0500d8106c0c360409ffc
00001c6c01d0400d8106c0c360411ffc00001c6c01d0200d8106c0c360403ffc
00009c6c01d0700d8106c0c360411ffc401f9c6c004000ff8086c08360405fa0
40001c6c004000000286c08360403fa0401f9c6c0040000d8086c0836041ff80
00001c6c009d100e008000c36041dffc00001c6c009d302a808000c360409ffc
401f9c6c00c000080086c09540219fa1
"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES
#define SHADER_API_GLES 1
#define tex2D texture2D


#ifdef VERTEX
#define gl_ModelViewProjectionMatrix glstate_matrix_mvp
uniform mat4 glstate_matrix_mvp;
#define gl_ModelViewMatrix glstate_matrix_modelview0
uniform mat4 glstate_matrix_modelview0;

varying highp vec4 xlv_TEXCOORD1;
varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;


uniform highp vec4 _ProjectionParams;
uniform highp vec4 _MainTex_ST;
attribute vec4 _glesMultiTexCoord0;
attribute vec4 _glesColor;
attribute vec4 _glesVertex;
void main ()
{
  highp vec4 tmpvar_1;
  highp vec4 tmpvar_2;
  tmpvar_2 = (gl_ModelViewProjectionMatrix * _glesVertex);
  highp vec4 o_i0;
  highp vec4 tmpvar_3;
  tmpvar_3 = (tmpvar_2 * 0.5);
  o_i0 = tmpvar_3;
  highp vec2 tmpvar_4;
  tmpvar_4.x = tmpvar_3.x;
  tmpvar_4.y = (tmpvar_3.y * _ProjectionParams.x);
  o_i0.xy = (tmpvar_4 + tmpvar_3.w);
  o_i0.zw = tmpvar_2.zw;
  tmpvar_1 = o_i0;
  tmpvar_1.z = -((gl_ModelViewMatrix * _glesVertex).z);
  gl_Position = tmpvar_2;
  xlv_COLOR = _glesColor;
  xlv_TEXCOORD0 = ((_glesMultiTexCoord0.xy * _MainTex_ST.xy) + _MainTex_ST.zw);
  xlv_TEXCOORD1 = tmpvar_1;
}



#endif
#ifdef FRAGMENT

varying highp vec4 xlv_TEXCOORD1;
varying highp vec2 xlv_TEXCOORD0;
varying lowp vec4 xlv_COLOR;
uniform highp vec4 _ZBufferParams;
uniform lowp vec4 _Color;
uniform sampler2D _MainTex;
uniform highp float _InvFade;
uniform sampler2D _CameraDepthTexture;
void main ()
{
  lowp vec4 tmpvar_1;
  tmpvar_1 = xlv_COLOR;
  lowp vec4 tmpvar_2;
  tmpvar_2 = texture2DProj (_CameraDepthTexture, xlv_TEXCOORD1);
  highp float z;
  z = tmpvar_2.x;
  highp float tmpvar_3;
  tmpvar_3 = (xlv_COLOR.w * clamp ((_InvFade * (1.0/(((_ZBufferParams.z * z) + _ZBufferParams.w)) - xlv_TEXCOORD1.z)), 0.0, 1.0));
  tmpvar_1.w = tmpvar_3;
  gl_FragData[0] = (((2.0 * tmpvar_1) * _Color) * texture2D (_MainTex, xlv_TEXCOORD0));
}



#endif"
}

}
Program "fp" {
// Fragment combos: 2
//   opengl - ALU: 4 to 11, TEX: 1 to 2
//   d3d9 - ALU: 4 to 10, TEX: 1 to 2
SubProgram "opengl " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_Color]
SetTexture 0 [_MainTex] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 4 ALU, 1 TEX
PARAM c[2] = { program.local[0],
		{ 2 } };
TEMP R0;
TEMP R1;
TEX R0, fragment.texcoord[0], texture[0], 2D;
MUL R1, fragment.color.primary, c[0];
MUL R0, R1, R0;
MUL result.color, R0, c[1].x;
END
# 4 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_Color]
SetTexture 0 [_MainTex] 2D
"ps_2_0
; 4 ALU, 1 TEX
dcl_2d s0
def c1, 2.00000000, 0, 0, 0
dcl v0
dcl t0.xy
texld r0, t0, s0
mul r1, v0, c0
mul r0, r1, r0
mul r0, r0, c1.x
mov_pp oC0, r0
"
}

SubProgram "xbox360 " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_Color]
SetTexture 0 [_MainTex] 2D
"ps_360
backbbaaaaaaaaoaaaaaaafeaaaaaaaaaaaaaaceaaaaaaaaaaaaaaliaaaaaaaa
aaaaaaaaaaaaaajaaaaaaabmaaaaaaidppppadaaaaaaaaacaaaaaabmaaaaaaaa
aaaaaahmaaaaaaeeaaadaaaaaaabaaaaaaaaaafaaaaaaaaaaaaaaagaaaacaaaa
aaabaaaaaaaaaagmaaaaaaaafpengbgjgofegfhiaaklklklaaaeaaamaaabaaab
aaabaaaaaaaaaaaafpfegjgoheedgpgmgphcaaklaaabaaadaaabaaaeaaabaaaa
aaaaaaaahahdfpddfpdaaadccodacodbdbdgdcdgcodaaaklaaaaaaaaaaaaaafe
baaaacaaaaaaaaaiaaaaaaaaaaaabiecaaabaaadaaaaaaabaaaadafaaaaapbka
aaabbaacaaaabcaameaaaaaaaaaadaadaaaaccaaaaaaaaaabaaiaaabbpbppgii
aaaaeaaamiapabacaaaaaaaacaaaaaaamiapababaaaaaaaaobacabaamiapiaaa
aaaaaaaaobabaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
}

SubProgram "ps3 " {
Keywords { "SOFTPARTICLES_OFF" }
Vector 0 [_Color]
SetTexture 0 [_MainTex] 2D
"sce_fp_rsx // 4 instructions using 2 registers
[Configuration]
24
ffffffff000040250001ffff000000000000840002000000
[Offsets]
1
_Color 1 0
00000020
[Microcode]
64
9e001700c8011c9dc8000001c8003fe13e020200c8011c9dc8020001c8003fe1
000000000000000000000000000000001e810200c8041c9dc8001001c8000001
"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_OFF" }
"!!GLES"
}

SubProgram "opengl " {
Keywords { "SOFTPARTICLES_ON" }
Vector 0 [_ZBufferParams]
Vector 1 [_Color]
Float 2 [_InvFade]
SetTexture 0 [_CameraDepthTexture] 2D
SetTexture 1 [_MainTex] 2D
"!!ARBfp1.0
OPTION ARB_precision_hint_fastest;
# 11 ALU, 2 TEX
PARAM c[4] = { program.local[0..2],
		{ 2 } };
TEMP R0;
TEMP R1;
TXP R1.x, fragment.texcoord[1], texture[0], 2D;
TEX R0, fragment.texcoord[0], texture[1], 2D;
MAD R1.x, R1, c[0].z, c[0].w;
RCP R1.x, R1.x;
ADD R1.x, R1, -fragment.texcoord[1].z;
MUL_SAT R1.w, R1.x, c[2].x;
MOV R1.xyz, fragment.color.primary;
MUL R1.w, fragment.color.primary, R1;
MUL R1, R1, c[1];
MUL R0, R1, R0;
MUL result.color, R0, c[3].x;
END
# 11 instructions, 2 R-regs
"
}

SubProgram "d3d9 " {
Keywords { "SOFTPARTICLES_ON" }
Vector 0 [_ZBufferParams]
Vector 1 [_Color]
Float 2 [_InvFade]
SetTexture 0 [_CameraDepthTexture] 2D
SetTexture 1 [_MainTex] 2D
"ps_2_0
; 10 ALU, 2 TEX
dcl_2d s0
dcl_2d s1
def c3, 2.00000000, 0, 0, 0
dcl v0
dcl t0.xy
dcl t1
texldp r0, t1, s0
texld r1, t0, s1
mad r0.x, r0, c0.z, c0.w
rcp r0.x, r0.x
add r0.x, r0, -t1.z
mul_sat r0.x, r0, c2
mov_pp r2.xyz, v0
mul_pp r2.w, v0, r0.x
mul r0, r2, c1
mul r0, r0, r1
mul r0, r0, c3.x
mov_pp oC0, r0
"
}

SubProgram "xbox360 " {
Keywords { "SOFTPARTICLES_ON" }
Float 2 [_InvFade]
Vector 1 [_Color]
Vector 0 [_ZBufferParams]
SetTexture 0 [_MainTex] 2D
SetTexture 1 [_CameraDepthTexture] 2D
"ps_360
backbbaaaaaaabfmaaaaaakiaaaaaaaaaaaaaaceaaaaaaaaaaaaabdaaaaaaaaa
aaaaaaaaaaaaabaiaaaaaabmaaaaaapkppppadaaaaaaaaafaaaaaabmaaaaaaaa
aaaaaapdaaaaaaiaaaadaaabaaabaaaaaaaaaajeaaaaaaaaaaaaaakeaaacaaac
aaabaaaaaaaaaalaaaaaaaaaaaaaaamaaaadaaaaaaabaaaaaaaaaajeaaaaaaaa
aaaaaamjaaacaaabaaabaaaaaaaaaaneaaaaaaaaaaaaaaoeaaacaaaaaaabaaaa
aaaaaaneaaaaaaaafpedgbgngfhcgbeegfhahegifegfhihehfhcgfaaaaaeaaam
aaabaaabaaabaaaaaaaaaaaafpejgohgeggbgegfaaklklklaaaaaaadaaabaaab
aaabaaaaaaaaaaaafpengbgjgofegfhiaafpfegjgoheedgpgmgphcaaaaabaaad
aaabaaaeaaabaaaaaaaaaaaafpfkechfgggggfhcfagbhcgbgnhdaahahdfpddfp
daaadccodacodbdbdgdcdgcodaaaklklaaaaaaaaaaaaaakibaaaaeaaaaaaaaai
aaaaaaaaaaaacigdaaadaaahaaaaaaabaaaadafaaaaapbfbaaaapckaaafaeaac
aaaabcaameaaaaaaaaaagaagbaambcaaccaaaaaaemehaaadaamamabloaacacab
miadacaeaamglaaaobaaabaabaaiaaabbpbppgiiaaaaeaaababidaibbpbppbpp
aaaaeaaamiaiacadaablmgbliladaaaaemihadadaamamablkbadabadmiabacab
acblmgaaoaadabaabfabacabaagmgmblkbabacacamcbacacaagmgmblmaababab
miaiacadaalbgmaaobacacaamiapiaaaaaaaaaaaobadaaaaaaaaaaaaaaaaaaaa
aaaaaaaa"
}

SubProgram "ps3 " {
Keywords { "SOFTPARTICLES_ON" }
Vector 0 [_ZBufferParams]
Vector 1 [_Color]
Float 2 [_InvFade]
SetTexture 0 [_CameraDepthTexture] 2D
SetTexture 1 [_MainTex] 2D
"sce_fp_rsx // 16 instructions using 2 registers
[Configuration]
24
ffffffff0000c0250003fffd000000000000840002000000
[Offsets]
3
_ZBufferParams 1 0
00000050
_Color 1 0
000000d0
_InvFade 1 0
000000a0
[Microcode]
256
b6001800c8011c9dc8000001c8003fe108000500a6001c9dc8020001c8000001
00013f7f00013b7f0001377f000000003e800140c8011c9dc8000001c8003fe1
08000400c8001c9dc8020001fe02000100000000000000000000000000000000
08001a0054001c9dc8000001c80000011e7e7d00c8001c9dc8000001c8000001
a8000300c8011c9fc8000001c8003fe108008200c8001c9d00020000c8000001
0000000000000000000000000000000010800200c9001c9d54000001c8000001
1e020200c9001c9dc8020001c800000100000000000000000000000000000000
9e001702c8011c9dc8000001c8003fe11e810200c8041c9dc8001001c8000001
"
}

SubProgram "gles " {
Keywords { "SOFTPARTICLES_ON" }
"!!GLES"
}

}

#LINE 79
 
		}
	} 	
	
	// ---- Dual texture cards
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				constantColor [_Color]
				combine constant * primary
			}
			SetTexture [_MainTex] {
				combine texture * previous DOUBLE
			}
		}
	}
	
	// ---- Single texture cards (does not do color tint)
	SubShader {
		Pass {
			SetTexture [_MainTex] {
				combine texture * primary
			}
		}
	}
}
}
