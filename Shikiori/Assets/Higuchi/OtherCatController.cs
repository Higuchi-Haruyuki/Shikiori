using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class OtherCatController : MonoBehaviour
{
    [SerializeField] private GameObject _player;
    [SerializeField] private float _lookAtDistance = 5f;
    [SerializeField] private AudioClip[] _audioClips;
    [SerializeField] private float _audioPlayInterval = 5f;
    private float _audioTimer = 0f;
    
    private AudioSource _audioSource;
    Vector3 _initialRotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _initialRotation = transform.eulerAngles;
        _audioSource = GetComponent<AudioSource>();
        SetRandomAudioClip();
    
        _audioTimer = _audioPlayInterval;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsPlayerInLookAtDistance())
        {
            Vector3 directionToPlayer = _player.transform.position - transform.position;
            directionToPlayer.y = 0; // 水平方向のみに制限
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);
            Vector3 eulerAngles = targetRotation.eulerAngles;
            eulerAngles.x = _initialRotation.x; // X軸の回転を制限
            eulerAngles.z = _initialRotation.z; // Z軸の回転を制限
            targetRotation = Quaternion.Euler(eulerAngles);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);

            _audioTimer += Time.deltaTime;
            if (_audioTimer >= _audioPlayInterval)
            {
                _audioTimer = 0f;
                PlayAudioClip();
            }
        }
        else
        {
            // プレイヤーが一定距離以上離れた場合、元の向きに戻す
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(_initialRotation), Time.deltaTime * 2f);
            _audioTimer = _audioPlayInterval; // タイマーをリセット。
        }
    }

    void SetRandomAudioClip()
    {
        if (_audioClips.Length > 0)
        {
            int randomIndex = Random.Range(0, _audioClips.Length);
            _audioSource.clip = _audioClips[randomIndex];
        }
    }

    void PlayAudioClip()
    {
        if(_audioSource.isPlaying) return;
        if (_audioSource.clip != null)
        {
            SetRandomAudioClip(); // 再生する前にランダムなクリップを設定
            _audioSource.Play();
        }
    }

    bool IsPlayerInLookAtDistance()
    {
        if (_player == null) return false;
        if(Mathf.Abs(_player.transform.position.y - transform.position.y) > 3f) return false; // 高さの差が大きい場合は無視

        float distance = Vector3.Distance(transform.position, _player.transform.position);
        return distance <= _lookAtDistance;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _lookAtDistance);
    }
}
