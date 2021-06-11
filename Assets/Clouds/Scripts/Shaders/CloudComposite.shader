Shader "Hidden/CloudComposite"
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
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			sampler2D _MainTex;
			sampler2D _Background;

			float4 frag (v2f i) : SV_Target
			{
				float4 cloudData = tex2D(_MainTex, i.uv);
				float transmittance = cloudData.a;

				float4 originalCol = tex2D(_Background, i.uv);
				
				
				return float4(originalCol.rgb * transmittance + cloudData.rgb, 1);
			}
			ENDCG
		}
	}
}
