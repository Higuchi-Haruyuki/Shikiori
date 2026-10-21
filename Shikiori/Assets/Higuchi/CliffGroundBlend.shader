// 崖・山用のシェーダー
// 崖の元テクスチャ（Tripo製のアトラス）のうち「緑っぽい部分」だけを、
// 地面と同じ草テクスチャに置き換えて、地面と見た目をそろえる。
// 草テクスチャは地面側のGroundBlendがグローバルに公開したものを使うので、季節の切り替えも地面と一緒に動く。
Shader "Custom/CliffGroundBlend"
{
    Properties
    {
        // 崖の元テクスチャ（GroundBlendが季節ごとに差し替える）
        _FromMap("From Map(今の季節の崖)", 2D) = "white" {}
        _FromNormalMap("From Normal Map(今の季節の崖)", 2D) = "bump" {}
        _ToMap("To Map(次の季節の崖)", 2D) = "white" {}
        _ToNormalMap("To Normal Map(次の季節の崖)", 2D) = "bump" {}
        _Blend("Blend", Range(0, 1)) = 0
        _NormalStrength("Normal Strength(崖)", Range(0, 10)) = 1

        // 草にする場所を決めるマスク用テクスチャ（夏の崖テクスチャ）。
        // 冬のテクスチャは白いので緑を判定できない。だから季節に関係なく夏のものを使う。
        _GrassMaskMap("Grass Mask Map(夏の崖テクスチャ)", 2D) = "black" {}
        // 緑っぽさがこの値を超えたら草にする
        _MaskThreshold("Mask Threshold", Range(-0.5, 0.5)) = 0.05
        // 岩と草の境目をどれくらいぼかすか
        _MaskSoftness("Mask Softness", Range(0.001, 0.5)) = 0.1

        // 草をワールド座標で貼るときの細かさ。地面(ProBuilderのTile)は1mで1回なので1が基準
        _GrassWorldScale("Grass World Scale", Float) = 1
        // 草の凹凸の強さ（地面のGroundBlendMatと同じ値にしておく）
        _GrassNormalStrength("Grass Normal Strength", Range(0, 10)) = 3.69
        // 3方向投影の境目のくっきりさ。大きいほど境目が狭くなる
        _TriplanarSharpness("Triplanar Sharpness", Range(1, 16)) = 4
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
            TEXTURE2D(_GrassMaskMap); SAMPLER(sampler_GrassMaskMap);

            // 地面のGroundBlendがShader.SetGlobal～で公開する値。
            // マテリアルごとの値ではないので、CBUFFERの外に書く。
            TEXTURE2D(_GlobalGrassFromMap); SAMPLER(sampler_GlobalGrassFromMap);
            TEXTURE2D(_GlobalGrassToMap); SAMPLER(sampler_GlobalGrassToMap);
            TEXTURE2D(_GlobalGrassFromNormalMap); SAMPLER(sampler_GlobalGrassFromNormalMap);
            TEXTURE2D(_GlobalGrassToNormalMap); SAMPLER(sampler_GlobalGrassToNormalMap);
            float _GlobalGrassBlend;

            CBUFFER_START(UnityPerMaterial)
                float4 _FromMap_ST;
                float _Blend;
                float _NormalStrength;
                float _MaskThreshold;
                float _MaskSoftness;
                float _GrassWorldScale;
                float _GrassNormalStrength;
                float _TriplanarSharpness;
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

            // 3方向（X・Y・Z軸）から投影するときの、それぞれの混ぜ具合を求める。
            // 上を向いた面はY方向からの投影が強くなる。
            half3 TriplanarWeights(half3 normalWS)
            {
                half3 weights = pow(abs(normalWS), _TriplanarSharpness);
                // 合計が1になるようにする
                return weights / (weights.x + weights.y + weights.z);
            }

            // 色テクスチャを3方向から投影して読む。
            half4 SampleTriplanar(TEXTURE2D_PARAM(tex, samp), float3 positionWS, half3 weights)
            {
                float3 p = positionWS * _GrassWorldScale;
                half4 x = SAMPLE_TEXTURE2D(tex, samp, p.zy); // 横(X方向)から見た絵
                half4 y = SAMPLE_TEXTURE2D(tex, samp, p.xz); // 真上(Y方向)から見た絵
                half4 z = SAMPLE_TEXTURE2D(tex, samp, p.xy); // 正面(Z方向)から見た絵
                return x * weights.x + y * weights.y + z * weights.z;
            }

            // 法線マップを3方向から投影して読み、ワールド空間の法線にして返す。
            // （ホワイトアウトブレンドという、面の向きに凹凸を足し込むやり方）
            half3 SampleTriplanarNormal(TEXTURE2D_PARAM(tex, samp), float3 positionWS, half3 normalWS, half3 weights)
            {
                float3 p = positionWS * _GrassWorldScale;
                half3 nX = UnpackNormalScale(SAMPLE_TEXTURE2D(tex, samp, p.zy), _GrassNormalStrength);
                half3 nY = UnpackNormalScale(SAMPLE_TEXTURE2D(tex, samp, p.xz), _GrassNormalStrength);
                half3 nZ = UnpackNormalScale(SAMPLE_TEXTURE2D(tex, samp, p.xy), _GrassNormalStrength);

                // それぞれの投影面の凹凸を、面の法線に足し込む
                nX = half3(nX.xy + normalWS.zy, abs(nX.z) * normalWS.x);
                nY = half3(nY.xy + normalWS.xz, abs(nY.z) * normalWS.y);
                nZ = half3(nZ.xy + normalWS.xy, abs(nZ.z) * normalWS.z);

                // 並びをワールドの軸(x, y, z)にそろえてから混ぜる
                return normalize(nX.zyx * weights.x + nY.xzy * weights.y + nZ.xyz * weights.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.positionWS = positionInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;

                // オブジェクトが鏡写しに拡大縮小されているとき-1になる
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = float4(normalInputs.tangentWS, sign);

                // 崖のテクスチャはアトラス（展開図）なので、タイリングはかけずにそのまま使う
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 vertexNormalWS = normalize(IN.normalWS);

                // ---- 1. 崖の元の色と凹凸（モデルのUVで読む） ----
                half4 cliffFrom = SAMPLE_TEXTURE2D(_FromMap, sampler_FromMap, IN.uv);
                half4 cliffTo = SAMPLE_TEXTURE2D(_ToMap, sampler_ToMap, IN.uv);
                half4 cliffColor = lerp(cliffFrom, cliffTo, _Blend);

                half3 cliffFromNormalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_FromNormalMap, sampler_FromNormalMap, IN.uv), _NormalStrength);
                half3 cliffToNormalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_ToNormalMap, sampler_ToNormalMap, IN.uv), _NormalStrength);
                half3 cliffNormalTS = normalize(lerp(cliffFromNormalTS, cliffToNormalTS, _Blend));

                // TBN行列で、崖の法線をワールド空間に変換する
                half3 tangentWS = normalize(IN.tangentWS.xyz);
                half3 bitangentWS = cross(vertexNormalWS, tangentWS) * IN.tangentWS.w;
                half3x3 tbn = half3x3(tangentWS, bitangentWS, vertexNormalWS);
                half3 cliffNormalWS = normalize(TransformTangentToWorld(cliffNormalTS, tbn));

                // ---- 2. 地面と同じ草の色と凹凸（ワールド座標の3方向投影で読む） ----
                half3 weights = TriplanarWeights(vertexNormalWS);

                half4 grassFrom = SampleTriplanar(
                    TEXTURE2D_ARGS(_GlobalGrassFromMap, sampler_GlobalGrassFromMap), IN.positionWS, weights);
                half4 grassTo = SampleTriplanar(
                    TEXTURE2D_ARGS(_GlobalGrassToMap, sampler_GlobalGrassToMap), IN.positionWS, weights);
                half4 grassColor = lerp(grassFrom, grassTo, _GlobalGrassBlend);

                half3 grassFromNormalWS = SampleTriplanarNormal(
                    TEXTURE2D_ARGS(_GlobalGrassFromNormalMap, sampler_GlobalGrassFromNormalMap),
                    IN.positionWS, vertexNormalWS, weights);
                half3 grassToNormalWS = SampleTriplanarNormal(
                    TEXTURE2D_ARGS(_GlobalGrassToNormalMap, sampler_GlobalGrassToNormalMap),
                    IN.positionWS, vertexNormalWS, weights);
                half3 grassNormalWS = normalize(lerp(grassFromNormalWS, grassToNormalWS, _GlobalGrassBlend));

                // ---- 3. どこを草にするか（マスク） ----
                half4 maskSource = SAMPLE_TEXTURE2D(_GrassMaskMap, sampler_GrassMaskMap, IN.uv);
                // 緑っぽさ：緑が赤・青より強いほどプラスになる（岩のベージュはマイナス）
                half greenness = maskSource.g - max(maskSource.r, maskSource.b);
                // しきい値のまわりを滑らかに0→1にする
                half grassMask = smoothstep(_MaskThreshold, _MaskThreshold + _MaskSoftness, greenness);

                half3 albedo = lerp(cliffColor.rgb, grassColor.rgb, grassMask);
                half3 normalWS = normalize(lerp(cliffNormalWS, grassNormalWS, grassMask));

                // ---- 4. 光の計算（地面のGroundBlendと同じLambert） ----
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half nDotL = saturate(dot(normalWS, mainLight.direction));
                half3 diffuse = nDotL * mainLight.color * mainLight.shadowAttenuation;

                // 環境光
                half3 ambient = SampleSH(normalWS);

                half3 color = albedo * (diffuse + ambient);
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
