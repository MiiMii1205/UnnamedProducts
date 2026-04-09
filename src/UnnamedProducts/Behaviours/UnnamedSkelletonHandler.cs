using System;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using Zorro.Core;

namespace UnnamedProducts.Behaviours;

public class UnnamedSkeletonHandler: MonoBehaviourPun
{
    public Character m_character = null!;
    
    [PunRPC]
    public void RPC_ClearAllStatusesBones(bool excludeCurse,
        int[] otherExclusions)
    {
        // Clear all statues
        int num = Enum.GetNames(typeof(CharacterAfflictions.STATUSTYPE)).Length;
        for (int i = 0; i < num; i++)
        {
            var statusType = (CharacterAfflictions.STATUSTYPE) i;
            
            if ((!excludeCurse || statusType != CharacterAfflictions.STATUSTYPE.Curse) &&
                !otherExclusions.Contains((int) statusType))
            {
                m_character.refs.afflictions.SubtractStatus(statusType, Mathf.Abs(5));
            }
        }
    }
    
    [PunRPC]
    public void RPC_ChangeStatsBones(int statusInt, float changeAmount, bool ifSkeleton, int feederId)
    {
        var statusType = (CharacterAfflictions.STATUSTYPE) statusInt;
        
        Character.GetCharacterWithPhotonID(feederId, out var feeder);
        
        if (ifSkeleton && !m_character.data.isSkeleton)
        {
            return;
        }

        bool passedOut = m_character.data.passedOut;
        if (changeAmount < 0f)
        {
            if (statusType == CharacterAfflictions.STATUSTYPE.Poison)
            {
                m_character.refs.afflictions.ClearPoisonAfflictions();
                int num = Mathf.RoundToInt(
                    Mathf.Min(m_character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Poison),
                        Mathf.Abs(changeAmount)) * 100f);
                if (feeder != null)
                {
                    GameUtils.instance.IncrementFriendPoisonHealing(num, feeder.photonView.Owner);
                }
                else
                {
                    Singleton<AchievementManager>.Instance.IncrementSteamStat(STEAMSTATTYPE.PoisonHealed, num);
                }
            }

            if (statusType == CharacterAfflictions.STATUSTYPE.Injury && feeder != null)
            {
                int amt = Mathf.RoundToInt(
                    Mathf.Min(m_character.refs.afflictions.GetCurrentStatus(CharacterAfflictions.STATUSTYPE.Injury),
                        Mathf.Abs(changeAmount)) * 100f);
                GameUtils.instance.IncrementFriendHealing(amt, feeder.photonView.Owner);
            }

            m_character.refs.afflictions.SubtractStatus(statusType, Mathf.Abs(changeAmount));
        }
        else
        {
            m_character.refs.afflictions.AddStatus(statusType, Mathf.Abs(changeAmount));
        }

        var statusSum = m_character.refs.afflictions.statusSum;
        if (passedOut && statusSum <= 1f)
        {
            Debug.Log("LIFE WAS SAVED");
            if (feeder != null)
            {
                GameUtils.instance.ThrowEmergencyPreparednessAchievement(feeder.photonView.Owner);
            }
        }
        
    }
    
}