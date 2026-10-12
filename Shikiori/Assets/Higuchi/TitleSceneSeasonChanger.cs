using UnityEngine;

public class TitleSceneSeasonChanger : MonoBehaviour
{
    [SerializeField] float _changeInterval = 3f;
    [SerializeField] SeasonManager _seasonManager;
    float _timer = 0f;

    // Update is called once per frame
    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _changeInterval)
        {
            _timer %= _changeInterval;
            _seasonManager.ChangeSeason(_seasonManager._seasonTransition[_seasonManager.CurrentSeason]);
        }
    }
}
