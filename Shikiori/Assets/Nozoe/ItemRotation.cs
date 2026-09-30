using UnityEngine;

public class ItemRotation : MonoBehaviour
{
    // 1秒間に何度回るか
    [SerializeField] private float m_rotationSpeed = 90.0f;

    // 上下に動く幅
    [SerializeField] private float m_bobHeight = 0.25f;

    // 1秒間に何往復するか
    [SerializeField] private float m_bobFrequency = 0.5f;

    // bob前の初期位置を記憶する箱
    private Vector3 m_startLocalPosition;

    // ゲーム開始時に一度だけ呼ばれる
    private void Awkae()
    {
        // 最初の位置を覚えておく(中心位置)
        m_startLocalPosition = transform.localPosition;
    }


    // Update is called once per frame
    private void Update()
    {
        // 回転
        transform.Rotate(Vector3.up, m_rotationSpeed * Time.deltaTime);

        // 上下運動
        float wave = Time.time * m_bobFrequency * Mathf.PI * 2.0f;


        float offsetY = Mathf.Sin(wave) * m_bobHeight;
        // このフレームで回す角度
       // float angle = m_rotationSpeed * Time.deltaTime;

        // Vector3.upを軸にして、angle度だけ回す
        //transform.Rotate(Vector3.up, angle);
    }
}
