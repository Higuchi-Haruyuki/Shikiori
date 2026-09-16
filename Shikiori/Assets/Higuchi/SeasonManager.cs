using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Attribute;

public class SeasonManager : MonoBehaviour
{
    /// <summary>
    /// デフォルトの季節
    /// </summary>
    private const GlobalSeason DEFAULT_SEASON = GlobalSeason.Summer;

    /// <summary>
    /// 現在の季節
    /// </summary>
    public GlobalSeason CurrentSeason {
        get { return _currentSeason; }
        private set { _currentSeason = value; }
    }

    /// <summary>
    /// 季節が変更されたときに呼び出されるイベント
    /// </summary>
    public Action<GlobalSeason/*遷移元の季節*/, GlobalSeason /*遷移先の季節*/> OnSeasonChanged;

    /*private変数*/
    // 現在の季節
    [SerializeField, ReadOnly] private GlobalSeason _currentSeason = DEFAULT_SEASON;

    void OnEnable()
    {
        _currentSeason = DEFAULT_SEASON;
        OnSeasonChanged += (oldSeason, newSeason) =>
        {
            Debug.Log($"Season changed from {oldSeason} to {newSeason}");
        };
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.enterKey.wasPressedThisFrame)
        {
            ChangeSeason(CurrentSeason == GlobalSeason.Summer ? GlobalSeason.Winter : GlobalSeason.Summer);
        }
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
}
