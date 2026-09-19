# 季節(Season) & ギミック(Gimic)システム マニュアル

このドキュメントは `Assets/Higuchi/Src` にある季節システムとギミックシステムの仕組みと、
**新しいギミックを追加する際の手順**をまとめたものです。

対象読者: 季節に応じて挙動を変えるギミック(オブジェクト)を新規追加したい人。

---

## 1. システム構成

| クラス | ファイル | 役割 |
|---|---|---|
| `GlobalSeason` | `Src/GlobalSeason.cs` | 季節を表す enum (`None`, `Spring`, `Summer`, `Fall`, `Winter`) |
| `SeasonManager` | `Src/SeasonManager.cs` | 現在の季節を保持し、季節が変わったときにイベントを発火する |
| `IGimic` | `Src/Gimic/IGimic.cs` | 季節変更を受け取るギミックが実装するインターフェース |
| `GimicManager` | `Src/Gimic/GimicManager.cs` | シーン上の全ギミックを `SeasonManager` のイベントに自動で登録する |
| `MushRoom` | `Src/Gimic/MushRoom.cs` | `IGimic` を実装したギミックの実装例 |

### データの流れ

1. `SeasonManager` が `ChangeSeason(newSeason)` を呼ばれると `_currentSeason` を更新し、
   登録されているデリゲート `OnSeasonChanged(oldSeason, newSeason)` を全て呼び出す。
2. `GimicManager` は `Awake()` 時にシーン上の `Gimic` タグが付いた GameObject を
   `GameObject.FindGameObjectsWithTag("Gimic")` で全て取得し、各 GameObject の
   `IGimic` コンポーネントの `OnSeasonChanged` を `SeasonManager.SubscribeToSeasonChange` に登録する。
3. 季節が変わるたびに、登録済みの全ギミックの `OnSeasonChanged` が自動で呼ばれる。

Unity のスクリプト実行順序上、同一フレーム内では全ての `Awake()` が全ての `Start()` より先に
実行されるため、`GimicManager.Awake()`(購読登録) は `SeasonManager.Start()`(初期季節への変更、
イベント発火) より先に走る。そのため通常の配置であれば、シーン開始時の最初の季節変更も
ギミック側でちゃんと受け取れる。

---

## 2. 新しいギミックを追加する手順

### 手順1: `IGimic` を実装したスクリプトを作成する

```csharp
using UnityEngine;

public class MyGimic : MonoBehaviour, IGimic
{
    public void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        // 季節が変わったときの処理をここに書く
        switch (newSeason)
        {
            case GlobalSeason.Winter:
                // 冬specificの処理
                break;
            case GlobalSeason.Summer:
                // 夏specificの処理
                break;
        }
    }
}
```

- `MonoBehaviour` を継承し、`IGimic` を実装する。
- `OnSeasonChanged` の中に、季節ごとの見た目や挙動の変化を実装する。
  (例: `MushRoom.cs` は季節変化をログ出力するだけの最小サンプル)

### 手順2: シーンに GameObject を配置し、スクリプトをアタッチする

作成したスクリプトを、ギミックとして動作させたい GameObject にアタッチする。

### 手順3: GameObject のタグを `Gimic` に設定する

**これが最重要ポイント。** `GimicManager` はタグが `Gimic` の GameObject しか探しに行かない
(`GimicManager._gimicTag`、デフォルト値 `"Gimic"`)。タグを設定し忘れると
季節変更イベントが一切呼ばれない。

Inspector 上部の Tag ドロップダウンから `Gimic` を選択する。

### 手順4: シーンに `SeasonManager` が存在することを確認する

シーン内に、タグ `SeasonManager` が付いた GameObject に `SeasonManager` コンポーネントが
アタッチされている必要がある(`GimicManager` が `Awake()` 時にこのタグで検索して参照を取得する)。
既存シーンにあれば、通常は追加作業不要。

以上で完了。シーン再生後、季節が変わるたびに自身の `OnSeasonChanged` が呼ばれるようになる。

---

## 3. 注意点・ハマりどころ

- **1オブジェクトにつき `IGimic` は1つまで有効**
  `GimicManager` は `gimicObject.GetComponent<IGimic>()` で取得するため、同じ GameObject に
  複数の `IGimic` 実装コンポーネントをアタッチしても、最初に見つかった1つしか
  季節変更イベントを受け取らない。複数のギミック挙動を持たせたい場合は、
  1つのコンポーネント内でまとめて処理するか、別々の GameObject に分ける。

- **実行時に動的生成(Instantiate)したギミックは自動登録されない**
  `GimicManager` はシーン開始時の `Awake()` 一度きりしかギミックを探しに行かない。
  実行中に `Instantiate` で生成したオブジェクトは自動では季節変更イベントを受け取れないため、
  生成後に手動で `SeasonManager.SubscribeToSeasonChange(gimic.OnSeasonChanged)` を呼ぶ必要がある。
  (`SeasonManager` への参照は `GameObject.FindGameObjectWithTag("SeasonManager")` などで取得する)

- **破棄時の購読解除は自前で行う**
  ギミックオブジェクトを `Destroy` する場合、`OnSeasonChanged` の購読が残ったままだと
  次回の季節変更時に破棄済みオブジェクトのメソッドが呼ばれてエラーになる可能性がある。
  破棄前に `SeasonManager.UnsubscribeFromSeasonChange(gimic.OnSeasonChanged)` を呼んでおくこと。

- **現在有効な季節遷移は Summer ⇔ Winter のみ**
  `SeasonManager._seasonTransition` の初期値は `None → Summer`、`Summer → Winter`、
  `Winter → Summer` のみが定義されている。`Spring` / `Fall` は enum には存在するが、
  現状の自動遷移(Enterキー押下)には含まれていない。`Spring`/`Fall` 用の処理を
  ギミックに書くこと自体は問題ないが、実機で確認する場合は `SeasonManager` 側の
  遷移テーブルを一時的に書き換えるか、`ChangeSeason(GlobalSeason.Spring)` を
  直接呼び出してテストする必要がある。

- **季節切り替えの動作確認方法**
  `SeasonManager.Update()` 内でキーボードの Enter キー押下を検知し、
  `_seasonTransition` に従って季節を切り替えている(デバッグ用)。
  実行中に Enter キーを押すことで季節変更をテストできる。

---

## 4. チェックリスト(新規ギミック追加時)

- [ ] `IGimic` を実装した `MonoBehaviour` を作成した
- [ ] `OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)` に季節ごとの処理を書いた
- [ ] スクリプトを GameObject にアタッチした
- [ ] GameObject のタグを `Gimic` に設定した
- [ ] シーンに `SeasonManager` タグ付きの `SeasonManager` が存在することを確認した
- [ ] (動的生成する場合)生成後に手動で購読、破棄前に購読解除を行うようにした
