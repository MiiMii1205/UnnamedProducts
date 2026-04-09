using Photon.Pun;
using UnityEngine;
using Zorro.Core.Serizalization;

namespace UnnamedProducts.Behaviours.Item.GarbageBag;

public struct UnnamedGarbageBagReference: IBinarySerializable
{
    public Transform locationTransform;

    public PhotonView view;
    
    public void Serialize(BinarySerializer serializer)
    {
        serializer.WriteInt(view.ViewID);
    }


    public void Deserialize(BinaryDeserializer deserializer)
    {
        view = PhotonView.Find(deserializer.ReadInt());
    }

    public BackpackData GetData()
    {
        return view.GetComponent<global::Item>().GetData<BackpackData>(DataEntryKey.BackpackData);
    }

    public static UnnamedGarbageBagReference GetFromBackpackItem(global::Item item)
    {
        UnnamedGarbageBagReference result = default(UnnamedGarbageBagReference);
        result.view = item.GetComponent<PhotonView>();
        result.locationTransform = item.transform;
        return result;
    }

    public bool TryGetGarbageBagItem(out UnnamedGarbageBagController garbageBag)
    {
        return view.TryGetComponent(out garbageBag);
    }
    
}