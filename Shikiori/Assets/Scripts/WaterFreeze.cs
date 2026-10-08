using UnityEngine;

public class WaterFreeze : MonoBehaviour
{
    // 凍っている割合 (0 = すべて水、1 = すべて凍る)
    [SerializeField] private float freezeProgress = 0.0f;

    // 流れた累積時間
    [SerializeField] private float flowTime = 0.0f;

    // シェーダーのプロパティを指す番号
    private static readonly int FlowTimeId = Shader.PropertyToID("_flowTime_");
    private static readonly int FreezeProgressId = Shader.PropertyToID("_freezeProgress_");

    // Update is called once per frame
    void Update()
    {
        flowTime += Time.deltaTime * (1.0f - freezeProgress);
        
        /* 
        Shader の Global プロパティに値を渡す
        Shader.SetGlobalFloat(プロパティを指す番号, 渡す値);
        */
        // flowTime をシェーダーに渡す
        Shader.SetGlobalFloat(FlowTimeId, flowTime);
        // freezeProgress をシェーダーに渡す
        Shader.SetGlobalFloat(FreezeProgressId, freezeProgress);
    }

    /// <summary>
    /// 水面を凍らせる
    /// </summary>
    /// <param name="iceRate">凍る割合(0.0f ~ 1.0f)</param>
    public void SetFreeze(float iceRate)
    {
        if (iceRate < 0.0f) {iceRate = 0.0f;}
        if (iceRate > 1.0f) {iceRate = 1.0f;}
        freezeProgress = iceRate;
    }
}
