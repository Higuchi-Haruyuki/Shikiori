using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GateController : MonoBehaviour
{
    [SerializeField] private float _openDuration = 3.0f; // ゲートが開く時間
    [SerializeField] private AudioClip _gateOpenSound; // ゲートが開くときの効果音
    private Collider _gateCollider;
    private AudioSource _audioSource;
    bool _isGateOpening = false;
    float _openTimer = 0.0f;

    private float _openWaitTimer = 0.0f; // ゲートが開くまでの待機時間
    private float _openWaitDuration = 2.0f; // ゲートが開くまでの待機時間（1秒）
    private bool _isSoundPlayed = false; // 音声が再生されたかどうかのフラグ

    void OnEnable()
    {
        ItemCountMover.OnReachedItemClearCount += HandleItemClearCountReached;
    }

    void Start()
    {
        _gateCollider = GetComponent<Collider>();
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.clip = _gateOpenSound;
    }
    void Update()
    {
    #if UNITY_EDITOR
        // DEBUG用
        if (Input.GetKeyDown(KeyCode.F2))
        {
            OpenGate();
        }
    #endif

        if(!_isGateOpening) return;


        if (_openWaitTimer < _openWaitDuration) // _openWaitDuration秒待機
        {
            _openWaitTimer += Time.deltaTime;
            return; // 待機時間中はゲートの開閉処理を行わない
        }

        // ゲートが開くアニメーション
        var targetYRotation = Quaternion.Euler(0, -90, 0); // 目標の回転角度
        _openTimer += Time.deltaTime;
        if(!_isSoundPlayed) // 開き始めて0.5秒後に音声を再生
        {
            _audioSource.Play();
            _isSoundPlayed = true; // 音声が再生されたことを記録
        }
        if (_openTimer >= _openDuration)
        {
            _openTimer = _openDuration;
            _isGateOpening = false; // ゲートの開閉が完了したらフラグをリセット
            _audioSource.Stop(); // ゲートが開いた後に音声を停止
        }
        transform.rotation = Quaternion.Slerp(Quaternion.identity, targetYRotation, _openTimer / _openDuration);
    }

    void HandleItemClearCountReached()
    {
        OpenGate();
    }

    void OpenGate()
    {
        // ゲートを開く処理をここに記述
        Debug.Log("ゲートが開きました。");
        // ゲートのコライダーを無効化して通過可能にする
        if (_gateCollider)
        {
            _gateCollider.enabled = false;
        }   
        _isGateOpening = true;
    }

}
