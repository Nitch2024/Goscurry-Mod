using BepInEx;
using DG.Tweening;
using HarmonyLib;
using Holoville.GameLib.GUISystems.MenuSystems;
using Holoville.HO2DToolkit;
using System.Reflection;
using UnityEngine;
namespace Goscurry;

static class AccessExtensions
{
    public static object Call(this object o, string methodName, params object[] args)
    {
        var mi = o.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return mi.Invoke(o, args);
    }

    public static T GetInstanceField<T>(this object instance, string fieldName)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        return (T) field.GetValue(instance);
    }
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Goscurry.exe")]
public class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
    }
}

// Skips the first screen directly to main menu
[HarmonyPatch(typeof(Holoville.GameLib.GUISystems.IntroGUI), "CombuEventHandler")]
class _IntroGUI
{
    static void Postfix(Holoville.GameLib.GUISystems.IntroGUI __instance)
    {
        //__instance.Call("ContinueMain"); // This skips the save creation so not good if no save is present
        __instance.Call("ButtonEventHandler", new HOtk2dButtonEvent(HOtk2dButtonEventType.Press, __instance.GetInstanceField<HOtk2dButton>("_btContinueMain")));
    }
}

// Disable menu animation creating misaligned keyboard indicators
[HarmonyPatch(typeof(Holoville.GameLib.GUISystems.MenuSystems.Components.HomePanel), "Start")]
class _HomePanel
{
    static void Postfix(Holoville.GameLib.GUISystems.MenuSystems.Components.HomePanel __instance)
    {
        __instance.GetInstanceField<Sequence>("_shakeTween").Call("Reset");
    }
}

[HarmonyPatch(typeof(Holoville.GameLib.GUISystems.MenuSystems.Components.MainMenu))]
class _MainMenu
{
    // Activate keyboard for main menu at screen setup
    [HarmonyPostfix]
    [HarmonyPatch("ScreenSetup")]
    static void ScreenSetupPostfix(Holoville.GameLib.GUISystems.MenuSystems.Components.MainMenu __instance)
    {
        IPanel homePanel = __instance.GetInstanceField<IPanel>("_homePanel");
        Holoville.HO2DToolkit.HOMenuControlSystem.Activate(homePanel.transform, "Test", false);
    }

    // Deactivate keyboard indicator before transition
    [HarmonyPostfix]
    [HarmonyPatch("ChangeState")]
    static void ChangeStatePostfix(Holoville.GameLib.GUISystems.MenuSystems.Components.MainMenu __instance)
    {
        Holoville.HO2DToolkit.HOMenuControlSystem.DeactivateAll();
    }

    // Activate keyboard indicator after transition
    [HarmonyPostfix]
    [HarmonyPatch("ChangeStateComplete")]
    static void ChangeStateCompletePostfix(Holoville.GameLib.GUISystems.MenuSystems.Components.MainMenu __instance)
    {
        Transform currPanelTrans = __instance.GetInstanceField<Transform>("_currPanelTrans");
        if (currPanelTrans == null)
        {
            IPanel homePanel = __instance.GetInstanceField<IPanel>("_homePanel");
            Holoville.HO2DToolkit.HOMenuControlSystem.Activate(homePanel.transform, "Test", false);
        }
        else
        {
            Holoville.HO2DToolkit.HOMenuControlSystem.Activate(currPanelTrans, "Test", false);
        }
    }
}

// Enable keyboard for all popups and make them instant to get rid of scaling highlight issue
[HarmonyPatch(typeof(Holoville.GameLib.GUISystems.Components.PopupDialog), "Open")]
class _PopupDialog
{
    static void Prefix(ref bool instant)
    {
        instant = true;
    }

    static void Postfix(Holoville.GameLib.GUISystems.Components.PopupDialog __instance)
    {
        HOMenuControlSystem.Activate(__instance.transform, "Test", false);
    }
}
