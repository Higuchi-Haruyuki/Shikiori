using UnityEngine;
using UnityEngine.InputSystem;
using Game.Attribute;

public class SceneLoadSystem : MonoBehaviour
{
    [SerializeField] FadeScreen _fadeScreen;
    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField] private bool _sceneEnd = false;

    public void EndScene()
    {
        _sceneEnd = true;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!_fadeScreen)
        {
            Debug.LogError("FadeScreenがアタッチされていません。");
        }
        else
        {
            _fadeScreen.OnFadeEnd += SubscribeToLoadNextScene;
            _fadeScreen.StartFade(1.0f, 0.0f);
        }
    }

    void OnDisable()
    {
        if(!_fadeScreen) return;
        _fadeScreen.OnFadeEnd -= LoadNextScene;
    }

    // Update is called once per frame
    void Update()
    {
        if(!_fadeScreen) return;

        // DEBUG用です。F1キーでシーン終了を呼び出す
        if(Keyboard.current.f1Key.wasPressedThisFrame) _sceneEnd = true;

        // シーン終了フラグが立ってるときにフェードアウトを行う
        if(_sceneEnd)
        {
            if(_fadeScreen.IsFading()) return;
            _fadeScreen.StartFade(0.0f,1.0f);
        }
    }

    void SubscribeToLoadNextScene()
    {
        if(!_fadeScreen) return;
        _fadeScreen.OnFadeEnd -= SubscribeToLoadNextScene;
        _fadeScreen.OnFadeEnd += LoadNextScene;
    }

    void LoadNextScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
    }
}
