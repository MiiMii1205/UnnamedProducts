using System.Runtime.CompilerServices;
using HarmonyLib;
using UnnamedProducts.Compatibility.Patchers;

namespace UnnamedProducts.Compatibility;

public static class UnnamedPiggyBankCompatibility
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
                    PiggyBank.Plugin.Id);
                UnnamedPlugin.Log.LogInfo($"PiggyBank support is {((bool) _enabled ? "enabled" : "disabled")}");
            }

            return (bool) _enabled;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void LoadCompatibilityBundle(UnnamedPlugin loader, Harmony harmony)
    {
        harmony.PatchAll(typeof(UnnamedPiggyBankPatcher));
    }
}