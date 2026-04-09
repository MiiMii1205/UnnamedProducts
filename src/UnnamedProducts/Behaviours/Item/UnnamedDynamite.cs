using System.Collections.Generic;
using System.Globalization;
using Photon.Pun;
using UnityEngine;
using Zorro.Settings;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedDynamite : Dynamite
{
    public bool isDud;
    private float m_explosionScaleModifier = 1f;
    private bool m_extinguished;

    public float baseFuseTime;
    public bool IsADud
    {
        get
        {
            isDud = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultDud).Value;
            return isDud;
        }
        
    }

    public new void Start()
    {
        isDud = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultDud).Value;
        m_explosionScaleModifier = GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1),
            SetupExplosionScaleModifier).Value;

        startingFuseTime = GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 2),
            SetupDefaultFuseTime).Value;
        
        base.Start();
        // Don't make it explode when its cooked
        if (isDud)
        {
            var cook = item.GetComponent<ItemCooking>();

            var newAdditionalCookingBehaviors = new List<AdditionalCookingBehavior>();

            foreach (var cookAdditionalCookingBehavior in cook.additionalCookingBehaviors)
            {
                if (cookAdditionalCookingBehavior is not CookingBehavior_Explode)
                {
                    newAdditionalCookingBehaviors.Add(cookAdditionalCookingBehavior);
                }
            }

            cook.additionalCookingBehaviors = newAdditionalCookingBehaviors.ToArray();

            cook.explosionPrefab = null;
        }
    }

    
    public override void OnInstanceDataSet()
    {
        isDud = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultDud).Value;
        m_explosionScaleModifier = GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 1),
            SetupExplosionScaleModifier).Value;
        

        // Don't make it explode when its cooked
        if (isDud)
        {
            var cook = item.GetComponent<ItemCooking>();

            var newAdditionalCookingBehaviors = new List<AdditionalCookingBehavior>();

            foreach (var cookAdditionalCookingBehavior in cook.additionalCookingBehaviors)
            {
                if (cookAdditionalCookingBehavior is not CookingBehavior_Explode)
                {
                    newAdditionalCookingBehaviors.Add(cookAdditionalCookingBehavior);
                }
            }
            
            cook.additionalCookingBehaviors = newAdditionalCookingBehaviors.ToArray();
            
            cook.explosionPrefab = null;
        }

        startingFuseTime = GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey + 2),
            SetupDefaultFuseTime).Value;
            
        base.OnInstanceDataSet();
        
        UnnamedPlugin.Log.LogInfo(
            $"{nameof(UnnamedDynamite)} {item.name} is {(isDud ? "a dud" : $"not a dud and will be {string.Format(CultureInfo.InvariantCulture, "{0:+0%;-0%;0%}", (m_explosionScaleModifier - 1f))}% bigger with a {startingFuseTime} sec timer instead of {baseFuseTime}")}.");

    }

    private FloatItemData SetupExplosionScaleModifier()
    {
        return new FloatItemData
        {
            Value = UnnamedPlugin.RandomUnnamedModifier,
        };
    }
    private FloatItemData SetupDefaultFuseTime()
    {
        return new FloatItemData
        {
            Value = baseFuseTime * UnnamedPlugin.RandomUnnamedModifier,
        };
    }

    [PunRPC]
    public new void RPC_Explode()
    {
        if (DEBUG_PauseOnExplode)
        {
            Debug.Break();
        }

        var spawned = Instantiate(explosionPrefab, transform.position,
            transform.rotation);

        // Bigger Boom?
        
        UnnamedPlugin.Log.LogInfo("BIGGER BOOOOM!!!");

        if (spawned.GetComponentInChildren<AOE>(true) is { } aoe)
        {
            aoe.range *= m_explosionScaleModifier;
            aoe.knockback *= m_explosionScaleModifier;
            aoe.fallTime *= m_explosionScaleModifier;
        }

        foreach (var componentsInChild in spawned.GetComponentsInChildren<AudioSource>(true))
        {
            componentsInChild.minDistance *= m_explosionScaleModifier; 
            componentsInChild.maxDistance *= m_explosionScaleModifier; 
        }
        
        if (spawned.GetComponentInChildren<Light>(true) is{}li)
        {
            li.range *= m_explosionScaleModifier;
        }
            
        
        if (spawned.GetComponentInChildren<ExplosionEffect>(true) is{} explosion)
        {
            explosion.baseScale *= m_explosionScaleModifier;
            explosion.explosionRadius *= m_explosionScaleModifier; 
        }
        
        if (!spawned.activeSelf)
        {
            spawned.SetActive(true);
        }
        
        gameObject.SetActive(false);
        _hasExploded = true;
    }

    private new FloatItemData SetupDefaultFuel()
    {
        return new FloatItemData()
        {
            Value = startingFuseTime * UnnamedPlugin.RandomUnnamedModifier
        };
    }

    [PunRPC]
    public void RPC_Extinguish()
    {
        m_extinguished = true;
        GetData<BoolItemData>(DataEntryKey.FlareActive).Value = false;

        sparks.gameObject.SetActive(false);
        sparksPhotosensitive.gameObject.SetActive(false);

        Destroy(trackable.currentTracker.gameObject);
    }

    private new void Update()
    {
        if (!m_extinguished)
        {
            if (!GetData<BoolItemData>(DataEntryKey.FlareActive).Value)
            {
                TestLightWick();
                return;
            }

            if (!trackable.hasTracker)
            {
                EnableFlareVisuals();
            }

            if (setting.Value != OffOnMode.ON)
            {
                sparks.gameObject.SetActive(value: true);
            }
            else
            {
                sparksPhotosensitive.gameObject.SetActive(value: true);
            }

            fuseTime = GetData(DataEntryKey.Fuel, SetupDefaultFuel).Value;
            item.SetUseRemainingPercentage(fuseTime / startingFuseTime);
            if (!photonView.IsMine)
            {
                return;
            }

            fuseTime -= Time.deltaTime;
            if (fuseTime <= 0f)
            {
                if (!isDud)
                {
                    if (Character.localCharacter.data.currentItem == item)
                    {
                        Character.localCharacter.refs.afflictions.AddStatus(CharacterAfflictions.STATUSTYPE.Injury,
                            0.25f);
                        Player.localPlayer.EmptySlot(Character.localCharacter.refs.items.currentSelectedSlot);
                        Character.localCharacter.refs.afflictions.UpdateWeight();
                    }

                    photonView.RPC(nameof(RPC_Explode), RpcTarget.All);
                    PhotonNetwork.Destroy(gameObject);
                    item.ClearDataFromBackpack();
                }
                else
                {
                    photonView.RPC(nameof(RPC_Extinguish), RpcTarget.All);
                }

                fuseTime = 0f;
            }

            GetData(DataEntryKey.Fuel, SetupDefaultFuel).Value = fuseTime;
        }
    }

    private BoolItemData SetupDefaultDud()
    {
        return new BoolItemData
        {
            Value = UnnamedPlugin.RandomUnnamedBool
        };
    }
}