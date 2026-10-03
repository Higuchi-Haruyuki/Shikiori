// 草用の光計算をしないシェーダー
// 根本->先端のグラデーション、色ムラ。風の揺れ、季節の色に対応
Shader "Custom/GrassLowPoly"
{
    // インスペクターにでるところ
    Properties
    {
        // 草の根元の色
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        // 草の先端の色
        _TipColor("Tip Color", Color) = (1, 1, 1, 1)
        // 季節の色
        _SeasonTint("Season Tint", Color) = (1, 1, 1, 1)
        // 季節の色をどれだけ混ぜるか
        _SeasonBlend("Season Blend", Range(0, 1)) = 0
        //色ムラの強さ。大きい程色ムラが強くなる
        _ColorVariation("Color Variation", Range(0, 0.15)) = 0.15
        // 色ムラの細かさ。小さいほど大きな塊で色が変わる。
        _NoiseScale("Noise Scale", Float) = 0.3
        // 風の速さ
        _WindSpeed("Wind Speed", Float) = 1.5
        // 風の強さ。先端がどれだけ横に動くか
        _WindStrength("Wind Strength", Float) = 0.15
        // 風の波の細かさ。大きいほど場所語のに揺れ方がばらばらになる。
        _WindFrequency("Wind Frequency", Float) = 0.5        
    }

    SubShader
    {
        // URPで動くことを宣言
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "FORWARDUnlit"
            Tags { "LightMode" = "UniversalForward" }
            
            // 画面描画。草は薄いので、裏から見ても消えないようにする。
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            //GPUインスタンス化に必要なマクロ
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // マテリアルの値をすべて入れたもの
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TipColor;
                half4 _SeasonTint;
                float _SeasonBlend;
                float _ColorVariation;
                float _NoiseScale;
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;       // 頂点の位置
                float2 uv : TEXCOORD0;              // UV(yが根本0->先端1)
                UNITY_VERTEX_INPUT_INSTANCE_ID      // 何本目の草かの番号
            };

            // 頂点シェーダー->フラグメントシェーダーに渡す値
            struct Varyings
            {
                float4 positionCS : SV_POSITION;    // 画面上での位置
                float2 uv : TEXCOORD0;              // UV(グラデーション用)
                float shade : TEXCOORD1;            // 色ムラの明るさ係数
            };      

            // 2次元の座標から、0-1のｐランダムっぽい値を作る関数。
            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // 滑らかに変化するノイズ(バリューノイズ)。
            float ValueNoise(float2 p){
                float2 cell = floor(p); // どのマス目か
                float2 local = frac(p); // マス目の中での位置
                // マス目の4つの角のランダム値。
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1, 0));
                float c = Hash21(cell + float2(0, 1));
                float d = Hash21(cell + float2(1, 1));
                // 角の値をなめらかに混ぜる。
                float2 u = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(a,b,u.x), lerp(c,d,u.x), u.y);
            }

            // 頂点シェーダー
            Varyings Vert(Attributes input)
            {
                Varyings output;

                // この草は何本目かをGPUに設定する。
                UNITY_SETUP_INSTANCE_ID(input);

                // 草一本の足元のワールド座標。
                // 行列の4列目が位置。
                float3 worldPos = GetObjectToWorldMatrix()._m03_m13_m23;

                // 頂点のワールド座標。位置、向き、大きさを反映させる。
                float3 pos = TransformObjectToWorld(input.positionOS.xyz);

                //風の揺れ。UVのyを2乗して先端ほど大きく揺らす。
                float bend = input.uv.y * input.uv.y;
                // 波の位相。
                float phase = dot(worldPos.xz,float2(_WindFrequency, _WindFrequency * 0.7)) + _Time.y * _WindSpeed;

                float2 wind = float2(sin(phase), cos(phase * 0.8)) * _WindStrength * bend;
                pos.xz += wind;

                //色ムラ。大きな色の塊(patch)と草一本ごとの差(blade)を混ぜる。
                float patch = ValueNoise(worldPos.xz * _NoiseScale);
                float blade = Hash21(worldPos.xz * 17.0);
                float mixed = patch * 0.7 + blade * 0.3;
                // 0.5を中心にプラス・マイナスに振る。
                output.shade = 1.0f + (mixed - 0.5) * 2.0 * _ColorVariation;

                // ワールド座標を画面上の座標に変換する。
                output.positionCS = TransformWorldToHClip(pos);
                output.uv = input.uv;
                return output;
            }

            // フラグメントシェーダー
            half4 Frag(Varyings input) : SV_Target
            {
                float3 color = lerp(_BaseColor.rgb, _TipColor.rgb, input.uv.y);

                // 色ムラをかける。
                color *= input.shade;
                
                // 季節の色を混ぜる。
                color = lerp(color, color *_SeasonTint.rgb, _SeasonBlend);
                
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
