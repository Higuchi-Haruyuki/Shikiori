using UnityEngine;

public class TItleUIController : MonoBehaviour
{

    [SerializeField] private TitleScenePlayerInput _titleScenePlayerInput;

    [SerializeField] private GameObject[] _choicePanel; // 選択肢のパネル
    [SerializeField] private UnityEngine.UI.Image[] _choiceImages; // 選択肢の画像を格納する配列
   
    [SerializeField] private float _choiceUpScale = 1.1f; // 選択肢が選択されているときの拡大率
    void Start()
    {
        ActiveChoiceImage(_titleScenePlayerInput.ChoiceIndex);
    }

    // Update is called once per frame
    void Update()
    {
        ActiveChoiceImage(_titleScenePlayerInput.ChoiceIndex);
    }

    void ActiveChoiceImage(int index)
    {
        for(int i = 0; i < _choiceImages.Length; i++)
        {
            _choicePanel[i].transform.localScale = (i == index) ? Vector3.one * _choiceUpScale : Vector3.one;
            _choiceImages[i].enabled = (i == index);
        }
    }
}
