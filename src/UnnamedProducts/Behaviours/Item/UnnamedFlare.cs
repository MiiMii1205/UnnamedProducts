using System.Collections.Generic;
using Random = UnityEngine.Random;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedFlare: Flare
{
    public bool isDud;

    public bool IsADud
    {
        get
        {
            isDud = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultDud).Value;
            return isDud;
        }

    }

    private void Start()
    {

        isDud = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultDud).Value;
        // Don't make it explode when its cooked
        if (isDud)
        {
            var cook = item.GetComponent<ItemCooking>();

            var newAdditionalCookingBehaviors = new List<AdditionalCookingBehavior>();

            foreach (var cookAdditionalCookingBehavior in cook.additionalCookingBehaviors)
            {
                if (cookAdditionalCookingBehavior is not CookingBehavior_Explode)
                {
                    newAdditionalCookingBehaviors.Add(cookAdditionalCookingBehavior);
                }
            }

            cook.additionalCookingBehaviors = newAdditionalCookingBehaviors.ToArray();

            cook.explosionPrefab = null;
        }
    }

    public override void OnInstanceDataSet()
    {
        isDud = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultDud).Value;

        // Don't make it explode when its cooked
        if (isDud)
        {
            var cook = item.GetComponent<ItemCooking>();

            var newAdditionalCookingBehaviors = new List<AdditionalCookingBehavior>();

            foreach (var cookAdditionalCookingBehavior in cook.additionalCookingBehaviors)
            {
                if (cookAdditionalCookingBehavior is not CookingBehavior_Explode)
                {
                    newAdditionalCookingBehaviors.Add(cookAdditionalCookingBehavior);
                }
            }

            cook.additionalCookingBehaviors = newAdditionalCookingBehaviors.ToArray();

            cook.explosionPrefab = null;
        }

        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedFlare)} {item} is {(isDud ? "a dud" : "not a dud")}");
        base.OnInstanceDataSet();
    }

    public new void LightFlare()
    {
        if (!isDud)
        {
            base.LightFlare();
        }
    }
    
    private BoolItemData SetupDefaultDud()
    {
        return new BoolItemData
        {
            Value = !UnnamedPlugin.IsUnnamedLucky(1/3f),
        };
    }
}