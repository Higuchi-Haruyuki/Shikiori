using UnityEngine;

public class ItemManager : MonoBehaviour
{
    // 「合計何個拾ったか」を覚えておく
    private int _collectedCount;

    // 一個拾った時に呼んでもらう。数字を増やすだけ。
    public void Collect()
    {
        _collectedCount++;
    }

    // 今の合計数を教えてあげる
    public int CollectedCount => _collectedCount;
}
