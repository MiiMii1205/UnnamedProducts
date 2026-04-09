using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using pworld.Scripts.Extensions;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedVine : OnNetworkStart
{
    private static readonly int JitterAmount = Shader.PropertyToID("_JitterAmount");

    private static readonly int BreakAmount = Shader.PropertyToID("_BreakAmount");

    private static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");

    public int maxPeople = 5;
    
    public SFX_Instance[] breakSfx;

    [Range(0f, 1f)] public float breakPoint = 0.4f;

    [Range(0f, 1f)] public float breakChance = 0.5f;

    public Vector3 axisMul = new Vector3(1f, 1f, 1f);

    public float shakeScale = 30f;

    public float fallTime = 5f;

    public float amount = 1f;

    public float startShakeDistance = 10f;

    public float startShakeAmount = 400f;

    public float climbingScreenShake = 240f;

    public float screenShakeTickTime = 0.2f;

    public bool debug;

    public bool isShaking;

    public float localTouchStamp;

    public int holdsPeople;

    public int peopleOnVine;

    public Transform fullMesh;

    public ParticleSystem breakParticles;

    private readonly Dictionary<Character, float> peopleOnVineDict = new Dictionary<Character, float>();

    private new PhotonView photonView;

    private Renderer rend;

    private AudioSource source;

    private JungleVine jungleVine;

    private List<Character> cachedPeopleOnVineList = new List<Character>();

    private float timeUntilBreak;

    private bool isBreaking;

    private bool isFallen;

    public bool LocalCharacterOnBridge => Time.time - localTouchStamp < 0.2f;

    private float DistanceToLocalPlayer => Vector3.Distance(Character.localCharacter.Center, base.transform.position);

    public override void NetworkStart()
    {
#if DEBUG
        holdsPeople = 0;
#else
	    holdsPeople = UnityEngine.Random.Range(1, maxPeople);
#endif

        UnnamedPlugin.Log.LogInfo(
            $"{nameof(UnnamedVine)} {this.gameObject.name} can hold {holdsPeople} players");
        
        photonView.RPC(nameof(SyncHoldsPeopleRPC), RpcTarget.All, holdsPeople);
        
        photonView.RPC(nameof(SyncBreakParticlesRPC), RpcTarget.All);
    }
    


    private void Awake()
    {
        jungleVine ??= GetComponent<JungleVine>();
        photonView ??= GetComponent<PhotonView>();
        source ??= GetComponent<AudioSource>();
        rend ??= GetComponentInChildren<Renderer>();

        rend.material.SetFloat(JitterAmount, 0f);
        rend.material.SetFloat(AlphaClip, 0.01f);
        
        if (holdsPeople == 0)
        {
            holdsPeople = 5;
        }
    }

    public void Update()
    {
        if (isShaking)
        {
            source.pitch += 0.1f * Time.deltaTime;
            source.volume += 0.1f * Time.deltaTime;
            source.enabled = true;
        }

        if (photonView.IsMine && isBreaking && !isShaking && !isFallen)
        {
            timeUntilBreak -= Time.deltaTime;
            if (timeUntilBreak < 0f)
            {
                photonView.RPC(nameof(ShakeVine_Rpc), RpcTarget.All);
            }
        }
    }
    
    public void AddPlayerToVine(Character character)
    {
        if (isBreaking)
        {
            return;
        }

        if (character.IsLocal)
        {
            localTouchStamp = Time.time;
        }

        if (photonView.IsMine)
        {
            if (!peopleOnVineDict.TryAdd(character, 0f))
            {
                peopleOnVineDict[character] = 0f;
            }

            UpdatePeopleOnVine();

            UnnamedPlugin.Log.LogInfo(
                $"AddPlayerToRope: {character}. {peopleOnVineDict.Keys.Count}, {holdsPeople}");
            
            if (peopleOnVine >= holdsPeople && !isShaking && holdsPeople < peopleOnVine)
            {
                isBreaking = true;
                timeUntilBreak = UnityEngine.Random.Range(2.5f, 7.5f);
            }
        }
    }

    private void FixedUpdate()
    {
        peopleOnVine = 0;

        if (debug)
        {
            UnnamedPlugin.Log.LogDebug($"FixedUpdate: {Time.frameCount}, People on Vine: {peopleOnVine}");
        }

        UpdatePeopleOnVine();
    }

    private void UpdatePeopleOnVine()
    {
        if (peopleOnVineDict.Keys.Count <= 0)
        {
            return;
        }

        cachedPeopleOnVineList = peopleOnVineDict.Keys.ToList();
        
        foreach (var cachedPeopleOnVine in cachedPeopleOnVineList)
        {
            if (cachedPeopleOnVine.data.isVineClimbing && cachedPeopleOnVine.data.heldVine == jungleVine)
            {
                peopleOnVine++;
            }
        }
    }

    public void RemovePlayerFromVine(Character character)
    {
        if (isBreaking)
        {
            return;
        }

        if (photonView.IsMine)
        {
            peopleOnVineDict.Remove(character);
            UnnamedPlugin.Log.LogInfo(
                $"RemovedPlayerToRope: {character}. {peopleOnVineDict.Keys.Count}, {holdsPeople}");
        }
    }

    [PunRPC]
    public void SyncHoldsPeopleRPC(int holdsPeople)
    {
        this.holdsPeople = holdsPeople;
    }
    
    [PunRPC]
    public void SyncBreakParticlesRPC()
    {
        breakParticles.transform.position = jungleVine.GetPosition(0.5f);
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        
        if (!PhotonNetwork.IsMasterClient && !newPlayer.IsLocal)
        {
            photonView.RPC(nameof(SyncHoldsPeopleRPC), newPlayer, holdsPeople);

            if (isFallen)
            {
                photonView.RPC(nameof(BreakImmediateRPC), newPlayer);
            }
            
        }
    }


    [PunRPC]
    private void BreakImmediateRPC()
    {
        isFallen = true;
        DestroyImmediate(jungleVine.colliderRoot.gameObject);
        
        rend.material.SetFloat(BreakAmount, 1f);

        UnnamedPlugin.Log.LogDebug($"Destroy: {base.gameObject}");
        UnnamedPlugin.Log.LogDebug(base.gameObject);
    }

    [PunRPC]
    private void ShakeVine_Rpc()
    {
        UnnamedPlugin.Log.LogDebug("start shake rock");
        isShaking = true;
        source.enabled = true;
        source.Play();
        if (!isShaking)
        {
            source.volume = 0.125f;
        }

        if (DistanceToLocalPlayer < startShakeDistance)
        {
            UnnamedPlugin.Log.LogDebug($"start shake {startShakeAmount}");
            GamefeelHandler.instance.AddPerlinShake(startShakeAmount);
        }

        StartCoroutine(RockShake());

        IEnumerator RockShake()
        {
            UnnamedPlugin.Log.LogDebug("Start shaking");
            var duration = 0f;
            var timeUntilShake = 0f;
            rend.material.SetFloat(JitterAmount, 1f);
            while (duration < fallTime)
            {
                timeUntilShake -= Time.deltaTime;
                
                if (LocalCharacterOnBridge && timeUntilShake <= 0f)
                {
                    GamefeelHandler.instance.AddPerlinShake(climbingScreenShake);
                    UnnamedPlugin.Log.LogDebug("Clime shake");
                    timeUntilShake = screenShakeTickTime;
                }
                
                duration += Time.deltaTime;
                yield return null;
            }

            rend.material.SetFloat(JitterAmount, 0f);
            UnnamedPlugin.Log.LogDebug("Done shaking");
            if (isShaking)
            {
                var pos = base.transform.position;
                
                if (jungleVine.colliderRoot)
                {
                    pos = jungleVine.GetPosition(jungleVine.GetPercentFromSegmentIndex(
                        jungleVine.GetClosestChild(Character.localCharacter.Center)));
                    
                }
                
                foreach (var t in breakSfx)
                {
                    t.Play(pos);
                }
            }

            isShaking = false;
            fullMesh.localPosition = 0.ToVec();
            source.volume = 0f;
            source.Stop();
            if (photonView.IsMine)
            {
                photonView.RPC(nameof(Fall_Rpc), RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void Fall_Rpc()
    {
        StartCoroutine(DestroyRoutine());

        IEnumerator DestroyRoutine()
        {
            isFallen = true;
            DestroyImmediate(jungleVine.colliderRoot.gameObject);
            
            foreach (var chara in Character.AllCharacters)
            {
                if (chara.data.isVineClimbing && chara.data.heldVine.gameObject == gameObject)
                {
                    chara.refs.vineClimbing.Stop();
                    chara.Fall(1, 0.25f);
                }
            }
            
            if (breakParticles)
            {
                breakParticles.Play();
            }

            var normalizedTime = 0f;
            while (normalizedTime < 1f)
            {
                normalizedTime += Time.deltaTime * 0.7f;
                rend.material.SetFloat(BreakAmount, normalizedTime);
                yield return null;
            }

            UnnamedPlugin.Log.LogDebug($"Destroy: {base.gameObject}");
            UnnamedPlugin.Log.LogDebug(base.gameObject);

            yield return null;
        }
    }
}