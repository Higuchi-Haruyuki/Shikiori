using UnityEngine;
using Unity.Cinemachine;

public class TitleSceneCameraController : MonoBehaviour
{
    [SerializeField] private CinemachineOrbitalFollow _virtualCamera;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _turnSpeed = 5f;
    [SerializeField] private float _pitchDegrees = 30f;

    private Vector3 _beforeFramePosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _virtualCamera = GetComponent<CinemachineOrbitalFollow>();
        _virtualCamera.enabled = true;
        _beforeFramePosition = transform.position;

        // 開始時に進む方向を向かせておく
        var center = _virtualCamera.FollowTarget;
        if(!center) return;
        var toCamera = transform.position - center.position;
        toCamera.y = 0;

        // 上方向との外戚で、円軌道の接線が求まる。
        var tangent = Vector3.Cross(Vector3.up, toCamera).normalized * Mathf.Sign(_rotationSpeed);

        // カメラを接線方向に回転させる
        transform.rotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(_pitchDegrees, 0, 0);
    }

    void FixedUpdate()
    {
        if(!_virtualCamera) return;
        _virtualCamera.HorizontalAxis.Value += Time.deltaTime * _rotationSpeed;

        // 移動方向に応じてカメラの向きを変える
        // 移動方向を求める
        var moveDirection = transform.position - _beforeFramePosition;
        _beforeFramePosition = transform.position;
        // 移動方向のy成分は無視する。
        moveDirection.y = 0;
        if(moveDirection.sqrMagnitude < 0.0001f) return;
        moveDirection.Normalize();

        // 指定したベクトルを向く回転量をクォータニオンでを求める。
        var lookRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        var targetRotation = lookRotation * Quaternion.Euler(_pitchDegrees, 0, 0);
        // 現在の回転量から目標の回転量までを補間する。
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * _turnSpeed);
    }

    // Update is called once per frame
    void Update()
    {


    }
}
