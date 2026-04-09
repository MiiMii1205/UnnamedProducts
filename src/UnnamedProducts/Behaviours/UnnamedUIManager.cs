using UnnamedProducts.Behaviours.Item.GarbageBag;
using UnnamedProducts.Behaviours.Item.GarbageBag.GUI;
using UnityEngine;

namespace UnnamedProducts.Behaviours;

public class UnnamedUIManager : MonoBehaviour
{
    public bool GarbageBagActive => m_garbageBagScreen.gameObject.activeSelf;

    public UnnamedGarbageBagScreen m_garbageBagScreen = null!;
    
    public static UnnamedUIManager Instance = null!;

    private void Awake()
    {
        Instance = this;
    }

    public void CloseGarbageBagScreen()
    {
        UnnamedPlugin.Log.LogDebug("Close garbage bag screen");
        Character.localCharacter.data.usingBackpackWheel = false;
        m_garbageBagScreen.gameObject.SetActive(value: false);
    }

    public void OpenGarbageBagScreen(UnnamedGarbageBagReference backpackReference)
    {
        if (!GUIManager.instance.wheelActive && !GUIManager.instance.windowBlockingInput)
        {
            Character.localCharacter.data.usingBackpackWheel = true;
            m_garbageBagScreen.InitWheel(backpackReference);
        }
    }
}