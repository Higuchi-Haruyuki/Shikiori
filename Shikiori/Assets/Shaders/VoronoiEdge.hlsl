// ファイルが2回読み込まれても壊れないようにする
// = pragma onece (c++)
#ifndef VORONOI_EDGE_INCLUDED
#define VORONOI_EDGE_INCLUDED

// Step 2-1: マスの番号 → そのマスの特徴点の位置（各成分 0〜1）
// 同じ番号からは必ず同じ値が出る（ピクセルごとに変わらない）
// ハッシュ関数
// 同じ入力に対して、必ず同じ出力を返す
float2 Hash22(float2 cell)
{
    // 入力されたマス番号から
    // x と y それぞれの数を作る
    float2 mixed = float2(dot(cell, float2(127.1, 311.7)),
               dot(cell, float2(269.5, 183.3)));

    // sin() で mixed を -1～1 の範囲の波打つ値にする
    // * 43758.5453 をすることで、小さな差を大きな差に広げる
    // frac() で小数部分のみを取り出す
    return frac(sin(mixed) * 43758.5453);
}

// 時間によって場所が変化する特徴点を返す
float2 FeaturePoint(float2 cell,float amplitude, float time, float randomness)
{
    float2 hash = Hash22(cell);

    float2 base = 0.5 + (hash - 0.5) * (1 - 2 * amplitude) * randomness;
    // sin() に float2 を入れると、x y それぞれで sin() を計算してくれる
    return base + amplitude * sin(time + 6.2831853 * hash);
}

// 境界との距離を返す
void VoronoiEdge_float(float2 UV, float Scale, float Amplitude, float Time, float Randomness, out float Out)
{
    // Amplitude を 0.0f ~ 0.5f の間にクランプ
    float amplitude = clamp(Amplitude, 0.0f, 0.5f);

    // Randomness を 0.0f ~ 1.0f の間にクランプ
    float randomness = clamp(Randomness, 0.0f, 1.0f);

    // 模様の座標（Blenderの Mapping → Voronoi.Scale）
    // Scale が 1 なら、マスは一つのみ
    float2 pos = UV * Scale;
    // 今いるマスの番号
    // 整数値のみを取り出す
    float2 cell = floor(pos);
    // マスの中での位置
    // floor() で切り捨てられた小数部分を使用
    float2 local = frac(pos);

    // pos = cell + local = floor(pos) + frac(pos)
    // が成り立つ

    // Step 2-2: 1回目のループ
    // 周り3×3マスの特徴点から、一番近いもの a を探す
    // これまでで一番近い距離の2乗（最初は十分大きい値）
    float  minDistSq = 8.0;
    // ピクセル → a のベクトル（2-3で使う）
    float2 toClosest = float2(0, 0);

    // x軸 左隣 -> 右隣
    for (int i = -1; i <= 1; ++i)
    {
        // y軸 下 -> 上
        for (int j = -1; j <= 1; ++j)
        {
            // 調べるマスの今いるマスから見た場所のずれ
            float2 offset = float2(i, j);
            // 調べるマスの特徴点（今のマスの左下が原点）
            float2 feature = offset + FeaturePoint(cell + offset, amplitude, Time, randomness);     
            // ピクセルから特徴点へのベクトル
            float2 toPoint = feature - local;                       
            // ピクセルから特徴点までの距離の2乗
            // 同じベクトルの内積はそのベクトルの大きさの2乗になる
            float  distSq  = dot(toPoint, toPoint);                 

            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                toClosest = toPoint;
            }
        }
    }

    // Step 2-3: 2回目のループ
    // a 以外の各特徴点 b について、境界までの距離 distToEdge を計算し、一番小さいものを取る
    float minEdge = 8.0;

    for (int i2 = -1; i2 <= 1; ++i2)
    {
        for (int j2 = -1; j2 <= 1; ++j2)
        {
            float2 offset  = float2(i2, j2);
            // ピクセルから b へのベクトル
            float2 toPoint = offset + FeaturePoint(cell + offset, amplitude, Time, randomness) - local;

            // a から b へのベクトル（長さ1ではない）
            float2 ab = toPoint - toClosest;
            // b が a 自身なら（ab がほぼ0）とばす
            if (dot(ab, ab) < 0.0001) continue;

            float distToEdge = dot((toClosest + toPoint) / 2, normalize(ab));

            minEdge = min(minEdge, distToEdge);
        }
    }

    // 境界までの距離
    Out = minEdge; 
}

#endif