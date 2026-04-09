using System;
using Peak.Afflictions;
using Photon.Pun;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class Action_Detergent: ItemAction
{
    public override void RunAction()
    {
        character.refs.afflictions.RemoveAffliction(Affliction.AfflictionType.Blind);
        character.refs.afflictions.RemoveAffliction(Affliction.AfflictionType.Sunscreen);
        character.refs.afflictions.RemoveAffliction(Affliction.AfflictionType.ZombieBite);
        character.refs.afflictions.RemoveAffliction(Affliction.AfflictionType.Glowing);
        character.refs.afflictions.RemoveAllThorns();
        character.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Spores, Mathf.Abs(5f));
        character.refs.afflictions.SubtractStatus(CharacterAfflictions.STATUSTYPE.Web, Mathf.Abs(5f));
        
        character.view.RPC(nameof(StickyItemRemover.RPC_WashOff), RpcTarget.All, character.view.ViewID);
         
    }
}