using UnityEngine;

// マテリアルをフェードさせる。
public class MaterialFadeSystem : MonoBehaviour
{
    [SerializeField] private SeasonManager _seasonManager;
    [SerializeField] private GlobalSeason _fadeInSeason = GlobalSeason.Summer;
    private Material _originalMaterial;
    private bool _isFadeIn = false;
    private float _fadeDuration = 3.0f;
    private float _fadeTimer = 0.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var renderer = GetComponent<Renderer>();
        if(!renderer)
        {
            Debug.LogError("Rendererがアタッチされていません。");
            return;
        }
        _originalMaterial = renderer.material;
        renderer.material = _originalMaterial;

        _seasonManager.SubscribeToSeasonChange(OnSeasonChanged);
    }

    void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        if (newSeason == _fadeInSeason)
        {
            FadeIn();
        }
    }

    void FadeIn()
    {
        _isFadeIn = true;
        _fadeTimer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (_isFadeIn)
        {
            _fadeTimer += Time.deltaTime;
            float alpha = Mathf.Clamp01(_fadeTimer / _fadeDuration);
            Color color = _originalMaterial.color;
            color.a = alpha;
            _originalMaterial.color = color;

            if (_fadeTimer >= _fadeDuration)
            {
                _isFadeIn = false;
            }
        }
    }
}
