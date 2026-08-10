using System.Runtime.CompilerServices;
using HarmonyLib;
using PEAKLib.Core;
using Photon.Pun;
using pworld.Scripts.Extensions;
using UnityEngine;
using UnnamedProducts.Behaviours;
using UnnamedProducts.Compatibility.Patchers;
using Zorro.Core;

namespace UnnamedProducts.Compatibility;

public static class UnnamedLuckyBlocksCompatibility
{
    private static bool _isLoaded;
    private static bool? _enabled;

    public static bool enabled
    {
        get
        {
            if (_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    "legocool.LuckyBlocks");
                UnnamedPlugin.Log.LogInfo($"Lucky Block support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void LoadCompatibilityBundle(UnnamedPlugin loader, Harmony harmony)
    {
        Outcomes.AddOutcome(SpawnUnnamedLuggage, 110, "Unnamed Luggage");
        Outcomes.AddOutcome(SpawnFireball, 80, "Fireball");
        Outcomes.AddOutcome(SetThingsOnFire, 70, "Set things on Fire");
        Outcomes.AddOutcome(SpawnUnnamedRope, 95, "Unnamed Rope/Anti-Rope");
        Outcomes.AddOutcome(SpawnUnnamedPiton, 95, "Unnamed Piton");
        Outcomes.AddOutcome(PlaceUnnamedStove, 85, "Unnamed Portable Stove");
        Outcomes.AddOutcome(PlaceUnnamedCannon, 95, "Unnamed Scout Cannon");
    }

    public static void PlaceUnnamedStove(LuckyBreakable lb, Collision coll)
    {
        Quaternion quaternion = Quaternion.LookRotation(Vector3.forward, coll.contacts[0].normal);
        PhotonNetwork.Instantiate($"{UnnamedPlugin.Id}:UnnamedPortableStovetop_Placed", coll.contacts[0].point, quaternion, 0, null);
    }
    public static void PlaceUnnamedCannon(LuckyBreakable lb, Collision coll)
    {
        Quaternion quaternion = Quaternion.LookRotation(Vector3.forward, coll.contacts[0].normal);
        PhotonNetwork.Instantiate($"{UnnamedPlugin.Id}:UnnamedScoutCannon_Placed", coll.contacts[0].point, quaternion, 0, null);
    }
    private static void SpawnUnnamedRope(LuckyBreakable lb, Collision col)
    {
        var rot = Quaternion.LookRotation(Vector3.forward, col.contacts[0].normal);

        if (Random.Range(0, 2) switch
            {
                0 => PhotonNetwork.Instantiate($"{UnnamedPlugin.Id}:UnnamedRopeAnchorForRopeShooterAnti_c",
                    col.contacts[0].point,
                    rot),
                1 => PhotonNetwork.Instantiate($"{UnnamedPlugin.Id}:UnnamedRopeAnchorForRopeShooter_c", col.contacts[0].point,
                    rot),
                _ => null
            } is { } rope && rope.TryGetComponent(out RopeAnchorWithRope anchor))
        {
            anchor.SpawnRope();
        }
    }
    private static void SpawnUnnamedPiton(LuckyBreakable lb, Collision col)
    {
        var isBad = (1.0f * Random.Range(1.0f - UnnamedPlugin.UnnamedModifier, 1.0f + UnnamedPlugin.UnnamedModifier)) >=
                    1.0f;
        
        PhotonNetwork.Instantiate($"{UnnamedPlugin.Id}:UnnamedClimbingSpikeHammered" + (isBad ? "_Bad" : "_Good"),
            col.contacts[0].point, Quaternion.LookRotation(-col.contacts[0].normal, Vector3.up));

        Singleton<AchievementManager>.Instance.IncrementSteamStat(STEAMSTATTYPE.PitonsPlaced, 1);
        GameUtils.instance.IncrementPermanentItemsPlaced();
    }

    private static void SetThingsOnFire(LuckyBreakable lb, Collision col)
    {
        if (NetworkPrefabManager.TryGetNetworkPrefab($"{UnnamedPlugin.Id}:StickyFireball", out var sfb))
        {
            var results = new Collider[20];
            var size = Physics.OverlapSphereNonAlloc(col.contacts[0].point, 5f, results,
                HelperFunctions.CharacterAndDefaultMask, QueryTriggerInteraction.Ignore);

            if (size > 0)
            {
                for (var i = 0; i < size; i++)
                {
                    var r = results[i];

                    if (r.gameObject.GetComponentInParent<Item>() is { } it)
                    {
                        var f = PhotonNetwork.Instantiate(sfb.name, lb.transform.position,
                            Quaternion.identity).GetComponent<StickyFireballController>();

                        f.StickTo(it.gameObject);
                    }
                    else if (r.gameObject.GetComponentInParent<Character>() is { } c)
                    {
                        var f = PhotonNetwork.Instantiate(sfb.name, lb.transform.position,
                            Quaternion.identity).GetComponent<StickyFireballController>();

                        f.StickTo(c.gameObject);
                    }
                }
            }
            else
            {
                PhotonNetwork.Instantiate(sfb.name, lb.transform.position,
                    Quaternion.identity);
            }
        }
    }

    private static void SpawnUnnamedLuggage(LuckyBreakable arg1, Collision col)
    {
        var rot = Quaternion.LookRotation(Vector3.forward, col.contacts[0].normal);

        var cat = 0;
        
        while (Util.Coinflip() && cat < 5)
        {
            cat++;
        }
        

        switch (cat)
        {
            case 1:
                PhotonNetwork.Instantiate(UnnamedPlugin.BigUnnamedLuggagePrefab.name,
                    col.contacts[0].point, rot);
                break;
            case 2:
                PhotonNetwork.Instantiate(UnnamedPlugin.EpicUnnamedLuggagePrefab.name,
                    col.contacts[0].point, rot);
                break;
            case 3:
                PhotonNetwork.Instantiate(UnnamedPlugin.AncientUnnamedLuggagePrefab.name,
                    col.contacts[0].point, rot);
                break;
            case 4:
                // TODO: Instantiate Clown 
                PhotonNetwork.Instantiate(UnnamedPlugin.ClownUnnamedLuggagePrefab.name,
                    col.contacts[0].point, rot);
                break;
            case 0:
            default:
                PhotonNetwork.Instantiate(UnnamedPlugin.SmallUnnamedLuggagePrefab.name, col.contacts[0].point, rot);
                break;
        }

        UnnamedPlugin.Log.LogInfo($"Spawned luggage at {col.contacts[0].point}");
    }

    private static void SpawnFireball(LuckyBreakable lb, Collision col)
    {
        Character.localCharacter.view.RPC(nameof(CharacterBurnController.SpawnFireballRPC), RpcTarget.All,
            col.contacts[0].point);
        UnnamedPlugin.Log.LogInfo($"Spawned luggage at {col.contacts[0].point}");
    }
}