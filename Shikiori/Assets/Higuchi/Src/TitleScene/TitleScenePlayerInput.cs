using UnityEngine;
using UnityEngine.InputSystem;

public class TitleScenePlayerInput : MonoBehaviour
{
    [SerializeField] private SceneLoadSystem _sceneLoadSystem;
    private TitleSceneController _playerInput;

    void OnEnable()
    {
        _playerInput = new TitleSceneController();
        _playerInput.Enable();

        _playerInput.Player.NextScene.started += OnGameStart;
    }

    void OnDisable()
    {
        _playerInput.Disable();
    }

    void OnGameStart(InputAction.CallbackContext context)
    {
        if(!_sceneLoadSystem) return;
        _sceneLoadSystem.EndScene();
        // ゲーム開始の処理をここに記述
        Debug.Log("ゲーム開始");
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
