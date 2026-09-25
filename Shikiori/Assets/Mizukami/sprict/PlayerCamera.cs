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
 
public class PlayerCamera : MonoBehaviour
{

    private GameObject mainCamera;              //メインカメラ格納用
    private GameObject playerObject;            //回転の中心となるプレイヤー格納用
    public float rotateSpeed = 1.0f;            //回転の速さ
    [SerializeField] private float _playerCameraDistance = 1.0f;    // プレイヤーとカメラの距離

    // Y軸回転量(ラジアン)  // 1π = 180°
    private float _yAngle = -Mathf.PI /2 ;  // 初期値より90°回したい

    // X軸回転量(ラジアン)
    private float _xAngle = Mathf.PI /4 - 0.3f;

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
    }

    private void FixedUpdate()
    {
        
    }

    // LateUpdate is called once per frame, after Update
    private void LateUpdate()   // Updateの後にされるUpdate。
    {
        GetInputValue();
        FollowPlayer();
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
        _yAngle -= rotationInputValue.x * rotateSpeed * Time.deltaTime;
        // Y軸の入力でカメラをプレイヤーを中心にX軸回転させる。
        _xAngle += rotationInputValue.y * rotateSpeed * Time.deltaTime;
        

        // X軸の回転量を制限する。
        // Mathf.Clamp(現在の値, 最小値, 最大値)
        // if (現在の値<最小値) 現在の値 = 最小値;    // みたいなやつ
        _xAngle = Mathf.Clamp(_xAngle, CLAMPED_X_ANGLE_MIN, CLAMPED_X_ANGLE_MAX);

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
        var unitCameraOffset = new Vector3(offsetX, height, offsetZ);

        // 今までは単位円(半径が1)での座標だったので、プレイヤーとカメラの距離を掛けて、カメラのオフセットを求める。
        var cameraOffset = unitCameraOffset * _playerCameraDistance;

        // プレイヤーの位置にカメラのオフセットを足すことで、カメラの位置を求める。
        transform.position = playerPos + cameraOffset;

        // カメラの向きは常にプレイヤーの位置を向くようにする。
        // カメラ自体が回転する。
        transform.LookAt(playerPos);

        //意味わからんならこれ読め
        //https://claude.ai/artifact/5A55UVemuydxiYXegrt9sS

    }
}