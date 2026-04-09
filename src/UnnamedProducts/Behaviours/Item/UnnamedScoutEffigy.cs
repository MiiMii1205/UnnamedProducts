using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Photon.Pun;
using Unity.Mathematics;
using UnityEngine;
using Zorro.Core;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedScoutEffigy: ScoutEffigy
{
    public bool isBad;
    public float revivalModifier;

    public override void OnInstanceDataSet()
    {
        isBad = GetData((DataEntryKey) UnnamedPlugin.UnnamedTotalBaseDataEntryKey, SetupDefaultIsBad).Value;
        revivalModifier = GetData((DataEntryKey) (UnnamedPlugin.UnnamedTotalBaseDataEntryKey+1), SetupDefaultRevival).Value;
        base.OnInstanceDataSet();
        UnnamedPlugin.Log.LogInfo($"{nameof(UnnamedScoutEffigy)} {item.name} is {(isBad ? "evil" : "not evil")} and will revive {string.Format(CultureInfo.InvariantCulture, "{0:+0%;-0%;0%}", revivalModifier)}% of players");
    }

    private BoolItemData SetupDefaultIsBad()
    {
        return new BoolItemData
        {
            Value = (1.0f * UnnamedPlugin.RandomUnnamedModifier) <
                    1.0f,
        };
    }
    private FloatItemData SetupDefaultRevival()
    {
        return new FloatItemData
        {
            Value = (1.0f * UnnamedPlugin.RandomUnnamedModifier) 
        };
    }

    public override GameObject FinishConstruction()
    {
        var deadTotal = Character.AllCharacters.Sum((chara) => (chara.data.dead || chara.data.fullyPassedOut ? 1 : 0));
        
        var amountOfRevives = 0f;
        
        if (deadTotal != 0)
        {
            amountOfRevives = (1f / deadTotal) * revivalModifier;
        }

        var actualAmountOfPlayerToRevive = Mathf.RoundToInt(amountOfRevives * deadTotal);

        UnnamedPlugin.Log.LogInfo($"Reviving {actualAmountOfPlayerToRevive} players...");

        if (actualAmountOfPlayerToRevive > 1)
        {
            List<Character> deadCharacters = [];

            foreach (var character in Character.AllCharacters)
            {
                if (character.data.dead || character.data.fullyPassedOut)
                {
                    deadCharacters.Add(character);
                }
            }

            for (var i = 1; i < amountOfRevives; ++i)
            {
                var character = deadCharacters.RandomSelection((_) => 1);

                character.photonView.RPC(nameof(Character.RPCA_ReviveAtPosition), RpcTarget.All, currentConstructHit.point +
                    Vector3.up *
                    (1f * (i + 1)), false);

                deadCharacters.Remove(character);

                if (Singleton<AchievementManager>.Instance)
                {
                    Singleton<AchievementManager>.Instance.AddToRunBasedInt(RUNBASEDVALUETYPE.ScoutsResurrected, 1);
                }

            }
        }

        base.FinishConstruction();


        if (isBad)
        {
            // Congratulations!
            if(GameHandler.IsOnIsland)
            {
                // removing the scout effigy from the face of the earth
                var chara = item.holderCharacter;
                var currentSelectedSlot = chara.refs.items.currentSelectedSlot;
                chara.player.EmptySlot(currentSelectedSlot);
                chara.refs.items.EquipSlot(currentSelectedSlot);
                chara.DieInstantly();
            }
        }
        
        return null!;
    }
}