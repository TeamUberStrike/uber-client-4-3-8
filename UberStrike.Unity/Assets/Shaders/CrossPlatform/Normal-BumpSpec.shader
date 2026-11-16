Shader "Cross Platform Shaders/Bumped Specular" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 1)
		_Shininess ("Shininess", Range (0.03, 1)) = 0.078125
		_MainTex ("Base (RGB) Gloss (A)", 2D) = "white" {}
		_BumpMap ("Normalmap", 2D) = "bump" {}
	}
	SubShader { 
		Tags { "RenderType"="Opaque" }
		LOD 400
	
		CGPROGRAM
			#pragma exclude_renderers gles
			#pragma surface surf BlinnPhong

			sampler2D _MainTex;
			sampler2D _BumpMap;
			fixed4 _Color;
			half _Shininess;

			struct Input {
				float2 uv_MainTex;
				float2 uv_BumpMap;
			};

			void surf (Input IN, inout SurfaceOutput o) {
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = tex.rgb * _Color.rgb;
				o.Gloss = tex.a;
				o.Alpha = tex.a * _Color.a;
				o.Specular = _Shininess;
				o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			}
		ENDCG
	}

	SubShader { 
		Tags { "RenderType"="Opaque" }
		LOD 250
	
		CGPROGRAM
			#pragma only_renderers gles
			#pragma surface surf MobileBlinnPhong exclude_path:prepass nolightmap noforwardadd halfasview

			inline fixed4 LightingMobileBlinnPhong (SurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
			{
				fixed diff = max (0, dot (s.Normal, lightDir));
				fixed nh = max (0, dot (s.Normal, halfDir));
				fixed spec = pow (nh, s.Specular*128) * s.Gloss;
	
				fixed4 c;
				c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * (atten*2);
				c.a = 0.0;
				return c;
			}

			sampler2D _MainTex;
			sampler2D _BumpMap;
			half _Shininess;

			struct Input {
				float2 uv_MainTex;
			};

			void surf (Input IN, inout SurfaceOutput o) {
				fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
				o.Albedo = tex.rgb;
				o.Gloss = tex.a;
				o.Alpha = tex.a;
				o.Specular = _Shininess;
				o.Normal = UnpackNormal (tex2D(_BumpMap, IN.uv_MainTex));
			}
		ENDCG
	}

	FallBack "Specular"
}
