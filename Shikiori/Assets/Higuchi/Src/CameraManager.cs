using UnityEngine;
using Game.Attribute;

/// <summary>
/// カメラの切り替え管理を行うクラス。
/// </summary>
public class CameraManager : MonoBehaviour
{
    [SerializeField] private SeasonManager _seasonManager; // 季節管理クラスの参照
    [SerializeField] private CameraSwitcher _cameraSwitcher; // カメラスイッチャーの参照

    [SerializeField] private float _itemGetCameraDuration = 3.0f; // アイテム取得カメラの表示時間

    [SerializeField, ReadOnly] private bool _isItemGetCameraActive = false; // アイテム取得カメラがアクティブかどうかのフラグ
    [SerializeField, ReadOnly] private float _itemGetCameraTimer = 0.0f; // アイテム取得カメラのタイマー

    [SerializeField] private float _seasonChangeCameraDuration = 3.0f; // 季節変更カメラの表示時間

    [SerializeField, ReadOnly] private bool _isSeasonChangeCameraActive = false; // 季節変更カメラがアクティブかどうかのフラグ
    [SerializeField, ReadOnly] private float _seasonChangeCameraTimer = 0.0f; // 季節変更カメラのタイマー

    void Start()
    {
        // アイテム変更イベントの購読
        ItemCountMover.OnItemCountChanged += HandleItemCountChanged;

        // アイテムクリア条件達成イベントの購読
        ItemCountMover.OnReachedItemClearCount += HandleItemClearCountReached;

        if(_seasonManager)
            _seasonManager.SubscribeToSeasonChange((oldSeason, newSeason) =>
            {
                if(oldSeason == GlobalSeason.None) return; // 初期状態のNoneからの遷移は無視する

                // 季節が変更されたときの処理
                _cameraSwitcher.SwitchToSeasonChangeCamera();
                _isSeasonChangeCameraActive = true;
                _seasonChangeCameraTimer = 0.0f;
            });

        // 最初は追従カメラ。
        _cameraSwitcher.SwitchToFollowCamera();
    }

    private void HandleItemCountChanged()
    {
        // アイテム取得カメラに切り替える
        _cameraSwitcher.SwitchToItemGetCamera();
        _isItemGetCameraActive = true;
        _itemGetCameraTimer = 0.0f;
    }

    private void HandleItemClearCountReached()
    {
        _cameraSwitcher.SwitchToItemGetCamera();
        _isItemGetCameraActive = true;
        _itemGetCameraTimer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if(_isItemGetCameraActive)
        {
            _itemGetCameraTimer += Time.deltaTime;
            if(_itemGetCameraTimer >= _itemGetCameraDuration)
            {
                // アイテム取得カメラの表示時間が経過したら、追従カメラに切り替える
                _cameraSwitcher.SwitchToFollowCamera();
                _isItemGetCameraActive = false;
                _itemGetCameraTimer = 0.0f;
            }
        }

        if(_isSeasonChangeCameraActive)
        {
            _seasonChangeCameraTimer += Time.deltaTime;
            if(_seasonChangeCameraTimer >= _seasonChangeCameraDuration)
            {
                // 季節変更カメラの表示時間が経過したら、追従カメラに切り替える
                _cameraSwitcher.SwitchToFollowCamera();
                _isSeasonChangeCameraActive = false;
                _seasonChangeCameraTimer = 0.0f;
            }
        }
    }
}
