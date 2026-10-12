using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TitleScenePlayerInput : MonoBehaviour
{
    [SerializeField] private SceneLoadSystem _sceneLoadSystem;
    [SerializeField] private AudioClip _pressButtonSound;
    private AudioSource _audioSource;
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
        _audioSource.Play();
        _sceneLoadSystem.EndScene();
        // ゲーム開始の処理をここに記述
        Debug.Log("ゲーム開始");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.clip = _pressButtonSound;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
