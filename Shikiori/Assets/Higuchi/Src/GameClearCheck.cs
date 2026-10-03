using UnityEngine;

public class GameClearCheck : MonoBehaviour
{
    [SerializeField] private SceneLoadSystem _sceneLoadSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("プレイヤーがゴールに到達しました。ゲームクリア！");
            _sceneLoadSystem.EndScene();
        }
    }
}
