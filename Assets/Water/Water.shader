Shader "Unlit/Water"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags {"Queue" = "AlphaTest" "RenderType"="Transparent" }
		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog

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
				float4 screenPos : TEXCOORD1;
				float3 viewVector : TEXCOORD2;
				float3 worldPos : TEXCOORD3;
				float3 worldNormal : TEXCOORD4;
			};

			static const float tau = 2 * 3.1415;

			float4 _Color;
			float4 shallowCol;
			float4 deepCol;
			float colDepthFactor;
			float edgeFade;
			float alphaFresnelPow;
			float3 params;
			float3 dirToSun;
			float planetBoundsSize;

			float smoothness;
			sampler2D _CameraDepthTexture;
			sampler2D waveNormalA;
			sampler2D waveNormalB;
			sampler2D foamNoiseTex;
			sampler3D DensityTex;


			float4 triplanarOffset(float3 vertPos, float3 normal, float3 scale, sampler2D tex, float2 offset) {
				float3 scaledPos = vertPos / scale;
				float4 colX = tex2D (tex, scaledPos.zy + offset);
				float4 colY = tex2D(tex, scaledPos.xz + offset);
				float4 colZ = tex2D (tex,scaledPos.xy + offset);
				
				// Square normal to make all values positive + increase blend sharpness
				float3 blendWeight = normal * normal;
				// Divide blend weight by the sum of its components. This will make x + y + z = 1
				blendWeight /= dot(blendWeight, 1);
				return colX * blendWeight.x + colY * blendWeight.y + colZ * blendWeight.z;
			}

			// Reoriented Normal Mapping
			// http://blog.selfshadow.com/publications/blending-in-detail/
			// Altered to take normals (-1 to 1 ranges) rather than unsigned normal maps (0 to 1 ranges)
			float3 blend_rnm(float3 n1, float3 n2)
			{
				n1.z += 1;
				n2.xy = -n2.xy;

				return n1 * dot(n1, n2) / n1.z - n2;
			}

			// Sample normal map with triplanar coordinates
			// Returned normal will be in obj/world space (depending whether pos/normal are given in obj or world space)
			// Based on: medium.com/@bgolus/normal-mapping-for-a-triplanar-shader-10bf39dca05a
			float3 triplanarNormal(float3 vertPos, float3 normal, float3 scale, float2 offset, sampler2D normalMap) {
				float3 absNormal = abs(normal);

				// Calculate triplanar blend
				float3 blendWeight = saturate(pow(normal, 4));
				// Divide blend weight by the sum of its components. This will make x + y + z = 1
				blendWeight /= dot(blendWeight, 1);

				// Calculate triplanar coordinates
				float2 uvX = vertPos.zy * scale + offset;
				float2 uvY = vertPos.xz * scale + offset;
				float2 uvZ = vertPos.xy * scale + offset;

				// Sample tangent space normal maps
				// UnpackNormal puts values in range [-1, 1] (and accounts for DXT5nm compression)
				float3 tangentNormalX = UnpackNormal(tex2D(normalMap, uvX));
				float3 tangentNormalY = UnpackNormal(tex2D(normalMap, uvY));
				float3 tangentNormalZ = UnpackNormal(tex2D(normalMap, uvZ));

				// Swizzle normals to match tangent space and apply reoriented normal mapping blend
				tangentNormalX = blend_rnm(half3(normal.zy, absNormal.x), tangentNormalX);
				tangentNormalY = blend_rnm(half3(normal.xz, absNormal.y), tangentNormalY);
				tangentNormalZ = blend_rnm(half3(normal.xy, absNormal.z), tangentNormalZ);

				// Apply input normal sign to tangent space Z
				float3 axisSign = sign(normal);
				tangentNormalX.z *= axisSign.x;
				tangentNormalY.z *= axisSign.y;
				tangentNormalZ.z *= axisSign.z;

				// Swizzle tangent normals to match input normal and blend together
				float3 outputNormal = normalize(
					tangentNormalX.zyx * blendWeight.x +
					tangentNormalY.xzy * blendWeight.y +
					tangentNormalZ.xyz * blendWeight.z
				);

				return outputNormal;
			}

			v2f vert (appdata_base v)
			{
				v2f o;
				float3 worldPos =  mul(unity_ObjectToWorld,v.vertex).xyz;
			

				float vertexAnimWeight = length(worldPos - _WorldSpaceCameraPos);
				vertexAnimWeight = saturate(pow(vertexAnimWeight / 10, 3));

				// Vertex wave anim
				float waveAnimDetail = 100;
				float maxWaveAmplitude = 0.001 * vertexAnimWeight; // 0.001
				float waveAnimSpeed = 1;

				float3 worldNormal = normalize(mul(unity_ObjectToWorld, float4(v.normal, 0)).xyz);
				float theta = acos(worldNormal.z);
				float phi = atan2(v.vertex.y, v.vertex.x);
				float waveA = sin(_Time.y * waveAnimSpeed + theta * waveAnimDetail);
				float waveB = sin(_Time.y * waveAnimSpeed + phi * waveAnimDetail);
				float waveVertexAmplitude = (waveA + waveB) * maxWaveAmplitude;
				v.vertex = v.vertex + float4(worldNormal, 0) * waveVertexAmplitude;

				// Set output properties
				o.worldNormal = worldNormal;
				o.worldPos = worldPos;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.vertex = UnityObjectToClipPos(v.vertex + float4(1, 1, 1, 0) * sin(_Time.x * 50) * 0.05);
				o.uv = v.texcoord.xy;
				o.screenPos = ComputeScreenPos(o.vertex);

				float3 viewVector = mul(unity_CameraInvProjection, float4((o.screenPos.xy/o.screenPos.w) * 2 - 1, 0, -1));
				o.viewVector = mul(unity_CameraToWorld, float4(viewVector,0));

				return o;
			}

			float calculateSpecular(float3 normal, float3 viewDir, float smoothness) {

				float specularAngle = acos(dot(normalize(dirToSun - viewDir), normal));
				float specularExponent = specularAngle / smoothness;
				float specularHighlight = exp(-specularExponent * specularExponent);
				return specularHighlight;
			}

			float4 test(float v) {
				return float4(v,v,v,1);
			}

			float3 worldToTexPos(float3 worldPos) {
				return worldPos / planetBoundsSize + 0.5;
			}


			fixed4 frag (v2f i) : SV_Target
			{

				float3 t = worldToTexPos(i.worldPos);
				float density = tex3D(DensityTex, t);

				//return test(density * params.x);
				float3 viewDir = normalize(i.viewVector);
				
				// -------- Specularity --------
				// Specular normal
				float waveSpeed = 0.35;
				float waveNormalScale = 0.05;
				float waveStrength = 0.4;
				
				float2 waveOffsetA = float2(_Time.x * waveSpeed, _Time.x * waveSpeed * 0.8);
				float2 waveOffsetB = float2(_Time.x * waveSpeed * - 0.8, _Time.x * waveSpeed * -0.3);
				float3 waveNormal1 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetA, waveNormalA);
				float3 waveNormal2 = triplanarNormal(i.worldPos, i.worldNormal, waveNormalScale, waveOffsetB, waveNormalB);
				float3 waveNormal = triplanarNormal(i.worldPos, waveNormal1, waveNormalScale, waveOffsetB, waveNormalB);
				float3 specWaveNormal = normalize(lerp(i.worldNormal, waveNormal, waveStrength));

				float f2 = dot(i.worldNormal, dirToSun);
				f2 = smoothstep(0,0.2,f2);
				//float v = max(max(abs(waveNormal.r), abs(waveNormal.g)), abs(waveNormal.b));
				//return test(v);
				float g = 1-((pow(dot(waveNormal1,i.worldNormal), 0.9)) > 0.93);
				float g2 = 1-((pow(dot(waveNormal2,i.worldNormal),0.9)) > 0.93);
				float glitter = g*g2 * 0.2 * f2;
				//return test(glitter);

				// Specular highlight
				float specThreshold = 0.7;
				
				float specularHighlight = calculateSpecular(specWaveNormal, viewDir, smoothness);

				float steppedSpecularHighlight = 0;
				steppedSpecularHighlight += (specularHighlight > specThreshold);
				steppedSpecularHighlight += (specularHighlight > specThreshold * 0.4) * 0.4;
				steppedSpecularHighlight += (specularHighlight > specThreshold * 0.2) * 0.2;
				specularHighlight = steppedSpecularHighlight;
				
				// -------- Calculate water depth --------
				float nonLinearDepth = SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, i.screenPos);
				float depth = LinearEyeDepth(nonLinearDepth);
				float dstToWater = i.screenPos.w;
				float waterViewDepth = depth - dstToWater;
				float waterDensityMap = density * 500;

				// -------- Foam --------
				float foamSize = 4; // 2.25
				float foamSpeed = 0.5;
				float foamNoiseScale = 13;
				float foamNoiseStrength = 4.7;
				float numFoamLines = 2.5;
				float2 noiseScroll = float2(_Time.x, - _Time.x * 0.3333) * 0.25;
				float foamNoise = triplanarOffset(i.worldPos, i.worldNormal, foamNoiseScale, foamNoiseTex, noiseScroll);
				foamNoise = smoothstep(0.2,.8,foamNoise);
				
				/*
				

				float foam = saturate(waterDensityMap / foamSize);
				float numFoamLines = 2.75;
				float foamAnim = sin((foam  - _Time.y * foamSpeed) * tau * (numFoamLines - 1)) * (1-foam) * 0.5 + 0.5;
				foam = min(foamAnim, foam);
				foam = foam < min(0.5, 0.5 + (foamNoise - 0.5) * foamNoiseStrength);
				*/
				float foamT = saturate(waterDensityMap / foamSize);
					
				//return test(foamNoise * (1-foam));
				float foamTime = _Time.y * foamSpeed;
				float mask = (1-foamT);
				float mask2 = smoothstep(1, 0.6, foamT) * (foamNoise-.5);
				mask2 = lerp(1, mask2, 1-(1-foamT)*(1-foamT));
			
				float v = sin(foamT * 3.1415 * (1 + 2 * (numFoamLines-1)) - foamTime) * (mask>0);
		
				v = saturate(v) + (foamT < 0.35 + foamNoise * 0.15);

				//return test(foamT);
			
				float foamAlpha = smoothstep(1,0.7,foamT);
			
				foamAlpha *= f2;
				//return test(f2);
				float foam = (v > 1-mask2) * foamAlpha;

		
				//float foamAnim = sin((foam  - _Time.y * foamSpeed) * tau * (numFoamLines - 1)) * (1-foam) * 0.5 + 0.5;
				//foam = min(foamAnim, foam);
				//foam = foam < 0.5;

				// -------- Water Transparency --------
				// Make water appear more transparent when viewed from above
		
				float alphaFresnel = 1-saturate(pow(saturate(dot(-viewDir, i.worldNormal)), alphaFresnelPow));
				alphaFresnel = max(0.7, alphaFresnel);
				float alphaFresnelNearFix = pow(saturate((i.screenPos.w - _ProjectionParams.y) / 4), 3);
				alphaFresnel = lerp(1, alphaFresnel, alphaFresnelNearFix);

				// Fade water at intersection with geometry
				float alphaEdge = 1 - exp(-waterViewDepth * edgeFade);

				// Dont want distant water to have any transparency because transparent water against sky causes issue with atmosphere shader
				//float opaqueWaterDst = 40;
				//float waterDstAlpha = saturate(dstToWater / opaqueWaterDst);
	
				// Calculate final alpha
				//return test(waterDstAlpha);
				float opaqueWater = max(0, max(foam, specularHighlight > 0.5));
				float alpha = saturate(max(opaqueWater, alphaEdge * alphaFresnel));
				//return test(alphaFresnel);
				
				// -------- Lighting and colour output --------
				float lighting = saturate(dot(i.worldNormal, dirToSun));

				
				float3 col = lerp(shallowCol, deepCol, 1-exp(-waterViewDepth * colDepthFactor));
				col = saturate(col * lighting + unity_AmbientSky) + specularHighlight;
				col = col + foam + glitter;

				return float4(col, alpha);
			}
			ENDCG
		}
	}
}
