using UnnamedProducts.Extensions;
using UnityEngine;

namespace UnnamedProducts.Behaviours.Item;

public class UnnamedStatusField : StatusField
{
    public new void Update()
    {
        if (Character.localCharacter &&
            Character.localCharacter.Center.CompareDistanceFast(transform.position, radius))
        {
            if (doNotApplyIfStatusesMaxed && Character.localCharacter.refs.afflictions.statusSum >= 1.0f)
            {
                inflicting = false;
            }
            else
            {
                var amountPerSecond = statusAmountPerSecond * UnnamedPlugin.RandomUnnamedModifier;

                UnnamedPlugin.Log.LogInfo(
                    $"Adjusting {Character.localCharacter.characterName}'s {statusType} by {amountPerSecond * Time.deltaTime} instead of {statusAmountPerSecond * Time.deltaTime} because {gameObject.name} is UNNAMED");

                Character.localCharacter.refs.afflictions.AdjustStatus(statusType,
                    amountPerSecond * Time.deltaTime);

                foreach (var additionalStatus in additionalStatuses)
                {
                    var additionalAmountPerSecond =
                        additionalStatus.statusAmountPerSecond * UnnamedPlugin.RandomUnnamedModifier;

                    UnnamedPlugin.Log.LogInfo(
                        $"Adjusting {Character.localCharacter.characterName}'s {additionalStatus.statusType} by {additionalAmountPerSecond * Time.deltaTime} instead of {additionalStatus.statusAmountPerSecond * Time.deltaTime} because {gameObject.name} is UNNAMED");

                    Character.localCharacter.refs.afflictions.AdjustStatus(additionalStatus.statusType,
                        additionalAmountPerSecond * Time.deltaTime);
                }

                if (!inflicting && statusAmountOnEntry != 0.0f &&
                    Time.time - lastEnteredTime > entryCooldown)
                {
                    var actualAmountOnEntry = statusAmountOnEntry *
                                              UnnamedPlugin.RandomUnnamedModifier;
                    UnnamedPlugin.Log.LogInfo(
                        $"Adjusting {Character.localCharacter.characterName}'s {statusType} (on entry) by {actualAmountOnEntry} instead of {statusAmountOnEntry} because {gameObject.name} is UNNAMED");

                    Character.localCharacter.refs.afflictions.AdjustStatus(statusType, actualAmountOnEntry);

                    lastEnteredTime = Time.time;
                }

                inflicting = true;
            }
        }
        else
        {
            inflicting = false;
        }
    }
}