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
    }

    // 今の合計数を教えてあげる
    public int CollectedCount => _collectedCount;


}