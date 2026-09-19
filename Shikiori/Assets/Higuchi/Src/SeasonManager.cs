using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Attribute;


/// <summary>
/// 季節を管理するクラス
/// </summary>
public class SeasonManager : MonoBehaviour
{
    /// <summary>
    /// デフォルトの季節
    /// </summary>
    private const GlobalSeason DEFAULT_SEASON = GlobalSeason.Summer;


    /// <summary>
    /// 季節の遷移を定義する辞書
    /// 遷移元の季節をキーとして、遷移先の季節を値として設定
    /// </summary>
    private Dictionary<GlobalSeason,GlobalSeason> _seasonTransition = new()
    {
        { GlobalSeason.None, GlobalSeason.Summer },
        { GlobalSeason.Summer, GlobalSeason.Winter },
        { GlobalSeason.Winter, GlobalSeason.Summer }
    };

    /// <summary>
    /// 現在の季節
    /// </summary>
    public GlobalSeason CurrentSeason {
        get { return _currentSeason; }
        private set { _currentSeason = value; }
    }

    /// <summary>
    /// 季節を変更する
    /// </summary>
    /// 季節が変更されないとき、イベントは発火しない。
    /// <param name="newSeason">新しい季節</param>
    public void ChangeSeason(GlobalSeason newSeason)
    {
        if (newSeason == _currentSeason) return;

        var oldSeason = _currentSeason;
        _currentSeason = newSeason;

        OnSeasonChanged?.Invoke(oldSeason, newSeason);
    }

    /// <summary>
    /// 季節変更イベントに登録する。
    /// OnStart()より前に登録する必要がある。
    /// </summary>
    /// <param name="callback">季節変更時に呼び出されるデリゲート</param>
    public void SubscribeToSeasonChange(Action<GlobalSeason, GlobalSeason> callback)
    {
        OnSeasonChanged += callback;
    }

    /// <summary>
    /// 季節変更イベントから登録を解除する
    /// </summary>
    /// <param name="callback">登録済みのデリゲート</param>
    public void UnsubscribeFromSeasonChange(Action<GlobalSeason, GlobalSeason> callback)
    {
        OnSeasonChanged -= callback;
    }

    /// <summary>
    /// 季節変更イベントの登録をすべて解除する
    /// </summary>
    public void ResetSeasonEvent()
    {
        OnSeasonChanged = null;
    }

    void OnEnable()
    {
        OnSeasonChanged += (oldSeason, newSeason) =>
        {
            Debug.Log($"Season changed from {oldSeason} to {newSeason}");
        };
    }

    void OnDisable()
    {
        ResetSeasonEvent();
    }

    void Start()
    {
        // 初期状態の季節を設定。    
        ChangeSeason(DEFAULT_SEASON);
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ChangeSeason(_seasonTransition[_currentSeason]);
        }
    }

    /// <summary>
    /// 季節が変更されたときに呼び出されるイベント
    /// </summary>
    private Action<GlobalSeason/*遷移元の季節*/, GlobalSeason /*遷移先の季節*/> OnSeasonChanged;

    /*private変数*/
    // 現在の季節
    [SerializeField, ReadOnly] private GlobalSeason _currentSeason = GlobalSeason.None;

}
