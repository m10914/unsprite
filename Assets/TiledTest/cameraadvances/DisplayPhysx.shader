Shader "Custom/DisplayPhysx" {
	Properties {
		_ChromoKey ("Chromokey", Vector) = (0,0,0,0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest Always

		CGPROGRAM
		#pragma surface surf Lambert

		float4 _ChromoKey;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = _ChromoKey.rgb;
			o.Alpha = 0.4;
			o.Emission = o.Albedo;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
