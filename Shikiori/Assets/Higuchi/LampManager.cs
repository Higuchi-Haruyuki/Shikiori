using UnityEngine;

public class LampManager : MonoBehaviour
{
    [SerializeField] private ItemCollectLamp[] _lamps;
    [SerializeField] private float _fadeDuration = 4.0f;
    int _currentLampIndex = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ItemCountMover.OnItemCountChanged += HandleLampOn;
        ItemCountMover.OnReachedItemClearCount += HandleLampOn;

        foreach (var lamp in _lamps)
        {
            if (lamp != null)
            {
                lamp.LampOff(0.0f);
            }
        }
    }

    void HandleLampOn()
    {
        if (_lamps == null || _lamps.Length == 0) return;

        var lamp = _lamps[_currentLampIndex];
        if (lamp != null)
        {
            lamp.LampOn(_fadeDuration);
        }
        _currentLampIndex = (_currentLampIndex + 1) % _lamps.Length;
    }
}
