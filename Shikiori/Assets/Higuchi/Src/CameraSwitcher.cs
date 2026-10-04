using Unity.Cinemachine;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // 追従カメラ
    [SerializeField] private CinemachineCamera _followCamera;

    // アイテム取得カメラ
    [SerializeField] private CinemachineCamera _itemGetCamera;

    // 季節変更カメラ
    [SerializeField] private CinemachineCamera _seasonChangeCamera;

    private const int ACTIVE_CAMERA_PRIORITY = 10; // アクティブなカメラの優先度
    private const int INACTIVE_CAMERA_PRIORITY = 0; // 非アクティブなカメラの優先度

    public void SwitchToFollowCamera()
    {
        _followCamera.Priority = ACTIVE_CAMERA_PRIORITY;
        _itemGetCamera.Priority = INACTIVE_CAMERA_PRIORITY;
        _seasonChangeCamera.Priority = INACTIVE_CAMERA_PRIORITY;
    }

    public void SwitchToItemGetCamera()
    {
        _followCamera.Priority = INACTIVE_CAMERA_PRIORITY;
        _itemGetCamera.Priority = ACTIVE_CAMERA_PRIORITY;
        _seasonChangeCamera.Priority = INACTIVE_CAMERA_PRIORITY;
    }

    public void SwitchToSeasonChangeCamera()
    {
        _followCamera.Priority = INACTIVE_CAMERA_PRIORITY;
        _seasonChangeCamera.Priority = ACTIVE_CAMERA_PRIORITY;
        _itemGetCamera.Priority = INACTIVE_CAMERA_PRIORITY;
    }
}
