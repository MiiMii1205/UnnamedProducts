using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours;

public class CharacterBurnController : MonoBehaviourPun
{
    public bool m_hasFire = false;

    public HashSet<StickyFireballController> m_fires = null!;

    private void Awake()
    {
        m_fires = [];
    }

    public void ExtinguishFires()
    {
        photonView.RPC(nameof(ExtinguishRPC), RpcTarget.All);
    }

    [PunRPC]
    public void ExtinguishRPC()
    {
        if (photonView.IsMine && m_fires.Count > 0)
        {
            m_hasFire = true;

            foreach (var st in m_fires)
            {
                st.Extinguish();
            }

            m_hasFire = false;
        }
    }

    public void RegisterFire(StickyFireballController stickyFireballController)
    {
        m_fires.Add(stickyFireballController);
    }
    public void DeRegisterFire(StickyFireballController stickyFireballController)
    {
        m_fires.Remove(stickyFireballController);
    }
}