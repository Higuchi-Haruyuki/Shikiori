using UnityEngine;

public class MushRoom : MonoBehaviour , IGimic
{

    public void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        // 季節が変わったときの処理をここに記述する
        Debug.Log($"MushRoom: Season changed from {oldSeason} to {newSeason}");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
