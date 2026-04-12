using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnnamedProducts.Behaviours;
using UnnamedProducts.Behaviours.Item;
using UnnamedProducts.Behaviours.Item.GarbageBag;
using UnnamedProducts.Behaviours.Item.GarbageBag.GUI;
using UnnamedProducts.Compatibility;
using UnnamedProducts.Patchers;
using Peak.Afflictions;
using PEAKLib.Core;
using PEAKLib.Core.Extensions;
using PEAKLib.Items;
using pworld.Scripts.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zorro.Core.CLI;
using Random = UnityEngine.Random;

namespace UnnamedProducts;

[BepInAutoPlugin]
[BepInDependency(ItemsPlugin.Id)]
[BepInDependency(CorePlugin.Id)]
[BepInDependency(PiggyBank.Plugin.Id, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.github.MiiMii1205.CanadianCuisine", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.github.BurningSulphur.Scouting4Food", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("evaisa.PeakThings", BepInDependency.DependencyFlags.SoftDependency)]
public partial class UnnamedPlugin : BaseUnityPlugin
{
    public const float UnnamedModifier = 50 / 100f;
    public const float UnnamedChance = 0.25f;
    public const float UnnamedLuggageChance = 0.125f;

    public const byte UnnamedTotalBaseDataEntryKey = ((byte) DataEntryKey.Scale) + 1;
    internal static ManualLogSource Log { get; private set; } = null!;
    internal static PluginInfo UnnamedInfo { get; private set; } = null!;

    private static readonly Dictionary<string, GameObject> UnnamedDatabase = new();

    public static Material SmallLuggageMaterial { get; private set; } = null!;
    public static Material SmallLuggageInteriorMaterial { get; private set; } = null!;
    public static Material LargeLuggageMaterial { get; private set; } = null!;
    public static Material EpicLuggageMaterial { get; private set; } = null!;
    public static Material LargeLuggageInteriorMaterial { get; private set; } = null!;
    public static Material AncientLuggageMaterial { get; private set; } = null!;
    public static Material AncientLuggageMetalMaterial { get; private set; } = null!;
    public static Material AncientLuggageCrystalMaterial { get; private set; } = null!;
    public static Material RespawnStatueCalderaMaterial { get; private set; } = null!;
    public static Material RespawnStatueAlpineMaterial { get; private set; } = null!;
    public static Material RespawnStatueMesaMaterial { get; private set; } = null!;
    public static Material RespawnStatueRockVFXMaterial { get; private set; } = null!;
    public static Material RespawnStatueRootsMaterial { get; private set; } = null!;
    public static Material RespawnStatueTropicsMaterial { get; private set; } = null!;
    public static Material RespawnStatueShoreMaterial { get; private set; } = null!;
    public static SpawnPool UnnamedSpawnPool { get; private set; } = 0;

    public static GameObject BigUnnamedLuggagePrefab { get; private set; } = null!;
    public static GameObject SmallUnnamedLuggagePrefab { get; private set; } = null!;
    public static GameObject EpicUnnamedLuggagePrefab { get; private set; } = null!;
    public static GameObject AncientUnnamedLuggagePrefab { get; private set; } = null!;
    private static TMP_FontAsset? _darumaFontAsset;

    public static TMP_FontAsset DarumaDropOne
    {
        get
        {
            if (_darumaFontAsset == null)
            {
                var assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                _darumaFontAsset = assets.FirstOrDefault(fontAsset =>
                    fontAsset.faceInfo.familyName == "Daruma Drop One"
                );

                Log.LogInfo("Daruma Drop One font found!");
            }

            return _darumaFontAsset!;
        }
    }

    private static Material? _darumaShadowMaterial;

    public static Material DarumaDropOneShadowMaterial
    {
        get
        {
            if (_darumaShadowMaterial == null)
            {
                _darumaShadowMaterial = ThrowHelper.ThrowIfArgumentNull(GameObject.Instantiate(DarumaDropOne.material));

                _darumaShadowMaterial.EnableKeyword("UNDERLAY_ON");
                _darumaShadowMaterial.SetFloat(UnderlayDilate, 1f);
                _darumaShadowMaterial.SetFloat(UnderlayOffsetY, -0.7f);
                _darumaShadowMaterial.SetFloat(UnderlaySoftness, 1f);
                _darumaShadowMaterial.SetColor(UnderlayColor, new Color(0f, 0f, 0f, 0.1960784f));

                Log.LogInfo("Shadow material for Critial Hit indicator was successfully generated!");
            }

            return _darumaShadowMaterial;
        }
    }


    public static GameObject UnnamedGarbageBagPrefab = null!;
    public static Texture2D GarbageBagIcon = null!;

    private void Awake()
    {
        // BepInEx gives us a logger which we can use to log information.
        // See https://lethal.wiki/dev/fundamentals/logging
        Log = Logger;

        UnnamedInfo = Info;

        AddLocalizedTextCsv();

        int max = 0;
        foreach (int v in Enum.GetValues(typeof(SpawnPool)))
        {
            if (v > max)
            {
                max = v;
            }
        }

        UnnamedSpawnPool = (SpawnPool) (max * 2);

        Log.LogInfo($"Highest spawn pool: {max}. Using {max * 2} as the unnamed pool");

        Log.LogInfo($"Plugin {Name} is loading...");


        GlobalEvents.OnItemRequested += CheckForStickyFireballs;

        this.LoadBundleWithName("unnamed.peakbundle", peakBundle =>
        {
            var antifreese = peakBundle.LoadAsset<GameObject>("Antifreeze.prefab");

            Log.LogInfo($"Loading Antifreeze...");

            foreach (var comp in antifreese.GetComponentsInChildren<MeshRenderer>())
            {
                comp.sortingOrder = 1;

                if (comp.sharedMaterial.shader.name == "Shader Graphs/LiquidEffect" ||
                    comp.material.shader.name == "Shader Graphs/LiquidEffect")
                {
                    // Add a Liquid controller to the renderer.
                    var liquidGameObject = comp.gameObject;
                    var liqu = liquidGameObject.GetOrAddComponent<LiquidController>();

                    Log.LogInfo($"Initializing Liquid Controller...");

                    liqu.mesh = liquidGameObject.GetComponent<MeshFilter>().sharedMesh;
                    liqu.rend = comp;

                    liqu.compensateShapeAmount = 0.132f;
                    liqu.Thickness = 0.5f;
                    liqu.fillAmount = 0.4f;
                    liqu.Recovery = 1;
                    liqu.MaxWobble = 0.03f;
                    liqu.WobbleSpeedMove = 0.5f;
                }
            }

            var exbrnact = antifreese.AddComponent<ExplodeOnBurningAction>();
            exbrnact.dontRunIfOutOfFuel = false;

            exbrnact.OnCastFinished = true;

            var restrc = antifreese.GetOrAddComponent<UnnamedSpawnRestriction>();
            restrc.biomeType = restrc.biomeType.AddToArray(Biome.BiomeType.Alpine);
            restrc.whenNightIsCold = true;
            restrc.hasColdNightRestrictions = true;

            var pool = antifreese.GetOrAddComponent<LootData>();
            pool.spawnLocations = UnnamedSpawnPool;


            Log.LogInfo($"Loading sticky fireball...");

            var stickyFireball = peakBundle.LoadAsset<GameObject>("StickyFireball.prefab");
            var sfc = stickyFireball.GetOrAddComponent<StickyFireballController>();

            sfc.destroyAfterTime = stickyFireball.GetComponent<DestroyAfterTime>();
            sfc.rb = stickyFireball.GetComponent<Rigidbody>();

            var audioS = sfc.GetComponent<AudioSource>();

            // Get the fire clip

            if (ItemDatabase.TryGetItem(62,
                    out var st))
            {
                var clip = st.GetComponent<Constructable>().constructedPrefab.gameObject.GetComponent<AudioSource>()
                    .clip;

                audioS.clip = clip;
                var cpf = st.GetComponent<Constructable>().constructedPrefab.GetComponentInChildren<Campfire>();
                var sfxiLight = cpf.fireStart;
                var sfxiExtinguish = cpf.extinguish;

                sfc.extinguish = sfxiExtinguish;
                sfc.fireStart = sfxiLight;
            }

            audioS.SetCustomCurve(AudioSourceCurveType.CustomRolloff,
                new AnimationCurve(
                [
                    new Keyframe(0.002f, 1f, -500.2007f, -500.2007f, 0, 0),
                    new Keyframe(0.004f, 0.5f, -125.0502f, -125.0502f, 0, 0),
                    new Keyframe(0.008f, 0.25f, -31.2625f, -31.2625f, 0, 0),
                    new Keyframe(0.016f, 0.125f, -7.8156f, -7.8156f, 0, 0),
                    new Keyframe(0.032f, 0.0625f, -1.9539f, -1.9539f, 0, 0),
                    new Keyframe(0.064f, 0.03125f, -0.4885f, -0.4885f, 0, 0),
                    new Keyframe(0.128f, 0.015625f, -0.1221f, -0.1221f, 0, 0),
                    new Keyframe(0.256f, 0.0078125f, -0.0305f, -0.0305f, 0, 0),
                    new Keyframe(0.512f, 0.00390625f, -0.00076f, -0.00076f, 0, 0),
                    new Keyframe(1, 0, -0.002f, -0.002f, 0, 0),
                ]));

            audioS.SetCustomCurve(AudioSourceCurveType.SpatialBlend,
                new AnimationCurve(
                [
                    new Keyframe(0, 1f, 0, 0, 0.3333f, 0.3333f),
                ]));

            audioS.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix,
                new AnimationCurve(
                [
                    new Keyframe(0, 1f, 0, 0, 0.3333f, 0.3333f),
                ]));


            audioS.SetCustomCurve(AudioSourceCurveType.Spread,
                new AnimationCurve(
                [
                    new Keyframe(0, 1f, 0, 0, 0.3333f, 0.3333f),
                ]));


            foreach (var renderer in sfc.GetComponentsInChildren<Renderer>(true))
            {
                var mats = renderer.materials;

                for (int index = 0, length = mats.Length; index < length; ++index)
                {
                    var mat = mats[index];

                    mat.shader =
                        ThrowHelper.ThrowIfArgumentNull(Shader.Find(mat.shader.name));
                }

                renderer.SetMaterials([..mats]);
            }

            if (ItemDatabase.TryGetItem(35,
                    out var item))
            {
                var cook = antifreese.GetOrAddComponent<ItemCooking>();

                var expl = peakBundle.LoadAsset<GameObject>("AntifreezeExplosion.prefab");

                var aex = expl.AddComponent<AntifreezeExplosionController>();

                aex.stickyFireball = sfc.gameObject;
                aex.m_amountOfFireballs = 20;

                var exx = Instantiate(item.GetComponent<ItemCooking>().explosionPrefab);

                exx.SetActive(false);
                DontDestroyOnLoad(exx);

                aex.explosionPrefab = exx;

                cook.explosionPrefab = aex.gameObject;

                NetworkPrefabManager.RegisterNetworkPrefab(ModDefinition.GetOrCreate(Info), aex.gameObject);
                NetworkPrefabManager.RegisterNetworkPrefab(ModDefinition.GetOrCreate(Info), exx);
            }

            StickyFireballController.FireballPrefab = sfc.gameObject;
            NetworkPrefabManager.RegisterNetworkPrefab(ModDefinition.GetOrCreate(Info), sfc.gameObject);

            UnnamedUniques.Add(antifreese.name.Trim());

            // Detergent

            var detergent = peakBundle.LoadAsset<GameObject>("LaundryDetergent.prefab");

            var det = detergent.GetOrAddComponent<Action_Detergent>();
            det.OnCastFinished = true;

            var drestrc = detergent.GetOrAddComponent<UnnamedSpawnRestriction>();

            drestrc.biomeType = drestrc.biomeType.AddToArray(Biome.BiomeType.Roots).AddToArray(Biome.BiomeType.Mesa)
                .AddToArray(Biome.BiomeType.Alpine);

            var dpool = detergent.GetOrAddComponent<LootData>();
            dpool.spawnLocations = UnnamedSpawnPool;


            UnnamedUniques.Add(detergent.name.Trim());

            // Garbage bag

            var bag = peakBundle.LoadAsset<GameObject>("GarbageBag.prefab");

            ItemDatabase.TryGetItem(91, out var bananaPeel);

            var bp = bananaPeel.GetComponent<BananaPeel>();

            var garbageBagC = bag.GetOrAddComponent<UnnamedGarbageBagController>();
            var garbageBagS = bag.GetOrAddComponent<UnnamedGarbageBagSlips>();
            var garbageBagAudio = bag.GetOrAddComponent<UnnamedGarbageBagAudio>();
            var garbageBagCook = bag.GetOrAddComponent<ItemCooking>();

            if (bag.TryGetComponent(out BackPackAudio backpackAudio))
            {
                garbageBagAudio.holdSFX = backpackAudio.holdSFX;
                garbageBagAudio.dropSFX = backpackAudio.dropSFX;

                Destroy(backpackAudio);
            }

            garbageBagC.slipSDSfxInstances = [bp.slipSFX[0]];

            // garbageBagCook.wreckWhenCooked = true;
            garbageBagCook.additionalCookingBehaviors = garbageBagCook.additionalCookingBehaviors.AddToArray(
                new CookingBehavior_DestroyAndReleaseGarbageBagItems()
                {
                    cookedAmountToTrigger = 1
                });

            garbageBagS.garbageBag = garbageBagC;
            garbageBagS.rig = bag.GetComponent<Rigidbody>();

            garbageBagS.slipOnCollision = true;
            garbageBagS.minSlipVelocity = 15f;

            garbageBagS.ragdollCharacterOnSlip = false;
            garbageBagS.pushForce = 2;
            garbageBagS.wholeBodyPushForce = 1;

            garbageBagC.fullBagGameObject = bag.transform.Find(@"GarbageBag/filled").gameObject;
            garbageBagC.emptyBagGameObject = bag.transform.Find(@"GarbageBag/empty").gameObject;

            garbageBagC.rightHandEmptyPosition = new Vector3(0.194100007f, 0.179000005f, -0.300900012f);
            garbageBagC.leftHandEmptyPosition = new Vector3(-0.187000006f, 0.179000005f, -0.291999996f);

            garbageBagC.rightHandFullPosition = new Vector3(0.407000005f, 0.179006964f, -0.42899999f);
            garbageBagC.leftHandFullPosition = new Vector3(-0.377000004f, 0.179000005f, -0.428999662f);

            garbageBagC.fullBagGameObject.SetActive(true);

            ShaderExtensions.ReplaceShaders(garbageBagC.fullBagGameObject);

            garbageBagC.fullBagGameObject.SetActive(false);

            garbageBagC.defaultPos = new Vector3(0f, -0.3f, 1.3166288f);

            garbageBagC.defaultPosFilled = new Vector3(0f, -0.3f, 1.3166288f);
            garbageBagC.defaultPosEmpty = new Vector3(0f, -0.3f, 0.81002724f);

            garbageBagC.defaultForward = new Vector3(0f, 0f, 1f);

            garbageBagC.mainRenderer = garbageBagC.fullBagGameObject.GetComponent<Renderer>();
            garbageBagC.addtlRenderers = [garbageBagC.emptyBagGameObject.GetComponent<Renderer>()];

            var bagV = bag.GetOrAddComponent<UnnamedGarbageBagVisuals>();

            bagV.item = garbageBagC;

            garbageBagC.m_unnamedGarbageBagVisuals = bagV;

            garbageBagC.openRadialMenuTime = 0f;

            GarbageBagIcon = peakBundle.LoadAsset<Texture2D>("GarbageBag Icon.png");

            garbageBagC.carryWeight = 1;

            garbageBagC.mass = 50;

            garbageBagC.UIData = new Item.ItemUIData
            {
                itemName = "Garbage Bag",
                icon = GarbageBagIcon,
                hasAltIcon = false,
                hasColorBlindIcon = false,
                altIcon = null,
                hasMainInteract = false,
                mainInteractPrompt = "OPEN",
                hasSecondInteract = false,
                secondaryInteractPrompt = null,
                hasScrollingInteract = false,
                scrollInteractPrompt = null,
                canDrop = true,
                canPocket = true,
                canBackpack = true,
                canThrow = true,
                isShootable = false,
                hideFuel = true,
                iconPositionOffset = default,
                iconRotationOffset = default,
                iconScaleOffset = 1.49f
            };

            garbageBagC.totalUses = -1;

            UnnamedUniques.Add(bag.name.Trim());


            // Garbage Bag Box

            var garbageBagBoxP = peakBundle.LoadAsset<GameObject>("GarbageBagBox.prefab");

            var spawnAndGrab = garbageBagBoxP.GetOrAddComponent<Action_SpawnAndGrab>();

            spawnAndGrab.itemToSpawn = bag.GetComponent<Item>();
            spawnAndGrab.OnCastFinished = true;

            var gbbpool = garbageBagBoxP.GetOrAddComponent<LootData>();
            gbbpool.spawnLocations = UnnamedSpawnPool;

            UnnamedUniques.Add(garbageBagBoxP.name.Trim());


            SmallLuggageMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLuggage.mat");
            SmallLuggageInteriorMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLuggage_interior.mat");
            EpicLuggageMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLuggage_epic.mat");
            LargeLuggageMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLuggage_large.mat");
            LargeLuggageInteriorMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLuggage_interior_large.mat");
            AncientLuggageMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLuggage_ancient.mat");
            AncientLuggageMetalMaterial = peakBundle.LoadAsset<Material>("M_UnnamedMetal.mat");
            AncientLuggageCrystalMaterial = peakBundle.LoadAsset<Material>("M_UnnamedRock_Crystal.mat");

            RespawnStatueCalderaMaterial = peakBundle.LoadAsset<Material>("M_UnnamedRock_Volcano.mat");
            RespawnStatueAlpineMaterial = peakBundle.LoadAsset<Material>("M_UnnamedRock_peak Snow.mat");
            RespawnStatueMesaMaterial = peakBundle.LoadAsset<Material>("M_UnnamedDesertSand.mat");
            RespawnStatueRootsMaterial = peakBundle.LoadAsset<Material>("M_UnnamedForest_rock.mat");
            RespawnStatueTropicsMaterial = peakBundle.LoadAsset<Material>("M_UnnamedRock 1.mat");
            RespawnStatueShoreMaterial = peakBundle.LoadAsset<Material>("M_UnnamedSaltRock.mat");

            RespawnStatueRockVFXMaterial = peakBundle.LoadAsset<Material>("M_UnnamedRock_staticTopColour.mat");

            // Fetch plane materials

            PlaneMaterials = new Dictionary<string, Material>();

            string[] materialList =
            [
                "Plane",
                "Plane 1",
                "Plane 3",
                "Plane 4",
                "Plane cleaner",
                "Plane Glass",
                "Plane seat",
                "Plane-snow",
                "PlaneWings",
                "PlaneWings-snow",
            ];

            foreach (var matName in materialList)
            {
                var planeMaterial = peakBundle.LoadAsset<Material>($"M_Unnamed{matName}.mat");

                planeMaterial.shader =
                    ThrowHelper.ThrowIfArgumentNull(Shader.Find(planeMaterial.shader.name));

                Log.LogInfo($"Added plane material named {planeMaterial.name}.");

                PlaneMaterials[matName] = planeMaterial;
            }

            // Fetch mirrage materials

            MirageMaterials = new Dictionary<string, Material>();

            string[] mirageMaterialList =
            [
                "Luggage_Large_Mirage",
                "Luggage_Ancient_Mirage",
                "Luggage_AncientChain_Mirage",
                "Luggage_AncientCrystal_Mirage",
                "Luggage-mirage",
                "Luggage_Epic_Mirage",
            ];

            foreach (var matName in mirageMaterialList)
            {
                var mirageMaterial = peakBundle.LoadAsset<Material>($"M_Unnamed{matName}.mat");

                mirageMaterial.shader =
                    ThrowHelper.ThrowIfArgumentNull(Shader.Find(mirageMaterial.shader.name));

                Log.LogInfo($"Added mirage material named {mirageMaterial.name}.");

                MirageMaterials[matName] = mirageMaterial;
            }

            SmallLuggageMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(SmallLuggageMaterial.shader.name));
            SmallLuggageInteriorMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(SmallLuggageInteriorMaterial.shader.name));
            EpicLuggageMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(EpicLuggageMaterial.shader.name));
            LargeLuggageMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(LargeLuggageMaterial.shader.name));
            LargeLuggageInteriorMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(LargeLuggageInteriorMaterial.shader.name));
            AncientLuggageMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(AncientLuggageMaterial.shader.name));
            AncientLuggageMetalMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(AncientLuggageMetalMaterial.shader.name));
            AncientLuggageCrystalMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(AncientLuggageCrystalMaterial.shader.name));
            RespawnStatueRockVFXMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueRockVFXMaterial.shader.name));

            RespawnStatueCalderaMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueCalderaMaterial.shader.name));
            RespawnStatueAlpineMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueAlpineMaterial.shader.name));
            RespawnStatueMesaMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueMesaMaterial.shader.name));
            RespawnStatueRootsMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueRootsMaterial.shader.name));
            RespawnStatueTropicsMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueTropicsMaterial.shader.name));
            RespawnStatueShoreMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(RespawnStatueShoreMaterial.shader.name));

            Log.LogInfo($"Added material named {SmallLuggageMaterial.name}.");
            Log.LogInfo($"Added material named {SmallLuggageInteriorMaterial.name}.");
            Log.LogInfo($"Added material named {EpicLuggageMaterial.name}.");
            Log.LogInfo($"Added material named {LargeLuggageMaterial.name}.");
            Log.LogInfo($"Added material named {LargeLuggageInteriorMaterial.name}.");
            Log.LogInfo($"Added material named {AncientLuggageMaterial.name}.");
            Log.LogInfo($"Added material named {AncientLuggageMetalMaterial.name}.");
            Log.LogInfo($"Added material named {AncientLuggageCrystalMaterial.name}.");
            Log.LogInfo($"Added material named {RespawnStatueRockVFXMaterial.name}.");

            Log.LogInfo($"Added material named {RespawnStatueCalderaMaterial.name}.");
            Log.LogInfo($"Added material named {RespawnStatueAlpineMaterial.name}.");
            Log.LogInfo($"Added material named {RespawnStatueMesaMaterial.name}.");
            Log.LogInfo($"Added material named {RespawnStatueRootsMaterial.name}.");
            Log.LogInfo($"Added material named {RespawnStatueTropicsMaterial.name}.");
            Log.LogInfo($"Added material named {RespawnStatueShoreMaterial.name}.");

            List<ushort> allUnnamedVariantsIdLists =
            [
                0, 1, 2, 7, 115, 17, 18, 24, 106, 27, 29, 32, 152, 99, 33, 70, 35, 42, 43, 44, 58, 98, 61, 62, 100, 63,
                64, 65, 107, 66, 67, 71, 104, 73
            ];


            for (int i = 0, length = allUnnamedVariantsIdLists.Count; i < length; ++i)
            {
                var itemId = allUnnamedVariantsIdLists[i];

                if (ItemDatabase.TryGetItem(itemId, out var it))
                {
                    var originalItemName = it.UIData.itemName;

                    var newVariant = GenerateInstancedVariant(it.gameObject, CleanItemName(it.gameObject.name));

                    var newIt = newVariant.gameObject.GetComponent<Item>();

                    CopyUnnamedMaterials(ref newVariant, peakBundle);
                    CopyUnnamedIcons(ref newIt, peakBundle);
                    PopulateUnnamedData(ref newIt, peakBundle);
                    CopyExplosionData(ref newIt, peakBundle);

                    // Strip LootData

                    var ld = newVariant.gameObject.GetComponent<LootData>();
                    Destroy(ld);

                    AddItemToDatabase(originalItemName, newVariant.gameObject);

                    // Hotfix for PeakThings
                    if (originalItemName == "Lantern")
                    {
                        newIt.gameObject.name += "_lant";
                    }

                    (new ItemContent(newIt)).Register(ModDefinition.GetOrCreate(Info));
                }
            }

            // Generate Unnamed Luggage 

            BigUnnamedLuggagePrefab = GenerateUnnamedLuggagePrefab("0_Items/LuggageBig");
            SmallUnnamedLuggagePrefab = GenerateUnnamedLuggagePrefab("0_Items/LuggageSmall");
            EpicUnnamedLuggagePrefab = GenerateUnnamedLuggagePrefab("0_Items/LuggageEpic");
            AncientUnnamedLuggagePrefab = GenerateUnnamedLuggagePrefab("0_Items/LuggageAncient");


            // Airport setup
            var setup = peakBundle.LoadAsset<GameObject>("UnnamedAirportSetup.prefab");

            ShaderExtensions.ReplaceShaders(setup);

            var text = setup.GetComponentInChildren<TextMeshProUGUI>(true);

            text.font = DarumaDropOne;
            text.lineSpacing = -50f;
            text.fontSharedMaterial =
                text.fontMaterial = text.material = DarumaDropOneShadowMaterial;

            var ltext = text.gameObject.GetOrAddComponent<LocalizedText>();

            ltext.fontStyle = LocalizedText.FontStyle.Shadow;

            ltext.index = "KIOSK_READY_TEXT";
            ltext.tmp = text;
            ltext.autoSet = true;

            UnnamedKioskSetup = setup;

            // Broken standees on island
            var brkStdSetup = peakBundle.LoadAsset<GameObject>("UnnamedBrokenStandee.prefab");

            ShaderExtensions.ReplaceShaders(brkStdSetup);

            UnnamedBrokenStandeeSetup = brkStdSetup;

            peakBundle.Mod.RegisterContent();

            Log.LogInfo("Unnamed items are loaded!");
        });

        this.LoadBundleWithName("unnamedui.peakbundle", bundle =>
        {
            var garbageBagScreen = bundle.LoadAsset<GameObject>("UnnamedGarbageBagWheel.prefab");

            var gbScr = garbageBagScreen.AddComponent<UnnamedGarbageBagScreen>();

            gbScr.maxCursorDistance = 190;
            gbScr.chosenItemText = gbScr.transform.Find(@"SelectedItemName").GetComponent<TextMeshProUGUI>();
            gbScr.currentlyHeldItem = gbScr.transform.Find(@"HeldItem").GetComponent<RawImage>();

            ShaderExtensions.ReplaceShaders(garbageBagScreen);

            for (int i = 0, length = garbageBagScreen.transform.Find(@"Wheel").childCount; i < length; i++)
            {
                var child = garbageBagScreen.transform.Find(@"Wheel").GetChild(i);

                if (child.name == "UI_PickupZone")
                {
                    // Pick up zone

                    var pickUpZone = child.gameObject
                        .GetOrAddComponent<UnnamedGarbageBagZone>();

                    pickUpZone.image = pickUpZone.transform.Find(@"Segment").GetComponentInChildren<RawImage>();
                    pickUpZone.button = pickUpZone.GetComponent<Button>();

                    gbScr.pickupZone = pickUpZone;

                    if (child.gameObject.TryGetComponent<BackpackWheelSlice>(out var bkcs))
                    {
                        Destroy(bkcs);
                    }
                }

                else
                {
                    // normal stash zone
                    var stashZone = child.gameObject
                        .GetOrAddComponent<UnnamedGarbageBagZone>();

                    stashZone.image = stashZone.transform.Find(@"Segment").GetComponentInChildren<RawImage>();
                    stashZone.button = stashZone.GetComponent<Button>();

                    gbScr.garbageBagZones = gbScr.garbageBagZones.AddToArray(stashZone);

                    if (child.gameObject.TryGetComponent<BackpackWheelSlice>(out var bkcs))
                    {
                        Destroy(bkcs);
                    }
                }
            }

            UnnamedGarbageBagPrefab = gbScr.gameObject;

            bundle.Mod.RegisterContent();

            Log.LogInfo("Garbage Bag UI is loaded!");
        });

        var harmony = new Harmony(Id);

        if (Unnamed4FoodCompatibility.enabled)
        {
            Unnamed4FoodCompatibility.LoadCompatibilityBundle(this);
        }

        if (UnnamedCanadianCompatibility.enabled)
        {
            UnnamedCanadianCompatibility.LoadCompatibilityBundle(this);
        }

        if (UnnamedPiggyBankCompatibility.enabled)
        {
            UnnamedPiggyBankCompatibility.LoadCompatibilityBundle(this, harmony);
        }


        harmony.PatchAll(typeof(UnnamedItemPatcher));
        harmony.PatchAll(typeof(UnnamedPatcher));
        harmony.PatchAll(typeof(UnnamedSpawnerPatcher));

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    public GameObject UnnamedKioskSetup { get; private set; } = null!;

    public GameObject UnnamedBrokenStandeeSetup { get; private set; } = null!;

    private static string CleanItemName(string dirtyName)
    {
        return dirtyName.Replace($"{ModDefinition.GetOrCreate(UnnamedInfo).Id}:", "").Replace("(Clone)", "")
            .Replace("(Instanced)", "");
    }

    private GameObject GenerateUnnamedLuggagePrefab(string normalPrefabName)
    {
        var newLuggage = Resources.Load<GameObject>(normalPrefabName);

        var brand = newLuggage.AddComponent<LuggageBrandHandler>();

        brand.shouldBeUnnamed = true;
        brand.luggage = newLuggage.GetComponent<Luggage>();

        newLuggage.SetActive(false);

        DontDestroyOnLoad(newLuggage);

        return RegisterOrGetNetworkPrefab(newLuggage);
    }

    public GameObject RegisterOrGetNetworkPrefab(GameObject prefab, string folder = "")
    {
        var spliting = prefab.name.Split(":").Last();
        var prfNm = $"{folder}{ModDefinition.GetOrCreate(Info).Id}:{spliting}";
        if (NetworkPrefabManager.TryRegisterNetworkPrefab(prfNm, prefab))
        {
            prefab.name = prfNm;
            return prefab;
        }

        if (NetworkPrefabManager.TryGetNetworkPrefab(prfNm, out var pr) && pr != null)
        {
            return pr;
        }

        throw new Exception("Failed to get network prefab!");
    }

    public GameObject RegisterOrGetItemPrefab(GameObject prefab)
    {
        RegisterOrGetNetworkPrefab(prefab, "0_Items/");
        prefab.name = prefab.name.Replace("0_Items/", "");
        return prefab;
    }

    public static void RenameToUnnamed(GameObject ob, bool withPrefix = true)
    {
        ob.name =
            $"{ModDefinition.GetOrCreate(UnnamedInfo).Id}:{(withPrefix ? "Unnamed" : "")}{ob.name.Replace("Unnamed", "")}";
    }

    public void CopyExplosionData(ref Item newIt, PeakBundle peakBundle)
    {
        if (newIt.gameObject.TryGetComponent<ItemCooking>(out var cook) && cook.explosionPrefab)
        {
            var exp = Instantiate(cook.explosionPrefab);
            exp.name = "Unnamed" + cook.explosionPrefab.name.Replace("Unnamed", "");


            if (exp.GetComponentInChildren<AOE>() is { } aoe && !IsUnnamed(aoe.gameObject))
            {
                RenameToUnnamed(aoe.gameObject);
            }

            cook.explosionPrefab = RegisterOrGetNetworkPrefab(exp);

            DontDestroyOnLoad(exp);

            exp.SetActive(false);
        }

        if (newIt.gameObject.TryGetComponent<Breakable>(out var brk))
        {
            for (var i = 0; i < brk.instantiateNonItemOnBreak.Count; i++)
            {
                var exp = GameObject.Instantiate(brk.instantiateNonItemOnBreak[i]);
                exp.name = "Unnamed" + brk.instantiateNonItemOnBreak[i].name.Replace("Unnamed", "");

                if (exp.GetComponentInChildren<AOE>() is { } aoe && !IsUnnamed(aoe.gameObject))
                {
                    RenameToUnnamed(aoe.gameObject);
                }

                brk.instantiateNonItemOnBreak[i] = RegisterOrGetNetworkPrefab(exp);

                DontDestroyOnLoad(exp);

                exp.SetActive(false);
            }
        }
    }

    private static void CheckForStickyFireballs(Item item, Character chara)
    {
        if (item && item.GetComponentInChildren<StickyFireballController>() is {m_isBurning: true})
        {
            // BURNING! 

            StickyFireballController.Burn(
                chara.GetBodypart(Random.value > 0.5 ? BodypartType.Hand_L : BodypartType.Hand_R));
        }
    }

    public void PopulateUnnamedData(ref Item it, PeakBundle peakBundle)
    {
        switch (it.UIData.itemName)
        {
            case "Frisbee":
            {
                var frisbee = it.GetComponent<Frisbee>();

                var frisbeeC = it.gameObject.GetOrAddComponent<UnnamedFrisbeeItem>();

                frisbeeC.frisbee = frisbee;
                frisbeeC.originalLiftForce = frisbee.liftForce;
                frisbeeC.originalVelocityForLift = frisbee.velocityForLift;

                break;
            }
            case "Portable Stove":
            {
                var ct = it.GetComponent<Constructable>();

                var portStove = GameObject.Instantiate(ct.constructedPrefab);
                portStove.name = "Unnamed" + ct.constructedPrefab.name;

                DontDestroyOnLoad(portStove);

                var g = peakBundle.LoadAsset<GameObject>("UnnamedStovePaint.prefab");

                foreach (var filt in portStove.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "Cube" && filt.TryGetComponent<MeshCollider>(out _))
                    {
                        var stovePaint = GameObject.Instantiate(g, filt.transform.parent, false);

                        stovePaint.transform.localPosition = Vector3.zero;
                        stovePaint.transform.localScale = 3.827086f.ToVec();
                        stovePaint.transform.localRotation = new Quaternion(0, 0, 0, 1);

                        ShaderExtensions.ReplaceShaders(stovePaint);
                    }
                }

                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "Cube" && filt.TryGetComponent<MeshCollider>(out _))
                    {
                        var stovePaint = GameObject.Instantiate(g, filt.transform.parent, false);

                        stovePaint.transform.localPosition = new Vector3(0, 0, -4.95003296e-05f);
                        stovePaint.transform.localScale = 3.827086f.ToVec();
                        stovePaint.transform.localRotation = new Quaternion(0, 0, 0, 1);

                        ShaderExtensions.ReplaceShaders(stovePaint);
                    }
                }

                portStove.SetActive(false);

                CopyUnnamedMaterials(ref portStove, peakBundle);

                var oldComponent = portStove.GetComponent<Campfire>();
                var pts = portStove.GetOrAddComponent<UnnamedCampfire>();

                pts.advanceToSegment = oldComponent.advanceToSegment;
                pts.state = oldComponent.state;
                pts.enableWhenLit = oldComponent.enableWhenLit;
                pts.disableWhenLit = oldComponent.disableWhenLit;
                pts.burnsFor = oldComponent.burnsFor;
                pts.cookTime = oldComponent.cookTime;
                pts.logRoot = oldComponent.logRoot;
                pts.endSize = oldComponent.endSize;
                pts.endRot = oldComponent.endRot;
                pts.beenBurningFor = oldComponent.beenBurningFor;
                pts.fireParticles = oldComponent.fireParticles;
                pts.smokeParticlesOff = oldComponent.smokeParticlesOff;
                pts.smokeParticlesLit = oldComponent.smokeParticlesLit;
                pts.moraleBoostRadius = oldComponent.moraleBoostRadius;
                pts.moraleBoostBaseline = oldComponent.moraleBoostBaseline;
                pts.moraleBoostPerAdditionalScout = oldComponent.moraleBoostPerAdditionalScout;
                pts.injuryReduction = oldComponent.injuryReduction;
                pts.fireStart = oldComponent.fireStart;
                pts.extinguish = oldComponent.extinguish;
                pts.moraleBoost = oldComponent.moraleBoost;
                pts.loop = oldComponent.loop;
                pts.nameOverride = oldComponent.nameOverride;
                pts.currentlyCookingItem = oldComponent.currentlyCookingItem;
                pts.mainRenderer = oldComponent.mainRenderer;
                pts.startRot = oldComponent.startRot;
                pts.startSize = oldComponent.startSize;
                pts.fireHasStarted = oldComponent.fireHasStarted;
                pts.view = oldComponent.view;
                pts._timebuffLastApplied = oldComponent._timebuffLastApplied;
                pts.disableFogFakeMountain = oldComponent.disableFogFakeMountain;
                pts._charactersInRadius = oldComponent._charactersInRadius;

                Destroy(oldComponent);

                portStove = RegisterOrGetNetworkPrefab(portStove);

                ct.constructedPrefab = portStove;

                break;
            }
            case "Airline Food":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.mesh.name == "plastic")
                    {
                        filt.mesh = peakBundle.LoadAsset<Mesh>("UnnamedAirplaneFoodPackaging.fbx");
                        filt.transform.localPosition = new Vector3(0, 0, 0.279865861f);
                        filt.transform.localScale = 3.436053f.ToVec();
                        filt.transform.localRotation = new Quaternion(-0.707106709f, 2.08616257e-07f, -1.1920929e-07f,
                            -0.707106948f);
                    }
                }


                break;
            }
            case "Pirate's Compass":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "PirateHat")
                    {
                        filt.mesh = peakBundle.LoadAsset<Mesh>("UnnamedCompass.fbx");
                    }
                }

                break;
            }
            case "Trail Mix":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "TrailMix")
                    {
                        filt.mesh = peakBundle.LoadAsset<Mesh>("UnnamedTrailMix.fbx");

                        var renderer = filt.gameObject.GetComponent<Renderer>();

                        renderer.SetMaterials(
                        [
                            renderer.materials[0],
                            peakBundle.LoadAsset<Material>("M_UnnamedTrailMixOpaque.mat"),
                            renderer.materials[1]
                        ]);
                    }
                }


                break;
            }

            case "Sunscreen":
            {
                if (it.gameObject.TryGetComponent<Action_Spawn>(out var spawn))
                {
                    var exp = GameObject.Instantiate(spawn.objectToSpawn);
                    exp.name = "Unnamed" + spawn.objectToSpawn.name.Replace("Unnamed", "");

                    if (exp.GetComponentInChildren<AOE>() is { } aoe && !IsUnnamed(aoe.gameObject))
                    {
                        RenameToUnnamed(aoe.gameObject);
                    }

                    spawn.objectToSpawn = RegisterOrGetNetworkPrefab(exp);

                    DontDestroyOnLoad(exp);

                    exp.SetActive(false);
                }

                break;
            }

            case "Scout Cannon":
            {
                var ct = it.GetComponent<Constructable>();

                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "Rend")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedCannon.fbx");
                    }
                }

                var scoutCan = GameObject.Instantiate(ct.constructedPrefab);
                scoutCan.name = "Unnamed" + ct.constructedPrefab.name;

                foreach (var filt in scoutCan.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "CannonFeet")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedCannonFeets.fbx");
                        filt.transform.localRotation = Quaternion.identity;
                    }

                    if (filt.gameObject.name == "Rend")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedCannonBody.fbx");
                    }

                    if (filt.gameObject.name == "Rend2")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedCannonInnerWall.fbx");
                    }
                }


                DontDestroyOnLoad(scoutCan);

                scoutCan.SetActive(false);

                CopyUnnamedMaterials(ref scoutCan, peakBundle);

                var oldComponent = scoutCan.GetComponent<ScoutCannon>();
                var sct = scoutCan.GetOrAddComponent<UnnamedScoutCannon>();

                sct.launchForce = oldComponent.launchForce;
                sct.itemLaunchForce = oldComponent.itemLaunchForce;
                sct.backpackLaunchForce = oldComponent.backpackLaunchForce;
                sct.fallFor = oldComponent.fallFor;
                sct.pullForce = oldComponent.pullForce;
                sct.pushForce = oldComponent.pushForce;
                sct.lit = oldComponent.lit;
                sct.fireTime = oldComponent.fireTime;
                sct.litParticle = oldComponent.litParticle;
                sct.fireParticle = oldComponent.fireParticle;
                sct.fireSFX = oldComponent.fireSFX;
                sct.anim = oldComponent.anim;
                sct.mpb = oldComponent.mpb;
                sct.view = oldComponent.view;
                sct.tube = oldComponent.tube;
                sct.entry = oldComponent.entry;
                sct.target = oldComponent.target;
                sct.targetID = oldComponent.targetID;
                sct.characters = oldComponent.characters;
                sct._mr = oldComponent._mr;

                var fuse = scoutCan.GetComponentInChildren<ScoutCannonFuse>();

                var fuseGameObject = fuse.gameObject;

                CopyUnnamedMaterials(ref fuseGameObject, peakBundle);

                fuse.scoutCannon = sct;

                Destroy(oldComponent);

                scoutCan = RegisterOrGetNetworkPrefab(scoutCan);

                ct.constructedPrefab = scoutCan;

                break;
            }

            case "Piton":
            {
                var g = peakBundle.LoadAsset<GameObject>("UnnamedPitonPackaging.prefab");
                var pitonWrap = GameObject.Instantiate(g, it.mainRenderer.transform, false);

                pitonWrap.transform.localPosition = Vector3.zero;
                pitonWrap.transform.localScale = 15.85264f.ToVec();
                pitonWrap.transform.localRotation =
                    new Quaternion(0.502378583f, 0.497610092f, 0.497610122f, -0.502378523f);

                it.addtlRenderers = it.addtlRenderers.AddToArray(pitonWrap.GetComponent<Renderer>());

                var oldComponent = it.gameObject.GetOrAddComponent<ClimbingSpikeComponent>();
                var spike = it.gameObject.GetOrAddComponent<UnnamedClimbingSpikeComponent>();

                var badPiton = peakBundle.LoadAsset<GameObject>("UnnamedClimbingSpikeHammered_Bad.prefab");
                var goodPiton = peakBundle.LoadAsset<GameObject>("UnnamedClimbingSpikeHammered_Good.prefab");

                ShaderExtensions.ReplaceShaders(badPiton);
                ShaderExtensions.ReplaceShaders(goodPiton);

                CopyUnnamedMaterials(ref badPiton, peakBundle);
                CopyUnnamedMaterials(ref goodPiton, peakBundle);

                badPiton = RegisterOrGetNetworkPrefab(badPiton);
                goodPiton = RegisterOrGetNetworkPrefab(goodPiton);

                foreach (var componentInChild in badPiton.gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in componentInChild.materials)
                    {
                        material.shader =
                            ThrowHelper.ThrowIfArgumentNull(
                                Shader.Find(material.shader.name));
                    }
                }

                foreach (var componentInChild in goodPiton.gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (var material in componentInChild.materials)
                    {
                        material.shader =
                            ThrowHelper.ThrowIfArgumentNull(
                                Shader.Find(material.shader.name));
                    }
                }


                spike.hammeredBadVersionPrefab = badPiton;
                spike.hammeredGoodVersionPrefab = goodPiton;

                spike.hammeredVersionPrefab = oldComponent.hammeredVersionPrefab;
                spike.climbingSpikeStartDistance = oldComponent.climbingSpikeStartDistance;
                spike.climbingSpikePreviewPrefab = oldComponent.climbingSpikePreviewPrefab;

                spike.climbingSpikePreviewDisableDistance = oldComponent.climbingSpikePreviewDisableDistance;
                spike.climbingSpikeStartDistanceGrounded = oldComponent.climbingSpikeStartDistanceGrounded;
                spike.climbingSpikePreviewDisableDistanceGrounded =
                    oldComponent.climbingSpikePreviewDisableDistanceGrounded;

                Destroy(oldComponent);

                break;
            }

            case "Dynamite":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<Dynamite>();
                var dyn = it.gameObject.GetOrAddComponent<UnnamedDynamite>();

                dyn.baseFuseTime = oldComponent.startingFuseTime;

                dyn._hasExploded = oldComponent._hasExploded;
                dyn.trackable = oldComponent.trackable;
                dyn.smokeVFXPrefab = oldComponent.smokeVFXPrefab;
                dyn.explosionPrefab = oldComponent.explosionPrefab;
                dyn.startingFuseTime = oldComponent.startingFuseTime;
                dyn.lightFuseRadius = oldComponent.lightFuseRadius;
                dyn.fuseTime = oldComponent.fuseTime;
                dyn.sparks = oldComponent.sparks;
                dyn.sparksPhotosensitive = oldComponent.sparksPhotosensitive;
                dyn.setting = oldComponent.setting;
                dyn.DEBUG_PauseOnExplode = oldComponent.DEBUG_PauseOnExplode;

                Destroy(oldComponent);

                if (dyn.explosionPrefab)
                {
                    var exp = GameObject.Instantiate(dyn.explosionPrefab);
                    exp.name = "Unnamed" + dyn.explosionPrefab.name.Replace("Unnamed", "");

                    exp = RegisterOrGetNetworkPrefab(exp);

                    DontDestroyOnLoad(exp);

                    exp.SetActive(false);

                    dyn.explosionPrefab = exp;
                }


                break;
            }

            case "Parasol":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<Parasol>();
                var para = it.gameObject.GetOrAddComponent<UnnamedParasol>();

                para.item = oldComponent.item;
                para.extraYDrag = oldComponent.extraYDrag;
                para.extraXZDrag = oldComponent.extraXZDrag;
                para.sinceGroundedOnClose = oldComponent.sinceGroundedOnClose;
                para.openParasol = oldComponent.openParasol;
                para.closedParasol = oldComponent.closedParasol;
                para.anim = oldComponent.anim;
                para.isOpen = oldComponent.isOpen;

                para.isOpen = oldComponent.isOpen;
                var actPara = it.gameObject.GetOrAddComponent<Action_Parasol>();

                actPara.parasol = para;


                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    var gameObjectName = filt.gameObject.name;

                    if (gameObjectName == "Parasol Open")
                    {
                        var go = filt.gameObject;

                        DestroyImmediate(filt);
                        DestroyImmediate(go.GetComponent<Collider>());

                        var g = peakBundle.LoadAsset<GameObject>("UnnamedParasolOpened.prefab");
                        var bandWrap = GameObject.Instantiate(g, go.transform, false);

                        var r = go.GetComponent<Renderer>();

                        var bwr = bandWrap.GetComponent<Renderer>();

                        bwr.materials = r.materials;
                        bwr.sharedMaterials = r.sharedMaterials;


                        DestroyImmediate(r);

                        bandWrap.transform.localPosition = Vector3.zero;
                        bandWrap.transform.localScale = 1f.ToVec();
                        bandWrap.transform.localRotation =
                            new Quaternion(0.707106829f, -3.7252903e-09f, -1.01044861e-09f, 0.707106829f);
                    }

                    if (gameObjectName == "Parasol Closed")
                    {
                        var go = filt.gameObject;

                        DestroyImmediate(filt);
                        DestroyImmediate(go.GetComponent<Collider>());

                        var g = peakBundle.LoadAsset<GameObject>("UnnamedParasolClosed.prefab");
                        var bandWrap = GameObject.Instantiate(g, go.transform, false);

                        var r = go.GetComponent<Renderer>();

                        var bwr = bandWrap.GetComponent<Renderer>();

                        bwr.materials = r.materials;
                        bwr.sharedMaterials = r.sharedMaterials;


                        DestroyImmediate(r);


                        bandWrap.transform.localPosition = Vector3.zero;
                        bandWrap.transform.localScale = 1f.ToVec();
                        bandWrap.transform.localRotation =
                            new Quaternion(0.707106829f, -3.7252903e-09f, -1.01044861e-09f, 0.707106829f);
                    }
                }

                Destroy(oldComponent);

                break;
            }
            case "Bandages":
            {
                var g = peakBundle.LoadAsset<GameObject>("UnnamedBandageWrapping.prefab");
                var bandWrap = GameObject.Instantiate(g, it.mainRenderer.transform, false);


                bandWrap.transform.localPosition = Vector3.zero;
                bandWrap.transform.localScale = 6.181997f.ToVec();
                bandWrap.transform.localRotation =
                    new Quaternion(0.707106829f, 0, 0, 0.707106829f);

                it.addtlRenderers = it.addtlRenderers.AddToArray(bandWrap.GetComponent<Renderer>());

                break;
            }
            case "Big Lollipop":
            {
                var g = peakBundle.LoadAsset<GameObject>("UnnamedPitonPackaging.prefab");
                var bandWrap = GameObject.Instantiate(g, it.mainRenderer.transform.parent, false);

                bandWrap.transform.localPosition = new Vector3(0, 10.5100002f, -0.0599999987f);
                bandWrap.transform.localScale = 18.1f.ToVec();
                bandWrap.transform.localRotation =
                    new Quaternion(0, 0.707106829f, 0, 0.707106829f);

                if (bandWrap.TryGetComponent<Renderer>(out var renderer))
                {
                    renderer.SetMaterials([peakBundle.LoadAsset<Material>("M_UnnamedLollypopWrapper.mat")]);
                }

                it.addtlRenderers = it.addtlRenderers.AddToArray(bandWrap.GetComponent<Renderer>());

                break;
            }
            case "First Aid Kit":
            {
                var g = peakBundle.LoadAsset<GameObject>("UnnamedFirstAidKitPackaging.prefab");
                var ropeWrap = GameObject.Instantiate(g, it.mainRenderer.transform, false);


                ropeWrap.transform.localPosition = Vector3.zero;
                ropeWrap.transform.localScale = 3.348572f.ToVec();
                ropeWrap.transform.localRotation =
                    new Quaternion(0.707106829f, 0, 0, 0.707106829f);

                it.addtlRenderers = it.addtlRenderers.AddToArray(ropeWrap.GetComponent<Renderer>());

                break;
            }
            case "Fortified Milk":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "Milk")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedMilk.fbx");
                    }
                }

                break;
            }
            case "Blowgun":
            {
                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "Blowgun")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedBlowgun.fbx");
                    }
                }

                break;
            }
            case "Lantern":
            case "Faerie Lantern":
            {
                if (it.gameObject.GetComponentInChildren<StatusField>(true) is { } oldComponent)
                {
                    var statfield = oldComponent.gameObject.AddComponent<UnnamedStatusField>();

                    statfield.statusType = oldComponent.statusType;
                    statfield.statusAmountPerSecond = oldComponent.statusAmountPerSecond;
                    statfield.statusAmountOnEntry = oldComponent.statusAmountOnEntry;
                    statfield.radius = oldComponent.radius;
                    statfield.lastEnteredTime = oldComponent.lastEnteredTime;
                    statfield.entryCooldown = oldComponent.entryCooldown;
                    statfield.doNotApplyIfStatusesMaxed = oldComponent.doNotApplyIfStatusesMaxed;
                    statfield.additionalStatuses = oldComponent.additionalStatuses;
                    statfield.inflicting = oldComponent.inflicting;

                    Destroy(oldComponent);
                }

                break;
            }
            case "Cure-All":
            {
                var g = peakBundle.LoadAsset<GameObject>("UnnamedCureAllLabel.prefab");
                var ropeWrap = GameObject.Instantiate(g, it.mainRenderer.transform, false);

                ropeWrap.transform.localPosition = new Vector3(4.4668468e-05f, 0, 0);
                ropeWrap.transform.localScale = 6.216943f.ToVec();
                ropeWrap.transform.localRotation =
                    new Quaternion(0.547461152f, -0.447533578f, -0.447533578f, 0.547461152f);

                it.addtlRenderers = it.addtlRenderers.AddToArray(ropeWrap.GetComponent<Renderer>());

                break;
            }
            case "Flare":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<Flare>();
                var flr = it.gameObject.GetOrAddComponent<UnnamedFlare>();

                flr.trackable = oldComponent.trackable;
                flr.flareVFXPrefab = oldComponent.flareVFXPrefab;
                flr.flareColor = oldComponent.flareColor;

                Destroy(oldComponent);

                var flareAct = it.gameObject.GetComponent<Action_Flare>();

                flareAct.flare = flr;

                break;
            }
            case "Rescue Claw":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<RescueHook>();
                var hok = it.gameObject.GetOrAddComponent<UnnamedRescueHook>();

                foreach (var filt in it.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filt.gameObject.name == "RescueClawBody")
                    {
                        filt.mesh = filt.sharedMesh = peakBundle.LoadAsset<Mesh>("UnnamedRescueClaw.fbx");
                    }
                }

                hok.dragPoint = oldComponent.dragPoint;
                hok.claw = oldComponent.claw;
                hok.clawButt = oldComponent.clawButt;
                hok.maxLength = oldComponent.maxLength;
                hok.dragForce = oldComponent.dragForce;
                hok.liftDragForce = oldComponent.liftDragForce;
                hok.extraDragSelf = oldComponent.extraDragSelf;
                hok.extraDragOther = oldComponent.extraDragOther;
                hok.launchForce = oldComponent.launchForce;
                hok.liftForce = oldComponent.liftForce;
                hok.actionReduceUses = oldComponent.actionReduceUses;
                hok.camera = oldComponent.camera;
                hok.currentDistance = oldComponent.currentDistance;
                hok.fly = oldComponent.fly;
                hok.hitNothing = oldComponent.hitNothing;
                hok.isPulling = oldComponent.isPulling;
                hok.sinceFire = oldComponent.sinceFire;
                hok.targetPlayer = oldComponent.targetPlayer;
                hok.targetPos = oldComponent.targetPos;
                hok.startingPos = oldComponent.startingPos;
                hok.rescuedCharacterStartingPos = oldComponent.rescuedCharacterStartingPos;
                hok.threwAchievement = oldComponent.threwAchievement;
                hok.targetRig = oldComponent.targetRig;
                hok.ropeRender = oldComponent.ropeRender;
                hok.line = oldComponent.line;
                hok.startingClawLocalPos = oldComponent.startingClawLocalPos;
                hok.maxWallHookTime = oldComponent.maxWallHookTime;
                hok.maxScoutHookTime = oldComponent.maxScoutHookTime;
                hok.maxLiftDistance = oldComponent.maxLiftDistance;
                hok.stopPullDistance = oldComponent.stopPullDistance;
                hok.stopPullFriendDistance = oldComponent.stopPullFriendDistance;
                hok.minRaycastDistance = oldComponent.minRaycastDistance;
                hok.rescueShot = oldComponent.rescueShot;
                hok.rescueHit = oldComponent.rescueHit;
                hok.pulLStrengthCurve = oldComponent.pulLStrengthCurve;
                hok.firePoint = oldComponent.firePoint;
                hok.range = oldComponent.range;
                hok.rangeDownward = oldComponent.rangeDownward;
                hok.curRange = oldComponent.curRange;
                hok.afterLetGoDragSeconds = oldComponent.afterLetGoDragSeconds;
                hok.afterLetGoDragTime = oldComponent.afterLetGoDragTime;
                hok.selfFallSeconds = oldComponent.selfFallSeconds;

                Destroy(oldComponent);

                break;
            }
            case "rope cannon":
            case "anti-rope cannon":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<RopeShooter>();
                var chl = it.gameObject.GetOrAddComponent<UnnamedRopeShooter>();

                chl.baseShooterLength = oldComponent.length;
                chl.baseReach = oldComponent.maxLength;

                var anchoredRope = GameObject.Instantiate(oldComponent.ropeAnchorWithRopePref);
                anchoredRope.name = "Unnamed" + oldComponent.ropeAnchorWithRopePref.name.Replace("Unnamed", "") + "_c";

                DontDestroyOnLoad(anchoredRope);

                CopyUnnamedMaterials(ref anchoredRope, peakBundle);

                anchoredRope.SetActive(false);

                var ropeComp = anchoredRope.GetComponent<RopeAnchorWithRope>();

                var ropePref = GameObject.Instantiate(ropeComp.ropePrefab);
                ropePref.name = "Unnamed" + ropeComp.ropePrefab.name.Replace("Unnamed", "") + "_c";

                DontDestroyOnLoad(ropePref);

                CopyUnnamedMaterials(ref ropePref, peakBundle);

                var ropePrefComp = ropePref.GetComponent<RopeBoneVisualizer>();

                ropePrefComp.ropeMaterial = GetUnnamedMaterial(ropePrefComp.ropeMaterial, peakBundle);

                ropePref.SetActive(false);

                var aus = ropePref.GetOrAddComponent<AudioSource>();

                aus.priority = 128;
                aus.volume = 0.125f;
                aus.pitch = 1;

                aus.reverbZoneMix = 1f;
                aus.rolloffMode = AudioRolloffMode.Custom;

                aus.spatialBlend = 1;
                aus.dopplerLevel = 0.25f;
                aus.maxDistance = 120f;
                aus.minDistance = 39.1865f;

                aus.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                [
                    new Keyframe(
                        0.006773710250854492f,
                        1.0f,
                        -3.1092653274536135f,
                        -3.1092653274536135f,
                        0.0f,
                        0.04103275015950203f
                    ),
                    new Keyframe(
                        1f,
                        0.0f,
                        0.020448748022317888f,
                        0.020448748022317888f,
                        0.1378156542778015f,
                        0.0f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));


                aus.SetCustomCurve(AudioSourceCurveType.Spread, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        0.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.loop = true;

                aus.resource = peakBundle.LoadAsset<AudioResource>("Au_RopeLoop.ogg");
                aus.playOnAwake = false;

                var breaker = ropePref.GetOrAddComponent<UnnamedRopeBreaker>();

                breaker.startBreakSfx = peakBundle.LoadAsset<SFX_Instance>("SFXI Climb Rope Grab 1.asset");

                breaker.breakSfx =
                [
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Break Coconut 1.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Bridge Break 4.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Climb Rope Grab 1.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI RopeShooter 2.asset"),
                ];


#if DEBUG

                breaker.breakChance = 1f;
#else
                breaker.breakChance = UnnamedChance;
#endif

                breaker.axisMul = new Vector3(2f, 1f, 0f);
                breaker.shakeScale = 33;
                breaker.fallTime = 3;
                breaker.amount = 0.1f;
                breaker.startShakeDistance = 30.87f;
                breaker.startShakeAmount = 10f;
                breaker.climbingScreenShake = 5f;
                breaker.screenShakeTickTime = 0.2f;

                ropeComp.ropePrefab = RegisterOrGetNetworkPrefab(ropePref);
                chl.ropeAnchorWithRopePref = RegisterOrGetNetworkPrefab(anchoredRope);

                chl.gunshotVFX = oldComponent.gunshotVFX;
                chl.fumesVFX = oldComponent.fumesVFX;
                chl.cantReFire = oldComponent.cantReFire;
                chl.spawnPoint = oldComponent.spawnPoint;
                chl.length = oldComponent.length;

                chl.hideOnFire = oldComponent.hideOnFire;
                chl.screenshakeIntensity = oldComponent.screenshakeIntensity;
                chl.startAmmo = oldComponent.startAmmo;
                chl.shotSound = oldComponent.shotSound;
                chl.emptySound = oldComponent.emptySound;
                chl.maxLength = oldComponent.maxLength;

                Destroy(oldComponent);

                break;
            }

            case "rope spool":
            case "anti-rope spool":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<RopeSpool>();
                var rop = it.gameObject.GetOrAddComponent<UnnamedRopeSpool>();

                rop.baseStartFuel = oldComponent.ropeStartFuel;

                var wrapper = peakBundle.LoadAsset<GameObject>("UnnamedRopeWrapper.prefab");
                var ropeWrap = GameObject.Instantiate(wrapper, it.mainRenderer.transform, false);

                ropeWrap.transform.localPosition = Vector3.zero;
                ropeWrap.transform.localRotation =
                    new Quaternion(-0.288089275f, 0.645759523f, 0.288090259f, 0.645757973f);
                ropeWrap.transform.localScale = new Vector3(0.0803361759f, 0.0803361833f, 0.0803361759f);

                if (it.UIData.itemName == "anti-rope spool")
                {
                    ropeWrap.GetComponent<Renderer>().material =
                        peakBundle.LoadAsset<Material>("M_UnnamedAnttiRopeWrapper.mat");
                }

                it.addtlRenderers = it.addtlRenderers.AddToArray(ropeWrap.GetComponent<Renderer>());


                var tierComponent = it.gameObject.GetOrAddComponent<RopeTier>();

                var ropeAnchor = GameObject.Instantiate(tierComponent.anchorPrefab);
                ropeAnchor.name = "Unnamed" + tierComponent.anchorPrefab.name.Replace("Unnamed", "");

                ropeAnchor = RegisterOrGetNetworkPrefab(ropeAnchor);

                DontDestroyOnLoad(ropeAnchor);

                CopyUnnamedMaterials(ref ropeAnchor, peakBundle);

                ropeAnchor.SetActive(false);

                tierComponent.anchorPrefab = ropeAnchor;

                var rp = GameObject.Instantiate(oldComponent.ropePrefab);
                rp.name = "Unnamed" + oldComponent.ropePrefab.name.Replace("Unnamed", "");

                rp = RegisterOrGetNetworkPrefab(rp);

                var ropePrefComp = rp.GetComponent<RopeBoneVisualizer>();

                ropePrefComp.ropeMaterial = GetUnnamedMaterial(ropePrefComp.ropeMaterial, peakBundle);

                DontDestroyOnLoad(rp);

                CopyUnnamedMaterials(ref rp, peakBundle);

                rp.SetActive(false);

                var aus = rp.GetOrAddComponent<AudioSource>();

                aus.priority = 128;
                aus.volume = 0.125f;
                aus.pitch = 1;

                aus.reverbZoneMix = 1f;
                aus.rolloffMode = AudioRolloffMode.Custom;

                aus.spatialBlend = 1;
                aus.dopplerLevel = 0.25f;
                aus.maxDistance = 120f;
                aus.minDistance = 39.1865f;

                aus.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                [
                    new Keyframe(
                        0.006773710250854492f,
                        1.0f,
                        -3.1092653274536135f,
                        -3.1092653274536135f,
                        0.0f,
                        0.04103275015950203f
                    ),
                    new Keyframe(
                        1f,
                        0.0f,
                        0.020448748022317888f,
                        0.020448748022317888f,
                        0.1378156542778015f,
                        0.0f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));


                aus.SetCustomCurve(AudioSourceCurveType.Spread, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        0.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));


                aus.resource = peakBundle.LoadAsset<AudioResource>("Au_RopeLoop.ogg");
                aus.playOnAwake = false;

                var breaker = rp.GetOrAddComponent<UnnamedRopeBreaker>();

                breaker.startBreakSfx = peakBundle.LoadAsset<SFX_Instance>("SFXI Climb Rope Grab 1.asset");

                breaker.breakSfx =
                [
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Break Coconut 1.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Bridge Break 4.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Climb Rope Grab 1.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI RopeShooter 2.asset"),
                ];

#if DEBUG

                breaker.breakChance = 1f;
#else
                breaker.breakChance = UnnamedChance;
#endif


                breaker.axisMul = new Vector3(2f, 1f, 0f);
                breaker.shakeScale = 33;
                breaker.fallTime = 3;
                breaker.amount = 0.1f;
                breaker.startShakeDistance = 30.87f;
                breaker.startShakeAmount = 10f;
                breaker.climbingScreenShake = 5f;
                breaker.screenShakeTickTime = 0.2f;


                rop.segments = oldComponent.segments;
                rop.minSegments = oldComponent.minSegments;
                rop.ropeStartFuel = oldComponent.ropeStartFuel;
                rop.ropeFuel = oldComponent.ropeFuel;
                rop.ropeBase = oldComponent.ropeBase;
                rop.ropeStart = oldComponent.ropeStart;
                rop.ropeSpoolTf = oldComponent.ropeSpoolTf;
                rop.ropeInstance = oldComponent.ropeInstance;
                rop.rig = oldComponent.rig;
                rop.rope = oldComponent.rope;
                rop.scroll = oldComponent.scroll;
                rop.segsVel = oldComponent.segsVel;
                rop.ropeTier = oldComponent.ropeTier;
                rop.isAntiRope = oldComponent.isAntiRope;
                rop.ropePrefab = rp;

                Destroy(oldComponent);

                var ra = it.gameObject.GetComponentInChildren<RopeAudio>();

                ra.ropeSpool = rop;

                break;
            }
            case "Scout Effigy":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<ScoutEffigy>();
                var sce = it.gameObject.GetOrAddComponent<UnnamedScoutEffigy>();

                var g = peakBundle.LoadAsset<GameObject>("UnnamedScoutEffigyPackaging.prefab");
                var pack = GameObject.Instantiate(g, it.mainRenderer.transform, false);

                pack.transform.localPosition = Vector3.zero;
                pack.transform.localScale = 16.66667f.ToVec();
                pack.transform.localRotation =
                    new Quaternion(0.499912739f, 0.500087261f, 0.499912739f, 0.500087261f);

                it.addtlRenderers = it.addtlRenderers.AddToArray(pack.GetComponent<Renderer>());


                sce.previewPrefab = oldComponent.previewPrefab;
                sce.constructedPrefab = oldComponent.constructedPrefab;
                sce.maxPreviewDistance = oldComponent.maxPreviewDistance;
                sce.maxConstructDistance = oldComponent.maxConstructDistance;
                sce.maxConstructVerticalAngle = oldComponent.maxConstructVerticalAngle;
                sce.angleToNormal = oldComponent.angleToNormal;
                sce.currentPreview = oldComponent.currentPreview;
                sce.angleOffset = oldComponent.angleOffset;
                sce.isAngleable = oldComponent.isAngleable;
                sce.currentConstructHit = oldComponent.currentConstructHit;
                sce.constructing = oldComponent.constructing;
                sce.valid = oldComponent.valid;

                Destroy(oldComponent);

                break;
            }

            case "Chain Launcher":
            {
                var oldComponent = it.gameObject.GetOrAddComponent<VineShooter>();
                var chl = it.gameObject.GetOrAddComponent<UnnamedVineShooter>();
                chl.baseReach = oldComponent.maxLength;
                var vinePref = GameObject.Instantiate(oldComponent.vinePrefab);
                vinePref.name = "Unnamed" + oldComponent.vinePrefab.name.Replace("Unnamed", "");

                DontDestroyOnLoad(vinePref);

                CopyUnnamedMaterials(ref vinePref, peakBundle);

                // Replace "Rope Spike" with the actual chain material
                foreach (var re in it.gameObject.GetComponentsInChildren<Renderer>(true))
                {
                    if (re.gameObject.name == "Rope")
                    {
                        var mats = re.materials;

                        for (int index = 0, length = mats.Length; index < length; ++index)
                        {
                            var sanitizedMaterialName =
                                mats[index].name.Replace("(Instance)", "").Replace("(Clone)", "").Trim();

                            if (sanitizedMaterialName == "M_RopeSpike")
                            {
                                var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedChainShooterChains.mat");
                                unnamedMaterial.shader =
                                    ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
                                mats[index] = unnamedMaterial;
                            }
                        }

                        re.SetMaterials([..mats]);
                    }
                }

                vinePref.SetActive(false);

                var aus = vinePref.GetOrAddComponent<AudioSource>();

                aus.priority = 128;
                aus.volume = 0.125f;
                aus.pitch = 1;

                aus.reverbZoneMix = 1f;
                aus.rolloffMode = AudioRolloffMode.Custom;

                aus.spatialBlend = 1;
                aus.dopplerLevel = 0.25f;
                aus.maxDistance = 120f;
                aus.minDistance = 39.1865f;

                aus.SetCustomCurve(AudioSourceCurveType.CustomRolloff, new AnimationCurve(
                [
                    new Keyframe(
                        0.006773710250854492f,
                        1.0f,
                        -3.1092653274536135f,
                        -3.1092653274536135f,
                        0.0f,
                        0.04103275015950203f
                    ),
                    new Keyframe(
                        1f,
                        0.0f,
                        0.020448748022317888f,
                        0.020448748022317888f,
                        0.1378156542778015f,
                        0.0f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.SpatialBlend, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));


                aus.SetCustomCurve(AudioSourceCurveType.Spread, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        0.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.SetCustomCurve(AudioSourceCurveType.ReverbZoneMix, new AnimationCurve(
                [
                    new Keyframe(
                        0.0f,
                        1.0f,
                        0,
                        0,
                        0.3333333432674408f,
                        0.3333333432674408f
                    )
                ]));

                aus.loop = true;
                aus.resource = peakBundle.LoadAsset<AudioResource>("Au_ChainLoop.ogg");
                aus.playOnAwake = false;

                var vine = vinePref.GetOrAddComponent<UnnamedVine>();

                vine.breakPoint = 0.4f;
                vine.breakChance = UnnamedChance;
                vine.axisMul = new Vector3(2f, 1f, 0f);
                vine.shakeScale = 33;
                vine.fallTime = 3;
                vine.amount = 0.1f;
                vine.startShakeDistance = 30.87f;
                vine.startShakeAmount = 10f;
                vine.climbingScreenShake = 5f;
                vine.screenShakeTickTime = 0.2f;

                var breakP = peakBundle.LoadAsset<GameObject>("VFX_ChainBreak.prefab");

                var breakVFX = GameObject.Instantiate(breakP, vine.transform, false);
                breakVFX.transform.localPosition = Vector3.zero;
                breakVFX.transform.localRotation = Quaternion.identity;
                breakVFX.transform.localScale = Vector3.one;

                ShaderExtensions.ReplaceShaders(breakVFX);
                CopyUnnamedMaterials(ref breakVFX, peakBundle);

                vine.breakParticles = breakVFX.transform.GetChild(0).GetComponent<ParticleSystem>();

                vine.breakSfx =
                [
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Break Coconut 1.asset"),
                    peakBundle.LoadAsset<SFX_Instance>("SFXI Bridge Break 4.asset"),
                ];

                vine.fullMesh = vine.transform.GetChild(0).GetChild(0);

                foreach (var sfxi in vinePref.GetComponent<SFX_PlayOneShot>().sfxs)
                {
                    if (sfxi.name == "SFXI ChainLauncher 4")
                    {
                        vine.breakSfx = vine.breakSfx.AddToArray(sfxi);
                    }
                }

                chl.disableOnFire = oldComponent.disableOnFire;
                chl.maxLength = oldComponent.maxLength;
                chl.actionReduceUses = oldComponent.actionReduceUses;

                Destroy(oldComponent);

                vinePref = RegisterOrGetNetworkPrefab(vinePref);

                chl.vinePrefab = vinePref;

                break;
            }
        }
    }

    private Material GetUnnamedPlaneMaterial(Material vanillaMaterial, bool isWings = false)
    {
        var materialName = vanillaMaterial.name;

        var sanitizedMaterialName = materialName.Replace("(Instance)", "").Replace("(Clone)", "").Trim();

        var possibleUnnamedMaterialName =
            $"{sanitizedMaterialName.Replace("m_", "M_").Replace("M_", "M_Unnamed").Trim()}";


        if (CustomStartsWith(sanitizedMaterialName, "M_Unnamed"))
        {
            var unnamedMaterial = vanillaMaterial;
            unnamedMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }

        if (isWings && possibleUnnamedMaterialName == "Plane" || possibleUnnamedMaterialName == "Plane-snow")
        {
            return PlaneMaterials[possibleUnnamedMaterialName.Replace("Plane", "PlaneWings")];
        }

        return PlaneMaterials[possibleUnnamedMaterialName];
    }

    public Dictionary<string, Material> PlaneMaterials { get; set; } = null!;

    public Dictionary<string, Material> MirageMaterials { get; set; } = null!;

    private Material GetUnnamedMaterial(Material vanillaMaterial, PeakBundle peakBundle)
    {
        var materialName = vanillaMaterial.name;

        var sanitizedMaterialName = materialName.Replace("(Instance)", "").Replace("(Clone)", "").Trim();

        var possibleUnnamedMaterialName =
            $"{sanitizedMaterialName.Replace("m_", "M_").Replace("M_", "M_Unnamed").Trim()}.mat";

        if (CustomStartsWith(sanitizedMaterialName, "M_Unnamed"))
        {
            var unnamedMaterial = vanillaMaterial;
            unnamedMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }

        if (sanitizedMaterialName == "LanternGlass")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedLanternGlass.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "Blowgun")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedBlowgun.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "Jug Glass")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedJug Glass.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "GlassBrown")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedGlassBrown.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "Glass-purple liquid")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedGlass-purple liquid.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "bean_can")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_Unnamedbean_can.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "chailk_sack")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_Unnamedchailk_sack.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "Material.001" || sanitizedMaterialName == "Material.002")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_Unnamedchailk_sack.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else if (sanitizedMaterialName == "RescueClawGlow")
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>("M_UnnamedRescueClawGlow.mat");
            unnamedMaterial.shader = ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
        else
        {
            var unnamedMaterial = peakBundle.LoadAsset<Material>(possibleUnnamedMaterialName);
            unnamedMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }
    }

    public void CopyUnnamedMaterials(ref GameObject go, PeakBundle peakBundle)
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = renderer.materials;

            for (int index = 0, length = mats.Length; index < length; ++index)
            {
                var mat = mats[index];
                var materialName = mat.name;
                var sanitizedMaterialName = materialName.Replace("(Instance)", "").Replace("(Clone)", "").Trim();
                var possibleUnnamedMaterialName =
                    $"{sanitizedMaterialName.Replace("m_", "M_").Replace("M_", "M_Unnamed").Trim()}.mat";

                try
                {
                    mats[index] = GetUnnamedMaterial(mats[index], peakBundle);
                }
                catch (Exception e)
                {
                    if (peakBundle.Contains(possibleUnnamedMaterialName))
                    {
                        Log.LogWarning(e.Message);
                    }
                }
            }

            renderer.SetMaterials([..mats]);
        }
    }

    public void CopyUnnamedIcons(ref Item it, PeakBundle peakBundle)
    {
        var possibleUnnamedIconName =
            $"Unnamed {it.UIData.icon.name}.png";

        if (peakBundle.Contains(possibleUnnamedIconName))
        {
            try
            {
                var unnamedIcon = peakBundle.LoadAsset<Texture2D>(possibleUnnamedIconName);
                it.UIData.icon = unnamedIcon;
            }
            catch (Exception e)
            {
                Log.LogWarning(e.Message);
            }
        }
    }

    public GameObject GenerateInstancedVariant(GameObject variant, string nameToUse)
    {
        if (variant.TryGetComponent<Item>(out var it) && ItemDatabase.TryGetItem(it.itemID, out var item))
        {
            var generateInstancedVariant = GameObject.Instantiate(item.gameObject);

            generateInstancedVariant.name = "Unnamed" + nameToUse.Replace("Unnamed", "");

            // Don't destroy this manually since we want it to act like a prefab.

            DontDestroyOnLoad(generateInstancedVariant);
            generateInstancedVariant.SetActive(false);

            return generateInstancedVariant;
        }

        return variant;
    }

    public GameObject GenerateInstancedVariantById(ushort id)
    {
        if (ItemDatabase.TryGetItem(id, out var item))
        {
            var generateInstancedVariant = GameObject.Instantiate(item.gameObject);

            generateInstancedVariant.name = "Unnamed" + item.name.Replace("Unnamed", "");

            // Don't destroy this manually since we want it to act like a prefab.

            DontDestroyOnLoad(generateInstancedVariant);
            generateInstancedVariant.SetActive(false);

            return generateInstancedVariant;
        }

        throw new NullReferenceException($"Item {id} not found in database!");
    }


    public static GameObject GetUnnamedVariant(Item item)
    {
        return !UnnamedDatabase.TryGetValue(item.UIData.itemName, out var variant) ? item.gameObject : variant;
    }

    public static bool HasUnnamedVariant(Item item)
    {
        return UnnamedDatabase.ContainsKey(item.UIData.itemName);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static bool CustomStartsWith(string a, string b)
    {
        int aLen = a.Length;
        int bLen = b.Length;

        int ap = 0;
        int bp = 0;

        while (ap < aLen && bp < bLen && a[ap] == b[bp])
        {
            ap++;
            bp++;
        }

        return (bp == bLen);
    }

    public static bool IsUnnamed(GameObject go)
    {
        return CustomStartsWith(go.name, $"{ModDefinition.GetOrCreate(UnnamedInfo).Id}:Unnamed");
    }

    public static bool IsUnnamedUnique(GameObject go)
    {
        var cleanedName = go.name.Replace($"{ModDefinition.GetOrCreate(UnnamedInfo).Id}:", "").Replace("(Clone)", "")
            .Replace("(Instance)", "").Trim();

        return UnnamedUniques.Contains(cleanedName);
    }

    private static readonly HashSet<string> UnnamedUniques = [];
    public static bool ShouldBeUnnamed => Random.value <= UnnamedChance;
    public static bool LuggageShouldBeUnnamed => Random.value <= UnnamedLuggageChance;

    public static float RandomUnnamedModifier => RandomModifier();

    public static float RandomModifier(float baseMod = UnnamedModifier)
    {
        return Random.Range(1.0f - baseMod, 1.0f +
                                            baseMod);
    }

    public static bool IsUnnamedLucky(float baseMod = UnnamedModifier)
    {
        return Random.value < UnnamedModifier;
    }

    public static bool RandomUnnamedBool => IsUnnamedLucky();

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (scene.name.Equals("Airport"))
        {
            Instantiate(UnnamedKioskSetup);
        }
        else if (scene.name.StartsWith("Level_") || scene.name.Equals("WilIsland"))
        {
            Log.LogInfo("GENERATING LEVEL SEEDS!!!");
            GenerateSeedStates(MapHandler.Instance.gameObject.GetComponent<LevelGeneration>());

            // Storing the state 
            var prevState = Random.state;

            foreach (var ms in MapHandler.Instance.segments)
            {
                Random.state = BiomeSeeds[ms._biome];

                var baseBiomeObject = ms._segmentParent.transform.parent.parent;

                if (ms._biome == Biome.BiomeType.Shore)
                {
                    // Test to see if plane is unnamed
                    if (

#if DEBUG
                        true
#else
	    ShouldBeUnnamed
#endif


                    )
                    {
                        var planeSetup = ms.segmentParent.transform.Find(@"crashed plane");

                        var planeBack = planeSetup.transform.Find("Back Half");
                        var planeFront = planeSetup.transform.Find("front half");
                        var planeLoneWing = planeSetup.transform.Find("Wings.003");

                        // Rip Harlin, you'll be missed
                        Instantiate(UnnamedBrokenStandeeSetup, planeBack);

                        var rendererList = planeBack.GetComponentsInChildren<Renderer>(true)
                            .AddRangeToArray(planeFront.GetComponentsInChildren<Renderer>(true))
                            .AddRangeToArray(planeLoneWing.GetComponentsInChildren<Renderer>(true));

                        foreach (var renderer in rendererList)
                        {
                            var isWings = renderer.gameObject.name.StartsWith("Tail") ||
                                          renderer.gameObject.name.StartsWith("Wings");

                            var mats = renderer.materials;

                            for (int index = 0, length = mats.Length; index < length; ++index)
                            {
                                var mat = mats[index];
                                var materialName = mat.name;
                                var sanitizedMaterialName =
                                    materialName.Replace("(Instance)", "").Replace("(Clone)", "").Trim();

                                try
                                {
                                    mats[index] = GetUnnamedPlaneMaterial(mats[index], isWings);
                                }
                                catch (Exception e)
                                {
                                    if (PlaneMaterials.ContainsKey(sanitizedMaterialName))
                                    {
                                        Log.LogWarning(e.Message);
                                    }
                                }
                            }

                            renderer.SetMaterials([..mats]);
                        }

                        // Set the plane's items to unnamed
                        var planeSetupItemSpawners = planeSetup.GetComponentsInChildren<SingleItemSpawner>(true);

                        foreach (var sis in planeSetupItemSpawners)
                        {
                            if (sis.prefab.gameObject.TryGetComponent(out Item it) &&
                                HasUnnamedVariant(it)
                               )
                            {
                                RenameToUnnamed(sis.gameObject);
                                sis.prefab = GetUnnamedVariant(sis.prefab.GetComponent<Item>());
                            }
                        }
                    }
                }

                var luggs = baseBiomeObject.GetComponentsInChildren<Luggage>(true);

                foreach (var lugg in luggs)
                {
                    var handler = lugg.gameObject.GetOrAddComponent<LuggageBrandHandler>();
                    handler.luggage = lugg;

                    if (ShouldBeUnnamed)
                    {
                        handler.shouldBeUnnamed = true;
                        if (lugg.gameObject.activeInHierarchy)
                        {
                            handler.RPC_SetLuggageAsUnnamed(true);
                        }
                    }
                }

                var luggsMirages = baseBiomeObject.GetComponentsInChildren<MirageLuggage>(true);

                foreach (var mirage in luggsMirages)
                {
                    // No need to use RPC. Mirages don't even have PhotonViews anyways
                    if (ShouldBeUnnamed)
                    {
                        SetMirageAsUnnamed(mirage);
                    }
                }

                var singleItemSpawns = baseBiomeObject.GetComponentsInChildren<SingleItemSpawner>(true);

                foreach (var sis in singleItemSpawns)
                {
                    if (sis.prefab.gameObject.TryGetComponent<Dynamite>(out _))
                    {
                        // Single item spawners don't have any view on them.
                        if (ShouldBeUnnamed)
                        {
                            RenameToUnnamed(sis.gameObject);
                            sis.prefab = GetUnnamedVariant(sis.prefab.GetComponent<Item>());
                        }
                    }
                }

                BiomeSeeds[ms._biome] = Random.state;
            }

            // Reinstante the state
            Random.state = prevState;
        }
    }

    private void SetMirageAsUnnamed(MirageLuggage mirage)
    {
        foreach (var mirageRenderer in mirage.renderers)
        {
            var newMats = new List<Material>();

            var mats = mirageRenderer.materials;

            foreach (var mat in mats)
            {
                newMats.Add(GetMirageMaterial(mat));
            }

            mirageRenderer.SetMaterials(newMats);
        }
    }

    private Material GetMirageMaterial(Material vanillaMaterial)
    {
        var materialName = vanillaMaterial.name;

        var sanitizedMaterialName = materialName.Replace("(Instance)", "").Replace("(Clone)", "").Trim();

        var possibleUnnamedMaterialName =
            $"{sanitizedMaterialName.Replace("m_", "M_").Replace("M_", "M_Unnamed").Trim()}";

        if (CustomStartsWith(sanitizedMaterialName, "M_Unnamed"))
        {
            var unnamedMaterial = vanillaMaterial;
            unnamedMaterial.shader =
                ThrowHelper.ThrowIfArgumentNull(Shader.Find(unnamedMaterial.shader.name));
            return unnamedMaterial;
        }

        return MirageMaterials[possibleUnnamedMaterialName];
    }

    private static readonly Dictionary<Biome.BiomeType, Random.State> BiomeSeeds = new();
    private static readonly int UnderlayDilate = Shader.PropertyToID("_UnderlayDilate");
    private static readonly int UnderlayOffsetY = Shader.PropertyToID("_UnderlayOffsetY");
    private static readonly int UnderlaySoftness = Shader.PropertyToID("_UnderlaySoftness");
    private static readonly int UnderlayColor = Shader.PropertyToID("_UnderlayColor");

    public static void GenerateSeedStates(LevelGeneration gens)
    {
        // Generate subseeds for each biome using the map's seed

        var prevState = Random.state;

        // Generate the master seed;
        Random.InitState(gens.seed);

        // Subseeds 

        foreach (Biome.BiomeType v in Enum.GetValues(typeof(Biome.BiomeType)))
        {
            var seed = Random.Range(int.MinValue, int.MaxValue);
            var mainState = Random.state;
            Random.InitState(seed);
            BiomeSeeds[v] = Random.state;
            Random.state = mainState;
        }

        Random.state = prevState;
    }

    public static void PopulateUnnamedDynamite(Biome.BiomeType type, List<SingleItemSpawner> list)
    {
        // Save rng state
        var prevState = Random.state;

        // Use the biome's seed
        Random.state = BiomeSeeds[type];

        foreach (var itemSpawner in list)
        {
            if (Random.value < UnnamedChance)
            {
                itemSpawner.prefab = GetUnnamedVariant(itemSpawner.prefab.GetComponent<Item>());
            }
        }

        // Save the biome state for later use
        BiomeSeeds[type] = Random.state;

        // Restore state
        Random.state = prevState;
    }


    private void AddLocalizedTextCsv()
    {
        using var reader = new StreamReader(Path.Join(Path.GetDirectoryName(Info.Location),
            "UnnamedLocalizedText.csv"));

        var currentLine = 0;

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();

            if (line == null)
            {
                break;
            }

            currentLine++;

            var valList = new List<string>(CSVReader.SplitCsvLine(line));

            var locName = valList.Deque();

            var endline = valList.Pop();

            if (endline != "ENDLINE")
            {
                Log.LogError($"Invalid localization at line {currentLine}");
            }

            if (locName != "CURRENT_LANGUAGE")
            {
                if (locName == "KIOSK_READY_TEXT")
                {
                    for (var i = 0; i < valList.Count; i++)
                    {
                        valList[i] = valList[i].Replace("#modname", Name).Replace("#version", Version);

#if DEBUG
                        valList[i] += " (DEBUG)";
#endif
                    }
                }

                LocalizedText.mainTable[locName] = valList;
                Log.LogDebug($"Added localization of {locName}");
            }
        }

        Log.LogDebug($"Added {currentLine - 1} localizations");
    }

    private static Vector3 GetSpawnPosition()
    {
        if (!MainCamera.instance)
        {
            Log.LogWarning("GetSpawnPosition: MainCamera not found, using default position");
            return Vector3.zero;
        }

        var transform = MainCamera.instance.transform;
        var maxRaycastDistance = 5000f;
        if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, maxRaycastDistance))
        {
            return hitInfo.point + hitInfo.normal * 0.1f;
        }

        const float fallbackDistance = 25f;
        return transform.position + transform.forward * fallbackDistance;
    }

    private static Quaternion GetSpawnRotation()
    {
        if (!MainCamera.instance)
        {
            return Quaternion.identity;
        }

        var vector3 = MainCamera.instance.transform.forward;
        vector3.y = 0.0f;
        var forward = vector3;

        return forward != Vector3.zero ? Quaternion.LookRotation(forward) : Quaternion.identity;
    }


    [ConsoleCommand]
    public static void SpawnBigUnnamedLuggage()
    {
        var spawnPosition = GetSpawnPosition();
        var spawnRotation = GetSpawnRotation();

        try
        {
            NetworkPrefabManager.SpawnNetworkPrefab(BigUnnamedLuggagePrefab.name, spawnPosition, spawnRotation);
            Log.LogInfo($"Spawned Big Unnamed luggage at {spawnPosition}");
        }
        catch (Exception ex)
        {
            Log.LogError($"spawnsmallluggage: Failed - {ex.Message}");
        }
    }

    [ConsoleCommand]
    public static void SpawnSmallUnnamedLuggage()
    {
        var spawnPosition = GetSpawnPosition();
        var spawnRotation = Quaternion.identity;

        try
        {
            NetworkPrefabManager.SpawnNetworkPrefab(SmallUnnamedLuggagePrefab.name, spawnPosition, spawnRotation);
            Log.LogInfo($"Spawned Small Unnamed luggage at {spawnPosition}");
        }
        catch (Exception ex)
        {
            Log.LogError($"spawnsmallluggage: Failed - {ex.Message}");
        }
    }

    [ConsoleCommand]
    public static void SpawnEpicUnnamedLuggage()
    {
        var spawnPosition = GetSpawnPosition();
        var spawnRotation = Quaternion.identity;

        try
        {
            NetworkPrefabManager.SpawnNetworkPrefab(EpicUnnamedLuggagePrefab.name, spawnPosition, spawnRotation);
            Log.LogInfo($"Spawned Epic Unnamed luggage at {spawnPosition}");
        }
        catch (Exception ex)
        {
            Log.LogError($"spawnsmallluggage: Failed - {ex.Message}");
        }
    }

    [ConsoleCommand]
    public static void SpawnAncientUnnamedLuggage()
    {
        var spawnPosition = GetSpawnPosition();
        var spawnRotation = Quaternion.identity;

        try
        {
            NetworkPrefabManager.SpawnNetworkPrefab(AncientUnnamedLuggagePrefab.name, spawnPosition, spawnRotation);
            Log.LogInfo($"Spawned Ancient Unnamed luggage at {spawnPosition}");
        }
        catch (Exception ex)
        {
            Log.LogError($"spawnsmallluggage: Failed - {ex.Message}");
        }
    }

    public static void ApplyUnnamedModifierToAffliction(ref Affliction affliction)
    {
        switch (affliction)
        {
            case Affliction_PoisonOverTime pois:
            {
                var poisStatusPerSecond = pois.statusPerSecond * RandomUnnamedModifier;
                Log.LogInfo($"Poison over time from {pois.statusPerSecond}/s to {poisStatusPerSecond}/s");
                pois.statusPerSecond = poisStatusPerSecond;
                break;
            }
            case Affliction_FasterBoi fast:
            {
                var moveSpeedMod = fast.moveSpeedMod * RandomUnnamedModifier;
                var climbSpeedMod = fast.climbSpeedMod * RandomUnnamedModifier;
                var drowsyOnEnd = fast.drowsyOnEnd * RandomUnnamedModifier;

                Log.LogInfo($"Faster boi move speed from {fast.moveSpeedMod}/s to {moveSpeedMod}/s");
                Log.LogInfo($"Faster boi climb speed from {fast.climbSpeedMod}/s to {climbSpeedMod}/s");
                Log.LogInfo($"Faster boi drowsiness on end from {fast.drowsyOnEnd} to {drowsyOnEnd}");

                fast.moveSpeedMod = moveSpeedMod;
                fast.climbSpeedMod = climbSpeedMod;
                fast.drowsyOnEnd = drowsyOnEnd;
                break;
            }
            case Affliction_Exhaustion exh:
            {
                var drainAmount = exh.drainAmount * RandomUnnamedModifier;
                Log.LogInfo($"Exhaustion from {exh.drainAmount} to {drainAmount}");
                exh.drainAmount = drainAmount;
                break;
            }
            case Affliction_AdjustColdOverTime adjc:
            {
                var statusPerSecond = adjc.statusPerSecond * RandomUnnamedModifier;
                Log.LogInfo($"Adjust COLD over time from {adjc.statusPerSecond}/s to {statusPerSecond}/s");
                adjc.statusPerSecond = statusPerSecond;

                break;
            }
            case Affliction_Chaos chaos:
            {
                var averageBonusStamina = chaos.averageBonusStamina * RandomUnnamedModifier;
                var statusAmountAverage = chaos.statusAmountAverage * RandomUnnamedModifier;

                Log.LogInfo($"Chaos bonus stam from {chaos.averageBonusStamina} to {averageBonusStamina}");
                Log.LogInfo($"Chaos average status amount from {chaos.statusAmountAverage} to {statusAmountAverage}");

                chaos.averageBonusStamina = averageBonusStamina;
                chaos.statusAmountAverage = statusAmountAverage;

                break;
            }
            case Affliction_AdjustStatus adj:
            {
                var statusAmount = adj.statusAmount * RandomUnnamedModifier;

                Log.LogInfo($"Adjust {adj.statusType} from {adj.statusAmount} to {statusAmount}");

                adj.statusAmount = statusAmount;
                break;
            }
            case Affliction_AddBonusStamina adb:
            {
                var staminaAmount = adb.staminaAmount * RandomUnnamedModifier;
                Log.LogInfo($"Add bonus stam from {adb.staminaAmount} to {staminaAmount}");
                adb.staminaAmount = staminaAmount;
                break;
            }
            case Affliction_AdjustDrowsyOverTime drow:
            {
                var statusPerSecond = drow.statusPerSecond * RandomUnnamedModifier;
                Log.LogInfo($"Adjust DROWSY over time from {drow.statusPerSecond} to {statusPerSecond}");
                drow.statusPerSecond = statusPerSecond;
                break;
            }
            case Affliction_AdjustStatusOverTime adjst:
            {
                var statusPerSecond = adjst.statusPerSecond * RandomUnnamedModifier;
                Log.LogInfo($"Adjust status over time from {adjst.statusPerSecond} to {statusPerSecond}");

                adjst.statusPerSecond = statusPerSecond;
                break;
            }
            case Affliction_ZombieBite zom:
            {
                var statusPerSecond = zom.statusPerSecond * RandomUnnamedModifier;
                var delayBeforeEffect = zom.delayBeforeEffect * RandomUnnamedModifier;
                Log.LogInfo($"Zombie bite status per seconds from {zom.statusPerSecond}/s to {statusPerSecond}/s");
                Log.LogInfo($"Zombie bite delay from {zom.delayBeforeEffect}/s to {delayBeforeEffect}/s");

                zom.statusPerSecond = statusPerSecond;
                zom.delayBeforeEffect = delayBeforeEffect;
                break;
            }

            case Affliction_ClimbingChalk chk:
            {
                var climbStaminaMultiplier = chk.climbStaminaMultiplier * RandomUnnamedModifier;
                Log.LogInfo(
                    $"Climbing Chalk climb stamina multiplier from {chk.climbStaminaMultiplier} to {climbStaminaMultiplier}");
                chk.climbStaminaMultiplier = climbStaminaMultiplier;
                break;
            }

            case Affliction_LowGravity lowg:
            {
                var lowGravAmount = Mathf.RoundToInt(lowg.lowGravAmount * RandomUnnamedModifier);
                Log.LogInfo(
                    $"Climbing Chalk climb stamina multiplier from {lowg.lowGravAmount} to {lowGravAmount}");
                lowg.lowGravAmount = Mathf.RoundToInt(lowg.lowGravAmount * RandomUnnamedModifier);
                break;
            }

            default:

                if (UnnamedCanadianCompatibility.enabled)
                {
                    UnnamedCanadianCompatibility.ApplyUnnamedModifierToAffliction(ref affliction);
                }

                if (Unnamed4FoodCompatibility.enabled)
                {
                    Unnamed4FoodCompatibility.ApplyUnnamedModifierToAffliction(ref affliction);
                }

                break;
        }
    }

    public void AddItemToDatabase(string originalItemName, GameObject newVariantGameObject)
    {
        UnnamedDatabase.Add(originalItemName, newVariantGameObject.gameObject);

        Log.LogInfo(
            $"Added item named {newVariantGameObject.gameObject.name} for {originalItemName}.");
    }

    public static string CleanedName(string dirtyString)
    {
        return dirtyString.Replace("(Clone)", "")
            .Replace("(Instanced)", "").Trim();
    }
}