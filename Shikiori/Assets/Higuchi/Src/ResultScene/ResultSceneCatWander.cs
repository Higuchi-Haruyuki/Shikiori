using UnityEngine;

/// <summary>
/// リザルトシーン用の猫の挙動。入力は受け付けず、開始位置の周りをランダムに歩き回る。
/// 「目的地を決めて歩く → しばらく立ち止まる」を繰り返す。
/// </summary>
[RequireComponent(typeof(Animator))]
public class ResultSceneCatWander : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 0.5f;          // 歩く速さ(m/秒)
    [SerializeField] private float _rotationSharpness = 4.0f;  // 向きを変える鋭さ
    [SerializeField] private float _wanderRadius = 3.0f;       // 開始位置からどれぐらいの範囲を歩き回るか
    [SerializeField] private float _arriveDistance = 0.2f;     // 目的地に着いたとみなす距離
    [SerializeField] private float _maxWalkTime = 6.0f;        // 目的地に着けない場合に諦めるまでの時間
    [SerializeField] private Vector2 _idleTimeRange = new Vector2(1.0f, 3.0f); // 立ち止まる時間(最小, 最大)

    [Header("障害物の確認")]
    [SerializeField] private LayerMask _obstacleLayers = ~0;   // 障害物とみなすレイヤー
    [SerializeField] private float _obstacleCheckDistance = 0.6f; // 前方の障害物を調べる距離
    [SerializeField] private float _obstacleCheckHeight = 0.3f;   // 足元から何mの高さでレイを飛ばすか

    private Animator _anime;
    private Vector3 _homePosition;  // 歩き回る範囲の中心
    private Vector3 _target;        // 現在の目的地
    private float _stateTimer;      // 現在の状態(歩く/立ち止まる)の残り時間
    private bool _isWalking;

    void Start()
    {
        _anime = GetComponent<Animator>();
        _homePosition = transform.position;
        StartIdle();
    }

    void FixedUpdate()
    {
        _stateTimer -= Time.fixedDeltaTime;

        if (!_isWalking)
        {
            if (_stateTimer <= 0f) StartWalk();
            return;
        }

        var toTarget = _target - transform.position;
        toTarget.y = 0f;

        // 到着・時間切れ・前方に障害物のどれかで立ち止まる
        if (toTarget.magnitude <= _arriveDistance || _stateTimer <= 0f || IsBlocked(toTarget.normalized))
        {
            StartIdle();
            return;
        }

        var movement = toTarget.normalized;
        transform.Translate(movement * _moveSpeed * Time.fixedDeltaTime, Space.World);

        Quaternion targetRotation = Quaternion.LookRotation(movement, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSharpness * Time.fixedDeltaTime);
    }

    private void StartWalk()
    {
        // 開始位置を中心とした円の中からランダムに目的地を選ぶ
        var offset = Random.insideUnitCircle * _wanderRadius;
        _target = _homePosition + new Vector3(offset.x, 0f, offset.y);

        _isWalking = true;
        _stateTimer = _maxWalkTime;
        _anime.SetBool("walk", true);
    }

    private void StartIdle()
    {
        _isWalking = false;
        _stateTimer = Random.Range(_idleTimeRange.x, _idleTimeRange.y);
        _anime.SetBool("walk", false);
    }

    private bool IsBlocked(Vector3 direction)
    {
        var origin = transform.position + Vector3.up * _obstacleCheckHeight;
        var hits = Physics.RaycastAll(origin, direction, _obstacleCheckDistance, _obstacleLayers, QueryTriggerInteraction.Ignore);
        foreach (var hit in hits)
        {
            // 自分自身のコライダーは無視する
            if (hit.collider.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        var center = Application.isPlaying ? _homePosition : transform.position;
        Gizmos.DrawWireSphere(center, _wanderRadius);
        if (_isWalking)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _target);
        }
    }
}
