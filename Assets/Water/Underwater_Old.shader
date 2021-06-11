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
			float3 scatteringCoefficients;
			float intensity;
			float density;
			float3 params;

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture;

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

			float3 calculateLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 originalCol, float2 uv) {
				const int numSteps = 10;
				float3 inScatteredLight = 0;
				float stepSize = rayLength / (numSteps - 1.0);
				float3 dirToSun = _WorldSpaceLightPos0.xyz;
				float dstToCam = 0;
				
				for (int i = 0; i < numSteps; i ++) {
					float dstThroughWaterToSun = raySphere(oceanCentre, oceanRadius, rayOrigin, dirToSun).y;
					float opticalDepthToSun = density * dstThroughWaterToSun * params.x;
					float opticalDepthToCam = density * dstToCam * params.y;
					
					float3 transmittance = exp(-(opticalDepthToSun + opticalDepthToCam) * scatteringCoefficients);
					inScatteredLight += density * transmittance;

					rayOrigin += rayDir * stepSize;
					dstToCam += stepSize;
				}
				inScatteredLight *= scatteringCoefficients * intensity * stepSize;
				float3 tOriginal = exp(-(dstToCam * density) * scatteringCoefficients);
				float3 o = originalCol * tOriginal;
				float w = exp(-rayLength * params.z);
			//	return w;
				return originalCol * w + inScatteredLight * (1-w);
				return inScatteredLight;//
			}


			float4 frag (v2f i) : SV_Target
			{
				float4 originalCol = tex2D(_MainTex, i.uv);

				float3 rayPos = _WorldSpaceCameraPos;
				float viewLength = length(i.viewVector);
				float3 rayDir = i.viewVector / viewLength;

				float nonlin_depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
				float sceneDepth = LinearEyeDepth(nonlin_depth) * viewLength;

				float2 hitInfo = raySphere(oceanCentre, oceanRadius, rayPos, rayDir);
				float dstToOcean = hitInfo.x;
				float dstThroughOcean = hitInfo.y;
				float3 rayOceanIntersectPos = rayPos + rayDir * dstToOcean - oceanCentre;

				// dst that view ray travels through ocean (before hitting terrain / exiting ocean)
				float oceanViewDepth = min(dstThroughOcean, sceneDepth - dstToOcean);


				if (oceanViewDepth > 0) {
					float3 clipPlanePos = rayPos + i.viewVector * _ProjectionParams.y;

					float dstAboveWater = oceanRadius - length(clipPlanePos - oceanCentre);
					if (dstAboveWater > 0) {
						const float epsilon = 0.0001;
			
						float3 light = calculateLight(rayPos + rayDir * epsilon, rayDir, oceanViewDepth - epsilon * 2, originalCol.rgb, i.uv.xy);
						return float4(light, 1);
					}

				}

				return originalCol;
			}
			ENDCG
		}
	}
}
