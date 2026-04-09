using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnnamedProducts.Extensions;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedScoutCannon: ScoutCannon
{
    public new void FireTargets()
    {
        UnnamedFireCannon();
    }

    public void LaunchItems(float force, Vector3 dir)
    {
        var itemsToLaunch = new List<global::Item>();
        var overlappedColliders = Physics.OverlapSphere(tube.position, 1f,
            HelperFunctions.GetMask(HelperFunctions.LayerType.AllPhysical));
        
        for (int i = 0, length = overlappedColliders.Length; i < length; ++i)
        {
            if (overlappedColliders[i].GetComponentInParent<global::Item>() is {itemState: ItemState.Ground} it &&
                !HelperFunctions.LineCheck(tube.position, it.Center(), HelperFunctions.LayerType.Map)
                    .transform && !itemsToLaunch.Contains(it))
            {
                itemsToLaunch.Add(it);
            }
        }

        foreach (var item in itemsToLaunch)
        {
            view.RPC(nameof(RPCA_UnnamedLaunchItem), RpcTarget.All, item.photonView.ViewID, force, dir);
        }
    }

    [PunRPC]
    public new void RPCA_SetTarget(int setTargetID)
    {
        base.RPCA_SetTarget(setTargetID);
    }

    private Vector3 UnnamedDir
    {
        get
        {
            var circle = (Random.insideUnitCircle).normalized * UnnamedPlugin.RandomUnnamedModifier;
            
            var dirOffset = tube.up * circle.y + tube.right * circle.x;
            
            return (tube.forward + (dirOffset * 0.25f)).normalized;
        }
    }

    [PunRPC]
    public void RPCA_UnnamedLaunchItem(int targetID, float force, Vector3 dir)
    {
        var photonView = PhotonNetwork.GetPhotonView(targetID);
        if (photonView == null)
        {
            return;
        }
        var component = photonView.GetComponent<global::Item>();
        if (component == null)
        {
            return;
        }
        if (component is Backpack)
        {
            component.rig.AddForce(
                dir * ((backpackLaunchForce * force) / launchForce),
                ForceMode.VelocityChange);
        }
        else
        {
            component.rig.AddForce(dir * ((itemLaunchForce * force) / launchForce), ForceMode.VelocityChange);
        }
    }

    [PunRPC]
    public void RPCA_UnnamedLaunchTarget(int targetID, float force, Vector3 dir)
    {
        var photonView = PhotonNetwork.GetPhotonView(targetID);
        
        if (photonView == null)
        {
            return;
        }
        
        var characterTarget = photonView.GetComponent<Character>();
        
        if (characterTarget == null)
        {
            return;
        }
        
        characterTarget.data.launchedByCannon = true;
        
        UnnamedPlugin.Log.LogInfo($"Launching {characterTarget.characterName} from {nameof(UnnamedScoutCannon)} {gameObject.name} with a force of {force} instead of {launchForce} and at {dir} instead of {tube.forward}.");
        
        characterTarget.RPCA_Fall((force * this.fallFor) / launchForce );
        characterTarget.AddForce(dir * force);
        StartCoroutine(ILaunch());

        IEnumerator ILaunch()
        {
            var c = 0.0f;
            while (c < fallFor)
            {
                c += Time.deltaTime;
                if (characterTarget)
                {
                    characterTarget.ClampSinceGrounded(1f);
                }
                yield return null;
            }

            if (characterTarget)
            {
                characterTarget.data.fallSeconds = 0.0f;
            }
            
            c = 0.0f;
            
            while (c < 4.0f)
            {
                c += Time.deltaTime;
                if ( characterTarget)
                {
                    characterTarget.ClampSinceGrounded(1f);
                }
                yield return null;
            }
        }
    }
    
    public void LaunchPlayers(float force, Vector3 dir)
    {
        var charactersToLaunch = new List<Character>();
        
        if (target)
        {
            charactersToLaunch.Add(target);
        }
        
        foreach (var playerCharacter in Character.AllCharacters)
        {
            if (playerCharacter.Center.SquareDistance(entry.position) <= (0.75 * 0.75) &&
                (playerCharacter != target))
            {
                charactersToLaunch.Add(playerCharacter);
            }
        }

        foreach (var botCharacter in Character.AllBotCharacters)
        {
            if (botCharacter.Center.SquareDistance(entry.position) <= (0.75 * 0.75) &&
                (botCharacter != target))
            {
                charactersToLaunch.Add(botCharacter);
            }
        }

        foreach (var character in charactersToLaunch)
        {
            view.RPC(nameof(RPCA_UnnamedLaunchTarget), RpcTarget.All, character.refs.view.ViewID, force, dir);
        }
    }

    public void UnnamedFireCannon()
    {
        var force = launchForce * UnnamedPlugin.RandomUnnamedModifier;
        var dir = UnnamedDir;
        LaunchPlayers(force, dir);
        LaunchItems(force, dir);
    }
}