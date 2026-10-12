# Tree B

枝が整った段状に広がる針葉樹。Unity 6000.3.12f1 / URP向け。高さ2.5、幅約1.872 Unity単位。根元が原点です。

## 導入

output/TreeB/Tree_B.unitypackage、またはB・Cをまとめたoutput/Trees_BC.unitypackageをインポートし、Assets/GeneratedTrees/TreeB/Tree_B.prefabを配置してください。Prefabはこの種類につき1つです。Tree_B_Comparison.unityは同じPrefabを通常・冬のマテリアルで並べた確認用シーンです。

## 季節マテリアル

外部から子オブジェクトのMeshRenderer.sharedMaterialsを次の組み合わせに変更します。季節切り替えのランタイム処理は含めていません。

| 子オブジェクト | 通常：スロット0 / 1 | 冬：スロット0 / 1 |
|---|---|---|
| Wood | Bark_Normal / BranchTop_Normal | Bark_Winter / BranchTop_Winter |
| Foliage | Foliage_Normal / FoliageTip_Normal | Foliage_Winter_Hidden / Foliage_Winter_Hidden |

Materialsフォルダーにすべてのマテリアルがあります。URP Litの単色を使用し、冬は葉をアルファクリッピングで隠し、枝の上面を白くします。積雪用の追加メッシュや別Prefabはありません。FBX/GLBは通常版の交換用モデルで、Unity用の季節マテリアルはunitypackage内にあります。

## 仕様

- Wood / Foliageの2メッシュ、18,394三角形、4マテリアルスロット。
- 10個のBoxColliderで幹・根元・樹冠を囲む複合当たり判定。枝葉の隙間にも判定がある近似です。冬も同じ当たり判定を使用します。
- 高さ1〜3へ調整する場合、一様なScaleを0.400〜1.200に設定します。
- 風、LOD、成長・伐採処理なし。20本配置時の実機パフォーマンス測定は未実施です。

## 編集・確認

output/TreeB/Tree_B.blendが編集用データです。Tree_B_Assetシーンがモデル本体、Appearance_Comparisonシーンが確認用です。同じフォルダーにFBX、GLB、Blenderプレビュー画像、Unityプレビュー画像、unity-validation.txtを保存しています。

tools/create_tree_bc.pyでBとCを再制作できます（共通処理としてcreate_tree_a.pyを参照します）。EditorフォルダーのC#は制作・検証用で、インポート後に実行する必要はありません。
