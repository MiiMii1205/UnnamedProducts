using Photon.Pun;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedRopeShooter : RopeShooter
{
    public float baseShooterLength;
    public float baseReach;

    public override void OnInstanceDataSet()
    {
        maxLength = baseReach * UnnamedPlugin.RandomUnnamedModifier;
        length = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, GetNewLength).Value;
        base.OnInstanceDataSet();
        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedRopeShooter)} {item.name} will shoot a rope {length} m of length");
        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedRopeShooter)} {item.name} currently has a reach of {maxLength} m");
    }

    [PunRPC]
    public new void Sync_Rpc(bool show)
    {
        base.Sync_Rpc(show);
    }

    private FloatItemData GetNewLength()
    {
        return new FloatItemData
        {
            Value = baseShooterLength * UnnamedPlugin.RandomUnnamedModifier,
        };
    }
}