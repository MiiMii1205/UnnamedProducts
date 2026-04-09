using System;
using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item.GarbageBag;

public class UnnamedGarbageBagVisuals: MonoBehaviour
{
    public global::Item item;

    protected bool m_shuttingDown;
    
    private void Awake()
    {
        item ??= GetComponent<global::Item>();
        
        if (item.view.IsMine)
        {
            item.view.RPC(nameof(UnnamedGarbageBagController.RPC_SetGarbageBagFilled), RpcTarget.All, GetBackpackData().FilledSlotCount() <= 0);
        }
    }

    public BackpackData GetBackpackData()
    {
        return item.GetData<BackpackData>(DataEntryKey.BackpackData);
    }
    public void RefreshVisuals()
    {
        if (item.view.IsMine)
        {
            item.view.RPC(nameof(UnnamedGarbageBagController.RPC_SetGarbageBagFilled), RpcTarget.All, GetBackpackData().FilledSlotCount() <= 0);
        }
    }

    public void OnApplicationQuit() => this.m_shuttingDown = true;

}