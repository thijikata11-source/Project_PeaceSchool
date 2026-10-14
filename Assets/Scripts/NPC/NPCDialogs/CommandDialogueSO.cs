using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCommandDialogue", menuName = "Dialogue/Command Dialogue Asset")]
public class CommandDialogueSO : ScriptableObject
{
    [System.Serializable]
    public class LeaderSpecificDialogueRule
    {
        [Header("Condition")]
        [Tooltip("Check this if this profile is a catch-all fallback for ANY character ID.")]
        public bool anyLeaderCharacter = false;
        [Tooltip("The explicit Leader Character ID needed to unlock this text package block.")]
        public int requiredFrontLeaderId;

        [Header("Dialogue Folders")]
        public DialogueContainerSO greetingDialogue;
        public DialogueContainerSO giveDialogue;
        public DialogueContainerSO recruitDialogue;
        public DialogueContainerSO trainingDialogue;
        public DialogueContainerSO releaseDialogue;
        public DialogueContainerSO checkDialogue;
        public DialogueContainerSO nothingDialogue;

        public DialogueContainerSO GetContainerByType(CommandDialogueType contextType)
        {
            switch (contextType)
            {
                case CommandDialogueType.Greeting: return greetingDialogue;
                case CommandDialogueType.Give: return giveDialogue;
                case CommandDialogueType.Recruit: return recruitDialogue;
                case CommandDialogueType.Training: return trainingDialogue;
                case CommandDialogueType.Release: return releaseDialogue;
                case CommandDialogueType.Check: return checkDialogue;
                case CommandDialogueType.Nothing: return nothingDialogue;
                default: return null;
            }
        }
    }

    [Tooltip("Add different profile rules here. Put specific character overrides at the top, and your catch-all default fallback profile at the very bottom.")]
    public List<LeaderSpecificDialogueRule> dialogueRules = new List<LeaderSpecificDialogueRule>();

    /// <summary>
    /// Loops through rules dynamically to find a matching or fallback dialogue asset depending on the leader.
    /// </summary>
    public DialogueContainerSO GetDialogueByContext(CommandDialogueType contextType, int currentFrontId)
    {
        LeaderSpecificDialogueRule fallbackRule = null;

        foreach (var rule in dialogueRules)
        {
            if (rule.anyLeaderCharacter)
            {
                fallbackRule = rule; // Cache the default option in case no character IDs match
                continue;
            }

            if (rule.requiredFrontLeaderId == currentFrontId)
            {
                return rule.GetContainerByType(contextType); // Exact ID match found!
            }
        }

        // No explicit match found; return the default fallback configuration profile if setup
        if (fallbackRule != null)
        {
            return fallbackRule.GetContainerByType(contextType);
        }

        return null;
    }
}

public enum CommandDialogueType
{
    Greeting,
    Give,
    Recruit,
    Training,
    Release,
    Check,
    Nothing
}
