using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnnamedProducts.Behaviours;
using PEAKLib.Core;
using Photon.Pun;
using Photon.Realtime;
using pworld.Scripts.Extensions;
using Sirenix.Utilities;
using UnityEngine;
using Zorro.Core;

namespace UnnamedProducts.Patchers;

internal static class UnnamedSpawnerPatcher
{
    [HarmonyPatch(typeof(Item), nameof(Item.IsValidToSpawn))]
    [HarmonyPostfix]
    public static void IsValidToSpawnPostfix(Item __instance, ref bool __result)
    {
        if ((UnnamedPlugin.IsUnnamed(__instance.gameObject) || UnnamedPlugin.IsUnnamedUnique(__instance.gameObject)) &&
            __instance.gameObject.TryGetComponent<UnnamedSpawnRestriction>(out var restrictions))
        {
            var meetsBiomeChecks = true;
            var meetsColdNights = true;
            
            var hasBiomeChecks = restrictions.biomeType.Length > 0;
            var hasColdNightChecks = restrictions.hasColdNightRestrictions;
            
            if (hasBiomeChecks)
            {
                if (Singleton<MapHandler>.Instance is { } handler)
                {
                    var current = handler.GetCurrentBiome();

                    if (!restrictions.biomeType.Contains(current))
                    {
                        meetsBiomeChecks = false;
                    }
                }
                else
                {
                    meetsBiomeChecks = false;
                }
            }
            else
            {
                meetsBiomeChecks = true;
            }


            if (hasColdNightChecks)
            {
                meetsColdNights = restrictions.whenNightIsCold == Ascents.isNightCold;
            }
            else
            {
                meetsColdNights = true;
            }

            __result = meetsColdNights || meetsBiomeChecks;

            UnnamedPlugin.Log.LogInfo(
                $"{__instance.GetName()} ({__instance.gameObject.name}) is {(__result ? "VALID" : "INVALID")} to spawn!");

            if (!meetsBiomeChecks)
            {
                if (hasColdNightChecks)
                {
                    UnnamedPlugin.Log.LogInfo(
                        meetsColdNights
                            ? $"We're NOT in {__instance.GetName()}'s biome, but nights are {(restrictions.whenNightIsCold ? "COLD" : "WARM")} (needed: {(restrictions.whenNightIsCold ? "COLD NIGHTS" : "WARM NIGHTS")}, got {(Ascents.isNightCold ? "COLD NIGHTS" : "WARM NIGHTS")})."
                            : $"We're NOT in {__instance.GetName()}'s biome and don't meet the night cold settings for global spawn (needed: {(restrictions.whenNightIsCold ? "COLD NIGHTS" : "WARM NIGHTS")}, got {(Ascents.isNightCold ? "COLD NIGHTS" : "WARM NIGHTS")}).");
                }
                else
                {
                    UnnamedPlugin.Log.LogInfo(
                        $"We're NOT in {__instance.GetName()}'s biome.");
                }
            }
            else
            {
                UnnamedPlugin.Log.LogInfo(
                    $"We're inside {__instance.GetName()}'s biome");
            }
        }
    }


    [HarmonyPatch(typeof(Luggage), nameof(Luggage.GetName))]
    [HarmonyPostfix]
    public static void GetNamePostfix(Item __instance, ref string __result)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __result = LocalizedText.GetText("UNNAMED_ITEM").Replace("#", __result);
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.GetObjectsToSpawn))]
    [HarmonyPostfix]
    public static void GetObjectsToSpawnPostfix(Spawner __instance,
        ref List<GameObject> __result,
        int spawnCount,
        bool canRepeat)
    {
        UnnamedPlugin.Log.LogInfo($"{ThrowHelper.ThrowIfArgumentNull(__result, "Results").Count} items to spawn!");

        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            if (__instance.isSingleItem)
            {
                var originalItem = __instance.spawnedObjectPrefab.gameObject.GetComponent<Item>();

                UnnamedPlugin.Log.LogInfo(
                    $"{originalItem.UIData.itemName} is to spawn.");

                if (originalItem.UIData.itemName == "Dynamite")
                {
                    var objectsToSpawn = new List<GameObject>();
                    for (var i = 0; i < spawnCount; ++i)
                    {
                        var unnamedVariant = UnnamedPlugin.GetUnnamedVariant(
                            originalItem);

                        UnnamedPlugin.Log.LogInfo(
                            $"Turning {originalItem.GetName()} into {unnamedVariant.GetComponent<Item>().GetName()}.");

                        objectsToSpawn.Add(unnamedVariant);
                    }

                    __result = objectsToSpawn;
                }
            }
            else
            {
                for (int i = 0, length = __result.Count; i < length; ++i)
                {
                    if (!UnnamedPlugin.IsUnnamed(__result[i]) &&
                        !UnnamedPlugin.IsUnnamedUnique(__result[i]))
                    {
                        var item = __result[i].GetComponent<Item>();

                        var unnamedVariant = UnnamedPlugin.GetUnnamedVariant(item);

                        UnnamedPlugin.Log.LogInfo(
                            $"Turning {item.GetName()} into {unnamedVariant.GetComponent<Item>().GetName()}.");

                        __result[i] = unnamedVariant;
                    }
                }
            }
        }
        else
        {
            for (int i = 0, length = __result.Count; i < length; i++)
            {
                if (UnnamedPlugin.ShouldBeUnnamed && (!UnnamedPlugin.IsUnnamed(__result[i]) &&
                                                    !UnnamedPlugin.IsUnnamedUnique(__result[i])))
                {
                    var item = __result[i].GetComponent<Item>();

                    var unnamedVariant = UnnamedPlugin.GetUnnamedVariant(item);

                    UnnamedPlugin.Log.LogInfo(
                        $"Turning {item.GetName()} into {unnamedVariant.GetComponent<Item>().GetName()}.");

                    __result[i] = unnamedVariant;
                }
            }
        }
    }
}