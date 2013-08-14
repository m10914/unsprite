Shader "Custom/RenderLightShader" {
	Properties {
		_Colour ("color", Vector) = (1,1,1,1)
		_LightPos ("lightpos", Vector) = (1,1,1,1)
		_MaxDist ("maxdist", Float) = (1,1,1,1)
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		ZTest Always
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGPROGRAM
		#pragma surface surf Lambert

		float4 _Colour;
		float4 _LightPos;
		float _MaxDist;

		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		void surf (Input IN, inout SurfaceOutput o) {

			float alpha = 1.0 - length(IN.worldPos.xy - _LightPos.xy) / _MaxDist;

			o.Albedo = _Colour.rgb;
			o.Alpha = alpha;
			o.Emission = o.Albedo;
		}
		ENDCG
	} 
	FallBack "Diffuse"
}
