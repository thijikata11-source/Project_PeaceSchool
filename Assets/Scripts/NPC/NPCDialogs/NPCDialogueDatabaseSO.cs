using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCDatabase", menuName = "Dialogue/NPC Dialogue Database")]
public class NPCDialogueDatabaseSO : ScriptableObject
{
    [System.Serializable]
    public class DialogueConditionRule
    {
        [Header("Conditions")]
        public bool anyTime = true;
        public DialogueTimePeriod timePeriod;

        public bool anyCharacter = true;
        public int frontCharacterId;

        [Header("Party Presence Validation")]
        public bool noPartyRequirements = true;
        public List<int> requiredPartyMemberIds = new List<int>();

        [Header("Possible Conversations")]
        public List<DialogueContainerSO> alternateDialogues = new List<DialogueContainerSO>();
    }

    public List<DialogueConditionRule> rules = new List<DialogueConditionRule>();

    public DialogueContainerSO GetDialogue(DialogueTimePeriod currentTime, int currentFrontId, CharacterSwitchSystem switchSystem)
    {
        int highestMatchScore = -1;
        List<DialogueContainerSO> bestMatches = new List<DialogueContainerSO>();

        foreach (var rule in rules)
        {
            // 1. Environmental Time check evaluation
            bool timeMatches = rule.anyTime || rule.timePeriod == currentTime;
            if (!timeMatches) continue;

            // 2. Active Front Leader check evaluation
            bool characterMatches = rule.anyCharacter || rule.frontCharacterId == currentFrontId;
            if (!characterMatches) continue;

            // 3. Companion Party verification lookup engine
            bool partyRequirementsMet = true;
            if (!rule.noPartyRequirements && switchSystem != null)
            {
                foreach (int requiredId in rule.requiredPartyMemberIds)
                {
                    if (!IsIdInActiveRoster(requiredId, switchSystem))
                    {
                        partyRequirementsMet = false;
                        break;
                    }
                }
            }

            // Reject the rule immediately if the companion conditions are missing
            if (!partyRequirementsMet) continue;

            // 4. CALCULATE STABILITY WEIGHT SCORE
            int ruleStrictnessScore = 0;
            if (!rule.anyTime) ruleStrictnessScore += 1;
            if (!rule.anyCharacter) ruleStrictnessScore += 2;
            if (!rule.noPartyRequirements) ruleStrictnessScore += 4;

            // FIX: If we find a rule that is strictly more specialized than anything seen before,
            // we MUST forcefully clear out any lower-tier rules that were cached previously!
            if (ruleStrictnessScore > highestMatchScore)
            {
                highestMatchScore = ruleStrictnessScore;
                bestMatches.Clear(); // Clears any old general fallback strings
                bestMatches.AddRange(rule.alternateDialogues);
            }
            else if (ruleStrictnessScore == highestMatchScore)
            {
                // Only pool together dialogues if they share the EXACT same level of strictness
                bestMatches.AddRange(rule.alternateDialogues);
            }
        }

        // Return a random selection from our highest priority valid pool
        if (bestMatches.Count > 0)
        {
            int randomIndex = Random.Range(0, bestMatches.Count);
            return bestMatches[randomIndex];
        }

        return null;
    }

    private bool IsIdInActiveRoster(int targetId, CharacterSwitchSystem switchSystem)
    {
        for (int i = 0; i < switchSystem.charactersInGameNumber; i++)
        {
            if (switchSystem.characterSlots[i] != null && switchSystem.characterSlots[i].characterId == targetId)
            {
                return true;
            }
        }
        return false;
    }
}
