using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours;

public class LuggageBrandHandler : MonoBehaviourPunCallbacks
{
    public bool isUnnamed;
    public bool shouldBeUnnamed;
    public Luggage luggage = null!;

    [PunRPC]
    public void RPC_RequestUpdate()
    {
        if (photonView.IsMine)
        {
            if (shouldBeUnnamed)
            {
                photonView.RPC(nameof(RPC_SetLuggageAsUnnamed), RpcTarget.All, true);
            }
        }
    }

    [PunRPC]
    public void RPC_SetLuggageAsUnnamed(bool hardSet)
    {
        if (hardSet)
        {
            shouldBeUnnamed = true;
        }

        if (shouldBeUnnamed && !isUnnamed)
        {
            UnnamedPlugin.Log.LogInfo(
                $"Setting luggage {luggage} brand to unnamed.");

            luggage.gameObject.name = "com.github.MiiMii1205.UnnamedProducts:Unnamed" + luggage.gameObject.name;

            var renderers = luggage.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                var newMats = new List<Material>();

                for (var i = 0; i < renderer.materials.Length; i++)
                {
                    var sanitizedName = renderer.materials[i].name.Replace("(Instance)", "").Replace("(Clone)", "")
                        .Trim();

                    switch (sanitizedName)
                    {
                        case "M_Luggage":

                            newMats.Add(Instantiate(UnnamedPlugin.SmallLuggageMaterial));
                            break;

                        case "M_Luggage_interior":
                            newMats.Add(Instantiate(UnnamedPlugin.SmallLuggageInteriorMaterial));
                            break;

                        case "M_Luggage_large":
                            newMats.Add(Instantiate(UnnamedPlugin.LargeLuggageMaterial));
                            break;

                        case "M_Luggage_interior_large":
                            newMats.Add(Instantiate(UnnamedPlugin.LargeLuggageInteriorMaterial));
                            break;

                        case "M_Luggage_ancient":
                            newMats.Add(Instantiate(UnnamedPlugin.AncientLuggageMaterial));
                            break;
                        case "M_Luggage_epic":
                            newMats.Add(Instantiate(UnnamedPlugin.EpicLuggageMaterial));
                            break;

                        case "M_Metal":
                            newMats.Add(Instantiate(UnnamedPlugin.AncientLuggageMetalMaterial));
                            break;

                        case "M_Rock_Crystal":
                            newMats.Add(Instantiate(UnnamedPlugin.AncientLuggageCrystalMaterial));
                            break;

                        case "M_Rock_Volcano":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueCalderaMaterial));
                            break;

                        case "M_DesertSand":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueMesaMaterial));
                            break;

                        case "M_Rock_peak Snow":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueAlpineMaterial));
                            break;

                        case "M_Rock 1":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueTropicsMaterial));
                            break;

                        case "M_Forest_rock":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueRootsMaterial));
                            break;

                        case "M_SaltRock":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueShoreMaterial));
                            break;

                        case "M_Rock_staticTopColour":
                            newMats.Add(Instantiate(UnnamedPlugin.RespawnStatueRockVFXMaterial));
                            break;

                        default:
                            newMats.Add(renderer.materials[i]);
                            break;
                    }
                }

                renderer.SetSharedMaterials(newMats);
                renderer.SetMaterials(newMats);
            }

            foreach (var t in luggage.heightBasedSpawnPools)
            {
                t.spawnPool |= UnnamedPlugin.UnnamedSpawnPool;
            }


            this.isUnnamed = true;
        }
    }

    public void SetLuggageAsUnnamed()
    {
        photonView.RPC(nameof(RPC_SetLuggageAsUnnamed), RpcTarget.All, false);
    }

    public override void OnEnable()
    {
        if (photonView.IsMine)
        {
            if (shouldBeUnnamed)
            {
                this.photonView.RPC(nameof(RPC_SetLuggageAsUnnamed), RpcTarget.All, true);
            }
        }
    }

    public void Start()
    {
        if (!photonView.IsMine)
        {
            if (shouldBeUnnamed && !isUnnamed)
            {
                this.photonView.RPC(nameof(RPC_RequestUpdate), RpcTarget.MasterClient);
            }
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        if (photonView.IsMine && !newPlayer.IsLocal)
        {
            if (shouldBeUnnamed)
            {
                this.photonView.RPC(nameof(RPC_SetLuggageAsUnnamed), newPlayer, true);
            }
        }
    }
}