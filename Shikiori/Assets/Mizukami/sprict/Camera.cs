using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.VisualScripting.AnnotationUtility;

public class Camera : MonoBehaviour
{
    GameObject player;
    private PlayerInput _playerInput;   // プレイヤーインプット型(入力のイベントとかがくる)
    private Vector2 _rotationInputValue; // マウスクリックやマウスパッド(右スティック)が入力されたときに各、x,yの値の大きさを受け取る変数
    Vector3 CameraPos;
    private void Awake()
    {
        _playerInput = new PlayerInput();
        // 回転Aciton
        _playerInput.Camera.Rotation.started += OnRotation; // 押され始めたら呼び出されるイベントに登録する
        _playerInput.Camera.Rotation.performed += OnRotation;   // 押されているときに呼び出されるイベントに登録する
        _playerInput.Camera.Rotation.canceled += OnRotation;    // 話されたときに呼び出されるイベントに登録する

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }
    private void Update()
    {
        RotationCamera();
    }
    // Update is called once per frame
    private void LateUpdate()   // Updateの後にされるUpdate。
    {

        Vector3 PlayerPos = player.transform.position;
        CameraPos = new(PlayerPos.x, PlayerPos.y + 8, PlayerPos.z - 10);
        transform.position = CameraPos;
    }
    public void OnRotation(InputAction.CallbackContext context)
    {
        _rotationInputValue = context.ReadValue<Vector2>();
    }
    private void RotationCamera()
    {
        Vector3 rotation = new Vector3(_rotationInputValue.x, 0, _rotationInputValue.y);
        transform.RotateAround(CameraPos, rotation, 0.5f);
    }
}
