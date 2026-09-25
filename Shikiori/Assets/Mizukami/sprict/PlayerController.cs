using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
 
    [SerializeField] private float _moveSpeed = 1f;    // プレイヤーの移動量
    [SerializeField] private float _rotationSpeed = 1f;

    
    private PlayerInput _playerInput;   // プレイヤーインプット型(入力のイベントとかがくる)
    private Vector2 _moveInputValue;  // キーやマウスパッド(左スティック)が入力されたときに各、x,yの値の大きさを受け取る変数

    // 次のUpdateで使うための前フレーム位置更新
    private Vector3 _prevPosition;

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
        if (_moveInputValue == Vector2.zero) return;
        Vector3 movement = new Vector3(_moveInputValue.x,0, _moveInputValue.y); //vector2で入力された値をvector3に変換する
        transform.Translate(movement * _moveSpeed, Space.World);// Translateは今ある場所から移動量分だけ動かす
        transform.rotation = Quaternion.LookRotation(movement, Vector3.up);
    }
}
