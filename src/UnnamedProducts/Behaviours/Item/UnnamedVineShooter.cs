using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedVineShooter: VineShooter
{
    public float baseReach;
    public override void OnInstanceDataSet()
    {
        maxLength = baseReach * UnnamedPlugin.RandomUnnamedModifier;
        base.OnInstanceDataSet();
        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedVineShooter)} {item.name} currently has a reach of {maxLength} m");
    }

}