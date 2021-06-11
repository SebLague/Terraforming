Shader "Hidden/Underwater"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
				float4 viewVector : TEXCOORD1;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;

				float3 viewVector = mul(unity_CameraInvProjection, float4(v.uv * 2 - 1, 0, -1));
				o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));

				return o;
			}

			float3 oceanCentre;
			float oceanRadius;
			float maxVisibility;
			float density;
			float blurDistance;
			float4 underwaterNearCol;
			float4 underwaterFarCol;
			float3 params;

			sampler2D _MainTex;
			sampler2D _BlurredTexture;
			sampler2D _CameraDepthTexture;
			sampler2D _BlueNoise;

			float2 squareUV(float2 uv) {
				float width = _ScreenParams.x;
				float height =_ScreenParams.y;
				//float minDim = min(width, height);
				float scale = 1000;
				float x = uv.x * width;
				float y = uv.y * height;
				return float2 (x/scale, y/scale);
			}

			// Returns vector (dstToSphere, dstThroughSphere)
			// If ray origin is inside sphere, dstToSphere = 0
			// If ray misses sphere, dstToSphere = maxValue; dstThroughSphere = 0
			float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir) {
				float3 offset = rayOrigin - sphereCentre;
				float a = 1; // Set to dot(rayDir, rayDir) if rayDir might not be normalized
				float b = 2 * dot(offset, rayDir);
				float c = dot (offset, offset) - sphereRadius * sphereRadius;
				float d = b * b - 4 * a * c; // Discriminant from quadratic formula

				// Number of intersections: 0 when d < 0; 1 when d = 0; 2 when d > 0
				if (d > 0) {
					float s = sqrt(d);
					float dstToSphereNear = max(0, (-b - s) / (2 * a));
					float dstToSphereFar = (-b + s) / (2 * a);

					// Ignore intersections that occur behind the ray
					if (dstToSphereFar >= 0) {
						return float2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
					}
				}
				// Ray did not intersect sphere
				static const float maxFloat = 3.402823466e+38;
				return float2(maxFloat, 0);
			}


			float4 frag (v2f i) : SV_Target
			{
				float blueNoise = tex2D(_BlueNoise, squareUV(i.uv) * params.x);
				blueNoise = blueNoise * 2 - 1;
				blueNoise = sign(blueNoise) * (1 - sqrt(1 - abs(blueNoise)));
				//return blueNoise;

				float4 originalCol = tex2D(_MainTex, i.uv);

				float3 rayPos = _WorldSpaceCameraPos;
				float viewLength = length(i.viewVector);
				float3 rayDir = i.viewVector / viewLength;

				float nonlin_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float sceneDepth = LinearEyeDepth(nonlin_depth) * viewLength;

				float2 hitInfo = raySphere(oceanCentre, oceanRadius, rayPos, rayDir);
				float dstToOcean = hitInfo.x;
				float dstThroughOceanShell = hitInfo.y;
				float3 rayOceanIntersectPos = rayPos + rayDir * dstToOcean - oceanCentre;

				// dst that view ray travels through ocean (before hitting terrain / exiting ocean)
				float oceanViewDepth = min(dstThroughOceanShell, sceneDepth - dstToOcean);


				if (oceanViewDepth > 0) {
					float3 clipPlanePos = rayPos + i.viewVector * _ProjectionParams.y;

					float dstAboveWater = oceanRadius - length(clipPlanePos - oceanCentre);
					if (dstAboveWater > 0) {
						// Looking through water to top layer of ocean
						

						
						float4 blurredCol = tex2D(_BlurredTexture, i.uv);
						float4 bgCol = lerp(originalCol, blurredCol, saturate(oceanViewDepth / blurDistance));

						float visibility = exp(-oceanViewDepth * density * 0.001);
						visibility *= maxVisibility;
						visibility = saturate(visibility + blueNoise * 0.025);

						float4 waterCol = lerp(underwaterFarCol, underwaterNearCol, visibility);
						float4 finalCol = lerp(waterCol, bgCol, visibility);
						if (dstThroughOceanShell <= oceanViewDepth) {
							//return 1;
						}
						return finalCol;

						return bgCol;
					}

				}

				return originalCol;
			}
			ENDCG
		}
	}
}
