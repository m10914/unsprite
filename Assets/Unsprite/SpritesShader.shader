Shader "Custom/SpritesShader" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_ChromoKey ("Chromokey", Vector) = (0,0,0,0)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		Lighting Off

		CGPROGRAM
		#pragma surface surf Lambert

		sampler2D _MainTex;
		float4 _ChromoKey;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {

			half4 c = tex2D (_MainTex, IN.uv_MainTex);
			float3 dif = c.xyz - _ChromoKey.xyz;
			float diff = length(dif);
			if(diff < 0.1) clip(-1);

			o.Albedo = c.rgb;
			o.Alpha = c.a;
			o.Emission = o.Albedo;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
