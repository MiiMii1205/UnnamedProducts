using System.Collections;
using System.Linq;
using UnnamedProducts.Extensions;
using Photon.Pun;
using pworld.Scripts.Extensions;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedRopeBreaker : OnNetworkStart, IPunOwnershipCallbacks
{
    private static readonly int JitterAmount = Shader.PropertyToID("_JitterAmount");

    private static readonly int BreakAmount = Shader.PropertyToID("_RopeBreakAmount");

    private static readonly int AlphaClip = Shader.PropertyToID("_AlphaClip");

    public int maxPeople = 5;

    [Range(0f, 1f)] public float breakChance = 0.5f;

    public SFX_Instance[] breakSfx;
    public SFX_Instance startBreakSfx;

    public float shakeScale = 30f;

    public float fallTime = 5f;

    public float amount = 1f;

    public float startShakeDistance = 10f;

    public float startShakeAmount = 400f;

    public float climbingScreenShake = 240f;

    public float screenShakeTickTime = 0.2f;

    public Vector3 axisMul = new Vector3(1f, 1f, 1f);

    public bool debug;

    public bool isShaking;

    public float localTouchStamp;

    public int holdsPeople;

    public int peopleOnRope;

    public Transform fullMesh;

    public ParticleSystem breakParticles;

    private Renderer rend;

    private AudioSource source;

    private Rope rope;

    private float timeUntilBreak;

    private bool isBreaking;

    private bool isFallen;

    public bool LocalCharacterOnBridge => Time.time - localTouchStamp < 0.2f;

    private float SqrDistanceToLocalPlayer => Character.localCharacter.Center.SquareDistance(transform.position);

    public override void NetworkStart()
    {
        
#if DEBUG
        holdsPeople = 0;
#else
        holdsPeople = UnityEngine.Random.Range(1, maxPeople);
#endif

        UnnamedPlugin.Log.LogInfo(
            $"UnnamedRope {this.rope.name} can hold {holdsPeople} players");

        photonView.RPC(nameof(SyncHoldsPeopleRPC), RpcTarget.All, holdsPeople);
    }

    private void Awake()
    {
        rope ??= GetComponent<Rope>();

        source ??= GetComponent<AudioSource>();
        rend ??= GetComponentInChildren<Renderer>();

        rend.material.SetFloat(JitterAmount, 0f);
        rend.material.SetFloat(AlphaClip, 0.01f);

        if (holdsPeople == 0)
        {
            holdsPeople = 5;
        }
    }

    [PunRPC]
    public void SyncHoldsPeopleRPC(int newHoldsPeople)
    {
        holdsPeople = newHoldsPeople;
    }

    public void OnOwnershipRequest(PhotonView targetView, Photon.Realtime.Player requestingPlayer)
    {
        UnnamedPlugin.Log.LogDebug(
            $"OnOwnershipRequest received for {targetView.name} from {requestingPlayer.NickName}. Granting ownership.");

        if (targetView == photonView)
        {
            photonView.TransferOwnership(requestingPlayer);
        }
    }

    public void OnOwnershipTransfered(PhotonView targetView, Photon.Realtime.Player previousOwner)
    {
        UnnamedPlugin.Log.LogDebug(
            $"OnOwnershipTransferred for {targetView.name} from {previousOwner.NickName}. Is Local Player now owner: {Equals(PhotonNetwork.LocalPlayer, targetView.Owner)}.");

        if ((targetView == photonView &&
             Equals(PhotonNetwork.LocalPlayer, targetView.Owner)))
        {
            UnnamedPlugin.Log.LogDebug(
                $"OWNERSHIP TRANSFERRED TO ME for rope: {targetView.name}. Please try pressing the cut key again.");
        }
    }


    public void OnOwnershipTransferFailed(PhotonView targetView, Photon.Realtime.Player senderOfFailedRequest)
    {
        UnnamedPlugin.Log.LogWarning(
            $"OnOwnershipTransferFailed for {targetView.name} to {senderOfFailedRequest.NickName}.");
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

    public void BreakAnchorJoint()
    {
        UnnamedPlugin.Log.LogInfo($"Breaking {gameObject.name} immediately.");

        if (photonView.IsMine)
        {
            DestroyImmediate(rope.simulationSegments.First<Transform>()
                .GetComponent<ConfigurableJoint>());
        }

        rope.ropeBoneVisualizer.StartTransform = null;
    }

    [PunRPC]
    private void BreakImmediateRPC()
    {
        isFallen = true;
        BreakAnchorJoint();

        rend.material.SetFloat(BreakAmount, 1f);

        rope.ropeBoneVisualizer.StartTransform = null;

        UnnamedPlugin.Log.LogDebug($"Destroy: {gameObject}");
        UnnamedPlugin.Log.LogDebug(gameObject);
    }

    [PunRPC]
    private void Fall_Rpc()
    {
        StartCoroutine(DestroyRoutine());

        IEnumerator DestroyRoutine()
        {
            isFallen = true;
            BreakAnchorJoint();
            if (breakParticles != null)
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

            UnnamedPlugin.Log.LogDebug($"Broke: {gameObject}");
            UnnamedPlugin.Log.LogDebug(gameObject);
            yield return null;
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
                photonView.RPC(nameof(ShakeRope_Rpc), RpcTarget.All);
            }
        }
    }

    private void FixedUpdate()
    {
        peopleOnRope = 0;

        if (debug)
        {
            UnnamedPlugin.Log.LogDebug($"FixedUpdate: {Time.frameCount}, peopleOnRope: {peopleOnRope}");
        }

        if (rope.charactersClimbing.Count <= 0)
        {
            return;
        }

        foreach (var cachedPeopleOnBridge in rope.charactersClimbing)
        {
            if (cachedPeopleOnBridge.data.isRopeClimbing && cachedPeopleOnBridge.data.heldRope == rope)
            {
                peopleOnRope = rope.charactersClimbing.Count;
            }
        }
    }

    [PunRPC]
    private void ShakeRope_Rpc()
    {
        UnnamedPlugin.Log.LogDebug("start shake rock");
        isShaking = true;
        source.enabled = true;
        source.Play();
        
        if (!isShaking)
        {
            source.volume = 0.125f;
        }

        if (SqrDistanceToLocalPlayer < (startShakeDistance * startShakeDistance))
        {
            UnnamedPlugin.Log.LogDebug($"start shake {startShakeAmount}");
            GamefeelHandler.instance.AddPerlinShake(startShakeAmount);
        }

        StartCoroutine(RockShake());

        IEnumerator RockShake()
        {
            UnnamedPlugin.Log.LogDebug("Start shaking");

            startBreakSfx.Play(rope.attachedToAnchor.transform.position);

            var duration = 0f;
            var timeUntilShake = 0f;
            rend.material.SetFloat(JitterAmount, 1f);
            while (duration < fallTime)
            {
                timeUntilShake -= Time.deltaTime;

                if (LocalCharacterOnBridge && timeUntilShake <= 0f)
                {
                    GamefeelHandler.instance.AddPerlinShake(climbingScreenShake);
                    UnnamedPlugin.Log.LogDebug("Climb shake");
                    timeUntilShake = screenShakeTickTime;
                }
                
                duration += Time.deltaTime;
                
                yield return null;
            }

            rend.material.SetFloat(JitterAmount, 0f);
            UnnamedPlugin.Log.LogDebug("Done shaking");
            
            if (isShaking)
            {
                var pos = Character.localCharacter.transform.FindClosest(rope.GetRopeSegments()
                    .ConvertAll((t) => t.position));

                foreach (var t in breakSfx)
                {
                    t.Play(pos);
                }
            }

            isShaking = false;
            // fullMesh.localPosition = 0.ToVec();
            source.volume = 0f;
            source.Stop();
            if (photonView.IsMine)
            {
                photonView.RPC(nameof(Fall_Rpc), RpcTarget.All);
            }
        }
    }

    public void AddPlayerToRope(Character character)
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
            UnnamedPlugin.Log.LogInfo($"AddPlayerToRope: {character}. {rope.charactersClimbing.Count}, {holdsPeople} ");

            if (rope.charactersClimbing.Count >= holdsPeople && !isShaking && holdsPeople <
                rope.charactersClimbing.Count)
            {
                isBreaking = true;
                timeUntilBreak = Random.Range(2.5f, 7.5f);
            }
        }
    }

    public void RemovePlayerFromRope(Character character)
    {
    }
}