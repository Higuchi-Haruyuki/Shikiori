using UnityEngine;
using TMPro;

public class ResultItemCountController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _itemCountText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // デバッグ用にアイテム数を保存してからロードする。
        ItemCountMover.SaveItemCount(3);

        // アイテム数をロードして表示する。
        var result = ItemCountMover.LoadItemCount();
        if (result.IsSuccess)
        {
            _itemCountText.text = $"アイテム獲得数 :  <color=yellow>{result.Value}</color> / 4";
        }
        else
        {
            Debug.LogError(result.ErrorMessage);
            _itemCountText.text = "アイテム数の取得に失敗しました。";
        }
    }

}
