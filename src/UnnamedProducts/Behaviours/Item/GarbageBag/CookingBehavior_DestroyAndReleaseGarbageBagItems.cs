using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item.GarbageBag;

public class CookingBehavior_DestroyAndReleaseGarbageBagItems : CookingBehavior_Explode
{
    public override void TriggerBehaviour(int cookedAmount)
    {
        if (itemCooking.item is UnnamedGarbageBagController nngbc)
        {
            var bagData = nngbc.GetData<BackpackData>(DataEntryKey.BackpackData);

            var hasSlots = false;
            foreach (var slot in bagData.itemSlots)
            {
                if (!slot.IsEmpty())
                {
                    hasSlots = true;
                    nngbc.view.RPC(nameof(UnnamedGarbageBagController.RPC_TakeItemOut),  RpcTarget.MasterClient, slot.itemSlotID);
                }
            }

            if (hasSlots)
            {
                nngbc.view.RPC(nameof(UnnamedGarbageBagController.RPC_ItemSlipped), RpcTarget.All);
            }
            
        }

        base.TriggerBehaviour(cookedAmount);
    }
}