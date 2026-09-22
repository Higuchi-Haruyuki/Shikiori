using System;
using UnityEngine;
using Game.Attribute;

public class FadeScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeCanvas;
    [SerializeField] private float _fadeDuration = 1.0f;
    [SerializeField] private float _fadeStartAlpha = 0.0f;
    [SerializeField] private float _fadeEndAlpha = 1.0f;
    [SerializeField, ReadOnly] private float _fadeTimer = 0.0f;
    private bool _isFading = false;

    /// <summary>
    /// フェード終了時に呼ばれるイベント
    /// </summary>
    public Action OnFadeEnd;

    /// <summary>
    /// フェードを開始する
    /// </summary>
    /// フェードの開始アルファから終了アルファまでFadeDuration秒かけてフェードを行う。
    public void StartFade()
    {
        _fadeTimer = 0.0f;
        _isFading = true;
    }

    /// <summary>
    /// フェードを開始する
    /// </summary>
    /// <param name="startAlpha">フェードの開始アルファ</param>
    /// <param name="endAlpha">フェードの終了アルファ</param>
    /// <param name="duration">フェードの持続時間</param>
    public void StartFade(float startAlpha, float endAlpha, float duration)
    {
        _fadeStartAlpha = startAlpha;
        _fadeEndAlpha = endAlpha;
        _fadeDuration = duration;
        _fadeTimer = 0.0f;
        _isFading = true;
    }

    public void StartFade(float startAlpha, float endAlpha)
    {
        _fadeStartAlpha = startAlpha;
        _fadeEndAlpha = endAlpha;
        _fadeTimer = 0.0f;
        _isFading = true;
    }

    public bool IsFading() => _isFading;

    void OnEnable()
    {
        if(!fadeCanvas)
        {
            Debug.LogError("FadeImageになにもアタッチされていません。");
        }
        else
        {
            fadeCanvas.alpha = _fadeStartAlpha;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_isFading)
        {
            _fadeTimer += Time.deltaTime;
            float alpha = Mathf.Lerp(_fadeStartAlpha, _fadeEndAlpha, _fadeTimer / _fadeDuration);
            fadeCanvas.alpha = alpha;

            if(_fadeTimer >= _fadeDuration)
            {
                _isFading = false;
                OnFadeEnd?.Invoke();
            }
        }
    }
}
