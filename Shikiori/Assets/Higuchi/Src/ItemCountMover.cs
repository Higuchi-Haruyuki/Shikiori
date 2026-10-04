using UnityEngine;
using System;

/// <summary>
/// アイテム数を保存・ロードするためのクラス
/// </summary>
public static class ItemCountMover
{
    private static readonly string ITEM_COUNT_KEY = "ItemCount";
    private static readonly int ITEM_CLEAR_COUNT = 4; // アイテムクリアの条件となる数
    public static Action OnReachedItemClearCount;
    public static Action OnItemCountChanged;
    /// <summary>
    /// アイテム数を保存する
    /// </summary>
    /// <param name="count">アイテム数</param>
    public static void SaveItemCount(int count)
    {
        PlayerPrefs.SetInt(ITEM_COUNT_KEY, count);
        PlayerPrefs.Save();
        Debug.Log($"アイテム数を保存しました: {count}");

        // アイテム数がクリア条件に達した場合、イベントを発火
        if (count >= ITEM_CLEAR_COUNT)
        {
            OnReachedItemClearCount?.Invoke();
            Debug.Log("アイテム数がクリア条件に達しました。");
            OnReachedItemClearCount = null; // イベントを解除して再度発火しないようにする
            OnItemCountChanged = null; // アイテム数変更イベントも解除
        }
        else
        {
            OnItemCountChanged?.Invoke();
            Debug.Log("アイテム数が変更されました。");
        }
    }

    /// <summary>
    /// アイテム数をロードする
    /// </summary>
    /// <returns>ロード結果。成功時はアイテム数、失敗時はエラーメッセージを返す。</returns>
    public static ErrorHandling.Result<int> LoadItemCount()
    {
        int value = PlayerPrefs.GetInt(ITEM_COUNT_KEY, -1);
        if (value == -1)
        {
            return ErrorHandling.Result<int>.Failure("アイテム数のロードに失敗しました。");
        }
        return ErrorHandling.Result<int>.Success(value);
    }
}
