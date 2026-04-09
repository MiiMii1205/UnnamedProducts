using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item.GarbageBag;

[RequireComponent(typeof(PhotonView))]
public class UnnamedGarbageBagSlips : MonoBehaviour
{
    public UnnamedGarbageBagController garbageBag;

    public bool slipOnCollision;

    public float minSlipVelocity;

    public bool ragdollCharacterOnSlip;

    public Rigidbody rig = null!;

    private Vector3 m_lastVelocity;

    public float pushForce = 2f;

    public float wholeBodyPushForce = 1f;
    private bool m_justSlipped;
    private bool m_justThrown;

    private void Awake()
    {
        garbageBag ??= GetComponent<UnnamedGarbageBagController>();
        rig ??= GetComponent<Rigidbody>();
        GlobalEvents.OnItemThrown += OnItemThrown;
    }

    private void OnItemThrown(global::Item obj)
    {
        if (obj == garbageBag)
        {
            m_justThrown = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (garbageBag.photonView.IsMine && garbageBag.itemState == ItemState.Ground && slipOnCollision &&
            garbageBag.rig &&
            collision.relativeVelocity.magnitude > minSlipVelocity
           )
        {
            if (((m_justThrown && ((Time.time - garbageBag.lastThrownTime) > 0.150f)) || !m_justThrown) && garbageBag
                    .shouldSlip)
            {
                garbageBag.view.RPC(nameof(UnnamedGarbageBagController.RPC_SlipItem), RpcTarget.MasterClient,
                    garbageBag.rig.linearVelocity, garbageBag.rig.angularVelocity);
                
                m_justSlipped = true;
                garbageBag.m_unnamedGarbageBagVisuals.RefreshVisuals();
                
                if (ragdollCharacterOnSlip)
                {
                    if (collision.transform.GetComponentInParent<Character>() is { } character)
                    {
                        var rb = collision.transform.GetComponentInParent<Rigidbody>();
                        var force = m_lastVelocity.normalized * pushForce;

                        character.AddForceToBodyPart(rb, force * pushForce,
                            force * wholeBodyPushForce);
                        character.Fall(2f, 15f);
                    }
                }
                
            }
        }
        else
        {
            m_justThrown = false;
        }
    }


    private void FixedUpdate()
    {
        if (garbageBag.photonView.IsMine && rig && !rig.isKinematic)
        {
            m_lastVelocity = rig.linearVelocity;

            if (m_lastVelocity.sqrMagnitude <= ((0.01) * (0.01)))
            {
                m_justSlipped = false;
            }
        }
    }
}