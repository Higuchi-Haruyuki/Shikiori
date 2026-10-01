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

        //---上下運動---
        // 1秒間に何往復するかから、1秒間に何ラジアン進むかを計算する
        float wave = Time.time * m_bobFrequency * Mathf.PI * 2.0f;

        // -0.25~0.25の範囲で上下運動するようにする
        float offsetY = Mathf.Sin(wave) * m_bobHeight;
        
        // 元の位置に上下のずれを足した場所に置く
        transform.localPosition = m_startLocalPosition + new Vector3(0.0f, offsetY, 0.0f);
    }
}
