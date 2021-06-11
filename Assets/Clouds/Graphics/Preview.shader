Shader "Unlit/Preview"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags
		{ 
			"RenderType"="Opaque"
			"PreviewType"="Plane"
		}
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			Texture3D<float4> NoiseTex;
			SamplerState samplerNoiseTex;

			// Debug settings:
			int debugGreyscale;
			int debugShowAllChannels;
			float debugNoiseSliceDepth;
			float4 debugChannelWeight;
			float debugTileAmount;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float2 squareUV(float2 uv) {
				float width = _ScreenParams.x;
				float height =_ScreenParams.y;
				float scale = 1000;
				float x = uv.x * width;
				float y = uv.y * height;
				return float2 (x/scale, y/scale);
			}


			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;

				float3 samplePos = float3(uv.xy, debugNoiseSliceDepth);
				float4 channels = NoiseTex.SampleLevel(samplerNoiseTex, samplePos, 0);
				
				if (debugShowAllChannels) {
					return channels;
				}
				else {
					float4 maskedChannels = (channels*debugChannelWeight);
					if (debugGreyscale || debugChannelWeight.w == 1) {
						return dot(maskedChannels,1);
					}
					else {
						return maskedChannels;
					}
				}
			}
			
			ENDCG
		}
	}
}
