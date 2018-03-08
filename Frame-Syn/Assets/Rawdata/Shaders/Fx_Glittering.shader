// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Fx/Eff/Equip/Glittering" {
		Properties {
				_texBase ("Base (RGB)", 2D) = "white" {}
				_texEff ("流光1 (RGB)", 2D) = "black" {}
				_texEff1 ("流光2 (RGB)", 2D) = "black" {}
				_texMask("Mask (RGB), R:Eff1,G:Eff2", 2D) = "black" {}

				_eff1Color_s("图层1.开始颜色",Color) = (1,1,1,1)
				_eff1Color_e("图层1.结束颜色",Color) = (1,1,1,1)

				_eff2Color_s("图层2.开始颜色",Color) = (1,1,1,1)
				_eff2Color_e("图层2.结束颜色",Color) = (1,1,1,1)

				uvMode("uv模式",Vector) = (6,0,6,0)
				uvMatFlag("uv选择: x : base UV, y : eff1, z : eff2, 启用设置为大于1",Vector) = (1,1,1,0)

				_eff1uvFlip("uv镜像 X 2,Y 1",float) = 0.0
				_eff2uvFlip("uv镜像 X 2,Y 1",float) = 0.0

				_baseCfgMS("基本UV 平移x, 平移y, 缩放x, 缩放y",Vector) = (0,0,1,1)

				_eff1CfgMS("第一层UV 平移x速度, 平移y速度, 缩放x, 缩放y",Vector) = (0,0,1,1)
				_eff1CfgRotate("UV 旋转速度",float) = 0.0

				_eff2CfgMS("第二层UV 平移x速度, 平移y速度, 缩放x, 缩放y",Vector) = (0,0,1,1)
				_eff2CfgRotate("UV 旋转速度",float) = 0.0

				_effColorPower("color 强度",Vector) = (1,1,1,1)
				_isRim("是否边缘光",float) = 0.0
				_RimPower("边缘强度",Range(0,1)) = 1.0
				_RimColor("边缘光颜色",Color) = (1,1,1,1)

				_alphaTest("Alpha切割",Range(0,1)) = 1.0
				_isLight("是否受光",float) = 1.0
				_isAmbientLight("是否受环境光",float) = 1.0
				_IsFog("是否雾效",float) = 1.0
				_AmbientLightColor("自定义环境光",Color) = (0.8,0.8,0.8,0.0)
			}
			SubShader {
					Tags { "RenderType" = "Opaque" "Queue"="AlphaTest" "LightMode" = "ForwardBase"}
					Pass {
							Name "BASE"

							CGPROGRAM

							#pragma vertex vert
							#pragma fragment frag

							#pragma multi_compile_fog

							#pragma multi_compile USE_TEXUV1_CAMERA_NOR USE_TEXUV1_BASE USE_TEXUV1_CAMERA_REL USE_TEXUV1_CAMERA_POS USE_TEXUV1_NORMAL_XY USE_TEXUV1_NORMAL_YZ USE_TEXUV1_NORMAL_ZX
							#pragma multi_compile USE_TEXUV2_CAMERA_NOR USE_TEXUV2_BASE USE_TEXUV2_CAMERA_REL USE_TEXUV2_CAMERA_POS USE_TEXUV2_NORMAL_XY USE_TEXUV2_NORMAL_YZ USE_TEXUV2_NORMAL_ZX

							#include "UnityCG.cginc"
							#include "Lighting.cginc"
							#include "AutoLight.cginc"

							sampler2D _texBase, _texMask, _texEff, _texEff1;

							fixed4 _baseCfgMS,_eff1CfgMS,_eff2CfgMS;
							fixed _eff1CfgRotate,_eff2CfgRotate;

							fixed4 _eff1Color_s,_eff1Color_e,_eff2Color_s,_eff2Color_e;

							fixed4 _effColorPower;

							fixed _isRim;
							fixed4 _RimColor;
							fixed _RimPower;
							float _isLight;
							float _isAmbientLight;

							fixed _cubeRatio;

							fixed4 _AmbientLightColor;

							float _IsFog;
							float _alphaTest;

							fixed4 uvMode;
							fixed4 uvMatFlag;
							fixed _eff1uvFlip,_eff2uvFlip;

							float4x4 uvMatBase;
							float4x4 uvMatEff1;
							float4x4 uvMatEff2;

							struct appdata {
									float4 vertex : POSITION;
									fixed2 Tex0 : TEXCOORD0;
									float3 normal : NORMAL;
							};

							struct v2f {
									float4 pos : SV_POSITION;
									fixed2 Tex0 : TEXCOORD0;
									fixed2 Tex1 : TEXCOORD1;
									fixed2 Tex2 : TEXCOORD2;
									float4 worNor : TEXCOORD3;
									fixed4 effColor1:TEXCOORD4;
									fixed4 effColor2:TEXCOORD5;
									UNITY_FOG_COORDS(6)
									float4 vNor : TEXCOORD7;
									float3 lightColor : TEXCOORD8; 
									float rimRatio : TEXCOORD9;
									LIGHTING_COORDS(10,11)
							};

							v2f vert (appdata v)
							 {
						 			v2f o = (v2f)0;
						 			o.pos = UnityObjectToClipPos(v.vertex);
						 			
						 			o.worNor = mul(unity_ObjectToWorld, float4(v.normal,0));
						 			o.worNor.xyz = normalize(o.worNor.xyz);

						 			o.vNor = mul(UNITY_MATRIX_MV,float4(v.normal,0));

						 			float3 Normal = normalize(o.worNor.xyz);
						 			float3 Pos = mul(unity_ObjectToWorld,v.vertex).xyz;

						 			float3 cam_pos = _WorldSpaceCameraPos.xyz;

						 			o.Tex0 = v.Tex0.xy;
						 			////////////////////////////////////////
						 			fixed4 uvMat;

						 			//////baes mt
						 			uvMatBase[2][0] = _baseCfgMS.x;
						 			uvMatBase[2][1] = _baseCfgMS.y;
						 			uvMatBase[0][0] = _baseCfgMS.z;
						 			uvMatBase[1][1] = _baseCfgMS.w;

						 			//////eff1 mt
						 			float speed = _Time.y * _eff1CfgRotate;

						 			uvMatEff1[3][0] = uvMatEff1[2][0] = _Time.y * _eff1CfgMS.x;
						 			uvMatEff1[3][1] = uvMatEff1[2][1] = _Time.y * _eff1CfgMS.y;

						 			if(_eff1CfgRotate > 0)
						 			{
						 					uvMatEff1[0][0] = cos(speed);
						 					uvMatEff1[1][0] = sin(speed);

						 					uvMatEff1[0][1] = -sin(speed);
						 					uvMatEff1[1][1] = cos(speed);
						 				}
						 			else
						 			{
						 					uvMatEff1[0][0] = uvMatEff1[1][1] = 1.0;
						 				}

						 			uvMatEff1[0][0] *= _eff1CfgMS.z;
						 			uvMatEff1[1][1] *= _eff1CfgMS.w;


						 			////////eff2 mt
						 			speed = _Time.y * _eff2CfgRotate;

						 			uvMatEff2[3][0] = uvMatEff2[2][0] = _Time.y * _eff2CfgMS.x;
						 			uvMatEff2[3][1] = uvMatEff2[2][1] = _Time.y * _eff2CfgMS.y;
						 				
						 			if(_eff2CfgRotate > 0)
						 			{
						 					uvMatEff2[0][0] = cos(speed);
						 					uvMatEff2[1][0] = sin(speed);

						 					uvMatEff2[0][1] = -sin(speed);
						 					uvMatEff2[1][1] = cos(speed);
						 				}
						 			else
						 			{
						 					uvMatEff2[0][0] = uvMatEff2[1][1] = 1.0;
						 				}

						 			uvMatEff2[0][0] *= _eff2CfgMS.z;
						 			uvMatEff2[1][1] *= _eff2CfgMS.w;


						 			// uv gen eff1
#ifdef USE_TEXUV1_BASE
										o.Tex1.xy = v.Tex0;
#endif

#ifdef USE_TEXUV1_CAMERA_NOR
										o.Tex1.xy = mul(float4(Normal,0), UNITY_MATRIX_V).xy;
#endif

#ifdef USE_TEXUV1_CAMERA_REL
								{
										o.Tex1.xy = v.Tex0;

										float3 V1 = normalize(cam_pos-Pos); 
										float3 T1 = -reflect(V1, Normal);

										o.Tex1.xy = normalize(mul(float4(T1,0), UNITY_MATRIX_V)).xy;
									}
#endif

#ifdef USE_TEXUV1_CAMERA_POS
										o.Tex1.xy = mul(float4(Pos,0), UNITY_MATRIX_V).xy;
#endif

#ifdef USE_TEXUV1_NORMAL_XY
										o.Tex1.xy = Normal.xy;
#endif

#ifdef USE_TEXUV1_NORMAL_YZ
										o.Tex1.xy = Normal.yz;
#endif

#ifdef USE_TEXUV1_NORMAL_ZX
										o.Tex1.xy = Normal.zx;
#endif
								if(_eff1uvFlip > 1)
										o.Tex1.x = 1-o.Tex1.x;
								else if(_eff1uvFlip > 0)
										o.Tex1.y = 1-o.Tex1.y;

										//uv gen eff2

#ifdef USE_TEXUV2_BASE
										{
												o.Tex2.xy = v.Tex0;
											}
#endif

#ifdef USE_TEXUV2_CAMERA_NOR
										{
												o.Tex2.xy = mul(float4(Normal,0), UNITY_MATRIX_V).xy;
											}
#endif

#ifdef USE_TEXUV2_CAMERA_REL
								{
										o.Tex2.xy = v.Tex0;

										float3 V1 = normalize(cam_pos-Pos); 
										float3 T1 = -reflect(V1, Normal);

										o.Tex2.xy = normalize(mul(float4(T1,0), UNITY_MATRIX_V)).xy;
									}
#endif

#ifdef USE_TEXUV2_CAMERA_POS
										o.Tex2.xy = mul(float4(Pos,0), UNITY_MATRIX_V).xy;
#endif

#ifdef USE_TEXUV2_NORMAL_XY
										o.Tex2.xy = Normal.xy;
#endif

#ifdef USE_TEXUV2_NORMAL_YZ
										o.Tex2.xy = Normal.yz;
#endif

#ifdef USE_TEXUV2_NORMAL_ZX
										o.Tex2.xy = Normal.zx;
#endif

								if(_eff2uvFlip > 1)
										o.Tex2.x = 1-o.Tex1.x;
								else if(_eff2uvFlip > 0)
										o.Tex2.y = 1-o.Tex1.y;

										//uv matrix
										if(uvMatFlag.x > 0)		//base uv
										{
												float4 uv0 = float4(o.Tex0.xy, 0, 1);
												o.Tex0 = mul(uv0, uvMatBase).xy;
											}
										if(uvMatFlag.y > 0)		//eff1 uv
										{
#ifdef USE_TEXUV1_CAMERA_NOR
										{
												float4 uv = float4(o.Tex1.xy, 0, 1);
												o.Tex1.xy = mul(uv, uvMatEff1).xy;
											}
#elif USE_TEXUV1_CAMERA_REL
										{
												float4 uv = float4(o.Tex1.xy, 0, 1);
												o.Tex1.xy = mul(uv, uvMatEff1).xy;
											}
#elif USE_TEXUV1_CAMERA_POS
										{
												float4 uv = float4(o.Tex1.xy, 0, 1);
												o.Tex1.xy = mul(uv, uvMatEff1).xy;
											}
#else
										{
												float4 uv = float4(o.Tex1.xy, 0,1);
												o.Tex1.xy = mul(uv, uvMatEff1).xy;
											}
#endif
											}
											if(uvMatFlag.z > 0)		//eff2 uv
											{
#ifdef USE_TEXUV2_CAMERA_NOR
										{
												float4 uv = float4(o.Tex2.xy, 0, 1);
												o.Tex2 = mul(uv, uvMatEff2).xy;
											}
#elif USE_TEXUV2_CAMERA_REL
										{
												float4 uv = float4(o.Tex2.xy, 0, 1);
												o.Tex2 = mul(uv, uvMatEff2).xy;
											}
#elif USE_TEXUV2_CAMERA_POS
										{
												float4 uv = float4(o.Tex2.xy, 0, 1);
												o.Tex2 = mul(uv, uvMatEff2).xy;
											}
#else
										{
												float4 uv = float4(o.Tex2, 0, 1);
												o.Tex2 = mul(uv, uvMatEff2).xy;
											}
#endif
											}
											/////////////////////////////////////
											float key = abs(sin(_Time.y));
											o.effColor1 = _eff1Color_s * (1.0 - key) + _eff1Color_e * key;
											o.effColor1.rgb *= _effColorPower.x;

											key = abs(sin(_Time.y));
											o.effColor2 = _eff2Color_s * (1.0 - key) + _eff2Color_e * key;
											o.effColor2.rgb *= _effColorPower.y;

											//光照计算
											float3 ambientC = 0.0;
											if(_isLight > 0.0)
											{
													ambientC = UNITY_LIGHTMODEL_AMBIENT.xyz;
													if(_isAmbientLight < 1.0)
															ambientC = _AmbientLightColor.rgb + _AmbientLightColor.a;
														}
													o.lightColor.rgb = ambientC + _LightColor0.rgb * LIGHT_ATTENUATION(i) * max(0,dot(o.worNor.xyz,normalize(_WorldSpaceLightPos0.xyz)));

											//边缘光
											o.rimRatio = 1.0;
											if(_isRim > 0.0)
											{
													float ratio = saturate(dot(float3(0,0,1), o.vNor.xyz));
													ratio = pow (ratio, _RimPower * 8);

													o.rimRatio = ratio;
													}


												UNITY_TRANSFER_FOG(o,o.pos);
												return o;
												}
											fixed4 frag (v2f i) : SV_Target
											{
														fixed4 base = tex2D(_texBase, i.Tex0.xy);	//贴图颜色

														fixed4 mask = tex2D(_texMask, i.Tex0.xy);	//x:eff1 mask, y:eff2 mask

														fixed4 eff1 = tex2D(_texEff,i.Tex1.xy);
														fixed4 eff2 = tex2D(_texEff1,i.Tex2.xy);

														base.rgb = base.rgb + eff1 * i.effColor1.rgb * mask.x;
														base.rgb = base.rgb + eff2 * i.effColor2.rgb * mask.y;

														if(mask.b > 0.5)
																base.rgb = base.rgb * i.lightColor.rgb;

																base.xyz = base.xyz * i.rimRatio + _RimColor.xyz * (1.0 - i.rimRatio);
																if(_IsFog > 0.0)
																		UNITY_APPLY_FOG(i.fogCoord, base);
																return base;
														}
														ENDCG
													}
												}
												//CustomEditor"FxGlitteringMaterialEditor"
												FallBack "Diffuse"
}