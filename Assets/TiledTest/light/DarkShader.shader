Shader "Custom/DarkShader" {
	Properties {
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		ZTest Always
		
		CGPROGRAM
		#pragma surface surf Lambert

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {

			o.Albedo = float3(0,0,0);
			o.Alpha = 1;
			o.Emission = o.Albedo;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
