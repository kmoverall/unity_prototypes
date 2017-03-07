Shader "Debug/Normal Dot View"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
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
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 worldPos : TEXCOORD0;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float3 worldNormal = UnityObjectToWorldNormal(i.normal);
				float3 cameraDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				float NdotV = dot(worldNormal, cameraDir);

				return float4(-1 * NdotV, NdotV, 0, 1);
			}
			ENDCG
		}
	}
}
