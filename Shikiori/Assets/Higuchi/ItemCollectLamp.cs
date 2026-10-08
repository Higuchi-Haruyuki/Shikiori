using UnityEngine;

public class ItemCollectLamp : MonoBehaviour
{
    private bool _isFadeOn = false;
    private bool _isFadeOff = false;
    private float _fadeTimer = 0.0f;
    private float _fadeDuration = 3.0f;
    private float _fadeWaitTime = 1.5f; // フェードの待機時間を追加
    private float _waitTimer = 0.0f;
    private Material _originalMaterial;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private Color _baseEmissionColor; // 基本の発光色を設定（必要に応じて変更）

    private void Awake()
    {
        var renderer = GetComponent<Renderer>();
        if(!renderer) return;
        _originalMaterial = renderer.material;
        _baseEmissionColor = _originalMaterial.GetColor(EmissionColorId);
    }
    private void OnDestroy()
    {
        if (_originalMaterial != null)
        {
            Destroy(_originalMaterial);
        }
    }

    public void LampOn(float fadeDuration)
    {
        _fadeDuration = fadeDuration;
        _isFadeOn = true;
        _isFadeOff = false;
        _waitTimer = 0.0f; // 待機タイマーをリセット
        _fadeTimer = 0.0f;
    }
    private void Update()
    {
        if (_isFadeOn)
        {
            if (_waitTimer < _fadeWaitTime)
            {
                _waitTimer += Time.deltaTime;
                return; // 待機時間中はフェード処理を行わない
            }
            _fadeTimer += Time.deltaTime;
            if (_fadeTimer >= _fadeDuration)
            {
                _isFadeOn = false;
                SetEmissionIntensity(1.0f);
            }
            else
            {
                float t = _fadeTimer / _fadeDuration;
                SetEmissionIntensity(Mathf.Lerp(0.0f, 1.0f, t));
            }
        }
        if(_isFadeOff)
        {
            _fadeTimer -= Time.deltaTime;
            if (_fadeTimer <= 0.0f)
            {
                _isFadeOff = false;
                SetEmissionIntensity(0.0f);
            }
            else
            {
                float t = _fadeTimer / _fadeDuration;
                SetEmissionIntensity(Mathf.Lerp(1.0f, 0.0f, t));
            }
        }
    }
    public void LampOff(float fadeDuration)
    {
        _fadeDuration = fadeDuration;
        _isFadeOff = true;
        _isFadeOn = false;
        _fadeTimer = fadeDuration;
    }

    private void SetEmissionIntensity(float intensity)
    {
        if (_originalMaterial != null)
        {
            _originalMaterial.SetColor(EmissionColorId, _baseEmissionColor * intensity);
        }
    }
}
