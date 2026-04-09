using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedRescueHook : RescueHook
{
    public bool initilized = false;

    public float originalMaxWallHookTime = 1f;

    public float originalMaxScoutHookTime = 2f;

    public float originalMaxLiftDistance = 10f;

    public float originalStopPullDistance = 5f;
    public float originalExtraDragOther = 5f;
    public float originalExtraDragSelf = 5f;

    public float originalStopPullFriendDistance = 5f;
    public AnimationCurve originalPullStreightCurve = null!;
    public float originalDragForce = 100f;
    public float originalLaunchForce = 100f;

    public static readonly AnimationCurve CheapPullCurve = new AnimationCurve([
        new Keyframe(0f, 0f, 0.7656034827232361f, 0.7656034827232361f, 0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.0611865371465683f, 0.12154553830623627f, 2.260493040084839f, 2.260493040084839f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.08146797120571136f, 0.055668897926807404f, 1.8049219846725464f, 1.8049219846725464f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.10174952447414398f, 0.18898552656173706f, 0.18176765739917755f, 0.18176765739917755f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.12203089892864227f, 0.20241129398345947f, 0.3971366882324219f, 0.3971366882324219f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.22343848645687103f, 0.3675885498523712f, -0.23029838502407074f, -0.23029838502407074f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.26400136947631836f, 0.3053727447986603f, 0.1550886482000351f, 0.1550886482000351f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.36540889739990234f, 0.3709683418273926f, 0.004185794852674007f, 0.004185794852674007f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.4465348720550537f, 0.45702189207077026f, -0.23031577467918396f, -0.23031577467918396f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.46681642532348633f, 0.43654316663742065f, 0.398702472448349f, 0.398702472448349f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.5073793530464172f, 0.45465630292892456f, -0.03340112045407295f, -0.03340112045407295f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.6389407515525818f, 0.5520424246788025f, 0.4354163706302643f, 0.4354163706302643f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.7067189812660217f, 0.5297003388404846f, 0.6874133944511414f, 0.6874133944511414f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.7507572770118713f, 0.6159927845001221f, 0.37300944328308105f, 0.37300944328308105f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.7710387706756592f, 0.6468660235404968f, 0.90341717004776f, 0.90341717004776f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.8116017580032349f, 0.7305435538291931f, 0.12526513636112213f, 0.12526513636112213f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.8318832516670227f, 0.7360053062438965f, -0.1320735365152359f, -0.1320735365152359f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.8521647453308105f, 0.7302467823028564f, 1.242547631263733f, 1.242547631263733f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.8724462389945984f, 0.7844229936599731f, 1.7651773691177368f, 1.7651773691177368f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.8927277326583862f, 0.8473890423774719f, -1.3040691614151f, -1.3040691614151f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.9130092263221741f, 0.7905305624008179f, 1.799979329109192f, 1.799979329109192f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.9332907199859619f, 0.8690110445022583f, 3.542067766189575f, 3.542067766189575f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.9535722136497498f, 1.02344810962677f, -0.9682108163833618f, -0.9682108163833618f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(0.9738537073135376f, 0.9812334179878235f, -0.4908786416053772f, -0.4908786416053772f,
            0.3333333432674408f, 0.3333333432674408f),
        new Keyframe(1f, 0.9923655986785889f, 0.8811438083648682f, 0.8811438083648682f, 0.3333333432674408f,
            0.3333333432674408f)
    ]);

    [PunRPC]
    public new void RPCA_LetGo()
    {
        base.RPCA_LetGo();
    }

    public new void Fire()
    {
        if (!initilized)
        {
            originalDragForce = dragForce;
            originalLaunchForce = launchForce;
            originalMaxLiftDistance = maxLiftDistance;
            originalMaxScoutHookTime = maxScoutHookTime;
            originalMaxWallHookTime = maxWallHookTime;
            originalExtraDragOther = extraDragOther;
            originalExtraDragSelf = extraDragSelf;
            originalStopPullDistance = stopPullDistance;
            originalStopPullFriendDistance = stopPullFriendDistance;
            originalPullStreightCurve = pulLStrengthCurve;
            
            CheapPullCurve.postWrapMode = WrapMode.PingPong;
            CheapPullCurve.preWrapMode = WrapMode.PingPong;

            initilized = true;
        }

        dragForce = originalDragForce *  UnnamedPlugin.RandomModifier(12.5f/100);
        
        launchForce = Mathf.Clamp(originalLaunchForce * UnnamedPlugin.RandomUnnamedModifier, 0.0f, originalLaunchForce * 1.25f ); 
        
        maxLiftDistance = originalMaxLiftDistance * UnnamedPlugin.RandomUnnamedModifier;

        maxScoutHookTime = originalMaxScoutHookTime * UnnamedPlugin.RandomUnnamedModifier;

        maxWallHookTime = originalMaxWallHookTime * UnnamedPlugin.RandomUnnamedModifier;

        stopPullDistance = originalStopPullDistance * UnnamedPlugin.RandomUnnamedModifier;

        extraDragOther = originalExtraDragOther * UnnamedPlugin.RandomModifier(12.5f / 100);
        extraDragSelf = originalExtraDragSelf * UnnamedPlugin.RandomModifier(12.5f / 100);

        stopPullFriendDistance =
            originalStopPullFriendDistance * UnnamedPlugin.RandomUnnamedModifier;
        
#if DEBUG
        pulLStrengthCurve =  CheapPullCurve;
#else
        pulLStrengthCurve = UnnamedPlugin.RandomUnnamedBool ? CheapPullCurve : originalPullStreightCurve;
#endif

        

        UnnamedPlugin.Log.LogInfo(nameof(Fire));
        UnnamedPlugin.Log.LogInfo($"Launching {nameof(UnnamedRescueHook)} {(pulLStrengthCurve.Equals(CheapPullCurve) ? "badly" : "correctly")} with a launch force of {launchForce} instead of {originalLaunchForce}");

        sinceFire = 0.0f;
        for (int i = 0, length = rescueShot.Length; i < length; ++i)
        {
            rescueShot[i].Play(transform.position);
        }

        var hit = GetHit(out var endFire);

        if (hit.transform)
        {
            var componentInParent = hit.transform.GetComponentInParent<Character>();
            UnnamedPlugin.Log.LogDebug(
                $"Hit: {hit.collider.name} Rig: {hit.rigidbody}, !hit.rigidbody: {!hit.rigidbody}");

            UnnamedPlugin.Log.LogDebug(hit.collider.gameObject);

            // evil
            var shouldHitCharacter = UnnamedPlugin.RandomUnnamedBool;

            if (componentInParent && shouldHitCharacter)
            {
                UnnamedPlugin.Log.LogInfo(
                    $"Grabbing {componentInParent.characterName} for at most {maxScoutHookTime} seconds instead of {originalMaxWallHookTime}");
                photonView.RPC(nameof(RPCA_RescueCharacter), RpcTarget.All, componentInParent.photonView);
            }
            else
            {
                UnnamedPlugin.Log.LogInfo(
                    $"Grabbing {hit.collider.gameObject.name} for at most {maxWallHookTime} seconds instead of {originalMaxWallHookTime}");
                photonView.RPC(nameof(RPCA_RescueWall), RpcTarget.All, false, hit.point);
            }
        }
        else
        {
            UnnamedPlugin.Log.LogInfo(
                $"Grabbing at {endFire} for at most {maxWallHookTime} seconds instead of {originalMaxWallHookTime}");
            photonView.RPC(nameof(RPCA_RescueWall), RpcTarget.All, true, endFire);
        }
    }
}