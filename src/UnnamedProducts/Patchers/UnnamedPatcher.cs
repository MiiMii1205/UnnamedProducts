using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnnamedProducts.Behaviours;
using UnnamedProducts.Behaviours.Item;
using UnnamedProducts.Behaviours.Item.GarbageBag;
using UnnamedProducts.Behaviours.Item.GarbageBag.GUI;
using Photon.Pun;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.UI.Extensions;
using Zorro.Core;
using Object = UnityEngine.Object;

namespace UnnamedProducts.Patchers;

public static class UnnamedPatcher
{
    [HarmonyPatch(typeof(CharacterItems), nameof(CharacterItems.HammerClimbingSpike))]
    [HarmonyPrefix]
    public static void HammerClimbingSpikePrefix(CharacterItems __instance, RaycastHit hit, ref bool __runOriginal)
    {
        if (__instance.currentClimbingSpikeComponent != null)
        {
            if (UnnamedPlugin.IsUnnamed(__instance.currentClimbingSpikeComponent.gameObject))
            {
                __runOriginal = false;

                var unnamedClimbingSpike = __instance.currentClimbingSpikeComponent.gameObject
                    .GetComponent<UnnamedClimbingSpikeComponent>();

                var itss = __instance.currentClimbingSpikeItemSlot;
                var isBad = false;
                if (itss != null && itss.data.TryGetDataEntry<BoolItemData>(
                        (DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey,
                        out var d))
                {
                    isBad = d.Value;
                }

                if (!(__instance.currentClimbingSpikeComponent != null) || !(PhotonNetwork.Instantiate(
                        (isBad
                            ? unnamedClimbingSpike.hammeredBadVersionPrefab
                            : unnamedClimbingSpike
                                .hammeredGoodVersionPrefab).gameObject.name, hit.point,
                        Quaternion.LookRotation(-hit.normal, Vector3.up), 0) != null))
                {
                    return;
                }

                if (__instance.currentClimbingSpikeItemSlot != null)
                {
                    ItemSlot itemSlot = __instance.currentClimbingSpikeItemSlot;
                    __instance.currentClimbingSpikeItemSlot = null;
                    __instance.currentClimbingSpikeComponent = null;
                    __instance.character.player.EmptySlot(Optionable<byte>.Some(itemSlot.itemSlotID));
                    if (__instance.character.data.currentItem != null)
                    {
                        __instance.EquipSlot(Optionable<byte>.None);
                    }

                    __instance.UpdateClimbingSpikeCount(__instance.character.player.itemSlots);
                    __instance.character.data.lastConsumedItem = Time.time;
                }

                __instance.character.refs.afflictions.UpdateWeight();
                Singleton<AchievementManager>.Instance.IncrementSteamStat(STEAMSTATTYPE.PitonsPlaced, 1);
                GameUtils.instance.IncrementPermanentItemsPlaced();
            }
        }
    }

    [HarmonyPatch(typeof(CharacterMovement), nameof(CharacterMovement.SetWaterMovementModifier))]
    [HarmonyPostfix]
    public static void WaterMovementPostfix(CharacterMovement __instance)
    {
        // Character became wet. Check if they're on fire and extinguish everything if they are.
        if (__instance.gameObject.TryGetComponent<CharacterBurnController>(out var g))
        {
            g.ExtinguishFires();
        }
    }


    [HarmonyPatch(typeof(CharacterVineClimbing), nameof(CharacterVineClimbing.StopVineClimbingRpc))]
    [HarmonyPostfix]
    public static void StopVineClimbingPostfix(CharacterVineClimbing __instance)
    {
        if (__instance.character.data.heldVine != null)
        {
            if (__instance.character.data.heldVine.TryGetComponent<UnnamedVine>(out var nnv))
            {
                nnv.RemovePlayerFromVine(__instance.character);
            }
        }
    }

    [HarmonyPatch(typeof(CharacterVineClimbing), nameof(CharacterVineClimbing.FixedUpdate))]
    [HarmonyPostfix]
    public static void FixedUpdatePostfix(CharacterVineClimbing __instance)
    {
        if (__instance.character.data.isVineClimbing && !__instance.character.data.heldVine)
        {
            // Jump down!
            __instance.character.refs.vineClimbing.Stop();
            __instance.character.Fall(1, 0.25f);
        }
    }

    [HarmonyPatch(typeof(CharacterVineClimbing), nameof(CharacterVineClimbing.GrabVineRpc))]
    [HarmonyPostfix]
    public static void GrabVineRpcPostfix(CharacterVineClimbing __instance, PhotonView ropeView, int segmentIndex)
    {
        if (ropeView.TryGetComponent<UnnamedVine>(out var nnv))
        {
            nnv.AddPlayerToVine(__instance.character);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    [HarmonyPostfix]
    public static void CharAwakePostfix(Character __instance)
    {
        var sk = __instance.gameObject.AddComponent<UnnamedSkeletonHandler>();
        var burn = __instance.gameObject.AddComponent<CharacterBurnController>();
        var stick = __instance.gameObject.AddComponent<StickyItemRemover>();

        sk.m_character = __instance;
    }

    [HarmonyPatch(typeof(Enum), nameof(Enum.GetValues))]
    [HarmonyPostfix]
    public static void GetValuePostfix(Enum __instance, Type enumType, ref Array __result)
    {
        if (enumType == typeof(SpawnPool))
        {
            UnnamedPlugin.Log.LogInfo(
                $"Adding \"UnnamedSpawnPool\" into {enumType.Name}'s values.");

            __result = ((IEnumerable<int>) __result).AddItem((int) UnnamedPlugin.UnnamedSpawnPool).ToArray();
        }
    }

    [HarmonyPatch(typeof(GUIManager), nameof(GUIManager.Awake))]
    [HarmonyPostfix]
    public static void GUIAwakePostfix(GUIManager __instance)
    {
        var man = __instance.gameObject.GetOrAddComponent<UnnamedUIManager>();

        if (man == null)
        {
            UnnamedPlugin.Log.LogError($"Can't add {nameof(UnnamedUIManager)} to {__instance.character.characterName}'s GUI");
        }
        else
        {
            var gui = Object.Instantiate(UnnamedPlugin.UnnamedGarbageBagPrefab, __instance.hudCanvas.transform);
            man.m_garbageBagScreen = gui.GetComponent<UnnamedGarbageBagScreen>();
            gui.SetActive(false);
        }
    }

    [HarmonyPatch(typeof(CactusBall), nameof(CactusBall.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(CactusBall __instance)
    {
        __instance.gameObject.AddComponent<StickyItemRemover>();
    }

    [HarmonyPatch(typeof(GUIManager), nameof(GUIManager.wheelActive), MethodType.Getter)]
    [HarmonyPostfix]
    public static void GUIWheelActivePost(GUIManager __instance, ref bool __result)
    {
        // We do this for mouse and input capture
        __result = UnnamedUIManager.Instance.GarbageBagActive || __result;
    }

    private static int CalculateGarbageBagCarryWeight(ItemSlot[] slots, bool fromGarbageBag = false)
    {
        var tot = 0; 
        for (var i = slots.Length - 1; i >= 0; i--)
        {
            var itemSlot = slots[i];
            
            if (!itemSlot.IsEmpty())
            {
                if (itemSlot is BackpackSlot)
                {
                    if (itemSlot.data.TryGetDataEntry(DataEntryKey.BackpackData, out BackpackData bpk))
                    {
                        tot += CalculateGarbageBagCarryWeight(bpk.itemSlots, fromGarbageBag);
                    }
                }
                else
                {
                    if (itemSlot.prefab.TryGetComponent(out UnnamedGarbageBagController nngb) &&
                        itemSlot.data.TryGetDataEntry(DataEntryKey.BackpackData,
                            out BackpackData gbd))
                    {
                        tot += CalculateGarbageBagCarryWeight(gbd.itemSlots, true);
                    }
                    else if (fromGarbageBag)
                    {
                        tot += itemSlot.prefab.CarryWeight;
                    }
                }
            }
        }

        return tot;
    }

    [HarmonyPatch(typeof(CharacterAfflictions), nameof(CharacterAfflictions.UpdateWeight))]
    [HarmonyPostfix]
    public static void UpdateGarbageBagWeight(CharacterAfflictions __instance)
    {
        var totWeight = CalculateGarbageBagCarryWeight(__instance.character.player.itemSlots);

        if (!__instance.character.player.backpackSlot.IsEmpty())
        {
            totWeight += CalculateGarbageBagCarryWeight([__instance.character.player.backpackSlot]);
        }

        if (!__instance.character.player.tempFullSlot.IsEmpty())
        {
            totWeight += CalculateGarbageBagCarryWeight([__instance.character.player.tempFullSlot]);
        }

        // Add the additionnal weight

        if (totWeight > 0)
        {
            var statusType = CharacterAfflictions.STATUSTYPE.Weight;
            var amount = 0.025f * totWeight;

            if ((!__instance.character.isZombie || __instance.StatusAffectsZombie(statusType)) &&
                __instance.character.photonView.IsMine)
            {
                __instance.currentStatuses[(int) statusType] += amount;
                __instance.currentStatuses[(int) statusType] =
                    Mathf.Clamp(__instance.currentStatuses[(int) statusType], 0f, __instance.GetStatusCap(statusType));
                __instance.currentStatuses[(int) statusType] =
                    __instance.RoundStatus(__instance.currentStatuses[(int) statusType]);
                __instance.currentIncrementalStatuses[(int) statusType] = 0f;
                __instance.currentDecrementalStatuses[(int) statusType] = 0f;
                __instance.character.ClampStamina();
                GUIManager.instance.bar.ChangeBar();

                __instance.PushStatuses();
            }
        }
    }

    [HarmonyPatch(typeof(LootData), nameof(LootData.GetRandomItems))]
    [HarmonyPrefix]
    public static void GetRandomItemsPrefix(LootData __instance, SpawnPool spawnPool, int count, ref bool __runOriginal,
        ref List<GameObject> __result, bool canRepeat = false)
    {
        if (LootData.AllSpawnWeightData == null)
        {
            LootData.PopulateLootData();
        }

        if (!LootData.AllSpawnWeightData!.ContainsKey(spawnPool))
        {
            __runOriginal = false;

            // Build a possible list of value

            var spawnPoolValueList = Enum.GetValues(spawnPool.GetType());

            var dictionary = new Dictionary<ushort, int>();

            foreach (int spawnPoolValue in spawnPoolValueList)
            {
                if (spawnPool.HasFlag((SpawnPool) spawnPoolValue) && LootData.AllSpawnWeightData.TryGetValue(
                        (SpawnPool) spawnPoolValue, out var values))
                {
                    // Add every items in the pool

                    UnnamedPlugin.Log.LogInfo($"Adding spawn pool {spawnPoolValue} to spawn list.");
                    foreach (var keyValuePair in values)
                    {
                        dictionary[keyValuePair.Key] = keyValuePair.Value;
                    }
                }
            }

            List<GameObject> list = [];
            for (var j = 0; j < count; ++j)
            {
                var key = dictionary.RandomSelection(i => i.Value).Key;
                if (!ItemDatabase.TryGetItem(key, out var item))
                {
                    UnnamedPlugin.Log.LogWarning($"Can't find item #{key} in database.");
                    continue;
                }

                if (spawnPool.HasFlag(UnnamedPlugin.UnnamedSpawnPool))
                {
                    // Is an unnamed spawner, so any item that doesn't have an unnamed versions aren't valid to spawn

                    if (!UnnamedPlugin.IsUnnamedUnique(item.gameObject) && !UnnamedPlugin.HasUnnamedVariant(item))
                    {
                        UnnamedPlugin.Log.LogInfo(
                            $"{item.GetName()} ({item.gameObject.name}) cannot spawn in an unnamed luggage because it has no Unnamed variant");
                        dictionary.Remove(key);
                        j--;
                        continue;
                    }
                }

                if (!item.IsValidToSpawn())
                {
                    UnnamedPlugin.Log.LogDebug($"{item.GetName()} ({item.gameObject.name}) IS INVALID TO SPAWN");
                    dictionary.Remove(key);
                    j--;
                    continue;
                }

                list.Add(item.gameObject);

                if (!canRepeat)
                {
                    dictionary.Remove(key);
                }
            }

            __result = list;
        }
    }
}