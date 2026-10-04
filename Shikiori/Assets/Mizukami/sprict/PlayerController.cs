using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerController : MonoBehaviour
{
 
    [SerializeField] private float _moveSpeed = 1f;    // プレイヤーの移動量
    [SerializeField] private float _rotationSpeed = 1f;

    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float _rotationSharpness = 10.0f;   // どのぐらいの鋭さか(動きのキビキビさ)


    private PlayerInput _playerInput;   // プレイヤーインプット型(入力のイベントとかがくる)
    private Vector2 _moveInputValue;  // キーやマウスパッド(左スティック)が入力されたときに各、x,yの値の大きさを受け取る変数

    Vector3 _movementtarget; // 回転の終着地

    Vector3 _movement;   // キャラクターを動かすための変数



    private void Awake()    // Start()の前に呼ばれる関数
    {
        _playerInput = new PlayerInput();   // PlayerInputのインスタンス作成

        // Actionイベントの登録
        // 移動Aciton
        _playerInput.Player.Move.started += OnMove; // 押され始めたら呼び出されるイベントに登録する
        _playerInput.Player.Move.performed += OnMove;   // 押されているときに呼び出されるイベントに登録する
        _playerInput.Player.Move.canceled += OnMove;    // 話されたときに呼び出されるイベントに登録する
        
        // Input Actionを機能させる処理
        _playerInput.Enable(); // これを書かないとイベント事態が起きない。
    }

    private void OnDestroy()    // ゲームオブジェクトが破壊されるときに呼ばれる関数
    {
        _playerInput.Dispose(); // 呼び出したからには壊せよ
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        MoveAndRotatePlayer();

        //// 現在フレーム位置
        //var position = transform.position;

        //// 移動量計算
        //var delta = position - _prevPosition;

        //// 次のUpdateで使うための前フレーム位置更新
        //_prevPosition = position;

        //// 製紙している状態だと、進行を特定できないため回転しない
        //if (delta == Vector3.zero)
        //    return;

        //// 進行方向(移動量ベクトル)にむくようなクォータニオンを取得
        //var rotation = Quaternion.LookRotation(delta, Vector3.up);

        //// オブジェクトの回転に反映
        //this.transform.rotation = rotation;



        
    }
    public void OnMove(InputAction.CallbackContext context) // (InputAction.CallbackContext型)
    {
        _moveInputValue = context.ReadValue<Vector2>();   // キーや左スティックが入力された値の大きさを受け取る

    }
    
    private void MoveAndRotatePlayer()
    {
        // forwardとrightは1~-1を使った方向の情報が入る
        // 例えば、forward(z軸)が(0,0,1)の時は前方向、(0,0,-1)の時は後ろ方向であることを示す。
        // rightも同様にright(x軸)が(1,0,0)の時は右方向、(-1,0,0)の時は左方向を向いていることを示す。
        var forward = cameraTransform.forward;   // z軸  R*(0,0,1)
        var right = cameraTransform.right;      // x軸   R*(1,0,0)

        // 上下方向の成分を除去 <- forwardやrightにはY方向の成分が混ざっているためなくす。
        forward.y = 0f;
        right.y = 0f;

        if (_moveInputValue == Vector2.zero) return;
      //  Vector3 _movement = new Vector3(_moveInputValue.x,0, _moveInputValue.y); //vector2で入力された値をvector3に変換する
        // なぜforward*_moveInputValue + right*_moveInputValue.xをしているのか
        // forward,rightはカメラがどの方向をみているかのデータ入っている。
        // _moveInputValueはどのぐらいスティックを倒したかのデータが入っている。
        // _moveInputValueもforwardと同様に1~-1のデータが入っている
        // もし、コントローラの移動スティックを思いっきり上に倒したら_moveInputValue.yに1のデータが、
        // 下に倒したら_moveInputValue.yに-1のデータが入る。
        // _moveInputValue.yが1の時は前方向に、-1の時は後ろ方向を示す。
        // それがカメラと入力された情報をかけ合わせると方向がわかる。
        // またその前後方向と左右方向を足すことで斜め移動なのかもわかる
        _movement = (forward * _moveInputValue.y +right * _moveInputValue.x).normalized;



        transform.Translate(_movement * _moveSpeed, Space.World);// Translateは今ある場所から移動量分だけ動かす

        Quaternion targetRotation = Quaternion.LookRotation(_movement, Vector3.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSharpness * Time.deltaTime);
    }

}
