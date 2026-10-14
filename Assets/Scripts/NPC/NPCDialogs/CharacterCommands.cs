using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection; // For dynamically replacing !PMembers strings

public enum CharacterPlayableType
{
    Playable,
    NonPlayable
}

public enum CommandActionType
{
    Give,
    Recruit,
    Training,
    Release,
    Check,
    Nothing
}

[RequireComponent(typeof(Collider))]
public class CharacterCommands : MonoBehaviour
{
    // STATIC LOCK: Tracks the single character currently interacting with the UI across all instances
    private static CharacterCommands activeCommandCharacter = null;

    [Header("Character Properties")]
    public CharacterPlayableType characterType = CharacterPlayableType.Playable;
    public int associatedCharacterId;
    public string charactername;
    public CharacterData localCharacterData;
    public GameObject localCharacterInGamePrefab;

    [Header("Core Infrastructure Systems")]
    public ActionMasterControl AMC;
    public CharacterSwitchSystem characterSS;
    public PartyMembers PMembers;
    public ObjectDialogues dialogueSystemReference;

    [Header("Dialogues Package Asset")]
    public CommandDialogueSO commandDialogueData;

    [Header("UI Menus Panels")]
    public GameObject outPartyPanel;
    public GameObject inPartyPanel;
    public TMPro.TMP_Text characterMenuNameText;

    [Header("Control Inputs Layout")]
    public Button commandButton;
    public Button upNavigationButton;
    public Button downNavigationButton;
    public Button interactButton;

    [Header("UI Selection Images")]
    public List<Image> outPartySelectableImages = new List<Image>();
    public List<CommandActionType> outPartyMappedActions = new List<CommandActionType>();
    [Space(10)]
    public List<Image> inPartySelectableImages = new List<Image>();
    public List<CommandActionType> inPartyMappedActions = new List<CommandActionType>();

    [Header("Selection Highlight Colors")]
    public Color normalColor = Color.white;
    public Color highlightedColor = Color.yellow;

    [Header("Scene Performance Inhibitors")]
    public List<GameObject> disableGameObjects = new List<GameObject>();
    public List<MonoBehaviour> disableScripts = new List<MonoBehaviour>();

    // Pure state tracking variables
    private bool isPlayerInsideCollider = false;
    private bool isPanelActive = false;
    private bool isDisplayingCommandDialogue = false;
    private int currentSelectionIndex = 0;

    // Self-contained text engine tracking properties
    private DialogueContainerSO activeDialogueAsset = null;
    private int dialoguePlaybackIndex = 0;
    private bool isTypingText = false;
    private string fullyCleanedSentenceText = "";
    private Coroutine activeTypeRoutine = null;
    private CommandDialogueType currentDialogueContext;
    private ObjectDialogues localObjectDialoguesScript = null;
    private void Start()
    {
        localObjectDialoguesScript = GetComponent<ObjectDialogues>();

        if (commandButton != null) commandButton.onClick.AddListener(OnCommandButtonPressed);
        if (upNavigationButton != null) upNavigationButton.onClick.AddListener(NavigateUp);
        if (downNavigationButton != null) downNavigationButton.onClick.AddListener(NavigateDown);
        if (interactButton != null) interactButton.onClick.AddListener(ExecuteActiveSelection);

        if (outPartyPanel != null) outPartyPanel.SetActive(false);
        if (inPartyPanel != null) inPartyPanel.SetActive(false);
        if (characterMenuNameText != null) characterMenuNameText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (isPanelActive || isDisplayingCommandDialogue)
        {
            if (activeCommandCharacter != null && activeCommandCharacter != this) return;

            if (AMC != null && AMC.masterActionControl != "PartyPanel")
            {
                AMC.masterActionControl = "PartyPanel";
            }

            SetInhibitorsState(false);

            if (isPanelActive && !isDisplayingCommandDialogue)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow)) NavigateUp();
                if (Input.GetKeyDown(KeyCode.DownArrow)) NavigateDown();
                if (Input.GetKeyDown(KeyCode.Return)) ExecuteActiveSelection();
            }
        }
    }
    public void OnCommandButtonResetState()
    {
        isPlayerInsideCollider = false;

        if (isPanelActive || isDisplayingCommandDialogue)
        {
            ClosePanelSequence();
        }
    }

    public void OnCommandButtonPressed()
    {
        if (!this.enabled) return;

        if (characterSS != null && characterSS.characterGameObjects != null)
        {
            GameObject activeHero = characterSS.characterGameObjects[characterSS.currentCharacterIndex];
            if (activeHero == gameObject)
            {
                isPlayerInsideCollider = false;
                return;
            }
        }

        if (!isPlayerInsideCollider || isPanelActive || isDisplayingCommandDialogue) return;
        if (activeCommandCharacter != null && activeCommandCharacter != this) return;
        if (IsStandardDialogueCurrentlyActive()) return;

        if (AMC != null && (AMC.masterActionControl == "idle" || string.IsNullOrEmpty(AMC.masterActionControl)))
        {
            activeCommandCharacter = this;
            StartCommandSequence();
        }
    }

    private bool IsStandardDialogueCurrentlyActive()
    {
        if (localObjectDialoguesScript != null)
        {
            FieldInfo localField = localObjectDialoguesScript.GetType().GetField("inDialogue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (localField != null && (bool)localField.GetValue(localObjectDialoguesScript)) return true;
        }
        if (dialogueSystemReference != null)
        {
            FieldInfo refField = dialogueSystemReference.GetType().GetField("inDialogue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (refField != null && (bool)refField.GetValue(dialogueSystemReference)) return true;
        }
        return false;
    }

    private void StartCommandSequence()
    {
        AMC.masterActionControl = "PartyPanel";
        SetInhibitorsState(false);
        DisableActivePlayerMovement(true);

        if (dialogueSystemReference != null) dialogueSystemReference.charactername = charactername;

        TriggerContextDialogue(CommandDialogueType.Greeting);
    }
    private void TriggerContextDialogue(CommandDialogueType type)
    {
        if (commandDialogueData == null || PMembers == null || dialogueSystemReference == null)
        {
            OpenPanelSequence();
            return;
        }

        activeDialogueAsset = commandDialogueData.GetDialogueByContext(type, PMembers.currentFrontId);
        currentDialogueContext = type;

        if (activeDialogueAsset != null && activeDialogueAsset.dialogues.Count > 0)
        {
            isDisplayingCommandDialogue = true;
            dialoguePlaybackIndex = 0;

            if (dialogueSystemReference.dialogueCanvas != null) dialogueSystemReference.dialogueCanvas.SetActive(true);

            if (dialogueSystemReference.interactUIButton != null)
            {
                dialogueSystemReference.interactUIButton.onClick.RemoveListener(ProgressDialogueLineText);
                dialogueSystemReference.interactUIButton.onClick.AddListener(ProgressDialogueLineText);
            }

            RenderActiveLineToUI();
        }
        else
        {
            if (type == CommandDialogueType.Greeting)
                OpenPanelSequence();
            else
                ClosePanelSequence();
        }
    }

    private void RenderActiveLineToUI()
    {
        if (dialogueSystemReference == null || activeDialogueAsset == null || dialoguePlaybackIndex >= activeDialogueAsset.dialogues.Count) return;

        if (dialogueSystemReference.partyMemberNameText != null) dialogueSystemReference.partyMemberNameText.text = "";
        if (dialogueSystemReference.npcNameText != null) dialogueSystemReference.npcNameText.text = charactername;

        string rawLine = activeDialogueAsset.dialogues[dialoguePlaybackIndex];
        fullyCleanedSentenceText = ProcessTextVariables(rawLine);

        if (activeTypeRoutine != null) StopCoroutine(activeTypeRoutine);
        activeTypeRoutine = StartCoroutine(TypeSentenceLoop(fullyCleanedSentenceText));
    }

    private IEnumerator TypeSentenceLoop(string fullText)
    {
        isTypingText = true;
        dialogueSystemReference.dialogueText.text = "";
        if (dialogueSystemReference.nextImage != null) dialogueSystemReference.nextImage.gameObject.SetActive(false);

        foreach (char c in fullText.ToCharArray())
        {
            dialogueSystemReference.dialogueText.text += c;
            yield return new WaitForSecondsRealtime(0.02f);
        }

        isTypingText = false;
        activeTypeRoutine = null;

        if (dialogueSystemReference.nextImage != null && dialoguePlaybackIndex < activeDialogueAsset.imageNext.Count && activeDialogueAsset.imageNext[dialoguePlaybackIndex] != null)
        {
            dialogueSystemReference.nextImage.sprite = activeDialogueAsset.imageNext[dialoguePlaybackIndex];
            dialogueSystemReference.nextImage.gameObject.SetActive(true);
        }
    }

    private void ProgressDialogueLineText()
    {
        if (isTypingText)
        {
            if (activeTypeRoutine != null) StopCoroutine(activeTypeRoutine);
            dialogueSystemReference.dialogueText.text = fullyCleanedSentenceText;
            isTypingText = false;
            activeTypeRoutine = null;
            if (dialogueSystemReference.nextImage != null && dialoguePlaybackIndex < activeDialogueAsset.imageNext.Count && activeDialogueAsset.imageNext[dialoguePlaybackIndex] != null)
            {
                dialogueSystemReference.nextImage.sprite = activeDialogueAsset.imageNext[dialoguePlaybackIndex];
                dialogueSystemReference.nextImage.gameObject.SetActive(true);
            }
            return;
        }
        dialoguePlaybackIndex++;
        if (activeDialogueAsset != null && dialoguePlaybackIndex < activeDialogueAsset.dialogues.Count)
        {
            RenderActiveLineToUI();
        }
        else
        {
            isDisplayingCommandDialogue = false;
            if (dialogueSystemReference.dialogueCanvas != null) dialogueSystemReference.dialogueCanvas.SetActive(false);
            if (dialogueSystemReference != null && dialogueSystemReference.interactUIButton != null)
            {
                dialogueSystemReference.interactUIButton.onClick.RemoveListener(ProgressDialogueLineText);
            }
            if (dialogueSystemReference != null)
            {
                dialogueSystemReference.RestoreDefaultInteractListener();
            }
            if (currentDialogueContext == CommandDialogueType.Greeting)
                OpenPanelSequence();
            else
                ClosePanelSequence();
        }
    }
    private void OpenPanelSequence()
    {
        isPanelActive = true;
        isDisplayingCommandDialogue = false;
        AMC.masterActionControl = "PartyPanel";
        currentSelectionIndex = 0;
        SetSafeInhibitorsState(false);
        DisableActivePlayerMovement(true);
        if (dialogueSystemReference != null)
        {
            if (dialogueSystemReference.dialogueCanvas != null)
                dialogueSystemReference.dialogueCanvas.SetActive(false);
            if (dialogueSystemReference.interactUIButton != null)
            {
                dialogueSystemReference.interactUIButton.onClick.RemoveListener(ExecuteActiveSelection);
                dialogueSystemReference.interactUIButton.onClick.AddListener(ExecuteActiveSelection);
            }
        }
        if (characterMenuNameText != null)
        {
            characterMenuNameText.text = charactername;
            characterMenuNameText.gameObject.SetActive(true);
        }
        HideAllSelectableUINodes();
        bool characterIsInParty = IsCharacterCurrentlyInParty();

        bool canModifyRoster = (PMembers != null && PMembers.currentFrontId == 0);

        if (characterType == CharacterPlayableType.NonPlayable)
        {
            if (outPartyPanel != null) outPartyPanel.SetActive(true);
            if (inPartyPanel != null) inPartyPanel.SetActive(false);
            for (int i = 0; i < outPartyMappedActions.Count; i++)
            {
                if (outPartyMappedActions[i] == CommandActionType.Give || outPartyMappedActions[i] == CommandActionType.Nothing)
                {
                    if (i < outPartySelectableImages.Count && outPartySelectableImages[i] != null)
                    {
                        outPartySelectableImages[i].gameObject.SetActive(true);
                    }
                }
            }
        }
        else
        {
            if (characterIsInParty)
            {
                if (inPartyPanel != null) inPartyPanel.SetActive(true);
                if (outPartyPanel != null) outPartyPanel.SetActive(false);

                for (int i = 0; i < inPartyMappedActions.Count; i++)
                {
                    if (inPartyMappedActions[i] == CommandActionType.Release && !canModifyRoster) continue;
                    if (i < inPartySelectableImages.Count && inPartySelectableImages[i] != null)
                        inPartySelectableImages[i].gameObject.SetActive(true);
                }
            }
            else
            {
                if (outPartyPanel != null) outPartyPanel.SetActive(true);
                if (inPartyPanel != null) inPartyPanel.SetActive(false);

                for (int i = 0; i < outPartyMappedActions.Count; i++)
                {
                    if (outPartyMappedActions[i] == CommandActionType.Recruit && !canModifyRoster) continue;
                    if (i < outPartySelectableImages.Count && outPartySelectableImages[i] != null)
                        outPartySelectableImages[i].gameObject.SetActive(true);
                }
            }
        }
        RefreshHighlightVisuals();
    }
    private void NavigateUp()
    {
        if (!isPanelActive || isDisplayingCommandDialogue) return;
        int maxItems = GetActiveSelectionListCount();
        if (maxItems <= 0) return;
        currentSelectionIndex--;
        if (currentSelectionIndex < 0) currentSelectionIndex = maxItems - 1;
        RefreshHighlightVisuals();
    }

    private void NavigateDown()
    {
        if (!isPanelActive || isDisplayingCommandDialogue) return;
        int maxItems = GetActiveSelectionListCount();
        if (maxItems <= 0) return;
        currentSelectionIndex++;
        if (currentSelectionIndex >= maxItems) currentSelectionIndex = 0;
        RefreshHighlightVisuals();
    }

    private void RefreshHighlightVisuals()
    {
        bool useInPartyList = IsCharacterCurrentlyInParty() && characterType == CharacterPlayableType.Playable;
        bool canModifyRoster = (PMembers != null && PMembers.currentFrontId == 0);

        if (characterType == CharacterPlayableType.NonPlayable)
        {
            foreach (var img in outPartySelectableImages)
            {
                if (img != null) img.color = normalColor;
            }
            int validCounter = 0;
            for (int i = 0; i < outPartyMappedActions.Count; i++)
            {
                if (outPartyMappedActions[i] == CommandActionType.Give || outPartyMappedActions[i] == CommandActionType.Nothing)
                {
                    if (validCounter == currentSelectionIndex)
                    {
                        if (i < outPartySelectableImages.Count && outPartySelectableImages[i] != null)
                        {
                            outPartySelectableImages[i].color = highlightedColor;
                        }
                        break;
                    }
                    validCounter++;
                }
            }
        }
        else
        {
            List<Image> activeImageList = useInPartyList ? inPartySelectableImages : outPartySelectableImages;
            List<CommandActionType> activeActionList = useInPartyList ? inPartyMappedActions : outPartyMappedActions;

            foreach (var img in activeImageList) { if (img != null) img.color = normalColor; }

            int validCounter = 0;
            for (int i = 0; i < activeActionList.Count; i++)
            {
                if (activeActionList[i] == CommandActionType.Recruit && !canModifyRoster) continue;
                if (activeActionList[i] == CommandActionType.Release && !canModifyRoster) continue;

                if (validCounter == currentSelectionIndex)
                {
                    if (i < activeImageList.Count && activeImageList[i] != null)
                    {
                        activeImageList[i].color = highlightedColor;
                    }
                    break;
                }
                validCounter++;
            }
        }
    }

    private int GetActiveSelectionListCount()
    {
        if (characterType == CharacterPlayableType.NonPlayable) return 2;
        bool useInPartyList = IsCharacterCurrentlyInParty() && characterType == CharacterPlayableType.Playable;
        List<CommandActionType> activeActionList = useInPartyList ? inPartyMappedActions : outPartyMappedActions;
        bool canModifyRoster = (PMembers != null && PMembers.currentFrontId == 0);

        int count = 0;
        foreach (var action in activeActionList)
        {
            if (action == CommandActionType.Recruit && !canModifyRoster) continue;
            if (action == CommandActionType.Release && !canModifyRoster) continue;
            count++;
        }
        return count;
    }
    private void ExecuteActiveSelection()
    {
        if (!isPanelActive || isDisplayingCommandDialogue) return;
        CommandActionType chosenAction = CommandActionType.Nothing;

        if (characterType == CharacterPlayableType.NonPlayable)
        {
            int validCounter = 0;
            for (int i = 0; i < outPartyMappedActions.Count; i++)
            {
                if (outPartyMappedActions[i] == CommandActionType.Give || outPartyMappedActions[i] == CommandActionType.Nothing)
                {
                    if (validCounter == currentSelectionIndex)
                    {
                        chosenAction = outPartyMappedActions[i];
                        break;
                    }
                    validCounter++;
                }
            }
        }
        else
        {
            bool useInPartyList = IsCharacterCurrentlyInParty() && characterType == CharacterPlayableType.Playable;
            List<CommandActionType> activeActionList = useInPartyList ? inPartyMappedActions : outPartyMappedActions;
            bool canModifyRoster = (PMembers != null && PMembers.currentFrontId == 0);

            int validCounter = 0;
            for (int i = 0; i < activeActionList.Count; i++)
            {
                if (activeActionList[i] == CommandActionType.Recruit && !canModifyRoster) continue;
                if (activeActionList[i] == CommandActionType.Release && !canModifyRoster) continue;

                if (validCounter == currentSelectionIndex)
                {
                    chosenAction = activeActionList[i];
                    break;
                }
                validCounter++;
            }
        }

        if (dialogueSystemReference != null && dialogueSystemReference.interactUIButton != null)
        {
            dialogueSystemReference.interactUIButton.onClick.RemoveListener(ExecuteActiveSelection);
        }
        if (outPartyPanel != null) outPartyPanel.SetActive(false);
        if (inPartyPanel != null) inPartyPanel.SetActive(false);
        isPanelActive = false;

        switch (chosenAction)
        {
            case CommandActionType.Give: TriggerContextDialogue(CommandDialogueType.Give); break;
            case CommandActionType.Recruit: ExecuteRecruitRosterAllocation(); TriggerContextDialogue(CommandDialogueType.Recruit); break;
            case CommandActionType.Training: TriggerContextDialogue(CommandDialogueType.Training); break;
            case CommandActionType.Release: ExecuteReleaseRosterAllocation(); TriggerContextDialogue(CommandDialogueType.Release); break;
            case CommandActionType.Check: TriggerContextDialogue(CommandDialogueType.Check); break;
            case CommandActionType.Nothing: TriggerContextDialogue(CommandDialogueType.Nothing); break;
        }
    }
    private void ExecuteRecruitRosterAllocation()
    {
        if (characterSS == null) return;
        if (characterSS.charactersInGameNumber >= 5)
        {
            Debug.LogWarning($"[CharacterCommands] Party roster full! Cannot recruit {charactername}.");
            return;
        }
        int targetIndex = characterSS.charactersInGameNumber;
        if (characterSS.characterSlots != null && targetIndex < characterSS.characterSlots.Length)
            characterSS.characterSlots[targetIndex] = localCharacterData;
        if (characterSS.characterGameObjects != null && targetIndex < characterSS.characterGameObjects.Length)
            characterSS.characterGameObjects[targetIndex] = localCharacterInGamePrefab;
        characterSS.charactersInGameNumber++;

        if (localCharacterInGamePrefab != null)
        {
            MonoBehaviour followScript = localCharacterInGamePrefab.GetComponent("NPCFollowPlayer") as MonoBehaviour;
            if (followScript != null) followScript.enabled = true;

            // PARENT REASSESSMENT: Instantly parent under 'InParty' folder
            if (characterSS.inPartyParent != null)
            {
                localCharacterInGamePrefab.transform.SetParent(characterSS.inPartyParent);
            }
        }
        characterSS.InitializeUI();
    }

    private void ExecuteReleaseRosterAllocation()
    {
        if (characterSS == null || PMembers == null) return;
        if (characterSS.charactersInGameNumber <= 1) return;
        int indexToRemove = -1;
        for (int i = 0; i < characterSS.charactersInGameNumber; i++)
        {
            if (characterSS.characterSlots != null && i < characterSS.characterSlots.Length && characterSS.characterSlots[i] != null && characterSS.characterSlots[i].characterId == associatedCharacterId)
            {
                indexToRemove = i;
                break;
            }
        }
        if (indexToRemove == -1) return;
        if (localCharacterInGamePrefab != null)
        {
            MonoBehaviour followScript = localCharacterInGamePrefab.GetComponent("NPCFollowPlayer") as MonoBehaviour;
            if (followScript != null) followScript.enabled = false;

            PlayerMovement3D movementScript = localCharacterInGamePrefab.GetComponent<PlayerMovement3D>();
            if (movementScript != null && characterSS.currentCharacterIndex != indexToRemove)
            {
                if (movementScript.animator != null)
                {
                    movementScript.animator.SetFloat("Speed", 0f);
                    movementScript.animator.SetFloat("MoveX", 0f);
                    movementScript.animator.SetFloat("MoveZ", 0f);
                }
            }

            // PARENT REASSESSMENT: Instantly parent under 'OutParty' folder
            if (characterSS.outPartyParent != null)
            {
                localCharacterInGamePrefab.transform.SetParent(characterSS.outPartyParent);
            }
        }
        for (int i = indexToRemove; i < characterSS.charactersInGameNumber - 1; i++)
        {
            if (characterSS.characterSlots != null && (i + 1) < characterSS.characterSlots.Length)
                characterSS.characterSlots[i] = characterSS.characterSlots[i + 1];
            if (characterSS.characterGameObjects != null && (i + 1) < characterSS.characterGameObjects.Length)
                characterSS.characterGameObjects[i] = characterSS.characterGameObjects[i + 1];
        }
        int lastActiveIndex = characterSS.charactersInGameNumber - 1;
        if (characterSS.characterSlots != null && lastActiveIndex < characterSS.characterSlots.Length)
            characterSS.characterSlots[lastActiveIndex] = null;
        if (characterSS.characterGameObjects != null && lastActiveIndex < characterSS.characterGameObjects.Length)
            characterSS.characterGameObjects[lastActiveIndex] = null;
        characterSS.charactersInGameNumber--;
        if (characterSS.currentCharacterIndex == indexToRemove || characterSS.currentCharacterIndex >= characterSS.charactersInGameNumber)
        {
            characterSS.currentCharacterIndex = 0;
            if (characterSS.characterSlots != null && characterSS.characterSlots.Length > 0 && characterSS.characterSlots[0] != null)
            {
                //  FIXED: Specified [0] array index allocation reference bounds properly
                PMembers.currentFrontId = characterSS.characterSlots[0].characterId;
                PMembers.currentFrontName = characterSS.characterSlots[0].characterName;
            }
        }
        characterSS.InitializeUI();
        if (characterSS.OAF != null) characterSS.OAF.SelectCharacter();
    }

    public void ClosePanelSequence()
    {
        isPanelActive = false;
        isDisplayingCommandDialogue = false;
        if (outPartyPanel != null) outPartyPanel.SetActive(false);
        if (inPartyPanel != null) inPartyPanel.SetActive(false);
        if (characterMenuNameText != null) characterMenuNameText.gameObject.SetActive(false);
        DisableActivePlayerMovement(false);
        SetSafeInhibitorsState(true);
        if (dialogueSystemReference != null) dialogueSystemReference.RestoreDefaultInteractListener();
        if (AMC != null) AMC.masterActionControl = "idle";
        if (activeCommandCharacter == this) activeCommandCharacter = null;
    }

    private void DisableActivePlayerMovement(bool shouldDisable)
    {
        if (characterSS == null) return;
        if (characterSS.characterGameObjects == null || characterSS.currentCharacterIndex >= characterSS.characterGameObjects.Length) return;
        GameObject activePlayerObj = characterSS.characterGameObjects[characterSS.currentCharacterIndex];
        if (activePlayerObj != null)
        {
            PlayerMovement3D movement = activePlayerObj.GetComponent<PlayerMovement3D>();
            if (movement != null)
            {
                if (shouldDisable)
                {
                    movement.enabled = false;
                    if (movement.animator != null)
                    {
                        movement.animator.SetFloat("Speed", 0f);
                        movement.animator.SetFloat("MoveX", 0f);
                        movement.animator.SetFloat("MoveZ", 0f);
                    }
                }
                else movement.enabled = true;
            }
        }
    }

    private void ToggleImageArrayVisibility(List<Image> imageList, bool shouldBeVisible)
    {
        if (imageList == null) return;
        foreach (var img in imageList)
        {
            if (img != null && img.gameObject != null) img.gameObject.SetActive(shouldBeVisible);
        }
    }

    private void HideAllSelectableUINodes()
    {
        ToggleImageArrayVisibility(outPartySelectableImages, false);
        ToggleImageArrayVisibility(inPartySelectableImages, false);
    }

    private void SetSafeInhibitorsState(bool makeActive)
    {
        if (!makeActive && activeCommandCharacter != null && activeCommandCharacter != this) return;
        SetInhibitorsState(makeActive);
    }

    private void SetInhibitorsState(bool makeActive)
    {
        foreach (var obj in disableGameObjects)
        {
            if (obj != null && obj.activeSelf != makeActive) obj.SetActive(makeActive);
        }
        foreach (var script in disableScripts)
        {
            if (script != null && script.enabled != makeActive) script.enabled = makeActive;
        }
    }

    private string ProcessTextVariables(string rawLine)
    {
        if (string.IsNullOrEmpty(rawLine)) return rawLine;
        if (dialogueSystemReference != null && dialogueSystemReference.allCharacterData != null)
        {
            foreach (var data in dialogueSystemReference.allCharacterData)
            {
                string targetSuffixTag = $"/{data.characterId}";
                if (rawLine.Contains(targetSuffixTag)) rawLine = rawLine.Replace(targetSuffixTag, data.characterName);
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
                else value = GetValueFromScript(this, rawVar);
                if (value != null) words[i] = value.ToString() + punctuation;
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

    private bool IsCharacterCurrentlyInParty()
    {
        if (characterSS == null || characterSS.characterSlots == null) return false;
        for (int i = 0; i < characterSS.charactersInGameNumber; i++)
        {
            if (i < characterSS.characterSlots.Length && characterSS.characterSlots[i] != null && characterSS.characterSlots[i].characterId == associatedCharacterId) return true;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (characterSS == null || characterSS.characterGameObjects == null || characterSS.currentCharacterIndex >= characterSS.characterGameObjects.Length) return;
        GameObject activeHero = characterSS.characterGameObjects[characterSS.currentCharacterIndex];
        if (activeHero != null && other.gameObject == activeHero) isPlayerInsideCollider = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (characterSS == null || characterSS.characterGameObjects == null || characterSS.currentCharacterIndex >= characterSS.characterGameObjects.Length) return;
        GameObject activeHero = characterSS.characterGameObjects[characterSS.currentCharacterIndex];
        if (activeHero != null && other.gameObject == activeHero)
        {
            isPlayerInsideCollider = false;
            if (activeCommandCharacter == this && (isPanelActive || isDisplayingCommandDialogue)) ClosePanelSequence();
        }
    }
}
