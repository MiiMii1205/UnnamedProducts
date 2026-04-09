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

public class UnnamedGarbageBagAudio : MonoBehaviour
{

    public UnnamedGarbageBagController bag;

    public SFX_Instance[] holdSFX;

    private bool holdTransition;

    public SFX_Instance[] dropSFX;

    private bool dropTransition;
    
    private void Start() => bag ??= GetComponent<UnnamedGarbageBagController>();

    private void Update()
    {
        if (bag)
        {
            if (bag.holderCharacter)
            {
                if (!holdTransition)
                {
                    for (int index = 0, length = holdSFX.Length; index < length; ++index)
                    {
                        holdSFX[index].Play(transform.position);
                    }

                    holdTransition = true;
                }
            }
            else
            {
                holdTransition = false;
            }

            if (bag.rig.useGravity)
            {
                if (!dropTransition)
                {
                    for (int index = 0, length = dropSFX.Length; index < length; ++index)
                    {
                        dropSFX[index].Play(transform.position);
                    }
                }

                dropTransition = true;
            }
            else
            {
                dropTransition = false;
            }
        }
    }
}
