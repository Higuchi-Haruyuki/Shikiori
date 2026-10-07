using UnityEngine;

public class GateController : MonoBehaviour
{
    [SerializeField] private float _openDuration = 3.0f; // ゲートが開く時間
    private Collider _gateCollider;
    bool isGateOpening = false;
    float _openTimer = 0.0f;

    private float _openWaitTimer = 0.0f; // ゲートが開くまでの待機時間
    private float _openWaitDuration = 2.0f; // ゲートが開くまでの待機時間（1秒）

    void OnEnable()
    {
        ItemCountMover.OnReachedItemClearCount += HandleItemClearCountReached;
    }

    void Start()
    {
        _gateCollider = GetComponent<Collider>();

    }
    void Update()
    {
        // DEBUG用
        if (Input.GetKeyDown(KeyCode.F2))
        {
            OpenGate();
        }

        if(!isGateOpening) return;

        if (_openWaitTimer < _openWaitDuration) // _openWaitDuration秒待機
        {
            _openWaitTimer += Time.deltaTime;
            return; // 待機時間中はゲートの開閉処理を行わない
        }

        // ゲートが開くアニメーション
        var targetYRotation = Quaternion.Euler(0, -90, 0); // 目標の回転角度
        _openTimer += Time.deltaTime;
        if (_openTimer >= _openDuration)
        {
            _openTimer = _openDuration;
            isGateOpening = false; // ゲートの開閉が完了したらフラグをリセット
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
        if (_gateCollider != null)
        {
            _gateCollider.enabled = false;
        }   
        isGateOpening = true;

    }

}
