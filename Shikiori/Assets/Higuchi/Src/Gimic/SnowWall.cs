using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class SnowWall : MonoBehaviour, IGimic
{
    public void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        // 子オブジェクトのタグを確認し、現在の季節に応じて有効化または無効化する
        foreach(Transform child in transform)
        {
            bool isActive = child.CompareTag(newSeason.ToString());
            GameObjectSetActive(child.gameObject, isActive);
        }

    }

    /// <summary>
    /// このスクリプトがアタッチされているオブジェクトから見て孫オブジェクトの有効化・無効化を行うメソッド
    /// </summary>
    /// <param name="obj">有効化または無効化する子オブジェクト</param>
    /// <param name="isActive">有効かどうか</param>
    private void GameObjectSetActive(GameObject obj, bool isActive)
    {
        foreach (var child in obj.GetComponentsInChildren<Transform>(true).ToList())
        {
            child.gameObject.SetActive(isActive);
        }
    }

}
