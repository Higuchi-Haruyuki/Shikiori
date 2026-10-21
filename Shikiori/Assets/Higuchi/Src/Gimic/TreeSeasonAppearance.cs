using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 季節に応じて木の見た目(マテリアル)をフェードで切り替えるギミック
/// 冬は枝の上面に雪を乗せ、葉を非表示にする。
/// </summary>
public class TreeSeasonAppearance : MonoBehaviour, IGimic
{
    [SerializeField] private GlobalSeason _winterSeason = GlobalSeason.Winter;
    [SerializeField] private float _fadeTime = 2.0f;

    // 幹・枝のRenderer (スロット0: 樹皮, スロット1: 枝の上面)
    [SerializeField] private Renderer _woodRenderer;
    // 葉のRenderer (スロット0: 葉, スロット1: 葉の先端)
    [SerializeField] private Renderer _foliageRenderer;

    // 通常時のマテリアル。配列の順序はRendererのスロット順と同じにすること。
    [SerializeField] private Material[] _normalWoodMaterials;
    [SerializeField] private Material[] _normalFoliageMaterials;

    // 冬のマテリアル。配列の順序はRendererのスロット順と同じにすること。
    [SerializeField] private Material[] _winterWoodMaterials;
    [SerializeField] private Material[] _winterFoliageMaterials;

    // 冬らしさの割合 (0: 通常, 1: 冬)
    private float _winterRate = 0.0f;
    // フェードの目標値 (0: 通常, 1: 冬)
    private float _targetRate = 0.0f;

    private ShadowCastingMode _foliageShadowMode;
    private MaterialPropertyBlock _propertyBlock;
    // 葉のフェード中だけ使う半透明マテリアル。
    private Material[] _fadeFoliageMaterials;

    // 元のマテリアルごとに作った半透明マテリアル。すべての木で使い回す。
    private static readonly Dictionary<Material, Material> s_fadeMaterialCache = new();
    private static readonly int s_baseColorID = Shader.PropertyToID("_BaseColor");

    public void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        _targetRate = newSeason == _winterSeason ? 1.0f : 0.0f;

        // 初期化時はフェードせずにすぐ切り替える。
        if(oldSeason == GlobalSeason.None)
        {
            _winterRate = _targetRate;
            ApplyWinterRate(_winterRate);
        }
    }

    void ApplyWinterRate(float rate)
    {
        // フェードが終わったら元のマテリアルに戻し、PropertyBlockを消す。
        if(rate <= 0.0f || rate >= 1.0f)
        {
            bool isWinter = rate >= 1.0f;
            ClearPropertyBlocks(_woodRenderer);
            ClearPropertyBlocks(_foliageRenderer);
            _woodRenderer.sharedMaterials = isWinter ? _winterWoodMaterials : _normalWoodMaterials;
            _foliageRenderer.sharedMaterials = isWinter ? _winterFoliageMaterials : _normalFoliageMaterials;
            _foliageRenderer.shadowCastingMode = _foliageShadowMode;
            return;
        }

        // 幹・枝は通常と冬の色を混ぜる。
        _woodRenderer.sharedMaterials = _normalWoodMaterials;
        for(int i = 0; i < _normalWoodMaterials.Length; i++)
        {
            Color normalColor = _normalWoodMaterials[i].GetColor(s_baseColorID);
            Color winterColor = _winterWoodMaterials[i].GetColor(s_baseColorID);
            _propertyBlock.Clear();
            _propertyBlock.SetColor(s_baseColorID, Color.Lerp(normalColor, winterColor, rate));
            _woodRenderer.SetPropertyBlock(_propertyBlock, i);
        }

        // 葉は半透明マテリアルに差し替えて、だんだん透明にする。
        _foliageRenderer.sharedMaterials = _fadeFoliageMaterials;
        for(int i = 0; i < _fadeFoliageMaterials.Length; i++)
        {
            Color color = _fadeFoliageMaterials[i].GetColor(s_baseColorID);
            color.a = 1.0f - rate;
            _propertyBlock.Clear();
            _propertyBlock.SetColor(s_baseColorID, color);
            _foliageRenderer.SetPropertyBlock(_propertyBlock, i);
        }

        // 薄くなったら影を消す。
        _foliageRenderer.shadowCastingMode = rate > 0.2f ? ShadowCastingMode.Off : _foliageShadowMode;
    }

    static void ClearPropertyBlocks(Renderer renderer)
    {
        for(int i = 0; i < renderer.sharedMaterials.Length; i++)
        {
            renderer.SetPropertyBlock(null, i);
        }
    }

    static Material GetFadeMaterial(Material source)
    {
        // 破棄済みのマテリアルが残っていたら作り直す。
        if(s_fadeMaterialCache.TryGetValue(source, out var fadeMaterial) && fadeMaterial) return fadeMaterial;

        fadeMaterial = new Material(source) { name = source.name + "_Fade" };
        fadeMaterial.SetFloat("_Surface", 1);
        fadeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        // 色のブレンドモードを変更する。
        fadeMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        fadeMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        // 深度を書き込まない。
        fadeMaterial.SetFloat("_ZWrite", 0);
        // 描画順を透明グループにする。
        fadeMaterial.SetOverrideTag("RenderType", "Transparent");
        fadeMaterial.renderQueue = (int)RenderQueue.Transparent;

        s_fadeMaterialCache[source] = fadeMaterial;
        return fadeMaterial;
    }

    void Awake()
    {
        if(!_woodRenderer) Debug.LogError("TreeSeasonAppearance: Wood の Renderer がないです。");
        if(!_foliageRenderer) Debug.LogError("TreeSeasonAppearance: Foliage の Renderer がないです。");

        _propertyBlock = new MaterialPropertyBlock();
        _foliageShadowMode = _foliageRenderer.shadowCastingMode;

        _fadeFoliageMaterials = new Material[_normalFoliageMaterials.Length];
        for(int i = 0; i < _normalFoliageMaterials.Length; i++)
        {
            _fadeFoliageMaterials[i] = GetFadeMaterial(_normalFoliageMaterials[i]);
        }
    }

    void Update()
    {
        if(Mathf.Approximately(_winterRate, _targetRate)) return;

        _winterRate = Mathf.MoveTowards(_winterRate, _targetRate, Time.deltaTime / _fadeTime);
        ApplyWinterRate(_winterRate);
    }
}
