Shader "Debug/Clip Space"
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

			float4 ScreenToClipPos(float4 sPos, float w)
			{
				float4 o = sPos * w;
				#if defined(UNITY_HALF_TEXEL_OFFSET)
				o.xy = float2(o.x, o.y) - w * 0.5 * _ScreenParams.zw;
				#else
				o.xy = float2(o.x, o.y) - w * 0.5;
				#endif
				o.xy *= 2;
				return o;
			}

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.color = ComputeScreenPos(o.vertex);
				o.color /= o.vertex.w;
				o.color = ScreenToClipPos(o.color, o.vertex.w);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				if (_ProjectionParams.x < 0)
					return fixed4(i.color.r, i.color.g * -1, 0, 1);
				else
					return fixed4(i.color.rg, 0,1);
			}
			ENDCG
		}
	}
}
