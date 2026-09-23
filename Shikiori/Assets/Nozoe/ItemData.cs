using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    // 表示名
    [SerializeField] private string _displayName;

    // 拾った時に加算する値
    [SerializeField] private int _scoreValue = 1;

    // 取得時に鳴らすSE
    [SerializeField] private AudioClip _pickupSound;

    public string DisplayName => _displayName;
    public int ScoreValue => _scoreValue;
    public AudioClip PickupSound => _pickupSound;
}
