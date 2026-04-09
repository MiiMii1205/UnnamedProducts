using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Peak.Afflictions;
using PEAKLib.Core;
using PEAKLib.Items;
using PEAKLib.Items.UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnnamedProducts.Compatibility;

public static class Unnamed4FoodCompatibility
{
    private static bool _isLoaded;
    private static bool? _enabled;

    private static List<string> itemList =
    [
        "Chrisps Blue",
        "Chrisps Red",
        "Beans",
        "Extra Extreme Energy Drink",
        "bandaid",
        "Climbers Chalk",
        "IceAxe"
    ];

    public static bool enabled
    {
        get
        {
            if (_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    "com.github.BurningSulphur.Scouting4Food");
                UnnamedPlugin.Log.LogInfo($"Scouting4Food support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void LoadCompatibilityBundle(UnnamedPlugin loader)
    {
        // stuff that require the dependency to be loaded

        var modDefinition = ModDefinition.GetOrCreate(BepInEx
            .Bootstrap.Chainloader.PluginInfos["com.github.BurningSulphur.Scouting4Food"]);

        if (modDefinition.Content.Count >= 14)
        {
            // Load the bundle NOW
            UnnamedPlugin.Log.LogInfo($"Loading {modDefinition.Name} bundle immediately!");
            LoadBundle(loader, modDefinition);
            _isLoaded = true;
        }
        else
        {
            UnnamedPlugin.Log.LogInfo($"Loading {modDefinition.Name} bundle later...");
            // Load the bundle later..
            BundleLoader.OnBundleLoaded += bundle =>
            {
                // We also need to check it scouting 4 food has loaded ALL its asset bundles
                if (bundle.Mod.Id == modDefinition.Id && !_isLoaded && modDefinition.Content.Count >= 14)
                {
                    LoadBundle(loader, modDefinition);
                    _isLoaded = true;
                }
            };
        }
    }

    private static void LoadBundle(UnnamedPlugin loader, ModDefinition modDefinition)
    {
        loader.LoadBundleWithName("unnamed4food.peakbundle", peakBundle =>
        {
            foreach (var content in modDefinition.Content)
            {
                if (content is UnityItemContent {Item: { } it} imc &&
                    itemList.Contains(CleanItemName(it.gameObject.name)))
                {
                    var cleanedGameObjectName = CleanItemName(it.gameObject.name);
                    var originalItemName = it.UIData.itemName;
                    var variant = imc.Item.gameObject;

                    var newVariant = loader.GenerateInstancedVariant(variant, cleanedGameObjectName);
                    var newIt = newVariant.gameObject.GetComponent<Item>();

                    loader.CopyUnnamedMaterials(ref newVariant, peakBundle);
                    loader.CopyUnnamedIcons(ref newIt, peakBundle);
                    PopulateUnnamedData(ref newIt, peakBundle, loader);
                    loader.CopyExplosionData(ref newIt, peakBundle);

                    // Strip LootData

                    var ld = newVariant.gameObject.GetComponent<LootData>();
                    Object.Destroy(ld);

                    loader.AddItemToDatabase(originalItemName, newVariant.gameObject);

                    (new ItemContent(newIt)).Register(ModDefinition.GetOrCreate(UnnamedPlugin.UnnamedInfo));
                }
            }

            UnnamedPlugin.Log.LogInfo($"UNNAMED items for {modDefinition.Name} are loaded!");
        });
    }

    private static void PopulateUnnamedData(ref Item it, PeakBundle peakBundle, UnnamedPlugin loader)
    {
        loader.PopulateUnnamedData(ref it, peakBundle);
        
        switch (it.UIData.itemName)
        {
            case "Ice Axe":
            {
                var list = it.GetComponents<MonoBehaviour>();

                foreach (var mb in list)
                {
                    var t = mb.GetType();
                    
                    if (t.Name.Contains("IceAxe"))
                    {
                        // Its the Ice Axe component
                        
                        var instantiateOnBreakFieldInfo = t.GetField("instantiateOnBreak");

                        var vanillaSpawnedPrefab = (GameObject) instantiateOnBreakFieldInfo.GetValue(mb);

                        var hookedIceAxe = Object.Instantiate(vanillaSpawnedPrefab);
                        hookedIceAxe.name = "Unnamed" + CleanItemName(vanillaSpawnedPrefab.name);

                        Object.DontDestroyOnLoad(hookedIceAxe);

                        hookedIceAxe.SetActive(false);

                        loader.CopyUnnamedMaterials(ref hookedIceAxe, peakBundle);

                        hookedIceAxe = loader.RegisterOrGetItemPrefab(hookedIceAxe);

                        instantiateOnBreakFieldInfo.SetValue(mb, hookedIceAxe);

                        break;
                    }
                }

                break;
            }

            case "Can o' Beans":
            {
                var spwn = it.gameObject.GetComponent<Action_Spawn>();
                
                var exp = Object.Instantiate(spwn.objectToSpawn);
                exp.name = "Unnamed" + CleanItemName(spwn.objectToSpawn.name).Replace("Unnamed", "");
                
                if (exp.GetComponentInChildren<AOE>() is { } aoe && !UnnamedPlugin.IsUnnamed(aoe.gameObject))
                {
                    UnnamedPlugin.RenameToUnnamed(aoe.gameObject);
                }

                spwn.objectToSpawn = loader.RegisterOrGetNetworkPrefab(exp);

                Object.DontDestroyOnLoad(exp);

                exp.SetActive(false);

                break;
            }
        }
    }

    private static string CleanItemName(string dirtyName)
    {
        return dirtyName.Replace("com.github.BurningSulphur.Scouting4Food:", "").Replace("(Clone)", "")
            .Replace("(Instanced)", "");
    }

    public static void ApplyUnnamedModifierToAffliction(ref Affliction affliction)
    {
        
    }
}