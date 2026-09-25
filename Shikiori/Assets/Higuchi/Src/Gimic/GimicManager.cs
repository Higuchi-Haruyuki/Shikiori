using UnityEngine;
using System.Collections.Generic;

public class GimicManager : MonoBehaviour
{
    [SerializeField] private string _gimicTag = "Gimic";
    [SerializeField] private string _seasonManagerTag = "SeasonManager";
    private SeasonManager _seasonManager;

    void SubcribeToSeasonChange()
    {

        var gimicObjects = GameObject.FindGameObjectsWithTag(_gimicTag);

        // シーン上のすべてのギミックに対して、季節変更イベントに登録する
        foreach (var gimicObject in gimicObjects)
        {
            var gimic = gimicObject.GetComponent<IGimic>();
            if (gimic == null) continue;
            
            // 季節変更イベントに登録する
            _seasonManager.SubscribeToSeasonChange(gimic.OnSeasonChanged);   
        }
    }

    void Awake()
    {
        if (_seasonManager == null)
        {
            _seasonManager = GameObject.FindGameObjectWithTag(_seasonManagerTag).GetComponent<SeasonManager>();
        }
        SubcribeToSeasonChange();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
