using System;
using UnnamedProducts.Extensions;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zorro.Core;

namespace UnnamedProducts.Behaviours.Item.GarbageBag.GUI;

public class UnnamedGarbageBagScreen : UIWheel
{
    public UnnamedGarbageBagZone[] garbageBagZones;

    public UnnamedGarbageBagZone pickupZone;

    public TextMeshProUGUI chosenItemText;
    public Optionable<UnnamedGarbageBagZone.ZoneData> chosenZone;

    public UnnamedGarbageBagReference garbageBag;

    public RawImage currentlyHeldItem;
    private int currentlyHeldItemCookedAmount;

    private void Awake()
    {
        chosenItemText.font = UnnamedPlugin.DarumaDropOne;
        chosenItemText.lineSpacing = -50f;
        chosenItemText.fontSharedMaterial =
            chosenItemText.fontMaterial = chosenItemText.material = UnnamedPlugin.DarumaDropOneShadowMaterial;
    }


    public void InitWheel(UnnamedGarbageBagReference gb)
    {
        garbageBag = gb;
        chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.None;
        chosenItemText.text = "";

        var itemSlots = garbageBag.GetData().itemSlots;
        var slotsLength = Mathf.Min(itemSlots.Length, 2);
        for (byte b = 0; b < slotsLength; b++)
        {
            garbageBagZones[b].InitItemSlot((gb, slotID: b), this);
        }

        base.gameObject.SetActive(value: true);

        pickupZone.InitPickupGarbageBag(gb, this);

        if (Character.localCharacter.data.currentItem != null &&
            Character.localCharacter.data.currentItem.UIData.canBackpack)
        {
            currentlyHeldItem.texture = Character.localCharacter.data.currentItem.UIData.GetIcon();
            UpdateCookedAmount(Character.localCharacter.data.currentItem);
            currentlyHeldItem.enabled = true;
        }
        else
        {
            UpdateCookedAmount(null);
            currentlyHeldItem.enabled = false;
        }
    }


    private void UpdateCookedAmount(global::Item item)
    {
        if (item == null || item.data == null)
        {
            currentlyHeldItemCookedAmount = 0;
            currentlyHeldItem.color = Color.white;
        }
        else if (item.data.TryGetDataEntry(DataEntryKey.CookedAmount, out IntItemData value) &&
                 currentlyHeldItemCookedAmount != value.Value)
        {
            currentlyHeldItem.color = Color.white;
            currentlyHeldItem.color = ItemCooking.GetCookColor(value.Value);
            currentlyHeldItemCookedAmount = value.Value;
        }
    }


    public override void Update()
    {
        if (!Character.localCharacter.input.interactIsPressed)
        {
            Choose();
            UnnamedUIManager.Instance.CloseGarbageBagScreen();
            return;
        }

        if ( garbageBag.view == null || !garbageBag.TryGetGarbageBagItem(out _))
        {
            UnnamedPlugin.Log.LogInfo("Garbage Bag got removed. Closing UI...");
            UnnamedUIManager.Instance.CloseGarbageBagScreen();
            return;
        }

        if (garbageBag.locationTransform != null &&
            garbageBag.locationTransform.position.SquareDistance(Character.localCharacter.Center) > (6f * 6f))
        {
            UnnamedPlugin.Log.LogInfo("Garbage Bag got too far away. Closing UI...");
            UnnamedUIManager.Instance.CloseGarbageBagScreen();
            return;
        }

        if (chosenZone is {IsSome: true, Value.isGarbageBagPickup: false} &&
            !garbageBagZones[chosenZone.Value.slotID].image.enabled)
        {
            currentlyHeldItem.transform.position = Vector3.Lerp(currentlyHeldItem.transform.position,
                garbageBagZones[chosenZone.Value.slotID].transform.GetChild(0).GetChild(0).position,
                Time.deltaTime * 20f);
        }
        else
        {
            currentlyHeldItem.transform.localPosition = Vector3.Lerp(currentlyHeldItem.transform.localPosition,
                Vector3.zero, Time.deltaTime * 20f);
        }

        base.Update();
    }


    public void Choose()
    {
        if (!chosenZone.IsSome)
        {
            return;
        }

        UnnamedPlugin.Log.LogDebug($"Chose zone {chosenZone.Value.slotID}");

        if (chosenZone.Value.isGarbageBagPickup)
        {
            if (chosenZone.Value.garbageBagReference.TryGetGarbageBagItem(out var pg) && pg.itemState != ItemState.Held)
            {
                pg.PickUpGarbageBag(Character.localCharacter);
            }
        }
        else if (chosenZone.Value.isStashZone)
        {
            TryStash(chosenZone.Value.slotID);
        }
        else if (chosenZone.Value.garbageBagReference.GetData().itemSlots[chosenZone.Value.slotID] is var slot &&
                 !slot.IsEmpty() && chosenZone.Value.garbageBagReference.TryGetGarbageBagItem(out var pg) &&
                 pg.itemState != ItemState.Held)
        {
            pg.photonView.RPC(nameof(UnnamedGarbageBagController.RPC_TakeAndGrabItem), RpcTarget.All, slot.itemSlotID,
                Character.localCharacter.photonView);
        }
        else if (Character.localCharacter.data.currentItem)
        {
            TryStash(chosenZone.Value.slotID);
        }
    }

    private void TryStash(byte backpackSlotID)
    {
        if (this.garbageBag.TryGetGarbageBagItem(out var bag))
        {
            bag.Stash(Character.localCharacter, backpackSlotID);
        }
    }


    public void Hover(UnnamedGarbageBagZone.ZoneData zoneData)
    {
        if (zoneData.isGarbageBagPickup)
        {
            if (zoneData.garbageBagReference.TryGetGarbageBagItem(out var gb) && gb.itemState != ItemState.Held)
            {
                chosenItemText.text = LocalizedText.GetText("CARRY").Replace("#", gb.GetItemName());
                chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.Some(zoneData);
            }
            else
            {
                chosenItemText.text = "";
                chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.None;
            }

            return;
        }

        if (zoneData.isStashZone)
        {
            var currentItem = Character.localCharacter.data.currentItem;

            if (currentItem != null)
            {
                chosenItemText.text = LocalizedText.GetText("STASHITEM").Replace("#", currentItem.GetItemName());
                chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.Some(zoneData);
            }
            else
            {
                chosenItemText.text = "";
                chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.None;
            }

            return;
        }

        ItemSlot itemSlot = garbageBag.GetData().itemSlots[zoneData.slotID];

        bool flag = false;

        if (itemSlot.IsEmpty() && Character.localCharacter.data.currentItem)
        {
            if (Character.localCharacter.data.currentItem)
            {
                chosenItemText.text = LocalizedText.GetText("STASHITEM")
                    .Replace("#", Character.localCharacter.data.currentItem.GetItemName());
                flag = true;
            }
        }
        else
        {
            global::Item prefab = itemSlot.prefab;
            if (prefab != null)
            {
                chosenItemText.text = LocalizedText.GetText("TAKEITEM").Replace("#", prefab.GetItemName(itemSlot.data));
                flag = true;
            }
        }

        if (flag)
        {
            chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.Some(zoneData);
        }
    }

    public void Dehover(UnnamedGarbageBagZone.ZoneData zoneData)
    {
        if (chosenZone.IsSome && chosenZone.Value.Equals(zoneData))
        {
            chosenItemText.text = "";
            chosenZone = Optionable<UnnamedGarbageBagZone.ZoneData>.None;
        }
    }


    public override void TestSelectSliceGamepad(Vector2 gamepadVector)
    {
        float num = 0f;
        UnnamedGarbageBagZone garbageBagZone = null;

        if (!(gamepadVector.sqrMagnitude < 0.5f))
        {
            float num2 = Vector3.Angle(gamepadVector, pickupZone.GetUpVector());

            if (garbageBagZone == null || num2 < num)
            {
                garbageBagZone = pickupZone;
                num = num2;
            }

            num2 = Vector3.Angle(gamepadVector, pickupZone.GetUpVector());

            if (garbageBagZone == null || num2 < num)
            {
                garbageBagZone = pickupZone;
                num = num2;
            }
        }

        if (garbageBagZone != null)
        {
            EventSystem.current.SetSelectedGameObject(garbageBagZone.button.gameObject);
            garbageBagZone.Hover();
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
            Dehover(chosenZone.Value);
        }
    }
}