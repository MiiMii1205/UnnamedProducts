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
            __instance.gameObject.TryGetComponent(out UnnamedSpawnRestriction restrictions))
        {
            var meetsBiomeChecks = true;
            var meetsColdNights = true;
            var meetsGlobalZombies = true;
            
            var hasBiomeChecks = restrictions.biomeType.Length > 0;
            var hasColdNightChecks = restrictions.hasColdNightRestrictions;
            var hasZombieRestrictions = restrictions.hasZombieRestrictions;

            var isNightCold = Ascents.isNightCold;
            var canZombieSpawnGlobally = UnnamedPlugin.DoesZombiesSpawnGlobally();
            
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
                meetsColdNights = restrictions.whenNightIsCold == isNightCold;
            }
            else
            {
                meetsColdNights = true;
            }

            if (hasZombieRestrictions)
            {
                meetsGlobalZombies = restrictions.whenZombieSpawnGlobally == canZombieSpawnGlobally;
            }

            if (hasColdNightChecks && hasBiomeChecks && hasZombieRestrictions)
            {
                __result = meetsColdNights || meetsBiomeChecks || meetsGlobalZombies;
                
            } else if (hasColdNightChecks && hasBiomeChecks && !hasZombieRestrictions)
            {
                __result = meetsColdNights || meetsBiomeChecks;
                
            } else if (hasColdNightChecks)
            {
                if (hasZombieRestrictions)
                {
                    __result = meetsColdNights || meetsGlobalZombies;
                }
                else
                {
                    __result = meetsColdNights;
                }
            }
            else if (hasBiomeChecks) 
            {
                if (hasZombieRestrictions)
                {
                    __result = meetsBiomeChecks || meetsGlobalZombies;
                }
                else
                {
                    __result = meetsBiomeChecks;
                }
            } else if (hasZombieRestrictions)
            {
                __result = meetsGlobalZombies;
            }

            UnnamedPlugin.Log.LogInfo(
                $"{__instance.GetName()} ({__instance.gameObject.name}) is {(__result ? "VALID" : "INVALID")} to spawn!");

            List<string> output = [];

            if (hasBiomeChecks)
            {
                output.Add((meetsBiomeChecks
                    ? $"We're inside {__instance.GetName()}'s biome"
                    : $"We're NOT in {__instance.GetName()}'s biome"));
            }

            if (hasColdNightChecks)
            {
                output.Add(
                    meetsColdNights
                        ? $"Nights are {(restrictions.whenNightIsCold ? "COLD" : "WARM")} for global spawn (needed: {(restrictions.whenNightIsCold ? "COLD NIGHTS" : "WARM NIGHTS")}, got {(isNightCold ? "COLD NIGHTS" : "WARM NIGHTS")})"
                        : $"Night are NOT {(restrictions.whenNightIsCold ? "COLD" : "WARM")}  for global spawn (needed: {(restrictions.whenNightIsCold ? "COLD NIGHTS" : "WARM NIGHTS")}, got {(isNightCold ? "COLD NIGHTS" : "WARM NIGHTS")})");
                
            }
            
            if (hasZombieRestrictions)
            {
                output.Add(
                    meetsGlobalZombies
                        ? $"Zombies can {(restrictions.whenZombieSpawnGlobally ? "span" : "not spawn")} globably (needed: {(restrictions.whenZombieSpawnGlobally ? "GLOBAL SPAWN" : "NO SPAWN")}, got {(canZombieSpawnGlobally ? "GLOBAL SPAWN" : "NO SPAWN")})"
                        : $"Zombies cannot {(restrictions.whenZombieSpawnGlobally ? "span" : "not spawn")}  globably (needed: {(restrictions.whenZombieSpawnGlobally ? "GLOBAL SPAWN" : "NO SPAWN")}, got {(canZombieSpawnGlobally ? "GLOBAL SPAWN" : "NO SPAWN")})");
                
            }

            UnnamedPlugin.Log.LogInfo(
                output.Join(null,",")
            );
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