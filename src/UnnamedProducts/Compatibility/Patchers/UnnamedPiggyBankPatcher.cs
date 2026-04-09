using HarmonyLib;
using UnnamedProducts.Behaviours.Item.GarbageBag;
using PiggyBank.Behaviours;
using PiggyBank.Behaviours.GUI;

namespace UnnamedProducts.Compatibility.Patchers;

public static class UnnamedPiggyBankPatcher
{
    [HarmonyPatch(typeof(PiggyBankScreen), nameof(PiggyBankScreen.InitWheel))]
    [HarmonyPostfix]
    public static void InitWheelPostfix(PiggyBankScreen __instance, PiggyBankReference pg)
    {
        if (__instance.currentlyHeldItem.enabled && Character.localCharacter.data.currentItem is UnnamedGarbageBagController)
        {
            __instance.currentlyHeldItem.enabled = false;
        }
    }
    
    [HarmonyPatch(typeof(PiggyBankZone), "canInteract", MethodType.Getter)]
    [HarmonyPostfix]
    public static void CanInteractPostfix(PiggyBankZone __instance, ref bool __result)
    {
        if (__result && Character.localCharacter.data.currentItem is UnnamedGarbageBagController)
        {
            __result = false;
        }
    }
}