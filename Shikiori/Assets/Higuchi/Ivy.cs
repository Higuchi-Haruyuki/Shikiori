using UnityEngine;

public class Ivy : MonoBehaviour,IGimic
{
    [SerializeField] private Collider _collider;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private GlobalSeason _activeSeason = GlobalSeason.Summer;
    [SerializeField] private float _fadeTime = 3.0f;
    private bool _isFadeIn = false;
    private bool _isFadeOut = false;
    private float _fadeTimer = 0.0f;

    private Material _originalMaterial;
    private readonly int _surfaceTypeID = Shader.PropertyToID("_Surface");
    private readonly int _baseColorID = Shader.PropertyToID("baseColorFactor");

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
        _collider.enabled = true;
    }

    void FadeOut()
    {
        _isFadeIn = false;
        _isFadeOut = true;
        _fadeTimer = 0.0f;
        _collider.enabled = false;
    }

    void SetBaseColorAlpha(float alpha)
    {
        Color baseColor = _originalMaterial.GetColor(_baseColorID);
        baseColor.a = alpha;
        _originalMaterial.SetColor(_baseColorID, baseColor);

        // 半分より薄くなったら影を消し、濃くなったら影を戻す。
        _renderer.shadowCastingMode = alpha < 0.5f 
            ? UnityEngine.Rendering.ShadowCastingMode.Off
            :UnityEngine.Rendering.ShadowCastingMode.On;

        // 透明度が0になったらレンダラーを無効化する
        _renderer.enabled = alpha > 0.0f;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _originalMaterial = _renderer.material;

        _originalMaterial.SetFloat(_surfaceTypeID,1);
        _originalMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        // 色のブレンドモードを変更する。
        _originalMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _originalMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        // 深度を書き込まない。
        _originalMaterial.SetFloat("_ZWrite", 0);
        // 描画順を透明グループにする。
        _originalMaterial.SetOverrideTag("RenderType", "Transparent");
        _originalMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
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
