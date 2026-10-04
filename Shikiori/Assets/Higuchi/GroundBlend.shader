Shader "Custom/GroundBlend"
{
    Properties
    {
        _FromMap("From Map(今の季節)", 2D) = "white" {}
        _FromNormalMap("From Normal Map(今の季節)", 2D) = "bump" {}
        _ToMap("To Map(次の季節)", 2D) = "white" {}
        _ToNormalMap("To Normal Map(次の季節)", 2D) = "bump" {}
        _Blend("Blend", Range(0, 1)) = 0
        _NormalStrength("Normal Strength", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_FromMap); SAMPLER(sampler_FromMap);
            TEXTURE2D(_FromNormalMap); SAMPLER(sampler_FromNormalMap);
            TEXTURE2D(_ToMap); SAMPLER(sampler_ToMap);
            TEXTURE2D(_ToNormalMap); SAMPLER(sampler_ToNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _FromMap_ST; // _FromMapのTillingとOffsetを格納する変数
                float _Blend; // ブレンド値を格納する変数
                float _NormalStrength; // 法線マップの強さを格納する変数
            CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            // メインライトの影を受け取るためのキーワード
            // （URPの設定に合わせて、Unityが必要なバリエーションを自動で選んでくれる）
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            // 影のふちをぼかす（ソフトシャドウ）ためのキーワード
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // 位置をまとめて変換する。
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);

                // 法線をワールド空間に変換する。
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;

                // オブジェクトが鏡写しに拡大縮小されているとき-1になる
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = float4(normalInputs.tangentWS, sign);

                OUT.uv = TRANSFORM_TEX(IN.uv, _FromMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 fromColor = SAMPLE_TEXTURE2D(_FromMap, sampler_FromMap, IN.uv);
                half4 toColor = SAMPLE_TEXTURE2D(_ToMap, sampler_ToMap, IN.uv);
                half4 albedo = lerp(fromColor, toColor, _Blend);

                // 光の計算をする。
                // メインライトの商法を取得。
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                // ライト情報を取得。
                Light mainLight = GetMainLight(shadowCoord);

                // フラグメントシェーダーに渡すときの補間で長さがずれるので正規化する。

                // ノーマルマップを読んで、季節でブレンドする。
                // UnpackNormalScale関数は、法線マップの値を[-1, 1]の範囲に変換し、強さを調整する。
                half3 fromNormalTS = 
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _FromNormalMap, sampler_FromNormalMap, IN.uv), 
                            _NormalStrength);

                half3 toNormalTS = 
                    UnpackNormalScale(
                        SAMPLE_TEXTURE2D(
                            _ToNormalMap, sampler_ToNormalMap, IN.uv), 
                            _NormalStrength);

                half3 normalTS = normalize(lerp(fromNormalTS, toNormalTS, _Blend));

                // TBN行列を作成する。
                half3 vertexNormalWS = normalize(IN.normalWS);
                half3 tangentWS = normalize(IN.tangentWS.xyz);
                half3 bitangentWS = cross(vertexNormalWS, tangentWS) * IN.tangentWS.w;
                half3x3 tbn = half3x3(tangentWS, bitangentWS, vertexNormalWS);

                // 法線をワールド空間に変換する。
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tbn));

                // Lambertの拡散反射を計算する。
                half nDotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = nDotL * mainLight.color * mainLight.shadowAttenuation;

                // 環境光
                half3 ambient = SampleSH(normalWS);

                half3 color = albedo.rgb * (diffuse + ambient);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back


            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
            };

            struct DepthVaryings
            {
                float4 positionHCS : SV_POSITION;
            };

            DepthVaryings depthVert(DepthAttributes IN)
            {
                DepthVaryings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half depthFrag(DepthVaryings IN) : SV_Target
            {
                return IN.positionHCS.z;
            }

            ENDHLSL
        }
    }
}
