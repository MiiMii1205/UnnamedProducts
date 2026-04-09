using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnnamedProducts.Behaviours.Item.GarbageBag.GUI;

public class UnnamedGarbageBagZone : UIWheelSlice, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{

    public RawImage image;

    private int cookedAmount;
    private bool hasItem;
    private bool isDepositZone;

    private BackpackData garbageBagData;
    private ItemSlot itemSlot;
    private UnnamedGarbageBagReference garbageBag;
    private UnnamedGarbageBagScreen garbageBagScreen;
    public byte garbageBagSlot { get; private set; }


    private bool canInteract
    {
        get
        {
            if (isGarbageBagPickup)
            {
                return true;
            }

            if (isDepositZone)
            {
                return true;
            }

            if (!hasItem)
            {
                if (Character.localCharacter.data.currentItem != null)
                {
                    return Character.localCharacter.data.currentItem.UIData.canBackpack;
                }

                return false;
            }

            return true;
        }
    }

    public bool isGarbageBagPickup => garbageBagSlot == byte.MaxValue;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Hover();
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        Dehover();
    }

    private void UpdateInteractable()
    {
        button.interactable = canInteract;
    }

    private void SharedInit(UnnamedGarbageBagReference gbRef, UnnamedGarbageBagScreen screen)
    {
        garbageBag = gbRef;
        garbageBagScreen = screen;

        if (true)
        {
            if (garbageBagSlot == byte.MaxValue)
            {
                base.gameObject.SetActive(value: true);
            }

            SetExplicitIcon(UnnamedPlugin.GarbageBagIcon);
        }
    }

    public void InitItemSlot((UnnamedGarbageBagReference, byte slotID) slot, UnnamedGarbageBagScreen wheel)
    {
        SharedInit(slot.Item1, wheel);
        garbageBagSlot = slot.slotID;
        garbageBagData = garbageBag.GetData();
        itemSlot = garbageBagData.itemSlots[garbageBagSlot];
        SetItemIcon(itemSlot.prefab, itemSlot.data);
        UpdateInteractable();
    }

    private void UpdateCookedAmount(global::Item? item, ItemInstanceData? itemInstanceData)
    {
        IntItemData value;

        if (item == null || itemInstanceData == null)
        {
            cookedAmount = 0;
            if (image != null)
            {
                image.color = Color.white;
            }
        }
        else if (itemInstanceData.TryGetDataEntry(DataEntryKey.CookedAmount, out value) &&
                 cookedAmount != value.Value)
        {
            if (image != null)
            {
                image.color = Color.white;
                image.color = ItemCooking.GetCookColor(value.Value);
            }

            cookedAmount = value.Value;
        }
    }


    public void Hover()
    {
        if (canInteract)
        {
            ZoneData zoneData = default(ZoneData);
            zoneData.isStashZone = isDepositZone;
            zoneData.isGarbageBagPickup = isGarbageBagPickup;
            zoneData.garbageBagReference = garbageBag;
            zoneData.slotID = garbageBagSlot;
            ZoneData zoneData2 = zoneData;
            garbageBagScreen.Hover(zoneData2);
        }
    }


    public void Dehover()
    {
        ZoneData zoneData = default(ZoneData);
        zoneData.isStashZone = isDepositZone;
        zoneData.isGarbageBagPickup = garbageBagSlot == byte.MaxValue;
        zoneData.garbageBagReference = garbageBag;
        zoneData.slotID = garbageBagSlot;
        ZoneData zoneData2 = zoneData;
        garbageBagScreen.Dehover(zoneData2);
    }


    public void InitPickupGarbageBag(UnnamedGarbageBagReference gb, UnnamedGarbageBagScreen gbs)
    {
        garbageBagSlot = byte.MaxValue;
        SharedInit(gb, gbs);
        UpdateInteractable();
    }
    
    private void SetItemIcon(global::Item iconHolder, ItemInstanceData itemInstanceData)
    {
        this.SetExplicitIcon(iconHolder?.UIData?.GetIcon());
        UpdateCookedAmount(iconHolder, itemInstanceData);
    }

    private void SetExplicitIcon(Texture2D? icon)
    {
        if (icon == null)
        {
            if (image != null)
            {
                image.enabled = false;
            }

            hasItem = false;
        }
        else
        {
            if (image != null)
            {
                image.enabled = true;
                image.texture = icon;
            }

            hasItem = true;
        }
 
    }


    public struct ZoneData : IEquatable<ZoneData>
    {
        public bool isStashZone;
        public bool isGarbageBagPickup;

        public UnnamedGarbageBagReference garbageBagReference;

        public byte slotID;

        public bool Equals(ZoneData other)
        {
            if (isGarbageBagPickup == other.isGarbageBagPickup)
            {
                return slotID == other.slotID;
            }

            return false;
        }

        public override bool Equals(object obj)
        {
            if (obj is ZoneData other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(isGarbageBagPickup, garbageBagReference, slotID);
        }
    }
}