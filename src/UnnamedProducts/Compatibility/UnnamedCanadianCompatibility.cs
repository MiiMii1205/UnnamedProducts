using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Peak.Afflictions;
using PEAKLib.Core;
using PEAKLib.Core.Extensions;
using PEAKLib.Items;
using PEAKLib.Items.UnityEditor;
using UnityEngine;

namespace UnnamedProducts.Compatibility;

public static class UnnamedCanadianCompatibility
{
    private static bool _isLoaded;
    private static bool? _enabled;

    private static List<string> itemList =
    [
        "AllDressed",
        "BigJoe",
        "CoffeeBar",
        "HostCuttings",
        "MapleCookies",
        "MapleToffee",
        "PapinoCookies",
        "Paw Cakes",
        "PoorManPudding",
        "Airplane Poutine",
        "Tourtiere",
        "SugarFudge",
        "SpruceBeer"
    ];

    public static bool enabled
    {
        get
        {
            if (_enabled == null)
            {
                _enabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(
                    "com.github.MiiMii1205.CanadianCuisine");
                UnnamedPlugin.Log.LogInfo($"CannadianCuisine support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }
    

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void LoadCompatibilityBundle(UnnamedPlugin loader)
    {
        var modDefinition = ModDefinition.GetOrCreate(BepInEx
            .Bootstrap.Chainloader.PluginInfos["com.github.MiiMii1205.CanadianCuisine"]);

        if (modDefinition.Content.Count >= 16)
        {
            // Load the bundle NOW
            UnnamedPlugin.Log.LogInfo($"Loading {modDefinition.Name} bundle immediately!");
            LoadBundle(loader, modDefinition);
            _isLoaded = true;
        }
        else
        {
            // Load the bundle later..
            UnnamedPlugin.Log.LogInfo($"Loading {modDefinition.Name} bundle later...");
            BundleLoader.OnBundleLoaded += bundle =>
            {
                if (bundle.Contains("AllDressed.prefab") && bundle.Mod.Id == modDefinition.Id && !_isLoaded)
                {
                    LoadBundle(loader, modDefinition);
                    _isLoaded = true;
                }
            };

        }
    }

    private static void LoadBundle(UnnamedPlugin loader, ModDefinition modDefinition)
    {
        loader.LoadBundleWithName("unnamedcanadian.peakbundle", peakBundle =>
        {
            foreach (var content in modDefinition.Content)
            {
                if (content is UnityItemContent {Item: { } it} imc && itemList.Contains(CleanItemName(it.gameObject.name)))
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
            case "Spruce Beer":
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
            case "All Dressed":
            {

                var g = peakBundle.LoadAsset<GameObject>("UnnamedAllDressedWrapper.prefab");
                var allDressedWrapper = Object.Instantiate(g, it.mainRenderer.transform, false);

                allDressedWrapper.transform.localPosition = new Vector3(0, 0, 0.0170000009f);
                allDressedWrapper.transform.localScale = Vector3.one;
                allDressedWrapper.transform.localRotation = new Quaternion(0, 1, 0, 0);

                ShaderExtensions.ReplaceShaders(allDressedWrapper);

                it.addtlRenderers = it.addtlRenderers.AddToArray(allDressedWrapper.GetComponent<Renderer>());
                
                break;
            }
            case "Poor Man Pudding":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "plastic")
                    {
                        filt.mesh = peakBundle.LoadAsset<Mesh>("UnnamedPoorManPuddingPackaging.fbx");
                    }
                }

                break;
            }
            case "Poutine":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "plastic")
                    {
                        filt.mesh = peakBundle.LoadAsset<Mesh>("UnnamedPoutinePackaging.fbx");
                    }
                }

                break;
            }
            case "Sugar Fudge":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "SugarFudge")
                    {
                        filt.mesh = peakBundle.LoadAsset<Mesh>("UnnamedPackagedSugarFudge.fbx");
                    }
                }
                
                foreach (var hnds in it.GetComponentsInChildren<HandVisual>(true))
                {
                    if (hnds.gameObject.name == "Hand_L")
                    {
                        hnds.transform.localPosition = new Vector3(-0.42899999f, 0.00439999998f, 0.217999995f);
                        hnds.transform.localRotation = new Quaternion(0.0922728255f, 0.701060474f, 0.701060474f,
                            -0.0922728255f);
                    }
                    if (hnds.gameObject.name == "Hand_R")
                    {
                        hnds.transform.localPosition = new Vector3(0.42899999f, 0.00439999998f, 0.217999995f);
                        hnds.transform.localRotation = new Quaternion(-0.0923201293f, 0.701054215f, 0.701054215f,
                            0.0923201293f);
                    }
                }
                
                var g = peakBundle.LoadAsset<GameObject>("UnnamedSugarFudgeWrapper.prefab");
                
                var plasticBox = Object.Instantiate(g, it.mainRenderer.transform, false);

                plasticBox.transform.localPosition = new Vector3(0, 0, 0.0170000009f);
                plasticBox.transform.localScale = Vector3.one;
                plasticBox.transform.localRotation = Quaternion.identity;
                
                ShaderExtensions.ReplaceShaders(plasticBox);

                foreach (var componentsInChild in plasticBox.GetComponentsInChildren<Renderer>(true))
                {

                    it.addtlRenderers = it.addtlRenderers.AddToArray(componentsInChild);

                }

                it.mainRenderer.transform.localRotation = new Quaternion(-0.258819014f, 0, 0, 0.965925872f);

                it.offsetLuggagePosition = new Vector3(0, 0.4f, -0.27f);
                it.offsetLuggageRotation = new Vector3(-120f, 0f, 180f);
                
                Object.Destroy(it.mainRenderer.GetComponent<Collider>());

                break;
            }
            case "Tourtiere":
            {
                
                var platePrefab = peakBundle.LoadAsset<GameObject>("UnnamedTourtiereAluminiumPlate.prefab");
                var labelPrefab = peakBundle.LoadAsset<GameObject>("UnnamedTourtiereLabel.prefab");

                var tourtierePlate = Object.Instantiate(platePrefab, it.mainRenderer.transform, false);
                var tourtiereLabel = Object.Instantiate(labelPrefab, it.mainRenderer.transform, false);
                
                ShaderExtensions.ReplaceShaders(tourtierePlate);
                ShaderExtensions.ReplaceShaders(tourtiereLabel);
                
                it.addtlRenderers = it.addtlRenderers.AddToArray(tourtierePlate.GetComponent<Renderer>())
                    .AddToArray(tourtiereLabel.GetComponent<Renderer>());

                break;
            }
        }
        
    }

    private static string CleanItemName(string dirtyName)
    {
        return dirtyName.Replace("com.github.MiiMii1205.CanadianCuisine:", "").Replace("(Clone)", "")
            .Replace("(Instanced)", "");
    }

    public static void ApplyUnnamedModifierToAffliction(ref Affliction affliction)
    {
        var affType = affliction.GetType();
        
        if (affType.Name.Contains("AfflictionWithConsequence"))
        {
            
            var mainAfflictionField = affType.GetField("mainAffliction"); 
            var consequentAfflictionField = affType.GetField("consequentAffliction");

            var mainAffliction = (Affliction) mainAfflictionField.GetValue(affliction);
            var consequentAffliction = (Affliction) consequentAfflictionField.GetValue(affliction);

            UnnamedPlugin.ApplyUnnamedModifierToAffliction(ref mainAffliction);
            UnnamedPlugin.ApplyUnnamedModifierToAffliction(ref consequentAffliction);

            mainAfflictionField.SetValue(affliction,mainAffliction);
            consequentAfflictionField.SetValue(affliction, consequentAffliction);
            
        } else if (affType.Name.Contains("AfflictionHighJump"))
        {
            var highJumpMultiplierField = affType.GetField("highJumpMultiplier");
            
            var highJumpMultiplier = ((float) highJumpMultiplierField.GetValue(affliction)) * UnnamedPlugin.RandomUnnamedModifier;
            UnnamedPlugin.Log.LogInfo($"High Jump multiplier from {(float) highJumpMultiplierField.GetValue(affliction)} to {highJumpMultiplier}");

            highJumpMultiplierField.SetValue(affliction, highJumpMultiplier);

        }
        
    }
}