Shader "VoxelShaders/Blocks"
{
	Properties
	{
		_MainTex("Block Texture Atlas", 2D) = "white" {}
	}
		SubShader
	{
		//Tags { "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}
		Tags { "RenderType" = "Opaque"}
		LOD 100
		Lighting Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vertexFunction
			#pragma fragment fragmentFunction
			#pragma target 2.0

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float GlobalLightLevel;
			float MinGlobalLightLevel;
			float MaxGlobalLightLevel;

			v2f vertexFunction(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.color = v.color;

				return o;
			}

			fixed4 fragmentFunction(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);		// Sample the texture into a color per pixel
				
				float shade = (MaxGlobalLightLevel - MinGlobalLightLevel) * GlobalLightLevel + MinGlobalLightLevel;
				shade *= i.color.a;
				shade = clamp(1 - shade, MinGlobalLightLevel, MaxGlobalLightLevel);
			
				//clip(col.a - 1);						// Remove the alpha if less then 0
				col = lerp(col, float4(0, 0, 0, 1), shade);

				return col;
			}

			ENDCG
		}
	}
}
