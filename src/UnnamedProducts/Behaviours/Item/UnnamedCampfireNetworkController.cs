using Photon.Pun;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedCampfireNetworkController: MonoBehaviourPunCallbacks
{
    public UnnamedCampfire campfire = null!;

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (campfire.view.IsMine && !newPlayer.IsLocal)
        {
            photonView.RPC(nameof(UnnamedCampfire.RPC_UpdateBurnsFor), newPlayer, campfire.burnsFor);
        }
    }
    
}