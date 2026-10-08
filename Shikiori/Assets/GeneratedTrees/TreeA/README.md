# Tree A — ローポリ針葉樹の試作

Unity 6000.3.12f1 / URP向け。高さ2.5、幅約1.24 Unity単位。原点は幹の根元です。

## Unityへの導入

1. output/TreeA/Tree_A.unitypackageをURPプロジェクトへインポートします。
2. Assets/GeneratedTrees/TreeA/Tree_A.prefabをシーンへ配置します。
3. 樹高1〜3にする場合、Prefabの一様なScaleを0.4〜1.2に設定します。

Prefabは1つです。Tree_A_Comparison.unityは同じPrefabの通常・冬の外観を並べた確認用シーンです。切り替え機能やランタイムの生成処理は含めていません。

## 季節の見た目

同じPrefabの子オブジェクトのMeshRenderer.sharedMaterialsを、外部から次の組み合わせに変更してください。配列の順序は固定です。

| 子オブジェクト | 通常のスロット0 / 1 | 冬のスロット0 / 1 |
|---|---|---|
| Wood | Bark_Normal / BranchTop_Normal | Bark_Winter / BranchTop_Winter |
| Foliage | Foliage_Normal / FoliageTip_Normal | Foliage_Winter_Hidden / Foliage_Winter_Hidden |

すべてMaterialsフォルダー内のURP Litマテリアルです。単色のみで、テクスチャは使用していません。冬の葉はアルファクリッピングで完全に隠します。影もクリップされます。雪は枝の上面の色分けで表現し、追加の積雪メッシュはありません。FBX/GLBは通常の外観を保存した交換用ファイルで、Unity用季節マテリアルはunitypackage内にあります。

## モデル仕様

- Wood / Foliageの2メッシュ、合計18,078三角形。フラットな法線で面を表現。
- 通常は4マテリアルスロット。冬は枝上面を白くし、葉を非表示にするマテリアルを利用。
- Collision配下の11個のBoxColliderで幹・根元・樹冠を囲む複合当たり判定。形状の近似のため、枝葉の間の空間にも判定があります。冬も当たり判定は同じです。
- 風、LOD、季節切り替え処理、成長・伐採処理なし。
- 近距離確認用の試作として枝を多めに作成。20本を配置した実機パフォーマンス測定は未実施です。

## ファイル

- output/TreeA/Tree_A.unitypackage: Prefab・メッシュ・季節マテリアル・比較シーン
- output/TreeA/Tree_A.fbx: UnityやDCCツール向け交換形式
- output/TreeA/Tree_A.glb: 一般的な3Dビューアー向け
- output/TreeA/Tree_A.blend: 編集用Blenderファイル。Tree_A_Assetシーンが本体、Appearance_Comparisonシーンは確認用。
- output/TreeA/Tree_A_preview.png: 実際のモデルをBlenderでレンダリングした比較画像
- output/TreeA/unity-validation.txt: Unityでの検証結果
- tools/create_tree_a.py: モデル再制作用のBlenderスクリプト

Editor/TreeAAssetBuilder.csは制作・検証用です。導入時に実行する必要はありません。

実装時に参照した公式API: [Unity PrefabUtility](https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/prefabutility)、[Blender FBX exporter](https://docs.blender.org/api/5.0/bpy.ops.export_scene.html)。
