using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueContainer", menuName = "Dialogue/Dialogue Container")]
public class DialogueContainerSO : ScriptableObject
{
    [TextArea(2, 5)]
    public List<string> dialogues = new List<string>();
    public List<Sprite> imageNext = new List<Sprite>();
}
