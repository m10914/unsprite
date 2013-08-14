Shader "Custom/PostEffectLighting" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_LightTex ("Light tex", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Cull Off
		ZTest Always

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _LightTex;

		struct Input {
			float2 uv_MainTex;
			float2 uv_LightTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			half3 c = tex2D (_MainTex, IN.uv_MainTex).rgb * float3(0.5f, 0.5f, 0.5f);
			half3 d = tex2D (_LightTex, IN.uv_LightTex).rgb * 2f + float3(0.5f, 0.5f, 0.5f);

			o.Albedo = c.rgb * d.rgb;
			o.Alpha = 1;
			o.Emission = o.Albedo;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
