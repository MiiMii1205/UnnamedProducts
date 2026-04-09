using System.Collections;
using PEAKLib.Core;
using Photon.Pun;
using pworld.Scripts.Extensions;
using UnityEngine;
using Zorro.Core.CLI;

namespace UnnamedProducts.Behaviours;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PositionSyncer))]
public class StickyFireballController : MonoBehaviourPun
{
    private static readonly Quaternion SmokeRotation = new(-0.707106829f, 2.98023153e-08f, -7.45057882e-09f, 0.707106769f);
    public Rigidbody rb;
    public global::Item? m_item;
    public DestroyAfterTime destroyAfterTime;

    public bool m_isStuck;
    public bool m_canStick;
    private float m_startupTime = 0.125f;
    private Collider m_collider;
    private ParticleSystem m_smokeParticles;
    private ParticleSystem m_fireParticles;
    private AudioSource m_loop;
    private CharacterBurnController? m_burningCharacter;
    
    private Coroutine rainCheckCoroutine;

    public SFX_Instance[] fireStart;

    public SFX_Instance[] extinguish;
    
    public GameObject disableOnExtingush;
    private bool m_isStuckToCharacter;
    private bool m_isStuckToItem;
    
    public bool m_isBurning;

    private void Awake()
    {
        rb ??= GetComponent<Rigidbody>();
        m_collider ??= rb.GetComponent<Collider>();
        m_loop ??= rb.GetComponent<AudioSource>();
        
        destroyAfterTime ??= GetComponent<DestroyAfterTime>();
        
        if (!m_smokeParticles)
        {
            var parts = GetComponentsInChildren<ParticleSystem>();
            
            foreach (var particleSystem in parts)
            {
                if (particleSystem.gameObject.name == "VFX_Smoke_Lit")
                {
                    m_smokeParticles = particleSystem;
                }
                if (particleSystem.gameObject.name == "VFX_Fire")
                {
                    m_fireParticles = particleSystem;
                }
            }
        }

        disableOnExtingush ??= transform.Find("DisableOnExtinguish").gameObject;

        destroyAfterTime.enabled = false;

        rb.AddForce((-Physics.gravity * 0.25f), ForceMode.Impulse);
        

    }

    private void Start()
    {
        m_smokeParticles.Play(true);
        m_isBurning = true;
        
        foreach (var t in fireStart)
        {
            t.Play(transform.position);
        }
        
        StartCoroutine(EnableSickyness());
        
        rainCheckCoroutine = StartCoroutine(CheckForRain());
    }

    private void Update()
    {
        m_smokeParticles.transform.rotation = SmokeRotation;
    }

    private IEnumerator CheckForRain()
    {
        var fireballTransform = transform;
        
        while (WindChillZone.instance)
        {
            yield return new WaitForSeconds(2f);
            
            // We know the WindChillZone in Tropics is rain. 
            if (WindChillZone.instance.windActive && WindChillZone.instance.windZoneBounds.Contains(fireballTransform.position))
            {
                UnnamedPlugin.Log.LogInfo($"Wind is blowing. Fireball will extinguish.");
                this.Extinguish();
                break;
            }
            
        }
    }

    private IEnumerator EnableSickyness()
    {
        m_canStick = false;
        yield return new WaitForSeconds(m_startupTime);
        UnnamedPlugin.Log.LogInfo($"Fireball {gameObject.name} is now sticky.");
        m_canStick = true;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (m_canStick && !this.m_isStuck && (!other.gameObject.TryGetComponent<StickyFireballController>(out _)))
        {
            if (other.gameObject.GetComponentInParent<Character>() is {IsLocal: true} chara)
            {
                UnnamedPlugin.Log.LogInfo($"Sticking Fireball {gameObject.name} to {chara.characterName}...");
                // Only stick to the character if it's the local character
                photonView.RequestOwnership();
                rb.isKinematic = true;
                m_collider.enabled = false;

                transform.position = other.GetContact(0).point;

                var target = other.rigidbody.transform;
                var bodypart = -1;

                if (chara.refs.ragdoll.partList.FindClosest(other.GetContact(0).point) is { } closest)
                {
                    target = closest.rig.transform;
                    bodypart = (int) closest.partType;
                }
                
                m_burningCharacter = chara.gameObject.GetOrAddComponent<CharacterBurnController>();
                m_burningCharacter.m_hasFire = true;
                m_burningCharacter.RegisterFire(this);
                
                rb.transform.SetParent(target, true);
                rb.transform.localPosition = 0f.ToVec();

                photonView.RPC(nameof(BurnCharacterRPC), RpcTarget.All, chara.view.ViewID, bodypart);
                
                m_isStuckToCharacter = true;
            }
            else if (other.gameObject.GetComponentInParent<global::Item>() is {itemState: ItemState.Ground} item)
            {
                // Only stick to the item  if it's on the ground

                UnnamedPlugin.Log.LogInfo($"Sticking Fireball {gameObject.name} to {item.UIData.itemName} [{item.gameObject.name}]...");

                rb.isKinematic = true;
                m_collider.enabled = false;

                transform.position = other.GetContact(0).point;
                
                rb.transform.SetParent(other.rigidbody.transform, true);
                rb.transform.localPosition = 0f.ToVec();
                
                photonView.RPC(nameof(BurnItemRPC), RpcTarget.All, item.GetComponent<PhotonView>());

            }
            else
            {

                UnnamedPlugin.Log.LogInfo($"Sticking Fireball {gameObject.name} to {other.gameObject.name}...");
                
                rb.isKinematic = true;
                m_collider.enabled = false;

                transform.position = other.GetContact(0).point;
                rb.transform.SetParent(other.gameObject.transform, true);
            }

            photonView.RPC(nameof(StickToSurfaceRPC), RpcTarget.All);
        }
    }


    [PunRPC]
    public void StickToSurfaceRPC()
    {
        UnnamedPlugin.Log.LogInfo($"Fireball {gameObject.name} is stuck on {gameObject.transform.parent.name}");

        this.m_isStuck = true;

        if (photonView.IsMine)
        {
            StartCoroutine(DoExtinguish(destroyAfterTime.time));    
        }
    }

    [PunRPC]
    public void BurnItemRPC(PhotonView item)
    {
        this.m_item = item.GetComponent<global::Item>();
        UnnamedPlugin.Log.LogInfo($"Fireball {gameObject.name} will stick to item {m_item.UIData.itemName} [{m_item.gameObject.name}]...");
        
        // Can't pick up an item that is burning
        // item.blockInteraction = true;
    }
    
    [PunRPC]
    public void CookItemRPC()
    {
        if(m_item)
        {
            UnnamedPlugin.Log.LogInfo($"Cooking {m_item.UIData.itemName} [{m_item.gameObject.name}]...");
            
            // Once extinguished, make the item available again
            // m_item.blockInteraction = false;
            
        }
    }
    
    [PunRPC]
    public void BurnCharacterRPC(int cid, int bodypart)
    {
        if (Character.GetCharacterWithPhotonID(cid, out var chara))
        {
            UnnamedPlugin.Log.LogInfo($"Fireball {gameObject.name} will stick to character {chara.characterName}...");
        }
    }

    public void Extinguish(float delay = 0f)
    {
        UnnamedPlugin.Log.LogInfo($"Fireball {gameObject.name} got extinguished");
        
        if(rainCheckCoroutine != null)
        {
            StopCoroutine(rainCheckCoroutine);
        }

        if (photonView.IsMine)
        {
            StartCoroutine(DoExtinguish(delay));
        }
    }


    private void StickTo(GameObject sticking)
    {
        if (sticking.GetComponentInParent<Character>() is {IsLocal: true} chara)
        {
            // Only stick to the character if it's the local character
            photonView.RequestOwnership();
            rb.isKinematic = true;
            m_collider.enabled = false;

            var target = sticking.transform;
            
            m_burningCharacter = chara.gameObject.GetOrAddComponent<CharacterBurnController>();
            m_burningCharacter.m_hasFire = true;
            m_burningCharacter.RegisterFire(this);

            rb.transform.SetParent(target, true);
            rb.transform.localPosition = 0f.ToVec();

            photonView.RPC(nameof(BurnCharacterRPC), RpcTarget.All, chara.view.ViewID, -1);

            m_isStuckToCharacter = true;
        }
        else if (sticking.GetComponentInParent<global::Item>() is {itemState: ItemState.Ground} item)
        {
            // Only stick to the item  if it's on the ground

            UnnamedPlugin.Log.LogInfo(
                $"Sticking Fireball {gameObject.name} to {item.UIData.itemName} [{item.gameObject.name}]...");

            rb.isKinematic = true;
            m_collider.enabled = false;
            rb.transform.SetParent(sticking.transform, true);
            rb.transform.localPosition = 0f.ToVec();

            photonView.RPC(nameof(BurnItemRPC), RpcTarget.All, item.GetComponent<PhotonView>());
        }
        else
        {

            UnnamedPlugin.Log.LogInfo($"Sticking Fireball {gameObject.name} to {sticking.name}...");

            rb.isKinematic = true;
            m_collider.enabled = false;
            rb.transform.SetParent(sticking.transform, true);
            
        }

        photonView.RPC(nameof(StickToSurfaceRPC), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_Extinguish()
    {
        m_smokeParticles.Stop();
        m_fireParticles.Stop();
        
        photonView.RPC(nameof(CookItemRPC), RpcTarget.All);

        if (m_item && m_item.view.IsMine && m_item.TryGetComponent<ItemCooking>(out var cooking))
        {
            cooking.FinishCooking();
        }

        if (m_burningCharacter && m_burningCharacter.photonView.IsMine)
        {
            m_burningCharacter.DeRegisterFire(this);
        }

        for (int k = 0; k < extinguish.Length; k++)
        {
            extinguish[k].Play(transform.position);
        }

        m_loop.Stop();

        disableOnExtingush.SetActive(false);
        m_isBurning = false;

        if (photonView.IsMine)
        {
            StartCoroutine(DestroyWhenCleared());
        }
        
    }

    private IEnumerator DestroyWhenCleared()
    {
        yield return new WaitUntil(() => m_smokeParticles.particleCount == 0 && m_fireParticles.particleCount == 0);
        PhotonNetwork.Destroy(gameObject);
    }

    private IEnumerator DoExtinguish(float delay = 0f)
    {
        yield return new WaitForSeconds(delay);
        photonView.RPC(nameof(RPC_Extinguish), RpcTarget.All);
    }

    public static GameObject FireballPrefab = null!;
    public static void Burn(Bodypart bodypart)
    {
        var fireball =
            NetworkPrefabManager.SpawnNetworkPrefab(FireballPrefab.name, bodypart.rig.transform.position,
                Quaternion.identity);

        UnnamedPlugin.Log.LogInfo(
            $"Setting {bodypart.character.characterName}'s {bodypart.partType} on fire!");
        
        var sfc = fireball.GetComponent<StickyFireballController>();

        sfc.StickTo(bodypart.gameObject);
    }

    [ConsoleCommand]
    public static void SetOnFire(int viewId)
    {
        var go = PhotonNetwork.GetPhotonView(viewId).gameObject;
        
        var fireball =
            NetworkPrefabManager.SpawnNetworkPrefab(FireballPrefab.name, go.transform.position,
                Quaternion.identity);

        UnnamedPlugin.Log.LogInfo(
            $"Setting {go.name} on fire!");
        
        var sfc = fireball.GetComponent<StickyFireballController>();
        sfc.StickTo(go);
        
    }
}