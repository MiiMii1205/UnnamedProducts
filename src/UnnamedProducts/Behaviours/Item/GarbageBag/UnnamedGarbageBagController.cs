using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Photon.Pun;
using Sirenix.Utilities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Zorro.Core;
using Zorro.Core.CLI;

namespace UnnamedProducts.Behaviours.Item.GarbageBag;

public class UnnamedGarbageBagController : global::Item
{
    public float openRadialMenuTime = 0.25f;
    private bool m_justSetActive = true;

    public UnnamedGarbageBagVisuals m_unnamedGarbageBagVisuals;

    public GameObject emptyBagGameObject;
    public GameObject fullBagGameObject;

    public float fullBagRadius;
    private bool m_isEmpty = true;

    public Vector3 rightHandEmptyPosition;
    public Vector3 leftHandEmptyPosition;
    public Vector3 rightHandFullPosition;
    public Vector3 leftHandFullPosition;

    public GameObject rightHandGameObject;
    public GameObject leftHandGameObject;

    private Character lastInteractor;

    private Renderer emptyBagRenderer;
    private Renderer fullBagRenderer;
    public Renderer[] allBagRenderers;

    public SFX_Instance[] slipSDSfxInstances;
    public Vector3 defaultPosFilled;
    public Vector3 defaultPosEmpty;

    public override void Awake()
    {
        base.Awake();

        emptyBagGameObject ??= transform.Find(@"GarbageBag/empty").gameObject;
        fullBagGameObject ??= transform.Find(@"GarbageBag/empty").gameObject;

        emptyBagRenderer = emptyBagGameObject.GetComponent<Renderer>();
        fullBagRenderer = fullBagGameObject.GetComponent<Renderer>();

        allBagRenderers = [emptyBagRenderer, fullBagRenderer];

        leftHandGameObject ??= transform.Find(@"Hand_L").gameObject;
        rightHandGameObject ??= transform.Find(@"Hand_R").gameObject;

        m_unnamedGarbageBagVisuals ??= GetComponent<UnnamedGarbageBagVisuals>();
        
        fullBagRadius = 0.599735f;

        this.OnStateChange += StateChanged;

        fullBagGameObject.SetActive(!m_isEmpty);
        emptyBagGameObject.SetActive(m_isEmpty);

        RefreshVisualReferences();
    }

    public override void AddPhysics()
    {
        base.AddPhysics();
        this.colliders = emptyBagGameObject.GetComponentsInChildren<Collider>(true).AddRangeToArray(fullBagGameObject
            .GetComponentsInChildren
                <Collider>(true));
    }

    private void RefreshVisualReferences()
    {
        if (m_isEmpty)
        {
            rightHandGameObject.transform.localPosition = rightHandEmptyPosition;
            leftHandGameObject.transform.localPosition = leftHandEmptyPosition;
            defaultPos = defaultPosEmpty;
        }
        else
        {
            rightHandGameObject.transform.localPosition = rightHandFullPosition;
            leftHandGameObject.transform.localPosition = leftHandFullPosition;
            defaultPos = defaultPosFilled;
        }
    }

    private void StateChanged(ItemState obj)
    {
        if (obj == ItemState.Held && m_justSetActive)
        {
            if (photonView.IsMine)
            {
                m_justSetActive = true;
                
                if (shouldSlip)
                {
                    photonView.RPC(nameof(RPC_DropRandomBaggedItem), RpcTarget.MasterClient);
                }
                
            }
            
        }
        
    }

    public bool holdOnFinish => false;

    public bool shouldSlip => !UnnamedPlugin.IsUnnamedLucky(0.25f) && FilledSlotCount() > 0;

    public override void Interact(Character interactor)
    {
        if (this.itemState == ItemState.InBackpack)
        {
            base.Interact(interactor);
        }
        else
        {
            if (interactor.input.interactIsPressed)
            {
                UnnamedUIManager.Instance.OpenGarbageBagScreen(UnnamedGarbageBagReference.GetFromBackpackItem(this));
            }
            else
            {
                base.Interact(interactor);
            }

            this.lastInteractor = interactor;
        }
    }

    public void ReleaseInteract(Character interactor)
    {
    }

    public void PickUpGarbageBag(Character interactor)
    {
        base.Interact(interactor);
    }

    private void DisableVisuals()
    {
        fullBagRenderer.shadowCastingMode = emptyBagRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    private void EnableVisuals()
    {
        fullBagRenderer.shadowCastingMode = emptyBagRenderer.shadowCastingMode = ShadowCastingMode.On;
    }

    public override string GetInteractionText()
    {
        return LocalizedText.GetText("open");
    }

    public bool IsConstantlyInteractable(Character interactor)
    {
        return false;
    }

    public float GetInteractTime(Character interactor)
    {
        return openRadialMenuTime;
    }

    public void Interact_CastFinished(Character interactor)
    {
    }

    public void CancelCast(Character interactor)
    {
    }

    [PunRPC]
    public void RPC_SetGarbageBagFilled(bool isEmpty)
    {
        if (m_isEmpty != isEmpty)
        {
            if (!isEmpty)
            {
                // Going from empty -> full
                if (itemState == ItemState.Ground && view.IsMine)
                {
                    // Move the garbage bag up a bit to account the collision differance
                    transform.position = transform.position + (-Physics.gravity.normalized * fullBagRadius);
                    rig.MovePosition(rig.position + (-Physics.gravity.normalized * fullBagRadius));
                }

                emptyBagGameObject.SetActive(false);
                fullBagGameObject.SetActive(true);
            }
            else
            {
                // Going form full -> empty
                fullBagGameObject.SetActive(false);
                emptyBagGameObject.SetActive(true);
            }

            m_isEmpty = isEmpty;
            RefreshVisualReferences();
        }
    }

    private bool HasSpace()
    {
        return GetData<BackpackData>(DataEntryKey.BackpackData).FilledSlotCount() < 2;
    }


    public int FilledSlotCount()
    {
        return GetData<BackpackData>(DataEntryKey.BackpackData).FilledSlotCount();
    }

    public void Stash(Character interactor, byte garbageBagSlotID)
    {
        if (!interactor.data.currentItem || !HasSpace())
        {
            return;
        }

        var inventory = interactor.refs.items;
        if (inventory.currentSelectedSlot.IsNone)
        {
            UnnamedPlugin.Log.LogError("Need item slot selected to stash item in backpack!");
            return;
        }

        var selectedItemSlot = interactor.player.GetItemSlot(inventory.currentSelectedSlot.Value);
        if (selectedItemSlot == null)
        {
            UnnamedPlugin.Log.LogError($"Failed to get a non-null item slot for {inventory.currentSelectedSlot.Value}");
            return;
        }

        if (selectedItemSlot.IsEmpty())
        {
            UnnamedPlugin.Log.LogError($"Item slot {selectedItemSlot.itemSlotID} is empty!");
            return;
        }

        view.RPC(nameof(RPCAddItemToGarbageBag), RpcTarget.All, interactor.player.GetComponent<PhotonView>(),
            inventory.currentSelectedSlot.Value, garbageBagSlotID);

        interactor.player.EmptySlot(inventory.currentSelectedSlot);

        if (inventory.currentSelectedSlot is {IsSome: true, Value: 250})
        {
            interactor.photonView.RPC(nameof(CharacterItems.DestroyHeldItemRpc), RpcTarget.All);
        }
        else
        {
            inventory.EquipSlot(Optionable<byte>.None);
        }
    }

    [ConsoleCommand]
    public static void PrintGarbageBags()
    {
        var garbageBags =
            FindObjectsByType<UnnamedGarbageBagController>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);

        foreach (var garbageBag in garbageBags)
        {
            var itemSlotList = garbageBag.GetData<BackpackData>(DataEntryKey.BackpackData).itemSlots
                .Where((ItemSlot slot) => !slot.IsEmpty()).ToList();

            UnnamedPlugin.Log.LogInfo($"Garbage Bag: {garbageBag.GetInstanceID()}, Full Slots: {itemSlotList.Count}");

            foreach (var item in itemSlotList)
            {
                UnnamedPlugin.Log.LogInfo($"Slot: {item.GetPrefabName()}, data entries: {item.data.data.Count}");
            }
        }
    }

    public override void OnInstanceDataRecieved()
    {
        base.OnInstanceDataRecieved();
        m_unnamedGarbageBagVisuals.RefreshVisuals();
    }

    [PunRPC]
    public void RPC_DropRandomBaggedItem()
    {
        DropRandomBaggedItem();
    }
    
    [PunRPC]
    public void RPC_SlipItem(Vector3 linearVelocity, Vector3 angularVelocity)
    {
        var it = DropRandomBaggedItem();
        
        it.rig.linearVelocity = linearVelocity;
        it.rig.angularVelocity = angularVelocity;
    }
    
    public global::Item DropRandomBaggedItem()
    {
        var bagData = GetData<BackpackData>(DataEntryKey.BackpackData);

        // Drop a random item
        ItemSlot slot;

        do
        {
            slot = bagData.itemSlots.GetRandom();
        } while (slot.IsEmpty());

        photonView.RPC(nameof(RPC_ItemSlipped), RpcTarget.All);
        
        return TakeItemOut(slot) as global::Item;
    }

    [PunRPC]
    public void RPC_TakeItemOut(byte slotID)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            TakeItemOut(GetData<BackpackData>(DataEntryKey.BackpackData).itemSlots[slotID]);
        }
    }

    [PunRPC]
    public void RPC_TakeAndGrabItem(byte slotID, PhotonView characterView)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            var slot = GetData<BackpackData>(DataEntryKey.BackpackData).itemSlots[slotID];
            var component = characterView.GetComponent<Character>();
            var part = component.GetBodypart(BodypartType.Hip);
            
            component.refs.items.lastEquippedSlotTime = 0.0f;
            var it = TakeItemOut(slot, part);
            it?.Interact(component);
        }
    }

    [PunRPC]
    public void RPC_ItemSlipped()
    {
        PlaySlipSFX();
    }
    
    [PunRPC]
    public void RPC_EmptySlot(byte slotId)
    {
        var slot = GetData<BackpackData>(DataEntryKey.BackpackData).itemSlots[slotId];
        slot.EmptyOut();

        if (view.IsMine)
        {
            m_unnamedGarbageBagVisuals.RefreshVisuals();
        }
    }
    
    public void PlaySlipSFX()
    {
        for (int i = 0, length = slipSDSfxInstances.Length; i < length; ++i)
        {
            slipSDSfxInstances[i].Play(transform.position);
        }
    }

    public global::Item? TakeItemOut(ItemSlot slot, Bodypart? part = null)
    {
        var tr = transform;
        var up = itemState == ItemState.Held ? tr.up : -Physics.gravity.normalized;
        
        var itemSpawnPosition = part != null ? part.transform.position + (part.transform.forward * 0.5f) : tr.position +
            (up * fullBagRadius);
        
        var itemGameObject =
            PhotonNetwork.InstantiateItemRoom(slot.GetPrefabName(), itemSpawnPosition, tr.rotation);

        if (itemGameObject == null)
        {
            return null;
        }

        itemGameObject.GetComponent<PhotonView>().RPC(nameof(SetItemInstanceDataRPC), RpcTarget.All, slot.data);
        
        // Stupid bug sometimes. We just have to double check.
        if (itemGameObject.TryGetComponent(out Rigidbody rb) && rb.isKinematic)
        {
            itemGameObject.GetComponent<global::Item>().SetKinematicNetworked(false);
        }
        
        view.RPC(nameof(RPC_EmptySlot),  RpcTarget.All, slot.itemSlotID);
        
        // Refreshing the Garbage Bag data manually since backpack codes are wack.
        photonView.RPC(nameof(SetItemInstanceDataRPC), RpcTarget.All, data);
        
        return itemGameObject.GetComponent<global::Item>();
    }

    [PunRPC]
    public void RPCAddItemToGarbageBag(PhotonView playerView, byte slotID, byte garbageBagSlotID)
    {
        var backpackData = GetData<BackpackData>(DataEntryKey.BackpackData);
        var itemSlot = playerView.GetComponent<Player>().GetItemSlot(slotID);
        if (itemSlot == null)
        {
            UnnamedPlugin.Log.LogError($"Can't add item because slot ID {slotID} is not valid.");
            return;
        }

        backpackData.AddItem(itemSlot.prefab, itemSlot.data, garbageBagSlotID);

        if (view.IsMine)
        {
            m_unnamedGarbageBagVisuals.RefreshVisuals();
        }
    }
}