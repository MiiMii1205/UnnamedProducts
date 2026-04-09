using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnnamedProducts.Behaviours;
using UnnamedProducts.Behaviours.Item;
using UnnamedProducts.Behaviours.Item.GarbageBag;
using UnnamedProducts.Extensions;
using Peak.Afflictions;
using Photon.Pun;
using Sirenix.Utilities;
using UnityEngine;
using Zorro.Core;

namespace UnnamedProducts.Patchers;

public static class UnnamedItemPatcher
{
    [HarmonyPatch(typeof(Item), nameof(Item.GetName))]
    [HarmonyPostfix]
    public static void GetNamePostfix(Item __instance, ref string __result)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __result = LocalizedText.GetText("UNNAMED_ITEM").Replace("#", __result);
        }
    }
    [HarmonyPatch(typeof(ClimbHandle), nameof(ClimbHandle.GetName))]
    [HarmonyPostfix]
    public static void GetNamePostfix(ClimbHandle __instance, ref string __result)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __result = LocalizedText.GetText("UNNAMED_ITEM").Replace("#", __result);
        }
    }
    [HarmonyPatch(typeof(ScoutCannonFuse), nameof(ScoutCannonFuse.GetName) )]
    [HarmonyPostfix]
    public static void GetNamePostfix(ScoutCannonFuse __instance, ref string __result)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.scoutCannon.gameObject))
        {
            __result = LocalizedText.GetText("UNNAMED_ITEM").Replace("#", __result);
        }
    }

    [HarmonyPatch(typeof(RopeSegment), nameof(RopeSegment.GetName))]
    [HarmonyPostfix]
    public static void GetNamePostfix(RopeSegment __instance, ref string __result)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.rope.gameObject))
        {
            __result = LocalizedText.GetText("UNNAMED_ITEM").Replace("#", __result);
        }
    }
    [HarmonyPatch(typeof(JungleVine), nameof(JungleVine.GetName))]
    [HarmonyPostfix]
    public static void GetNamePostfix(JungleVine __instance, ref string __result)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __result = LocalizedText.GetText("UNNAMED_ITEM").Replace("#", __result);
        }
    }
    [HarmonyPatch(typeof(ShittyPiton), nameof(ShittyPiton.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(ShittyPiton __instance)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            var prevHangTime = __instance.totalSecondsOfHang;
            __instance.totalSecondsOfHang *= UnnamedPlugin.RandomUnnamedModifier;
            UnnamedPlugin.Log.LogInfo($"Hang time for {__instance.gameObject.name} will be {__instance.totalSecondsOfHang} instead of {prevHangTime} because {__instance.gameObject.name} is UNNAMED");
        }
    }
    
    [HarmonyPatch(typeof(Breakable), nameof(Breakable.RPC_NonItemBreak))]
    [HarmonyPrefix]
    public static void BreakableNonItemPrefix(Breakable __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __runOriginal = false;

            for (int i = 0, length= __instance.instantiateNonItemOnBreak.Count; i < length; ++i)
            {
                var spawned = Object.Instantiate(__instance.instantiateNonItemOnBreak[i],
                    __instance.transform.position, __instance.transform.rotation);

                if (!spawned.activeSelf)
                {
                    spawned.SetActive(true);
                }
                
                if (spawned.TryGetComponent(out Rigidbody component))
                {
                    component.linearVelocity = __instance.item.rig.linearVelocity;
                    component.angularVelocity = __instance.item.rig.angularVelocity;
                }
            }
        }
    }



    [HarmonyPatch(typeof(Action_Spawn), nameof(Action_Spawn.RPCSpawn) )]
    [HarmonyPrefix]
    public static void RPCSpawnPrefix(Action_Spawn __instance, Vector3 position, Quaternion rotation,
        ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __runOriginal = false;

            var spawned =Object.Instantiate(__instance.objectToSpawn, position, rotation);
            if (!spawned.activeSelf)
            {
                spawned.SetActive(true);
            }
            
        }
    }
    [HarmonyPatch(typeof(Item), nameof(Item.HideRenderers) )]
    [HarmonyPrefix]
    public static void RPCSpawnPrefix(Item __instance, ref bool __runOriginal)
    {
        if (__instance is UnnamedGarbageBagController nngbc)
        {
            __runOriginal = false;
            
            (nngbc.allBagRenderers).ForEach(
                (meshRenderer => meshRenderer.enabled = false));
            
        }
    }

    
    [HarmonyPatch(typeof(ItemCooking), nameof(ItemCooking.RPC_CookingExplode))]
    [HarmonyPrefix]
    public static void CookingExplodePrefix(ItemCooking __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __runOriginal = false;

            if (__instance.item.TryGetComponent(out UnnamedDynamite dyn) && dyn.IsADud)
            {
                UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedDynamite)} {__instance.item.gameObject.name} is a dud. Not exploding");
            } else if (__instance.item.TryGetComponent(out UnnamedFlare flr) && flr.IsADud)
            {
                UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedFlare)} {__instance.item.gameObject.name} is a dud. Not exploding");
            }
            else
            {

                if (__instance.explosionPrefab)
                {
                    var spawned = Object.Instantiate(__instance.explosionPrefab, __instance.transform.position,
                        __instance.transform.rotation);

                    if (!spawned.activeSelf)
                    {
                        spawned.SetActive(true);
                    }
                }

                if (Character.localCharacter.data.currentItem == __instance.item)
                {
                    Player.localPlayer.EmptySlot(Character.localCharacter.refs.items.currentSelectedSlot);
                    Character.localCharacter.refs.afflictions.UpdateWeight();
                }

                __instance.item.ClearDataFromBackpack();

                if (__instance.photonView.IsMine)
                {
                    PhotonNetwork.Destroy(__instance.gameObject);
                }
            }

        }
    }

    [HarmonyPatch(typeof(Action_ModifyStatus), nameof(Action_ModifyStatus.RunAction))]
    [HarmonyPrefix]
    public static void ModifyStatusPrefix(Action_ModifyStatus __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            __runOriginal = false;

            var actualChangeAmount = __instance.changeAmount * UnnamedPlugin.RandomUnnamedModifier;
            
            if (__instance.ifSkeleton && !__instance.character.data.isSkeleton)
            {
                return;
            }

            var passedOut = __instance.character.data.passedOut;
            var hasFeeder = __instance.item.TryGetFeeder(out var feeder);
            
            UnnamedPlugin.Log.LogInfo(
                $"Applying {actualChangeAmount} of {__instance.statusType} instead of {__instance.changeAmount} because {__instance.item.name} is UNNAMED");

            if (__instance.changeAmount < 0f)
            {
                if (__instance.statusType == CharacterAfflictions.STATUSTYPE.Poison)
                {
                    __instance.character.refs.afflictions.ClearPoisonAfflictions();

                    var poisonPoints = Mathf.RoundToInt(
                        Mathf.Min(
                            __instance.character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE
                                .Poison),
                            Mathf.Abs(actualChangeAmount)) * 100f);

                    if (hasFeeder)
                    {
                        GameUtils.instance.IncrementFriendPoisonHealing(poisonPoints, feeder.photonView.Owner);
                    }
                    else
                    {
                        Singleton<AchievementManager>.Instance.IncrementSteamStat(STEAMSTATTYPE.PoisonHealed,
                            poisonPoints);
                    }
                }

                if (__instance.statusType == CharacterAfflictions.STATUSTYPE.Injury &&
                    hasFeeder)
                {
                    var healedInjuryPoints = Mathf.RoundToInt(
                        Mathf.Min(
                            __instance.character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE
                                .Injury),
                            Mathf.Abs(actualChangeAmount)) * 100f);

                    GameUtils.instance.IncrementFriendHealing(healedInjuryPoints, feeder.photonView.Owner);
                }

                __instance.character.refs.afflictions.SubtractStatus(__instance.statusType, Mathf.Abs(
                    actualChangeAmount));
            }
            else
            {
                __instance.character.refs.afflictions.AddStatus(__instance.statusType,
                    Mathf.Abs(actualChangeAmount));
            }

            var statusSum = __instance.character.refs.afflictions.statusSum;
            if (passedOut && statusSum <= 1f)
            {
                UnnamedPlugin.Log.LogDebug("LIFE WAS SAVED");

                if (hasFeeder)
                {
                    GameUtils.instance.ThrowEmergencyPreparednessAchievement(feeder.photonView.Owner);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Action_RestoreHunger), nameof(Action_RestoreHunger.RunAction))]
    [HarmonyPrefix]
    public static void RestoreHungerPrefix(Action_RestoreHunger __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            __runOriginal = false;

            var actualRestorationAmount = __instance.restorationAmount * UnnamedPlugin.RandomUnnamedModifier;

            UnnamedPlugin.Log.LogInfo(
                $"Restoring {actualRestorationAmount} hunger instead of {__instance.restorationAmount} because {__instance.item.name} is UNNAMED");
            
            __instance.character.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Hunger,
                actualRestorationAmount);
        }
    }
    [HarmonyPatch(typeof(Action_Flare), nameof(Action_Flare.RunAction))]
    [HarmonyPrefix]
    public static void RestoreHungerPrefix(Action_Flare __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            __runOriginal = false;

            if (__instance.item.TryGetComponent<UnnamedFlare>(out var nnf) && !nnf.isDud)
            {
                __runOriginal = true;
            }

        }
    }
    
    [HarmonyPatch(typeof(Action_BecomeSkeleton), nameof(Action_BecomeSkeleton.RunAction))]
    [HarmonyPrefix]
    public static void BecomeSkeletonPrefix(Action_BecomeSkeleton __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject) && __instance.item.CompareTag("BookOfBones"))
        {
            var shouldTurnEveryoneElse = __instance.item.GetData<BoolItemData>(
                (DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1), () => new BoolItemData()
                {
                    Value = (1.0f * Random.Range(1.0f - UnnamedPlugin.UnnamedModifier,
                                1.0f + UnnamedPlugin.UnnamedModifier)) >=
                            1.0f
                }).Value;

            if (shouldTurnEveryoneElse)
            {

                UnnamedPlugin.Log.LogInfo(
                    $"Book of bones will skeletonize everyone BUT {__instance.character.characterName} because {__instance.item.name} is UNNAMED");
                
                __runOriginal = false;
                
                foreach (var chara in Character.AllCharacters)
                {
                    if (chara == __instance.character)
                    {
                        continue;
                    }

                    chara.view.RPC(nameof(CharacterData.RPC_SyncSkeleton), RpcTarget.All, !chara.data.isSkeleton);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(Action_ClearAllStatus), nameof(Action_ClearAllStatus.RunAction))]
    [HarmonyPrefix]
    public static void ClearAllPrefix(Action_ClearAllStatus __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject) && __instance.item.CompareTag("BookOfBones"))
        {

            var shouldTurnEveryoneElse = __instance.item.GetData<BoolItemData>(
                (DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1), () => new BoolItemData()
                {
                    Value = (1.0f * Random.Range(1.0f - UnnamedPlugin.UnnamedModifier,
                                1.0f + UnnamedPlugin.UnnamedModifier)) >=
                            1.0f
                }).Value;
            
            if (shouldTurnEveryoneElse)
            {
                __runOriginal = false;

                UnnamedPlugin.Log.LogInfo(
                    $"Book of bones will skeletonize everyone BUT {__instance.character.characterName} because {__instance.item.name} is UNNAMED");

                
                // Do nothing since you're not a skeleton lol;
                foreach (var chara in Character.AllCharacters)
                {
                    if (chara == __instance.character)
                    {
                        continue;
                    }

                    chara.view.RPC(nameof(UnnamedSkeletonHandler.RPC_ClearAllStatusesBones), RpcTarget.All, __instance.excludeCurse, __instance.otherExclusions.Convert((s)=>(int)s).ToArray());
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(Action_ClearAllStatus), nameof(Action_ClearAllStatus.RunAction))]
    [HarmonyPostfix]
    public static void ClearAllPost(Action_ClearAllStatus __instance)
    {
        __instance.character.GetComponent<CharacterBurnController>().ExtinguishFires();
    }
    
    [HarmonyPatch(typeof(ScoutCannon), nameof(ScoutCannon.FireTargets))]
    [HarmonyPrefix]
    public static void FireTargetsPre(ScoutCannon __instance, ref bool __runOriginal)
    {
        if (__instance is UnnamedScoutCannon cannon)
        {
            __runOriginal = false;

            cannon.UnnamedFireCannon();
        }
    }
    
    [HarmonyPatch(typeof(RescueHook), nameof(RescueHook.Fire) )]
    [HarmonyPrefix]
    public static void FireTargetsPre(RescueHook __instance, ref bool __runOriginal)
    {
        if (__instance is UnnamedRescueHook hook)
        {
            __runOriginal = false;

            hook.Fire();
        }
    }
    
    [HarmonyPatch(typeof(Action_ModifyStatus), nameof(Action_ModifyStatus.RunAction))]
    [HarmonyPrefix]
    public static void ClearAllPrefix(Action_ModifyStatus __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject) && __instance.item.CompareTag("BookOfBones"))
        {

            var shouldTurnEveryoneElse = __instance.item.GetData<BoolItemData>(
                (DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1), () => new BoolItemData()
                {
                    Value = (1.0f * Random.Range(1.0f - UnnamedPlugin.UnnamedModifier,
                                1.0f + UnnamedPlugin.UnnamedModifier)) >=
                            1.0f
                }).Value;
            
            if (shouldTurnEveryoneElse)
            {

                UnnamedPlugin.Log.LogInfo(
                    $"Book of bones will apply {__instance.changeAmount} of {__instance.statusType} to everyone BUT {__instance.character.characterName} because {__instance.item.name} is UNNAMED");

                __runOriginal = false;
                
                var hasFeeder = __instance.item.TryGetFeeder(out var feeder);
                
                // Do nothing since you're not a skeleton lol;
                foreach (var chara in Character.AllCharacters)
                {
                    if (chara == __instance.character)
                    {
                        continue;
                    }
                    
                    chara.view.RPC(nameof(UnnamedSkeletonHandler.RPC_ChangeStatsBones), RpcTarget.All, (int) __instance.statusType, __instance.changeAmount, __instance.ifSkeleton,
                        hasFeeder ? feeder.view.ViewID : -1);
                }
            }
        }
    }


    [HarmonyPatch(typeof(Action_ModifyStatus), nameof(Action_ModifyStatus.RunAction))]
    [HarmonyPostfix]
    public static void ModifyStatusesPostfix(Action_ModifyStatus __instance)
    {
        if (__instance is {statusType: CharacterAfflictions.STATUSTYPE.Hot, changeAmount: < 0f})
        {
            // We removed the hot, so no more fire
            __instance.character.GetComponent<CharacterBurnController>().ExtinguishFires();
        }
    }

    [HarmonyPatch(typeof(Action_GiveExtraStamina), nameof(Action_GiveExtraStamina.RunAction))]
    [HarmonyPrefix]
    public static void GiveExtraStaminaPrefix(Action_GiveExtraStamina __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            __runOriginal = false;

            var actualAmount = __instance.amount * UnnamedPlugin.RandomUnnamedModifier;
            
            UnnamedPlugin.Log.LogInfo(
                $"Giving {actualAmount} extra stamina instead of {__instance.amount} because {__instance.item.name} is UNNAMED");
            
            __instance.character.AddExtraStamina(actualAmount);
        }
    }

    [HarmonyPatch(typeof(Action_AddOrRemoveThorns), nameof(Action_AddOrRemoveThorns.RunAction))]
    [HarmonyPrefix]
    public static void AddOrRemoveThornsPrefix(Action_AddOrRemoveThorns __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            __runOriginal = false;

            var actualThornCount = Mathf.RoundToInt(__instance.thornCount * UnnamedPlugin.RandomUnnamedModifier);
            
            UnnamedPlugin.Log.LogInfo(
                $"Applying {actualThornCount:+0;-0;0} thorns instead of {__instance.thornCount} because {__instance.item.name} is UNNAMED");
            
            int i = actualThornCount;

            if (i > 0)
            {
                while (i > 0)
                {
                    if (__instance.specificBodyPart)
                    {
                        var vector = Vector3.Lerp(__instance.minOffset, __instance.maxOffset, Random.Range(0f, 1f));
                        var transform = __instance.character.GetBodypart(__instance.location).transform;
                        var position = transform.position + transform.TransformVector(vector);
                        __instance.character.refs.afflictions.AddThorn(position);
                    }
                    else
                    {
                        __instance.character.refs.afflictions.AddThorn(999);
                    }

                    i--;
                }
            }
            else
            {
                for (; i < 0; i++)
                {
                    __instance.character.refs.afflictions.RemoveRandomThornLinq();
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(RopeShooter), nameof(RopeShooter.OnPrimaryFinishedCast))]
    [HarmonyPrefix]
    public static void LaunchRopePrefix(RopeShooter __instance)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            var mod = UnnamedPlugin.RandomUnnamedModifier;
            
            UnnamedPlugin.Log.LogInfo(
                $"Setting rope length to {__instance.length * mod} instead of {__instance.length} because {__instance.item.name} is UNNAMED");

            __instance.length *= mod;
        }
    }
    
    [HarmonyPatch(typeof(Action_RaycastDart), nameof(Action_RaycastDart.FireDart))]
    [HarmonyPrefix]
    public static void RaycastSelfPrefix(Action_RaycastDart __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            if (UnnamedPlugin.RandomUnnamedBool)
            {
                __runOriginal = false;

                var chara = __instance.character;
                
                if (chara)
                {
                    Debug.Log("no u");
                    __instance.DartImpact(chara, __instance.spawnTransform.position,
                        chara.GetBodypart(BodypartType.Head).transform.position);
                }
            }
        }
    }

    [HarmonyPatch(typeof(CompassPointer), nameof(CompassPointer.UpdateHeadingPirate))]
    [HarmonyPrefix]
    public static void UpdateHeadingPrefix(CompassPointer __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject) && Luggage.ALL_LUGGAGE.Count > 0 && __instance.item.inActiveList)
        {
            __runOriginal = false;

            var currentDistance = float.MaxValue;
            var found = false;
            foreach (var item in Luggage.ALL_LUGGAGE)
            {
                if (item.TryGetComponent<LuggageBrandHandler>(out var lbh) && lbh.isUnnamed && item.Center() is var center && center.SquareDistance(__instance.transform.position) < currentDistance)
                {
                    currentDistance = center.SquareDistance(__instance.transform.position);
                    __instance.currentLuggageVector = center - __instance.transform.position;
                    found = true;
                }
            }

            if (!found)
            {
                __instance.heading =  Quaternion.Euler(0.0f, Time.time * __instance.warpSpeed, 0.0f) * Vector3.forward;
            }
            else
            {
                __instance.heading = __instance.currentLuggageVector;
                
            }

        }
    }
    

    [HarmonyPatch(typeof(Action_ApplyAffliction), nameof(Action_ApplyAffliction.RunAction))]
    [HarmonyPrefix]
    public static void ApplyAfflictionPrefix(Action_ApplyAffliction __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            __runOriginal = false;

            if (__instance.affliction == null)
            {
                Debug.LogError("Your affliction is null bro");
                return;
            }

            var affcp = __instance.affliction.Copy();

            UnnamedPlugin.ApplyUnnamedModifierToAffliction(ref affcp);
            
            affcp.totalTime *= UnnamedPlugin.RandomUnnamedModifier;

            UnnamedPlugin.Log.LogInfo(
                $"Setting {affcp.GetType().Name}'s total time to {affcp.totalTime} instead of {__instance.affliction.totalTime} because {__instance.item.name} is UNNAMED");


            __instance.character.refs.afflictions.AddAffliction(affcp);

            if (__instance.extraAfflictions != null)
            {
                Affliction[] extraAfflictions = __instance.extraAfflictions;
                
                foreach (var affliction in extraAfflictions)
                {
                    var acp = affliction.Copy();

                    UnnamedPlugin.ApplyUnnamedModifierToAffliction(ref acp);

                    acp.totalTime *= UnnamedPlugin.RandomUnnamedModifier;

                    UnnamedPlugin.Log.LogInfo(
                        $"Setting {acp.GetType().Name}'s total time to {acp.totalTime} instead of {affliction.totalTime} because {__instance.item.name} is UNNAMED");
                    
                    __instance.character.refs.afflictions.AddAffliction(acp);
                }
                
            }
        }
    }

    [HarmonyPatch(typeof(Rope), nameof(Rope.AddCharacterClimbing))]
    [HarmonyPostfix]
    public static void AddCharacterClimbingPostfix(Rope __instance, Character character)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject) &&
            __instance.gameObject.TryGetComponent(out UnnamedRopeBreaker nnrb))
        {
            // Not using RPCs since non-local character can also start rope-climbing
            nnrb.AddPlayerToRope(character);
        }
    }
    
    [HarmonyPatch(typeof(Rope), nameof(Rope.RemoveCharacterClimbing))]
    [HarmonyPostfix]
    public static void RemoveCharacterClimbingPostfix(Rope __instance, Character character)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject) &&
            __instance.gameObject.TryGetComponent(out UnnamedRopeBreaker nnrb))
        {

            // Not using RPCs since non-local character can also start rope-climbing
            nnrb.RemovePlayerFromRope(character);
        }
    }

    [HarmonyPatch(typeof(Parasol), nameof(Parasol.ToggleOpen))]
    [HarmonyPrefix]
    public static void ToggleOpenPrefix(Parasol __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.item.gameObject))
        {
            // Closing always works 
            __runOriginal = __instance.isOpen || (1.0f * UnnamedPlugin.RandomUnnamedModifier) >=
                1.0f;
        }
    }

    [HarmonyPatch(typeof(AOE), nameof(AOE.Explode))]
    [HarmonyPrefix]
    public static void ExplodePrefix(AOE __instance, ref bool __runOriginal)
    {
        if (UnnamedPlugin.IsUnnamed(__instance.gameObject))
        {
            __runOriginal = false;

            if (__instance.range == 0f)
            {
                return;
            }

            var actualAmount = __instance.statusAmount * UnnamedPlugin.RandomUnnamedModifier;

            Collider[] array = Physics.OverlapSphere(__instance.transform.position, __instance.range,
                HelperFunctions.GetMask(
                    __instance.mask));

            var list = new List<Character>();

            for (var i = 0; i < array.Length; i++)
            {
                CharacterRagdoll.TryGetCharacterFromCollider(array[i], out var character);

                if (character != null && !list.Contains(character))
                {
                    var distanceToCharacter = Vector3.Distance(__instance.transform.position, character.Center);

                    if (distanceToCharacter > __instance.range)
                    {
                        continue;
                    }

                    var factor = __instance.GetFactor(distanceToCharacter);

                    if (factor < __instance.minFactor || (__instance.requireLineOfSigh && (bool) HelperFunctions
                            .LineCheck(__instance.transform.position, character.Center,
                                HelperFunctions.LayerType.TerrainMap)
                            .transform))
                    {
                        continue;
                    }

                    list.Add(character);
                    var zero = Vector3.zero;
                    zero = ((!__instance.useSingleDirection)
                        ? (character.Center - __instance.transform.position).normalized
                        : __instance.singleDirectionForwardTF.forward);

                    if (Mathf.Abs(__instance.statusAmount) > 0f)
                    {
                        if (__instance.illegalStatus != "")
                        {
                            if (__instance.illegalStatus.ToUpperInvariant().Equals("BLIND"))
                            {
                                var affliction_Blind = new Affliction_Blind
                                {
                                    totalTime = actualAmount * factor
                                };

                                UnnamedPlugin.Log.LogInfo(
                                    $"Applying {__instance.illegalStatus}  to {character.characterName} with an total time of {actualAmount * factor} instead of {__instance.statusAmount * factor} because {__instance.name} is UNNAMED");
                                
                                character.refs.afflictions.AddAffliction(affliction_Blind);
                            }
                            else
                            {

                                UnnamedPlugin.Log.LogInfo(
                                    $"Applying {__instance.illegalStatus} to {character.characterName} with an amount of {actualAmount * factor} instead of {__instance.statusAmount * factor} because {__instance.name} is UNNAMED");

                                character.AddIllegalStatus(__instance.illegalStatus, actualAmount * factor);
                            }
                        }
                        else
                        {

                            UnnamedPlugin.Log.LogInfo(
                                $"Applying {actualAmount * factor} {__instance.statusType} to {character.characterName} instead of {__instance.statusAmount * factor} because {__instance.name} is UNNAMED");
                            
                            character.refs.afflictions.AdjustStatus(__instance.statusType, actualAmount * factor);
                            if (__instance.addtlStatus.Length != 0)
                            {
                                for (var j = 0; j < __instance.addtlStatus.Length; j++)
                                {

                                    UnnamedPlugin.Log.LogInfo(
                                        $"Applying {actualAmount * factor} {__instance.addtlStatus[j]} to {character.characterName} instead of {__instance.statusAmount * factor} because {__instance.name} is UNNAMED");
                                    
                                    character.refs.afflictions.AdjustStatus(__instance.addtlStatus[j],
                                        actualAmount * factor);
                                }
                            }
                        }
                    }

                    if (__instance.hasAffliction)
                    {
                        var acp = __instance.affliction.Copy();

                        UnnamedPlugin.ApplyUnnamedModifierToAffliction(ref acp);
                        
                        acp.totalTime *= UnnamedPlugin.RandomUnnamedModifier;
                        
                        UnnamedPlugin.Log.LogInfo(
                            $"Setting {acp.GetType().Name}'s total time to {acp.totalTime} instead of {__instance.affliction.totalTime} because {__instance.gameObject.name} is UNNAMED");

                        character.refs.afflictions.AddAffliction(__instance.affliction);
                    }

                    character.AddForce(zero * factor * __instance.knockback, 0.7f, 1.3f);
                    if (__instance.fallTime > 0f && character.photonView.IsMine)
                    {
                        var falltime = factor * (__instance.fallTime * UnnamedPlugin.RandomUnnamedModifier);
                        
                        UnnamedPlugin.Log.LogInfo(
                            $"Making {character.characterName} fall for {falltime} instead of {factor * __instance.fallTime} because {__instance.gameObject.name} is UNNAMED");

                        character.Fall(factor * __instance.fallTime);
                    }
                }
                else
                {
                    if (!__instance.canLaunchItems)
                    {
                        continue;
                    }

                    var launchedItem = array[i].GetComponentInParent<Item>();

                    if (!(launchedItem != null) || !launchedItem.photonView.IsMine ||
                        launchedItem.itemState != 0)
                    {
                        continue;
                    }

                    var distanceToLaunchedItem = Vector3.Distance(__instance.transform.position, launchedItem.Center());

                    if (distanceToLaunchedItem > __instance.range)
                    {
                        continue;
                    }

                    var launchFactor = __instance.GetFactor(distanceToLaunchedItem);
                    if (!(launchFactor < __instance.minFactor) && (!__instance.requireLineOfSigh || !HelperFunctions
                            .LineCheck(__instance.transform.position, launchedItem.Center(),
                                HelperFunctions.LayerType.TerrainMap).transform))
                    {
                        if (__instance.procCollisionEvents &&
                            launchedItem.TryGetComponent<EventOnItemCollision>(out var component))
                        {
                            component.TriggerEvent();
                        }

                        if (__instance.cooksItems)
                        {
                            launchedItem.cooking.FinishCooking();
                        }

                        var launchDirection = (launchedItem.Center() - __instance.transform.position).normalized;

                        launchedItem.rig.AddForce(launchDirection * launchFactor * __instance.knockback *
                                                  __instance.itemKnockbackMultiplier,
                            ForceMode.Impulse);
                    }
                }
            }
        }
    }
}