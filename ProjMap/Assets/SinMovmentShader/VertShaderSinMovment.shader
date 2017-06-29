// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Object Space"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}

		_Speed("Speed",Range(0.1,4)) = 1
		_Amount("Amount", Range(0.1,10)) = 3
		_Distance("Distance", Range( 0, 2 )) = 0.3
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
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			half _Speed;
			half _Amount;
			half _Distance;
			
			v2f vert (appdata v)
			{
				v2f o;
				float4 _objectOrigin = mul(unity_ObjectToWorld, v.vertex);
				v.vertex.y += _objectOrigin.y;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;

				// Takes the x position of each vert as it was authored and moves it along the object's x-axis
				// The amount it's moved used a sine wave to have a nice wave shape to it
				// The vertex's y position is used otherwise all verts will be moved uniformly
				//v.vertex.y += sin( _Time.z * _Speed + v.vertex.y * _Amount ) * _Distance;

				//v.vertex.y = sin((_Time.y *_Speed + v.vertex *_Amount) * _Distance);

				//v.vertex.y = sin(_Time.y *_Speed);

				//v.vertex.y += sin( _Time.y *_Speed + v.vertex * length(ObjSpaceViewDir(v.vertex.y)));
				
				//moves the y axis in relation to world 0,0,0
				//v.vertex.y += sin( _Time.y *_Speed / (_objectOrigin + 1));				
	
				// moves vertex on Y axis in sin of distance from z times the _distance value
				//v.vertex.y += sin(_objectOrigin.z * _Distance); 

				// This line takes our mesh's vertices and turns them into screen 
				//positions that the fragment shader can fill in
			
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
