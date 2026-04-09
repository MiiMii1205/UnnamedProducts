using Photon.Pun;
using UnityEngine.UI.Extensions;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedCampfire: Campfire
{
    public UnnamedCampfireNetworkController networkController = null!; 
    public new void Awake()
    {
        view = GetComponent<PhotonView>();
        networkController = gameObject.GetOrAddComponent<UnnamedCampfireNetworkController>();
        networkController.campfire = this;
        base.Awake();
    }

    public void Start()
    {
        if (view.IsMine)
        {
            var newBurnFor = burnsFor * UnnamedPlugin.RandomUnnamedModifier;
            view.RPC(nameof(RPC_UpdateBurnsFor), RpcTarget.All, newBurnFor);
        }
    }

    [PunRPC]
    public void RPC_UpdateBurnsFor(float newBurnFor)
    {
        burnsFor = newBurnFor;
        UnnamedPlugin.Log.LogInfo(
            $"{nameof(UnnamedCampfire)} {gameObject.name} will burn for {burnsFor} seconds");

    }

    [PunRPC]
    public new void Extinguish_Rpc()
    {
        base.Extinguish_Rpc();
    }
    
    [PunRPC]
    public new void Light_Rpc(bool updateSegment)
    {
        base.Light_Rpc(updateSegment);
    }
}