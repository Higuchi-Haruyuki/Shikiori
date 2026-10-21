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

    // 地面用：ONにすると、この季節テクスチャを「草テクスチャ」として全シェーダーに公開する。
    // 崖のCliffGroundBlendシェーダーがこれを読んで、地面と同じ草を描く。
    // 地面のGroundBlendだけONにすること（複数ONにすると取り合いになる）。
    [SerializeField] private bool _publishAsGlobalGrass = false;

    // 崖用：草にする場所を決めるマスク（夏の崖テクスチャ）。
    // 空のままなら、_seasonTexturesの夏の_baseMapを自動で使う。
    [SerializeField] private Texture _grassMaskMap;

    /*シェーダーのプロパティ名*/
    // 季節テクスチャ（URP Lit）側
    // GroundBlendシェーダー側
    private static readonly int FromMapId = Shader.PropertyToID("_FromMap");
    private static readonly int ToMapId = Shader.PropertyToID("_ToMap");
    private static readonly int FromNormalMapId = Shader.PropertyToID("_FromNormalMap");
    private static readonly int ToNormalMapId = Shader.PropertyToID("_ToNormalMap");
    private static readonly int BlendId = Shader.PropertyToID("_Blend");
    // CliffGroundBlendシェーダー側
    private static readonly int GrassMaskMapId = Shader.PropertyToID("_GrassMaskMap");
    // 全シェーダー共通（グローバル）
    private static readonly int GlobalGrassFromMapId = Shader.PropertyToID("_GlobalGrassFromMap");
    private static readonly int GlobalGrassToMapId = Shader.PropertyToID("_GlobalGrassToMap");
    private static readonly int GlobalGrassFromNormalMapId = Shader.PropertyToID("_GlobalGrassFromNormalMap");
    private static readonly int GlobalGrassToNormalMapId = Shader.PropertyToID("_GlobalGrassToNormalMap");
    private static readonly int GlobalGrassBlendId = Shader.PropertyToID("_GlobalGrassBlend");

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

        // 崖用のマテリアルなら、草にする場所のマスクを渡す
        if (_runtimeMaterial.HasProperty(GrassMaskMapId))
        {
            Texture mask = _grassMaskMap;
            if (!mask && TryGetSeasonTexture(GlobalSeason.Summer, out SeasonTexture summer))
            {
                mask = summer._baseMap;
            }
            _runtimeMaterial.SetTexture(GrassMaskMapId, mask);
        }
    }

    private void Update()
    {
        if (!_isBlending) return;

        _blendTimer += Time.deltaTime;
        float blend = Mathf.Clamp01(_blendTimer / _blendDuration);
        _runtimeMaterial.SetFloat(BlendId, blend);
        if (_publishAsGlobalGrass) Shader.SetGlobalFloat(GlobalGrassBlendId, blend);

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

        // 地面なら、同じテクスチャを崖のシェーダーにも公開する
        if (_publishAsGlobalGrass)
        {
            Shader.SetGlobalTexture(GlobalGrassFromMapId, _runtimeMaterial.GetTexture(FromMapId));
            Shader.SetGlobalTexture(GlobalGrassToMapId, _runtimeMaterial.GetTexture(ToMapId));
            Shader.SetGlobalTexture(GlobalGrassFromNormalMapId, _runtimeMaterial.GetTexture(FromNormalMapId));
            Shader.SetGlobalTexture(GlobalGrassToNormalMapId, _runtimeMaterial.GetTexture(ToNormalMapId));
        }
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
