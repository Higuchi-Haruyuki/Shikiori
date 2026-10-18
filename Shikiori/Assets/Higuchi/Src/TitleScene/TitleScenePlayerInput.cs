using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class TitleScenePlayerInput : MonoBehaviour
{
    [SerializeField] private SceneLoadSystem _sceneLoadSystem;
    [SerializeField] private AudioClip _pressButtonSound;
    public int ChoiceIndex {get; private set;} = 0; // 選択肢のインデックスを保持する変数
    private AudioSource _audioSource;
    private TitleSceneController _playerInput;

    enum Choice : int
    {
        StartGame = 0,
        ExitGame = 1
    }

    void OnEnable()
    {
        _playerInput = new TitleSceneController();
        _playerInput.Enable();

        _playerInput.Player.NextScene.started += HandleChoiceSelection;
        _playerInput.Player.NextChoice.performed += (context) => {
            ChoiceIndex = (ChoiceIndex + 1) % 2; // 選択肢の数に応じて変更
        };
        _playerInput.Player.PreviousChoice.performed += (context) => {
            ChoiceIndex = (ChoiceIndex - 1 + 2) % 2; // 選択肢の数に応じて変更
        };
    }

    void OnDisable()
    {
        _playerInput.Disable();
    }

    void HandleChoiceSelection(InputAction.CallbackContext context)
    {
        if(ChoiceIndex == (int)Choice.StartGame)
        {
            OnGameStart(context);
        }
        else if(ChoiceIndex == (int)Choice.ExitGame)
        {
            Application.Quit();
        }
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
