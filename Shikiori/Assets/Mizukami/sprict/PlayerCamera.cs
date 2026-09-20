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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class PlayerCamera : MonoBehaviour
{

    private GameObject mainCamera;              //メインカメラ格納用
    private GameObject playerObject;            //回転の中心となるプレイヤー格納用
    public float rotateSpeed = 2.0f;            //回転の速さ

    //呼び出し時に実行される関数
    void Start()
    {
        //メインカメラとユニティちゃんをそれぞれ取得
        mainCamera = UnityEngine.Camera.main.gameObject;
        playerObject = GameObject.Find("unitychan");
    }


    //単位時間ごとに実行される関数
    void Update()
    {
        //rotateCameraの呼び出し
        rotateCamera();
    }

    //カメラを回転させる関数
    private void rotateCamera()
    {
        //Vector3でX,Y方向の回転の度合いを定義
        Vector3 angle = new Vector3(Input.GetAxis("Mouse X") * rotateSpeed, Input.GetAxis("Mouse Y") * rotateSpeed, 0);

        //transform.RotateAround()をしようしてメインカメラを回転させる
        mainCamera.transform.RotateAround(playerObject.transform.position, Vector3.up, angle.x);
        mainCamera.transform.RotateAround(playerObject.transform.position, transform.right, angle.y);
    }
}