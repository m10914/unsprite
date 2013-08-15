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

		// 0 pass - grab pass
		GrabPass { }

		// 1st pass - multiplying light textures
		Pass
		{
			CGPROGRAM
			#pragma fragment pix
			#pragma vertex vert

			sampler2D _LightTex;
			sampler2D _GrabTexture;

			struct a2v
			{
				float4 vertex	: POSITION;
				float2 texcoord	: TEXCOORD;
			};

			struct v2f
			{
				float4 position : POSITION;
				float2 texcoord: TEXCOORD;
			};

			v2f vert(a2v IN) 
			{ 
				v2f OUT;

				float4 pos = float4(IN.vertex.xyz, 1.0);
				OUT.position 	= mul(UNITY_MATRIX_MVP, pos);
				OUT.texcoord = IN.texcoord.xy;
				return OUT; 
			}

			float4 pix ( v2f IN ) : COLOR
			{
				float4 color = tex2D(_GrabTexture, IN.texcoord);
				float4 color2 = tex2D(_LightTex, IN.texcoord);
				return sqrt(color*color + color2*color2);
			}
			ENDCG
		}


		//2nd pass - apply result lightmap to source image
		Pass
		{
			CGPROGRAM
			#pragma fragment pix
			#pragma vertex vert

			sampler2D _MainTex;
			sampler2D _GrabTexture;

			struct a2v
			{
				float4 vertex	: POSITION;
				float2 texcoord	: TEXCOORD;
			};

			struct v2f
			{
				float4 position : POSITION;
				float2 texcoord: TEXCOORD;
			};

			v2f vert(a2v IN) 
			{ 
				v2f OUT;

				float4 pos = float4(IN.vertex.xyz, 1.0);
				OUT.position 	= mul(UNITY_MATRIX_MVP, pos);
				OUT.texcoord = IN.texcoord.xy;
				return OUT; 
			}

			float4 pix ( v2f IN ) : COLOR
			{
				float4 mainColor = tex2D(_MainTex, IN.texcoord);
				float4 lightColor = tex2D(_GrabTexture, IN.texcoord);// + float4(0.2, 0.2, 0.2, 0);

				return mainColor * lightColor;
			}
			ENDCG
		}
	} 
}
