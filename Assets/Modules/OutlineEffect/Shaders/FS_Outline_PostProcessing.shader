Shader "FS/Outline/PostProcessing"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BlurTex("Blur", 2D) = "white"{}
	}
	SubShader
	{
		Pass
		{
			Cull Off ZWrite Off ZTest Always

			CGPROGRAM
			#pragma vertex vert_blur
			#pragma fragment frag_blur
			#include "UnityCG.cginc"
			
			struct a2v
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};
			
			struct v2f_blur
			{
				float4 pos : SV_POSITION;
				float2 uv  : TEXCOORD0;
				float4 uv01 : TEXCOORD1;
				float4 uv23 : TEXCOORD2;
				float4 uv45 : TEXCOORD3;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _Offset;
			
			v2f_blur vert_blur(a2v v)
			{
				v2f_blur o;
				_Offset *= _MainTex_TexelSize.xyxy;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;
				o.uv01 = v.texcoord.xyxy + _Offset.xyxy * float4(1, 1, -1, -1);
				o.uv23 = v.texcoord.xyxy + _Offset.xyxy * float4(1, 1, -1, -1) * 2.0;
				o.uv45 = v.texcoord.xyxy + _Offset.xyxy * float4(1, 1, -1, -1) * 3.0;
				
				return o;
			}

			fixed4 frag_blur(v2f_blur i) : SV_Target
			{
				fixed4 color = fixed4(0,0,0,0);
				color += 0.40 * tex2D(_MainTex, i.uv);
				color += 0.15 * tex2D(_MainTex, i.uv01.xy);
				color += 0.15 * tex2D(_MainTex, i.uv01.zw);
				color += 0.10 * tex2D(_MainTex, i.uv23.xy);
				color += 0.10 * tex2D(_MainTex, i.uv23.zw);
				color += 0.05 * tex2D(_MainTex, i.uv45.xy);
				color += 0.05 * tex2D(_MainTex, i.uv45.zw);
				return color;
			}
			ENDCG
		}
		Pass
		{
			Cull Off ZWrite Off ZTest Always
 
			CGPROGRAM
			#pragma vertex vert_cull
			#pragma fragment frag_cull

			#include "UnityCG.cginc"

			struct a2v
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f_cull
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _BlurTex;
			float4 _BlurTex_TexelSize;

			v2f_cull vert_cull(a2v v)
			{
				v2f_cull o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif	

				return o;
			}
 
			fixed4 frag_cull(v2f_cull i) : SV_Target
			{
				fixed4 colorMain = tex2D(_MainTex, i.uv);
				fixed4 colorBlur = tex2D(_BlurTex, i.uv);
				return colorBlur - colorMain;
			}

			ENDCG
		}
		Pass
		{
			Cull Off ZWrite Off ZTest Always
 
			CGPROGRAM
			#pragma vertex vert_add
			#pragma fragment frag_add

			#include "UnityCG.cginc"

			struct a2v
			{
				float4 vertex : POSITION;
				float4 texcoord : TEXCOORD0;
			};

			struct v2f_add
			{
				float4 pos : SV_POSITION;
				float2 uv  : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			sampler2D _BlurTex;
			float4 _BlurTex_TexelSize;
			float _OutlineStrength;

			v2f_add vert_add(a2v v)
			{
				v2f_add o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = v.texcoord.xy;
				o.uv1.xy = o.uv.xy;

				#if UNITY_UV_STARTS_AT_TOP
				if (_MainTex_TexelSize.y < 0)
					o.uv.y = 1 - o.uv.y;
				#endif	

				return o;
			}
 
			fixed4 frag_add(v2f_add i) : SV_Target
			{
				fixed4 ori = tex2D(_MainTex, i.uv1);
				fixed4 blur = tex2D(_BlurTex, i.uv);
				fixed4 final = ori + blur * _OutlineStrength;
				return final;
			}

			ENDCG
		}
	}
}