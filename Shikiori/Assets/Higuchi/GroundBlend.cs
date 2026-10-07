using System;
using UnityEngine;

public class GroundBlend : MonoBehaviour, IGimic
{
    [Serializable] 
    private struct SeasonTexture
    {
        public GlobalSeason _season;
        public Texture _baseMap;
        public Texture _normalMap;
    }

    [SerializeField] private Renderer[] _groundRenderers;
    [SerializeField] private Material _blendMaterial;
    [SerializeField] private SeasonTexture[] _seasonTextures;

    [SerializeField] private float _blendDuration = 1.0f;
    /*シェーダーのプロパティ名*/
    // 季節テクスチャ（URP Lit）側
    // GroundBlendシェーダー側
    private static readonly int FromMapId = Shader.PropertyToID("_FromMap");
    private static readonly int ToMapId = Shader.PropertyToID("_ToMap");
    private static readonly int FromNormalMapId = Shader.PropertyToID("_FromNormalMap");
    private static readonly int ToNormalMapId = Shader.PropertyToID("_ToNormalMap");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");

    // ランタイム用のマテリアル
    private Material _runtimeMaterial;

    private bool _isBlending = false;
    private float _blendTimer = 0.0f;

    private void Awake()
    {
        _runtimeMaterial = new Material(_blendMaterial);
        foreach (var renderer in _groundRenderers)
        {
            renderer.material = _runtimeMaterial;
        }
    }

    private void Update()
    {
        if (!_isBlending) return;

        _blendTimer += Time.deltaTime;
        float blend = Mathf.Clamp01(_blendTimer / _blendDuration);
        _runtimeMaterial.SetFloat(BlendId, blend);

        if (blend >= 1.0f)
        {
            _isBlending = false;
        }
    }

    private void OnDestroy()
    {
        if(_runtimeMaterial) Destroy(_runtimeMaterial);
    }

    // 季節変更時にGimicManagerから呼ばれる
    public void OnSeasonChanged(GlobalSeason from, GlobalSeason to)
    {
        if(!TryGetSeasonTexture(to, out SeasonTexture seasonTexture))
        {
            Debug.LogWarning($"GroundBlend: 季節テクスチャが見つかりませんでした。from={from}, to={to}");
            return;
        }
        SetNextSeasonTextures(seasonTexture);
        _isBlending = true;
        _blendTimer = 0.0f;
        if(from == GlobalSeason.None)
        {
            _blendTimer = _blendDuration; // 初回はブレンドせずに即座に切り替える
            return;
        }
    }

    private void SetNextSeasonTextures(SeasonTexture next)
    {
        // 今の To を From へ移す（色も凹凸も、runtime マテリアル自身から読む）
        _runtimeMaterial.SetTexture(FromMapId, _runtimeMaterial.GetTexture(ToMapId));
        _runtimeMaterial.SetTexture(FromNormalMapId, _runtimeMaterial.GetTexture(ToNormalMapId));

        // 新しい季節のテクスチャを To にセットする
        _runtimeMaterial.SetTexture(ToMapId, next._baseMap);
        _runtimeMaterial.SetTexture(ToNormalMapId, next._normalMap);
    }

    /// <summary>
    /// 指定された季節に対応するテクスチャを取得する
    /// </summary>
    /// <returns>見つかったらtrue</returns>
    private bool TryGetSeasonTexture(GlobalSeason season, out SeasonTexture seasonTexture)
    {
        foreach (var tex in _seasonTextures)
        {
            if (tex._season == season)
            {
                seasonTexture = tex;
                return true;
            }
        }
        seasonTexture = default;
        return false;
    }
}
