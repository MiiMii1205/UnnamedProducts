using Photon.Pun;

namespace UnnamedProducts.Behaviours;

public class StickyItemRemover : MonoBehaviourPun
{
    [PunRPC]
    public void RPC_WashOff(int viewId)
    {
        var chara = PhotonNetwork.GetPhotonView(viewId).GetComponent<Character>();
        
        foreach (var stuckItem in StickyItemComponent.ALL_STUCK_ITEMS)
        {
            if (stuckItem.stuckToCharacter == chara && stuckItem is CactusBall && stuckItem.photonView.IsMine)
            {
                // Destroy the cactus ball
                PhotonNetwork.Destroy(stuckItem.gameObject);
            }
        }   
    }
}