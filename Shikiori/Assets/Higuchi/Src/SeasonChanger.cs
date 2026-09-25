using UnityEngine;

public class SeasonChanger : MonoBehaviour
{
    [SerializeField] private SeasonManager _seasonManager; // シーズンマネージャーの参照


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        // 衝突したオブジェクトが "Player" タグを持っているか確認
        if (collision.gameObject.CompareTag("Player"))
        {
            // シーズンを変更するメソッドを呼び出す
            _seasonManager.ChangeSeason(_seasonManager._seasonTransition[_seasonManager.CurrentSeason]);
        }
    }
}
