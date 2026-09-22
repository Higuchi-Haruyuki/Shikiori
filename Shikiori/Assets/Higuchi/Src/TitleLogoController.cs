using UnityEngine;

public class TitleLogoController : MonoBehaviour
{
    [SerializeField] private FadeScreen _fadeScreen;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _fadeScreen.StartFade(0.0f,1.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
