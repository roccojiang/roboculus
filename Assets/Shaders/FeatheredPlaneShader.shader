Shader "Unlit/FeatheredPlaneShader"
{
	Properties
	{
		_Radius ("Radius", float) = 3.0
		_MainTex ("Texture", 2D) = "white" {}
		_TexTintColor("Texture Tint Color", Color) = (1,1,1,1)
		_PlaneColor("Plane Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		// Render the Occlusion shader before all
        // opaque geometry to prime the depth buffer.
        Tags { "Queue"="Geometry-1" }
		
		LOD 100
		Blend SrcAlpha OneMinusSrcAlpha
		
		ZWrite On
		ZTest LEqual

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
				float4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _TexTintColor;
			fixed4 _PlaneColor;
			float _ShortestUVMapping;
			float _Radius;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.color = v.color;
				
				float dist = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex));
				float alpha = smoothstep(0, _Radius, dist); 
				o.color.a = 1 - alpha;
				
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv) * _TexTintColor;
				col = lerp( _PlaneColor, col, col.a);
				col.a *= i.color.a;
				return col;
			}
			ENDCG
		}
	}
}
