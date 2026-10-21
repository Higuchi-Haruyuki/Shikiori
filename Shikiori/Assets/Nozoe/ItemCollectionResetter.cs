using UnityEngine;

/// <summary>
/// Gameシーン開始時にアイテム取得数をリセットするクラス。
/// ItemCollectionState は ScriptableObject なので、シーンを読み直しても値が残る。
/// そのため、プレイ開始のたびに明示的に 0 へ戻す。
/// </summary>
public class ItemCollectionResetter : MonoBehaviour
{
    // リセット対象の共有データ（Item に設定しているものと同じアセットを入れる）
    [SerializeField] private ItemCollectionState _collectionState;

    // Awake はシーン読み込み直後に呼ばれる。
    // アイテムを拾う（OnTriggerEnter）より必ず先に実行される。
    void Awake()
    {
        if (_collectionState == null)
        {
            Debug.LogError("ItemCollectionState が設定されていません。");
            return;
        }
        _collectionState.ResetCount();
    }
}
