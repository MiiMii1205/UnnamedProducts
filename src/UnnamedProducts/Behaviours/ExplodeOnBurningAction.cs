using Peak.Afflictions;

namespace UnnamedProducts.Behaviours;

public class ExplodeOnBurningAction: ItemAction
{
    public bool dontRunIfOutOfFuel;

    private static bool HoldsLitItem(Character c)
    {
        return c.data.currentItem &&
               c.data.currentItem.HasData(DataEntryKey.FlareActive) &&
               c.data.currentItem.GetData<BoolItemData>(DataEntryKey.FlareActive).Value;
    }
    
    private static bool HasHeatingElement(Character c)
    {
        return c.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.ColdOverTime, out var aff) &&
               aff.timeElapsed > 1f;
    }
    private static bool IsCurrentlyBurning(Character c)
    {
        return c.TryGetComponent(out CharacterBurnController burning) &&
               burning.m_hasFire;
    }
    
    public override void RunAction()
    {
        if ((HoldsLitItem(character) || HasHeatingElement(character) || IsCurrentlyBurning(character)) && 
            item.TryGetComponent< ItemCooking>(out var itemCooking) && 
            (!dontRunIfOutOfFuel || !(itemCooking.item.GetData<FloatItemData>(DataEntryKey.Fuel).Value <= 0f)))
        {
            itemCooking.Explode();
        }
    }
}