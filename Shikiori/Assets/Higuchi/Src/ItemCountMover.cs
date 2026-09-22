using UnityEngine;

/// <summary>
/// アイテム数を保存・ロードするためのクラス
/// </summary>
public static class ItemCountMover
{
    private static readonly string ITEM_COUNT_KEY = "ItemCount";
    
    /// <summary>
    /// アイテム数を保存する
    /// </summary>
    /// <param name="count">アイテム数</param>
    public static void SaveItemCount(int count)
    {
        PlayerPrefs.SetInt(ITEM_COUNT_KEY, count);
        PlayerPrefs.Save();
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
