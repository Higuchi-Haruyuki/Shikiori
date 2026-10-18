using System.Linq;
using UnityEngine;
using System.Collections.Generic;

public class SnowWall : MonoBehaviour, IGimic
{
     [SerializeField] private Collider _collider;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private GlobalSeason _activeSeason = GlobalSeason.Winter;
    [SerializeField] private float _fadeTime = 2.0f;
    private bool _isFadeIn = false;
    private bool _isFadeOut = false;
    private float _fadeTimer = 0.0f;

    private Material _originalMaterial;
    private readonly int _baseColorID = Shader.PropertyToID("_BaseColor");

    public void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        if(oldSeason == GlobalSeason.None)
        {
            SetBaseColorAlpha(newSeason == _activeSeason ? 1.0f : 0.0f);
            return;
        }
        if (newSeason == _activeSeason) FadeIn();
        else if(oldSeason == _activeSeason) FadeOut();

    }

    void FadeIn()
    {
        _isFadeIn = true;
        _isFadeOut = false;
        _fadeTimer = 0.0f;
        if(_collider)
            _collider.enabled = true;
    }

    void FadeOut()
    {
        _isFadeIn = false;
        _isFadeOut = true;
        _fadeTimer = 0.0f;
        if(_collider)
            _collider.enabled = false;
    }

    void SetBaseColorAlpha(float alpha)
    {
        Color baseColor = _originalMaterial.GetColor(_baseColorID);
        baseColor.a = alpha;
        _originalMaterial.SetColor(_baseColorID, baseColor);

        // 半分より薄くなったら影を消し、濃くなったら影を戻す。
        _renderer.shadowCastingMode = alpha < 0.8f 
            ? UnityEngine.Rendering.ShadowCastingMode.Off
            :UnityEngine.Rendering.ShadowCastingMode.On;

        // 透明度が0になったらレンダラーを無効化する
        _renderer.enabled = alpha > 0.0f;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if(!_collider) _collider = GetComponent<Collider>();
        if(!_renderer) _renderer = GetComponent<Renderer>();
        if(!_renderer) Debug.LogError("SnowWall: Renderer がないです。");
        _originalMaterial = _renderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        if(_isFadeIn)
        {
            _fadeTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(_fadeTimer / _fadeTime);
            SetBaseColorAlpha(alpha);
            if(alpha >= 1.0f)
            {
                _isFadeIn = false;
            }
        }
        else if(_isFadeOut)
        {
            _fadeTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(1.0f - (_fadeTimer / _fadeTime));
            SetBaseColorAlpha(alpha);
            if(alpha <= 0.0f)
            {
                _isFadeOut = false;
            }
        }
    }

}
