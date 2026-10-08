using UnityEngine;

[RequireComponent(typeof(WaterFreeze))]
public class PondSeasonManager : MonoBehaviour, IGimic
{
    private WaterFreeze _waterFreeze;
    [SerializeField] private float _fadeTime = 1.0f;

    private bool _isFadeSummer = false;

    private bool _isFadeWinter = false;
    private float _fadeTimer = 0.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _waterFreeze = GetComponent<WaterFreeze>();
    }

    // Update is called once per frame
    void Update()
    {
        if(_isFadeWinter)
        {
            _fadeTimer += Time.deltaTime;
            float freezeRate = Mathf.Clamp01(_fadeTimer / _fadeTime);
            _waterFreeze.SetFreeze(freezeRate);
            if(freezeRate >= 1.0f)
            {
                _isFadeWinter = false;
            }
        }
        else if(_isFadeSummer)
        {
            _fadeTimer += Time.deltaTime;
            float freezeRate = Mathf.Clamp01(1.0f - (_fadeTimer / _fadeTime));
            _waterFreeze.SetFreeze(freezeRate);
            if(freezeRate <= 0.0f)
            {
                _isFadeSummer = false;
            }
        }
    }

    public void OnSeasonChanged(GlobalSeason oldSeason, GlobalSeason newSeason)
    {
        Debug.Log($"PondSeasonManager: OnSeasonChanged: {oldSeason} -> {newSeason}");
        if(oldSeason == GlobalSeason.None)
        {
            _waterFreeze.SetFreeze(newSeason == GlobalSeason.Winter ? 1.0f : 0.0f);
            return;
        }
        if(newSeason == GlobalSeason.Summer)
        {
            OnFadeSummer();
        }
        else
        {
            OnFadeWinter();
        }
    }

    private void OnFadeSummer()
    {
        _isFadeSummer = true;
        _fadeTimer = 0.0f;
    }

    private void OnFadeWinter()
    {
        _isFadeWinter = true;
        _fadeTimer = 0.0f;
    }
}
