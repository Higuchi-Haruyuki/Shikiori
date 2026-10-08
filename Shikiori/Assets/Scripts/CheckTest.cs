using System.Runtime.CompilerServices;
using UnityEngine;

public class CheckTest : MonoBehaviour
{
    [SerializeField] private float iceRate = 0.0f;

    private WaterFreeze waterFreeze;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waterFreeze = GetComponent<WaterFreeze>(); 
    }

    // Update is called once per frame
    void Update()
    {
        waterFreeze.SetFreeze(iceRate);
    }
}
