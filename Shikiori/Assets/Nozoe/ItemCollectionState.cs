using UnityEngine;

[CreateAssetMenu(fileName = "ItemCollectionState", menuName = "Scriptable Objects/ItemCollectionState")]
public class ItemCollectionState: ScriptableObject
{
    // 「合計何個拾ったか」を覚えておく
    private int _collectedCount;

    private void OnEnable()
    {
        _collectedCount = 0;
    }

    // 一個拾った時に呼んでもらう。数字を増やすだけ。
    public void Collect(ItemData itemData)
    {
        _collectedCount += itemData.ScoreValue;
        ItemCountMover.SaveItemCount(_collectedCount);
    }

    // ゲーム開始時に呼んでもらう。合計数を 0 に戻す。
    // ScriptableObject はシーンをまたいで値が残るので、毎プレイ明示的にリセットする必要がある。
    public void ResetCount()
    {
        _collectedCount = 0;
    }

    // 今の合計数を教えてあげる
    public int CollectedCount => _collectedCount;


}