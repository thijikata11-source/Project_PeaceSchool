using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class CharacterDialogues : MonoBehaviour
{
    [Header("Character Settings")]
    public string characterName;
    public TMP_Text characterNameText;
    public Animator characterAnimator;

    [Header("Dialogue UI")]
    public GameObject dialogueCanvas;
    public TMP_Text dialogueText;
    public Image nextImage;
    public Button interactUIButton;
    public RawImage characterPortrait;

    [Header("Camera Render")]
    public Camera characterCamera;
    public RenderTexture portraitTemplate;

    [Header("Animation Clips")]
    public AnimationClip neutral, smiling, blushing, angry, worried, crying, shocked, laughing;

    [Header("Special Animations")]
    public List<AnimationClip> specialAnimations = new List<AnimationClip>(); // ✅ drag custom clips here

    [Header("Dependencies")]
    public GlobalTimeSystem timeSystem;
    public WeatherCycleSystem weatherSystem;
    public PlayerProfile playerProfile;

    [Header("Dialogue Database")]
    public ConditionalDialogueDatabase dialogueDatabase;

    [Header("Scripts to Disable During Dialogue")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("Buttons to Disable On Trigger")]
    public List<Button> buttonsToDisableOnTrigger = new List<Button>();

    [Header("GameObjects to Disable During Dialogue")]
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("Decision System")]
    public DialogueDecisionManager decisionManager;

    private int currentLine = 0;
    private bool inDialogue = false;
    private bool typing = false;
    private bool playerInside = false;

    private List<DialogueEntry> activeDialogueSet = new List<DialogueEntry>();

    [Header("Typewriter Settings")]
    public float typewriterSpeed = 0.03f;

    private RenderTexture uniqueRT;

    [Header("Rotation Settings")]
    public Transform characterRoot;
    public Transform playerTransform;
    public float rotationSpeed = 5f;
    private Quaternion originalRotation;
    private bool rotateToPlayer = false;
    private bool rotateBack = false;

    public bool rotatePlayerToo = true; // ✅ rotate player to face character

    [Header("Character Acts")]
    public CharacterActs characterActs;   // assign in inspector

    private void Start()
    {
        interactUIButton.onClick.AddListener(OnInteractPressed);
        if (characterCamera != null) characterCamera.enabled = false;
    }

    private void OnDestroy()
    {
        if (uniqueRT != null)
        {
            uniqueRT.Release();
            Destroy(uniqueRT);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            foreach (var btn in buttonsToDisableOnTrigger)
                if (btn != null) btn.interactable = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            foreach (var btn in buttonsToDisableOnTrigger)
                if (btn != null) btn.interactable = true;
        }
    }

    void OnInteractPressed()
    {
        if (playerInside && !inDialogue)
            StartDialogue();
        else if (inDialogue && !typing)
            NextLine();
    }

    void StartDialogue()
    {
        inDialogue = true;
        currentLine = 0;
        dialogueCanvas.SetActive(true);

        if (characterNameText != null)
            characterNameText.text = characterName;

        if (characterRoot != null)
            originalRotation = characterRoot.rotation;

        rotateToPlayer = true;
        rotateBack = false;

        if (characterActs != null)
        {
            characterActs.enabled = false;
            if (characterAnimator != null)
            {
                characterAnimator.Rebind();
                characterAnimator.Update(0f);
            }
        }

        if (characterCamera != null && portraitTemplate != null)
        {
            if (uniqueRT != null)
            {
                uniqueRT.Release();
                Destroy(uniqueRT);
            }

            uniqueRT = new RenderTexture(portraitTemplate);
            characterCamera.targetTexture = uniqueRT;
            if (characterPortrait != null) characterPortrait.texture = uniqueRT;
            characterCamera.enabled = true;
        }

        foreach (var script in scriptsToDisable)
            if (script != null) script.enabled = false;

        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);

        if (timeSystem != null) timeSystem.enabled = false;

        activeDialogueSet = GetConditionalDialogue();
        if (activeDialogueSet.Count > 0)
            HandleDialogueEntry(activeDialogueSet[currentLine]);
    }

    void NextLine()
    {
        currentLine++;
        if (currentLine < activeDialogueSet.Count)
            HandleDialogueEntry(activeDialogueSet[currentLine]);
        else
            EndDialogue();
    }

    void HandleDialogueEntry(DialogueEntry entry)
    {
        if (entry.isDecisionPoint && entry.decisionSet != null)
        {
            dialogueText.text = ParseDialogueText(entry.decisionSet.prompt);
            decisionManager.ShowDecisionOptions(entry.decisionSet.options);
        }
        else
        {
            StartCoroutine(TypeLine(entry.lineText));
        }
    }

    void EndDialogue()
    {
        inDialogue = false;
        dialogueCanvas.SetActive(false);

        if (characterCamera != null) characterCamera.enabled = false;

        foreach (var script in scriptsToDisable)
            if (script != null) script.enabled = true;

        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(true);

        if (timeSystem != null) timeSystem.enabled = true;

        TriggerAnimation("neutral");

        if (characterRoot != null)
            characterRoot.rotation = originalRotation;

        if (characterActs != null)
            characterActs.enabled = true;
    }

    IEnumerator TypeLine(string line)
    {
        typing = true;
        dialogueText.text = "";
        nextImage.gameObject.SetActive(false);

        if (line.Contains("?success"))
        {
            if (decisionManager != null)
            {
                decisionManager.requirementsMet = "Yes";
                Debug.Log("RequirementsMet flipped to YES");
            }
            line = line.Replace("?success", "").Trim();
        }

        ApplyStatChanges(line);
        line = CleanStatCommands(line);

        string parsedLine = ParseDialogueText(line);

        foreach (char c in parsedLine)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        typing = false;
    }

    void ApplyStatChanges(string line)
    {
        if (characterActs != null && characterActs.characterSystem != null)
        {
            var cs = characterActs.characterSystem;

            if (line.Contains("affectionLevel+"))
                cs.affectionLevel += ExtractValue(line, "affectionLevel+");
            if (line.Contains("affectionLevel-"))
                cs.affectionLevel -= ExtractValue(line, "affectionLevel-");
            if (line.Contains("heartLevel+"))
                cs.heartLevel += ExtractValue(line, "heartLevel+");
            if (line.Contains("heartLevel-"))
                cs.heartLevel -= ExtractValue(line, "heartLevel-");

            cs.UpdateRelationship(playerProfile);
        }
    }

    int ExtractValue(string line, string keyword)
    {
        int start = line.IndexOf(keyword) + keyword.Length;
        string number = "";
        while (start < line.Length && char.IsDigit(line[start]))
        {
            number += line[start];
            start++;
        }
        return int.TryParse(number, out int result) ? result : 0;
    }

    string CleanStatCommands(string line)
    {
        line = Regex.Replace(line, @"affectionLevel[+-]\d+", "");
        line = Regex.Replace(line, @"heartLevel[+-]\d+", "");
        return line.Trim();
    }

    public string ParseDialogueText(string rawText)
    {
        string result = "";
        string[] words = rawText.Split(' ');

        foreach (string word in words)
        {
            if (word.StartsWith("!"))
            {
                TriggerAnimation(word.Substring(1));
            }
            else if (word.StartsWith("/"))
            {
                string keyword = word.Substring(1).ToLower();
                if (playerProfile != null)
                {
                    switch (keyword)
                    {
                        case "playername": result += playerProfile.PlayerName + " "; break;
                        case "playergold":
                        case "playergolds": result += playerProfile.PlayerGolds.ToString() + " "; break;
                        case "playerlumber":
                        case "playerlumbers": result += playerProfile.PlayerLumbers.ToString() + " "; break;
                        case "playerstamina": result += playerProfile.PlayerStamina.ToString() + " "; break;
                        case "playerhealth": result += playerProfile.PlayerHealthStatus + " "; break;
                        default: result += word + " "; break;
                    }
                }
            }
            else
            {
                result += word + " ";
            }
        }

        return result.TrimEnd();
    }

    List<DialogueEntry> GetConditionalDialogue()
    {
        string segment = timeSystem.GetCurrentDailySegment().Replace(" ", "");
        string weekday = timeSystem.weekDayText.text;
        string weather = weatherSystem.currentWeather;

        ConditionalDialogueSet bestMatch = null;
        int bestScore = -1;

        foreach (var set in dialogueDatabase.conditionalDialogues)
        {
            int score = 0;

            bool matchSegment = set.segmentCondition == SegmentCondition.Any || set.segmentCondition.ToString() == segment;

            // ✅ Flags Enum weekday check
            bool matchWeekday =
                set.weekdayCondition == WeekdayCondition.Any ||
                (set.weekdayCondition & (WeekdayCondition)System.Enum.Parse(typeof(WeekdayCondition), weekday)) != 0;

            bool matchWeather = set.weatherCondition == WeatherCondition.Any || set.weatherCondition.ToString() == weather;

            bool matchRequirements =
                set.requirementsCondition == RequirementsCondition.Any ||
                (set.requirementsCondition == RequirementsCondition.Yes && decisionManager.requirementsMet == "Yes") ||
                (set.requirementsCondition == RequirementsCondition.No && decisionManager.requirementsMet == "No");

            bool matchRelationship = true;
            if (characterActs != null && characterActs.characterSystem != null)
            {
                var cs = characterActs.characterSystem;
                matchRelationship =
                    set.relationshipCondition == RelationshipCondition.Any ||
                    set.relationshipCondition.ToString() == cs.RelationshipStatus.ToString();
            }

            if (matchSegment) score++;
            if (matchWeekday) score++;
            if (matchWeather) score++;
            if (matchRequirements) score++;
            if (matchRelationship) score++;

            if (matchSegment && matchWeekday && matchWeather && matchRequirements && matchRelationship && score > bestScore)
            {
                bestMatch = set;
                bestScore = score;
            }
        }

        if (bestMatch != null)
            return bestMatch.lines;

        return new List<DialogueEntry>() { new DialogueEntry { lineText = "...", isDecisionPoint = false } };
    }

    void TriggerAnimation(string animKeyword)
    {
        if (characterAnimator == null) return;
        AnimationClip clip = null;
        switch (animKeyword.ToLower())
        {
            case "neutral": clip = neutral; break;
            case "smiling": clip = smiling; break;
            case "blushing": clip = blushing; break;
            case "angry": clip = angry; break;
            case "worried": clip = worried; break;
            case "crying": clip = crying; break;
            case "shocked": clip = shocked; break;
            case "laughing": clip = laughing; break;
        }

        // ✅ Check special animations list
        if (clip == null && specialAnimations.Count > 0)
        {
            foreach (var anim in specialAnimations)
            {
                if (anim != null && anim.name.ToLower() == animKeyword.ToLower())
                {
                    clip = anim;
                    Debug.Log("Playing special animation: " + anim.name);
                    break;
                }
            }
        }

        if (clip != null)
            characterAnimator.CrossFade(clip.name, 0.2f);
    }

    public void LoadBranch(List<string> branchLines)
    {
        activeDialogueSet = new List<DialogueEntry>();
        foreach (var line in branchLines)
        {
            activeDialogueSet.Add(new DialogueEntry { lineText = line, isDecisionPoint = false });
        }

        currentLine = 0;

        if (activeDialogueSet.Count > 0)
            HandleDialogueEntry(activeDialogueSet[currentLine]);
        else
            EndDialogue();
    }

    public void OverrideDialogueWithSuccess(List<string> successLines)
    {
        activeDialogueSet = new List<DialogueEntry>();
        foreach (var line in successLines)
        {
            activeDialogueSet.Add(new DialogueEntry { lineText = line, isDecisionPoint = false });
        }

        currentLine = 0;
        if (activeDialogueSet.Count > 0)
            HandleDialogueEntry(activeDialogueSet[currentLine]);
        else
            EndDialogue();
    }

    void Update()
    {
        if (rotateToPlayer && characterRoot != null && playerTransform != null)
        {
            Vector3 direction = (playerTransform.position - characterRoot.position).normalized;
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            characterRoot.rotation = Quaternion.Slerp(characterRoot.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            if (rotatePlayerToo)
            {
                Vector3 playerDir = (characterRoot.position - playerTransform.position).normalized;
                playerDir.y = 0;
                Quaternion playerTargetRotation = Quaternion.LookRotation(playerDir);
                playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, playerTargetRotation, Time.deltaTime * rotationSpeed);
            }

            if (Quaternion.Angle(characterRoot.rotation, targetRotation) < 1f)
                rotateToPlayer = false;
        }

        if (rotateBack && characterRoot != null)
        {
            characterRoot.rotation = Quaternion.Slerp(characterRoot.rotation, originalRotation, Time.deltaTime * rotationSpeed);

            if (Quaternion.Angle(characterRoot.rotation, originalRotation) < 1f)
                rotateBack = false;
        }
    }
}
