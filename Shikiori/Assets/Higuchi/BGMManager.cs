using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [SerializeField] private SceneLoadSystem _sceneLoadSystem;
    [SerializeField] private AudioClip _bgmClip;
    [SerializeField] private float _fadeDuration = 1.0f;
    private AudioSource _audioSource;
    private float _fadeTimer = 0.0f;
    bool _isFadingIn = false;
    bool _isFadingOut = false;

    private float _originalVolume;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = _bgmClip;
        _audioSource.loop = true;
        _audioSource.Play();
        _originalVolume = _audioSource.volume;
        FadeIn();
        if(_sceneLoadSystem)
        {
            _sceneLoadSystem.OnSceneEnd += FadeOut;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(_isFadingIn)
        {
            FadeInProgress();
        }
        else if(_isFadingOut)
        {
            FadeOutProgress();
        }
    }

    void FadeIn()
    {
        _isFadingIn = true;
        _isFadingOut = false;
        _fadeTimer = 0.0f;
    }

    void FadeInProgress()
    {
        if (_fadeTimer < _fadeDuration)
        {
            _fadeTimer += Time.deltaTime;
            float volume = Mathf.Clamp01(_fadeTimer / _fadeDuration) * _originalVolume;
            _audioSource.volume = volume;
        }
        else
        {
            _isFadingIn = false;
        }
    }

    void FadeOut()
    {
        _isFadingOut = true;
        _isFadingIn = false;
        _fadeTimer = _fadeDuration;
    }

    void FadeOutProgress()
    {
        if (_fadeTimer > 0.0f)
        {
            _fadeTimer -= Time.deltaTime;
            float volume = Mathf.Clamp01(_fadeTimer / _fadeDuration) * _originalVolume;
            _audioSource.volume = volume;
        }
        else
        {
            _isFadingOut = false;
        }
    }
}
