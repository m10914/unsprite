Shader "Custom/DisplayGridlines" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		scale ("float scale", Float) = 10
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Lighting Off
		Cull Off

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		float scale;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {

			float fmd = fmod(IN.worldPos.x/scale, 1);
			float fmy = fmod(IN.worldPos.y/scale, 1);
			if( fmd > 0.05 || fmd < -0.05 || fmy > 0.05 || fmy < - 0.05 || IN.worldPos.x < 0 || IN.worldPos.y < 0) clip(-1);
			
			o.Albedo = float3(1,1,1);
			o.Alpha = 1;
			o.Emission = o.Albedo;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
