namespace UnnamedProducts.Behaviours.Item;

public class UnnamedRopeSpool: RopeSpool
{
    public float baseStartFuel;
    
    public override void OnInstanceDataSet()
    {
        ropeStartFuel = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, GetNew).Value;
        base.OnInstanceDataSet();
        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedRopeSpool)} {item.name} has {ropeStartFuel} fuel instead of {baseStartFuel}.");
    }
    private FloatItemData GetNew()
    {
        return new FloatItemData
        {
            Value = baseStartFuel * UnnamedPlugin.RandomUnnamedModifier,
        };
    }
}