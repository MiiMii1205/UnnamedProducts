using Photon.Pun;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedFrisbeeItem: MonoBehaviourPunCallbacks
{
    public Frisbee frisbee = null!;
    public float originalLiftForce = 10f;

    public float originalVelocityForLift = 10f;
    public void Start()
    {
        GlobalEvents.OnItemThrown += RerollPhysicsStats;
    }
    
    private void RerollPhysicsStats(global::Item obj)
    {
        if (obj == frisbee.item && photonView.IsMine)
        {
            photonView.RPC(nameof(RPC_SetNewFrisbeeStats), RpcTarget.All, UnnamedPlugin.UnnamedModifier,
                UnnamedPlugin.UnnamedModifier);
        }
    }

    [PunRPC]
    public void RPC_SetNewFrisbeeStats(float liftMod, float velLiftMod)
    {
        frisbee.liftForce = originalLiftForce *= liftMod;
        frisbee.velocityForLift = originalVelocityForLift *= liftMod;
    }
}