using UnityEngine;

public class Item : MonoBehaviour
{
    // このアイテムが何の種類かを表すデータ
    [SerializeField] private ItemData _itemData;

    // 取得時アニメーションを再生するためのAnimator
    [SerializeField] private Animator _animator;

    //合計取得数を管理している(共有データ)
    [SerializeField] private ItemCollectionState _collectionState;

    //これアイテムがもう拾われたかどうかを覚えておく
    private bool _isCollected;

    private void OnTriggerEnter(Collider other)
    {
        // 既に拾われていたら何もしない
        if (_isCollected)
        {
            return;
        }

        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        _isCollected = true;

        // 取得時アニメーションへの合図
        if (_animator != null)
        {
            _animator.SetTrigger("Collected");
        }

        // 取得音
        if (_itemData.PickupSound != null)
        {
            AudioSource.PlayClipAtPoint(_itemData.PickupSound, transform.position);
        }

        //　拾ったことを知らせる
        _collectionState.Collect(_itemData);

        // 見た目と当たり判定を消す
        gameObject.SetActive(false);
    }

}
