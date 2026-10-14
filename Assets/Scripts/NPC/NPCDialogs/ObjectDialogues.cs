using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Reflection; // For accessing variables dynamically

public partial class ObjectDialogues : MonoBehaviour
{
    public string charactername;

    private static ObjectDialogues activeDialogueObject = null;
    private static bool justClosedThisFrame = false;

    [Header("Scriptable Object Database")]
    [Tooltip("Assign the unique NPC Dialogue asset profile here.")]
    public NPCDialogueDatabaseSO npcDialogueDatabase;

    [Header("Character Data Registry")]
    [Tooltip("Add all your CharacterData ScriptableObjects here so the dialogue system can look up their names by ID.")]
    public List<CharacterData> allCharacterData = new List<CharacterData>();

    [Header("Current Runtime Time Setting")]
    [Tooltip("Pass your environment time profile condition here.")]
    public DialogueTimePeriod currentEnvironmentTime = DialogueTimePeriod.Morning;

    // These references will be populated dynamically from the selected ScriptableObject asset
    private List<string> activeDialoguesList = new List<string>();
    private List<Sprite> activeImagesList = new List<Sprite>();

    [Header("UI References")]
    public GameObject dialogueCanvas;
    public TMP_Text dialogueText;
    public Image nextImage;
    public Button interactUIButton;
    [Tooltip("Assign your Skip Dialogue UI button here.")]
    public Button skipDialogueButton;

    [Header("Separated Name UI Panels")]
    [Tooltip("The TMP component used for displaying the local NPC's name.")]
    public TMP_Text npcNameText;

    [Tooltip("The TMP component used for displaying Party Member names ([PM] or,, etc.).")]
    public TMP_Text partyMemberNameText;

    public ActionMasterControl AMC;

    [Header("Scripts to Disable During Dialogue")]
    public List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

    [Header("Buttons to Disable During Dialogue")]
    public List<Button> buttonsToDisable = new List<Button>();

    [Header("GameObjects to Disable During Dialogue")]
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("Other Canvases That Block Dialogue")]
    public List<GameObject> blockingCanvases = new List<GameObject>();

    [Header("External Script Reference")]
    public MonoBehaviour externalScript;

    [Header("Party System Reference")]
    public PartyMembers PMembers; // Used to fetch currentFrontId

    private int currentLine = 0;
    private bool inDialogue = false;
    private bool typing = false;

    private bool isMovementLocked = false;
    public CharacterSwitchSystem characterSS;

    // Handcrafted color palette for IDs 0 through 3
    private readonly Color[] primaryLightColors = new Color[]
    {
        new Color(0.8f, 0.7f, 1.0f),    // ID 0 = Light Purple
        new Color(1.0f, 1.0f, 0.7f),    // ID 1 = Light Yellow
        new Color(0.7f, 1.0f, 0.7f),    // ID 2 = Light Green
        new Color(1.0f, 0.71f, 0.76f)   // ID 3 = Light Grey
    };
    private void Start()
    {
        if (interactUIButton != null)
        {
            interactUIButton.onClick.AddListener(HandleGlobalClick);
        }

        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.AddListener(HandleSkipPressed);
        }
    }

    private void OnDestroy()
    {
        if (interactUIButton != null)
        {
            interactUIButton.onClick.RemoveListener(HandleGlobalClick);
        }

        if (skipDialogueButton != null)
        {
            skipDialogueButton.onClick.RemoveListener(HandleSkipPressed);
        }
    }
}
public partial class ObjectDialogues : MonoBehaviour
{
    private string currentCleanedLineText = ""; // Caches processed sentence data
    private Coroutine activeTypeRoutine = null;  // Safely tracks the active typing coroutine

    // FIXED: Instead of wiping every scene actor's listeners with RemoveAllListeners, 
    // target and replace only our local instance to maintain universal scene compatibility.
    public void RestoreDefaultInteractListener()
    {
        if (interactUIButton == null) return;
        interactUIButton.onClick.RemoveListener(HandleGlobalClick);
        interactUIButton.onClick.AddListener(HandleGlobalClick);
    }

    public void HandleGlobalClick()
    {
        if (justClosedThisFrame) return;

        // 🛑 CRITICAL COMBAT SHIELD: If ActionMasterControl is active in combat, block dialogue inputs completely
        if (AMC != null && (AMC.masterActionControl == "BattleMode" || AMC.masterActionControl == "PartyPanel")) return;

        // If an active dialogue object exists but is no longer actively in dialogue, clear it
        if (activeDialogueObject != null && !activeDialogueObject.inDialogue)
        {
            activeDialogueObject = null;
        }

        if (activeDialogueObject != null)
        {
            if (activeDialogueObject == this)
            {
                OnInteractPressed();
            }
            return;
        }

        if (IsCurrentCharacterInside() && !AnyBlockingCanvasOpen())
        {
            activeDialogueObject = this;
            OnInteractPressed();
        }
    }

   


    void HandleSkipPressed()
    {
        if (!inDialogue) return;

        if (typing)
        {
            if (activeTypeRoutine != null)
            {
                StopCoroutine(activeTypeRoutine);
                activeTypeRoutine = null;
            }

            dialogueText.text = currentCleanedLineText;
            typing = false;

            if (currentLine < activeImagesList.Count && activeImagesList[currentLine] != null)
            {
                nextImage.sprite = activeImagesList[currentLine];
                nextImage.gameObject.SetActive(true);
            }
        }
        else
        {
            EndDialogue();
        }
    }

    private bool IsCurrentCharacterInside()
    {
        if (characterSS == null || characterSS.characterGameObjects == null) return false;

        if (characterSS.currentCharacterIndex < 0 || characterSS.currentCharacterIndex >= characterSS.characterGameObjects.Length) return false;
        GameObject currentCharacter = characterSS.characterGameObjects[characterSS.currentCharacterIndex];
        if (currentCharacter == null) return false;

        Collider triggerCollider = GetComponent<Collider>();
        Collider characterCollider = currentCharacter.GetComponent<Collider>();

        if (triggerCollider == null || characterCollider == null) return false;

        return triggerCollider.bounds.Intersects(characterCollider.bounds);
    }

    void OnInteractPressed()
    {
        // 🛑 CRITICAL COMBAT SHIELD: Secondary defensive block
        if (AMC != null && AMC.masterActionControl == "BattleMode") return;
        if (AnyBlockingCanvasOpen()) return;

        if (!inDialogue)
        {
            if (characterSS.currentCharacterIndex >= 0 && characterSS.currentCharacterIndex < characterSS.characterGameObjects.Length)
            {
                GameObject player = characterSS.characterGameObjects[characterSS.currentCharacterIndex];
                if (player != null) StartCoroutine(RunDialogueSequenceAfterFacing(player));
            }
        }
        else if (!typing)
        {
            NextLine();
        }
        else
        {
            HandleSkipPressed();
        }
    }
    IEnumerator RunDialogueSequenceAfterFacing(GameObject player)
    {
        yield return StartCoroutine(SmoothRotateToTarget(player, gameObject, 0.25f));
        StartDialogue();
    }

    IEnumerator SmoothRotateToTarget(GameObject player, GameObject targetObject, float duration)
    {
        if (player == null || targetObject == null) yield break;
        Vector3 dirToObject = targetObject.transform.position - player.transform.position;
        dirToObject.y = 0;

        if (dirToObject != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToObject);
            Quaternion startRotation = player.transform.rotation;
            float time = 0f;
            while (time < duration)
            {
                player.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, time / duration);
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            player.transform.rotation = targetRotation;
        }
    }

    void StartDialogue()
    {
        if (npcDialogueDatabase == null)
        {
            Debug.LogError($"Dialogue Database missing on {gameObject.name}!", this);
            activeDialogueObject = null;
            return;
        }

        if (PMembers == null)
        {
            Debug.LogError($"PartyMembers reference missing on {gameObject.name}!", this);
            activeDialogueObject = null;
            return;
        }

        DialogueContainerSO selectedDialogue = npcDialogueDatabase.GetDialogue(currentEnvironmentTime, PMembers.currentFrontId, characterSS);

        if (selectedDialogue == null || selectedDialogue.dialogues.Count == 0)
        {
            Debug.LogWarning($"No matching dialogue found for Time: {currentEnvironmentTime}, Front Character ID: {PMembers.currentFrontId}");
            activeDialogueObject = null;
            return;
        }

        foreach (string line in selectedDialogue.dialogues)
        {
            if (!IsSpeakerPresent(line))
            {
                Debug.Log($"[Dialogue Blocked] Blocked entirely because a required party member is absent.");
                activeDialogueObject = null;
                return;
            }
        }

        activeDialoguesList = selectedDialogue.dialogues;
        activeImagesList = selectedDialogue.imageNext;

        // FIXED: Explicitly lock our static variable here
        activeDialogueObject = this;
        inDialogue = true;
        currentLine = 0;
        dialogueCanvas.SetActive(true);
        LockMovementScripts();

        foreach (var script in scriptsToDisable) { if (script != null) script.enabled = false; }
        foreach (var button in buttonsToDisable) { if (button != null) button.interactable = false; }
        foreach (var obj in objectsToDisable) { if (obj != null) obj.SetActive(false); }
        Time.timeScale = 0f;
        DisplayLine(currentLine);
    }
    void NextLine()
    {
        currentLine++;
        if (currentLine < activeDialoguesList.Count)
        {
            DisplayLine(currentLine);
        }
        else
        {
            EndDialogue();
        }
    }
    void DisplayLine(int index)
    {
        if (index < 0 || index >= activeDialoguesList.Count) return;
        string fullyParsedLine = ProcessDialogue(activeDialoguesList[index]);
        currentCleanedLineText = ParseSpeakerDataAndClean(fullyParsedLine);
        if (activeTypeRoutine != null)
        {
            StopCoroutine(activeTypeRoutine);
        }
        activeTypeRoutine = StartCoroutine(TypeLine(currentCleanedLineText));
    }
    private bool IsSpeakerPresent(string rawLine)
    {
        if (string.IsNullOrEmpty(rawLine)) return true;
        if (rawLine.StartsWith("[PM]")) return true;
        if (rawLine.StartsWith("[") && rawLine.Contains("]"))
        {
            int closingBracketIndex = rawLine.IndexOf(']');
            string idString = rawLine.Substring(1, closingBracketIndex - 1);
            if (int.TryParse(idString, out int parsedId))
            {
                for (int i = 0; i < characterSS.charactersInGameNumber; i++)
                {
                    if (characterSS.characterSlots != null && i < characterSS.characterSlots.Length && characterSS.characterSlots[i] != null && characterSS.characterSlots[i].characterId == parsedId)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        return true;
    }
}
public partial class ObjectDialogues : MonoBehaviour
{
    private string ParseSpeakerDataAndClean(string line)
    {
        if (string.IsNullOrEmpty(line)) return line;
        if (line.StartsWith("[PM]"))
        {
            ApplySpeakerName(PMembers.currentFrontId, isPartyMember: true);
            return line.Replace("[PM]", "").TrimStart();
        }
        if (line.StartsWith("[") && line.Contains("]"))
        {
            int closingBracketIndex = line.IndexOf(']');
            string idString = line.Substring(1, closingBracketIndex - 1);
            if (int.TryParse(idString, out int parsedId))
            {
                ApplySpeakerName(parsedId, isPartyMember: true);
                return line.Substring(closingBracketIndex + 1).TrimStart();
            }
        }
        ApplySpeakerName(-1, isPartyMember: false);
        return line;
    }
    private void ApplySpeakerName(int characterId, bool isPartyMember)
    {
        if (isPartyMember)
        {
            if (npcNameText != null) npcNameText.text = "";
            CharacterData data = allCharacterData.Find(c => c.characterId == characterId);
            if (partyMemberNameText != null)
            {
                bool isCurrentlyInActiveParty = false;
                for (int i = 0; i < characterSS.charactersInGameNumber; i++)
                {
                    if (characterSS.characterSlots != null && i < characterSS.characterSlots.Length && characterSS.characterSlots[i] != null && characterSS.characterSlots[i].characterId == characterId)
                    {
                        isCurrentlyInActiveParty = true;
                        break;
                    }
                }
                if (isCurrentlyInActiveParty)
                {
                    partyMemberNameText.text = data != null ? data.characterName : "Unknown";
                }
                else
                {
                    partyMemberNameText.text = data != null ? $"{data.characterName} (Absent)" : "Unknown";
                }
                partyMemberNameText.color = GetSpeakerColor(characterId);
            }
        }
        else
        {
            if (partyMemberNameText != null) partyMemberNameText.text = "";
            if (npcNameText != null) npcNameText.text = charactername;
        }
    }
    private Color GetSpeakerColor(int characterId)
    {
        if (characterId >= 0 && characterId < primaryLightColors.Length)
        {
            return primaryLightColors[characterId];
        }
        return GenerateUniquePastelColor(characterId);
    }
    private Color GenerateUniquePastelColor(int id)
    {
        Random.InitState(id * 265443576);
        float hue = Random.value;
        float saturation = 0.35f;
        float value = 0.95f;
        return Color.HSVToRGB(hue, saturation, value);
    }
    void EndDialogue()
    {
        if (activeTypeRoutine != null)
        {
            StopCoroutine(activeTypeRoutine);
            activeTypeRoutine = null;
        }
        typing = false;
        inDialogue = false;
        dialogueCanvas.SetActive(false);
        UnlockMovementScripts();
        AMC.masterActionControl = "idle";
        foreach (var script in scriptsToDisable) { if (script != null) script.enabled = true; }
        foreach (var button in buttonsToDisable) { if (button != null) button.interactable = true; }
        foreach (var obj in objectsToDisable) { if (obj != null) obj.SetActive(true); }
        Time.timeScale = 1f;
        // FIXED: Immediately clear the static variable so other actors aren't locked behind Character 1
        activeDialogueObject = null;
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(ResetDialogueLockRoutine());
        }
        else
        {
            justClosedThisFrame = false;
        }
    }
    IEnumerator ResetDialogueLockRoutine()
    {
        justClosedThisFrame = true;
        activeDialogueObject = null; // Secondary fallback safety clearance
        yield return new WaitForEndOfFrame();
        justClosedThisFrame = false;
    }
    IEnumerator TypeLine(string line)
    {
        typing = true;
        dialogueText.text = "";
        nextImage.sprite = null;
        nextImage.gameObject.SetActive(false);
        foreach (char c in line.ToCharArray())
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.03f);
        }
        typing = false;
        activeTypeRoutine = null;
        if (currentLine < activeImagesList.Count && activeImagesList[currentLine] != null)
        {
            nextImage.sprite = activeImagesList[currentLine];
            nextImage.gameObject.SetActive(true);
        }
    }
    string ProcessDialogue(string rawLine)
    {
        if (string.IsNullOrEmpty(rawLine)) return rawLine;
        foreach (var data in allCharacterData)
        {
            string targetSuffixTag = $"/{data.characterId}";
            if (rawLine.Contains(targetSuffixTag))
            {
                rawLine = rawLine.Replace(targetSuffixTag, data.characterName);
            }
        }
        string[] words = rawLine.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].StartsWith("!"))
            {
                string rawVar = words[i].Substring(1);
                string punctuation = "";
                if (rawVar.EndsWith(",") || rawVar.EndsWith(".") || rawVar.EndsWith("?") || rawVar.EndsWith("!"))
                {
                    punctuation = rawVar.Substring(rawVar.Length - 1);
                    rawVar = rawVar.Substring(0, rawVar.Length - 1);
                }
                object value = null;
                if (rawVar.StartsWith("PMembers.", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (PMembers != null)
                    {
                        string varName = rawVar.Substring("PMembers.".Length);
                        value = GetValueFromScript(PMembers, varName);
                    }
                }
                else
                {
                    value = GetValueFromScript(this, rawVar);
                }
                if (value != null)
                {
                    words[i] = value.ToString() + punctuation;
                }
            }
        }
        return string.Join(" ", words);
    }
    private object GetValueFromScript(MonoBehaviour script, string varName)
    {
        FieldInfo field = script.GetType().GetField(varName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) return field.GetValue(script);
        return null;
    }
    private bool AnyBlockingCanvasOpen()
    {
        foreach (var canvas in blockingCanvases)
        {
            if (canvas != null && canvas.activeInHierarchy) return true;
        }
        return false;
    }
    void LockMovementScripts()
    {
        isMovementLocked = true;
        foreach (var character in characterSS.characterGameObjects)
        {
            if (character == null) continue;
            var movement = character.GetComponent("PlayerMovement3D") as MonoBehaviour;
            if (movement != null) movement.enabled = false;
        }
    }
    void UnlockMovementScripts()
    {
        isMovementLocked = false;
        for (int i = 0; i < characterSS.charactersInGameNumber; i++)
        {
            if (characterSS.characterGameObjects == null || i >= characterSS.characterGameObjects.Length) continue;
            var character = characterSS.characterGameObjects[i];
            if (character == null) continue;
            var movement = character.GetComponent("PlayerMovement3D") as MonoBehaviour;
            if (movement != null) movement.enabled = (i == characterSS.currentCharacterIndex);
        }
    }
}