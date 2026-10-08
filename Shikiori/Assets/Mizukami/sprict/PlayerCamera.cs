//using UnityEngine;
//using UnityEngine.InputSystem;
//using static Unity.VisualScripting.AnnotationUtility;

//public class Camera : MonoBehaviour
//{
//    GameObject player;
//    private PlayerInput _playerInput;   // プレイヤーインプット型(入力のイベントとかがくる)
//    private Vector2 _rotationInputValue; // マウスクリックやマウスパッド(右スティック)が入力されたときに各、x,yの値の大きさを受け取る変数
//    Vector3 CameraPos;
//    private void Awake()
//    {
//        _playerInput = new PlayerInput();
//        // 回転Aciton
//        _playerInput.Camera.Rotation.started += OnRotation; // 押され始めたら呼び出されるイベントに登録する
//        _playerInput.Camera.Rotation.performed += OnRotation;   // 押されているときに呼び出されるイベントに登録する
//        _playerInput.Camera.Rotation.canceled += OnRotation;    // 話されたときに呼び出されるイベントに登録する

//        _playerInput.Enable();

//    }
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {
//        player = GameObject.FindGameObjectWithTag("Player");
//    }
//    private void Update()
//    {
//        RotationCamera();
//    }
//    // Update is called once per frame
//    private void LateUpdate()   // Updateの後にされるUpdate。
//    {

//        Vector3 PlayerPos = player.transform.position;
//        CameraPos = new(PlayerPos.x, PlayerPos.y + 8, PlayerPos.z - 10);
//        transform.position = CameraPos;
//    }
//    public void OnRotation(InputAction.CallbackContext context)
//    {
//        _rotationInputValue = context.ReadValue<Vector2>();
//    }
//    private void RotationCamera()
//    {
//        Vector3 rotation = new Vector3(_rotationInputValue.x, 0, _rotationInputValue.y);
//        transform.RotateAround(this.transform.position, rotation, 0.5f);
//    }
//}

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerCamera : MonoBehaviour
{

    private GameObject mainCamera;              //メインカメラ格納用
    private GameObject playerObject;            //回転の中心となるプレイヤー格納用

    //[FormerlySerializedAs("rotateSpeed")]
    [SerializeField] private float _rotateSpeed = 1.0f;            //回転の速さ
    [SerializeField] private float _playerCameraDistance = 1.0f;    // プレイヤーとカメラの距離

    [SerializeField] private float _rotationSharpness = 12.0f;  // 回転の追いつきの速さ
    [SerializeField] private float _followSharpness = 10.0f;    // 移動の追いつきの速さ

    [Header("壁ののめりこみ防止")]
    // ~0の~はビット反転演算子であり、0ビットを反転する   
    [SerializeField] private LayerMask _collisionLayers = ~0;   // カメラが避けるレイヤー(Player自身は除外推奨)
    [SerializeField] private float _collisionRadius = 0.2f;     // カメラの当たり判定の半径
    [SerializeField] private float _minDistance = 0.3f;         // 近づける限界の距離
    [SerializeField] private float _collisionRecoverSharpness = 6.0f;   // 壁から離れた時に」元の距離へ戻る速さ

    private float _currentDistance; // 実際に使うカメラ距離

    // 入力で決まる目標の角度
    // 入力はこっちに入るが、カメラはまだ動かない。

    // Y軸回転量(ラジアン)  // 1π = 180°
    private float _targetYAngle = -Mathf.PI /2 ;  // 初期値より90°回したい
    //private float _yAngle = -Mathf.PI / 2;  // 初期値より90°回したい

    // X軸回転量(ラジアン)
    private float _targetXAngle = Mathf.PI /4 - 0.3f;

    // カメラが実際に使う角度(カメラを動かすための変数)

    private float _yAngle = -Mathf.PI / 2.0f;
    private float _xAngle = 0.0f;

    private Vector3 _smoothedPivot;

    // -Math.PI(-180°) / 2f = -90° + 0.3f(0.3* 1ラジアン(57°)) =  = -90°+ 17°  = およそ73°
    // ラジアンで考えると... π/2 + 0.3 * 180 / π (π= 180°)
    // つまり、1ラジアンの0.3倍分を計算している
    private const float CLAMPED_X_ANGLE_MIN = -Mathf.PI / 2.0f + 1f; // X軸回転量の最小値(ラジアン)
    private const float CLAMPED_X_ANGLE_MAX = Mathf.PI / 2.0f - 0.8f; // X軸回転量の最大値(ラジアン)
    private PlayerInput _playerInput;   // プレイヤーインプット型(入力のイベントとかがくる)


   

    private void Awake()
    {
        _playerInput = new PlayerInput();

       _playerInput.Enable();

    }

    //呼び出し時に実行される関数
    void Start()
    {
        //メインカメラとプレイヤーをそれぞれ取得
        mainCamera = Camera.main.gameObject;
        playerObject = GameObject.FindGameObjectWithTag("Player");
        _currentDistance = _playerCameraDistance;

        _xAngle = _targetXAngle;
        _yAngle = _targetYAngle;
    }

    private void FixedUpdate()
    {
        
    }

    // LateUpdate is called once per frame, after Update
    private void LateUpdate()   // Updateの後にされるUpdate。
    {
        GetInputValue();    // 入力 -> 目標
        SmoothValues();     // 現在 -> 目標へ寄せる
        FollowPlayer();     // 現在の値でカメラを置く
    }

    private void OnDestroy()
    {
        _playerInput.Disable();
    }


    private void GetInputValue()
    {
        // 入力値を取得する
        // データを入れる変数 = プレイヤーインプットクラスのカメラRotationの値を読む
        Vector2 rotationInputValue = _playerInput.Player.CameraRotation.ReadValue<Vector2>();




        // _yAngleはy軸を横方向にぐるっと回る
        // X軸の入力でカメラをプレイヤーを中心にY軸回転させる。
        _targetYAngle -= rotationInputValue.x * _rotateSpeed * Time.deltaTime;
        //_yAngle -= rotationInputValue.x * _rotateSpeed * Time.deltaTime;

        // Y軸の入力でカメラをプレイヤーを中心にX軸回転させる。
        _targetXAngle += rotationInputValue.y * _rotateSpeed * Time.deltaTime;
        

        // X軸の回転量を制限する。
        // Mathf.Clamp(現在の値, 最小値, 最大値)
        // if (現在の値<最小値) 現在の値 = 最小値;    // みたいなやつ
        _targetXAngle = Mathf.Clamp(_targetXAngle, CLAMPED_X_ANGLE_MIN, CLAMPED_X_ANGLE_MAX);

    }

    private void FollowPlayer()
    {
        // z…前後、x…左右、y…上下
        // プレイヤーの位置を原点と考える。
        // Var … C++でいうautoと同じ
        var playerPos = playerObject.transform.position;
        playerPos.z -= 0.1f;
        playerPos.y += 0.51f;

        // 基本的な考え方は、XZ平面の単位円とYZ平面の単位円を組み合わせて、
        // カメラとプレイヤーのオフセットを求める。

        // 横から見た(YZ平面)図: pitch(_xAngle)から「高さ」と「水平半径」を求める。
        // 高さは sin(pitch)、水平半径は cos(pitch)。
        var height = Mathf.Sin(_xAngle);

        // X軸の回転量によってXZ平面上の水平半径が変化する。
        var xzRadius = Mathf.Cos(_xAngle);

        // 真上から見た(XZ平面)図: 水平半径をyaw(_yAngle)でX・Zに配分する。
        // XもZも同じ水平半径を土台にしているので、どちらにもcos(pitch)がかかる。
        var offsetX = xzRadius * Mathf.Cos(_yAngle);
        var offsetZ = xzRadius * Mathf.Sin(_yAngle);

        // 単位円(半径1)上でのオフセットがまとまったので、ベクトルにする。
        // 単位ベクトル(プレイヤー->カメラの方向)
        var direction = new Vector3(offsetX, height, offsetZ).normalized;

        // ここから壁判定
        float targetDistance = _playerCameraDistance;

        if (Physics.SphereCast(
            playerPos,              // 始点(プレイヤー)
            _collisionRadius,       // 球の半径
            direction,              // カメラへ向かう方向
            out RaycastHit hit,
            _playerCameraDistance,  // 最大距離
            _collisionLayers,
            QueryTriggerInteraction.Ignore
            ))
        {
            // 壁の手前までに縮める(最低距離は確保)
            targetDistance = Mathf.Max(hit.distance,_minDistance);
        }

        if (targetDistance < _currentDistance)
        {
            _currentDistance = targetDistance;  // 即座に縮める(めり込み防止を優先)
        }
        else
        {
            float t = CalcSmoothT(_collisionRecoverSharpness);
            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, t);
        }

            // 今までは単位円(半径が1)での座標だったので、プレイヤーとカメラの距離を掛けて、カメラのオフセットを求める。
            var cameraOffset = direction * _currentDistance;

        // プレイヤーの位置にカメラのオフセットを足すことで、カメラの位置を求める。
        transform.position = playerPos + cameraOffset;

        // カメラの向きは常にプレイヤーの位置を向くようにする。
        // カメラ自体が回転する。
        transform.LookAt(playerPos);

        //意味わからんならこれ読め
        //https://claude.ai/artifact/5A55UVemuydxiYXegrt9sS

    }

    private void SmoothValues()
    {
        // このフレームで「残り何割」近づくか
        float rotationT = CalcSmoothT(_rotationSharpness);
        // 現在の角度を目標に、rotationTの割合だけ近づける
        _yAngle = Mathf.Lerp(_yAngle, _targetYAngle, rotationT);    // _yAngle + (_targetYAngle - _yAngle) * rotationT
        _xAngle = Mathf.Lerp(_xAngle, _targetXAngle, rotationT);

        // 中心点も同じように、プレイヤー位置へ近づける
        float followT = CalcSmoothT(_followSharpness);
        _smoothedPivot = Vector3.Lerp(_smoothedPivot, GetTargetPivot(), followT);

    }

    

    private static float CalcSmoothT(float sharpness)
    {
        // 1- (このフレーム後に残る割合) = このフレームで縮める割合
        return 1.0f - Mathf.Exp(-sharpness * Time.deltaTime);
        // return で返される値はこのフレームでどのぐらい(%)で縮めるか
        // sharpness=15かつ60fpsだとする。
        // まず、Time.deltaTimeは1/60である。約0.0167秒
        // -sharpness * Time.deltaTime = 15*0.0167 = 0.25   // 全体で15の鋭さで動くとしたら、15*1/60分
        // Exc(0.25) = 0.779 -> このフレームが終わったら残り縮める%が77.9%になる
        // 1-0.779 = 0.221 -> このフレームで残り22.9%縮める
    }

    private Vector3 GetTargetPivot()
    {
        Vector3 pivot = playerObject.transform.position;    // プレイヤー位置をコピー
        pivot.y += _playerCameraDistance;                   // コピーの高さをあげる
        return pivot;
    }

}