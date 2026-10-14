using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/ConditionalDialogueDatabase")]
public class ConditionalDialogueDatabase : ScriptableObject
{
    [Header("Conditional Dialogues")]
    public List<ConditionalDialogueSet> conditionalDialogues;
}
