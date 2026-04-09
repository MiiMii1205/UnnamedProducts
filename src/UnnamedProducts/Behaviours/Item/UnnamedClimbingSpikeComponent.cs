using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedClimbingSpikeComponent: ClimbingSpikeComponent
{
    public GameObject hammeredGoodVersionPrefab = null!;
    public GameObject hammeredBadVersionPrefab = null!;

    public bool isBad;

    public override void OnInstanceDataSet()
    {        
        base.OnInstanceDataSet();
        isBad = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, DefaultBadValue).Value;
        hammeredVersionPrefab = isBad ? hammeredBadVersionPrefab : hammeredGoodVersionPrefab;
        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedClimbingSpikeComponent)} {item.name} is {(isBad ? "bad" : "good")}");
    }

    private static BoolItemData DefaultBadValue()
    {
        return new BoolItemData
        {
            Value = (1.0f * Random.Range(1.0f -UnnamedPlugin.UnnamedModifier, 1.0f +UnnamedPlugin.UnnamedModifier) ) >= 1.0f,
        };
    }
}