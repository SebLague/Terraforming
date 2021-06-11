Shader "Custom/Water Depth Replacement"
{
	SubShader
	{
		Pass
		{
			Tags {"LightMode"="ShadowCaster"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f { 
				V2F_SHADOW_CASTER;
			};

			v2f vert(appdata_base v)
			{
				v2f o;

				// Vertex wave anim
				float3 worldPos =  mul(unity_ObjectToWorld,v.vertex).xyz;

				float vertexAnimWeight = length(worldPos - _WorldSpaceCameraPos);
				vertexAnimWeight = saturate(pow(vertexAnimWeight / 10, 3));

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

				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}
}