using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CharacterSwitchSystem : MonoBehaviour
{
    [Header("UI References")]
    public GameObject characterSelectionPanel;
    public Image[] holderUI = new Image[5];
    public Image[] portraitUI = new Image[5];
    public TMP_Text nameCharacterText;

    [Header("Character Data Slots")]
    public CharacterData[] characterSlots = new CharacterData[5];

    [Header("GameObjects")]
    public Transform transformObjectA;
    public Transform transformObjectB;
    public GameObject[] characterGameObjects = new GameObject[5];

    [Header("Dynamic Hierarchy Management")]
    [Tooltip("Leave unassigned to automatically discover the 'InParty' GameObject in the scene")]
    public Transform inPartyParent;
    [Tooltip("Leave unassigned to automatically discover the 'OutParty' GameObject in the scene")]
    public Transform outPartyParent;

    [Header("UI Buttons")]
    public Button leftButton;
    public Button upButton;
    public Button downButton;
    public Button rightButton;

    [Header("Settings")]
    public int charactersInGameNumber = 1;

    [Tooltip("Keyword to search for in Armature names")]
    public string armatureKeyword = "metarig";

    [Header("Reset Values for Anchors")]
    public Vector3 resetPositionA = Vector3.zero;
    public Vector3 resetRotationA = Vector3.zero;
    public Vector3 resetScaleA = Vector3.one;

    public Vector3 resetPositionB = Vector3.zero;
    public Vector3 resetRotationB = Vector3.zero;
    public Vector3 resetScaleB = Vector3.one;

    [Header("IK Look References")]
    public Transform activeCharacterLookAt;
    public Transform inActiveCharacterLookAt;

    [Header("Fade Transition")]
    public Animator fadeAnimator;
    public AnimationClip fadeInClip;

    [HideInInspector] public int currentCharacterIndex = 0;
    private bool isMovementLocked = false;
    private bool rbHeld = false;

    [Header("Core Dependencies")]
    public ActionMasterControl AMC;
    public PartyMembers PMembers;
    public ObjectAccurateFollower OAF;
    void Start()
    {
        // 🛠️ HIERARCHY AUTO-DISCOVERY: Safely fallback to scene searches if unassigned
        if (inPartyParent == null)
        {
            GameObject inPartyGo = GameObject.Find("InParty");
            if (inPartyGo != null) inPartyParent = inPartyGo.transform;
            else Debug.LogWarning("[CharacterSwitchSystem] Could not find a GameObject named 'InParty' in the scene hierarchy!");
        }

        if (outPartyParent == null)
        {
            GameObject outPartyGo = GameObject.Find("OutParty");
            if (outPartyGo != null) outPartyParent = outPartyGo.transform;
            else Debug.LogWarning("[CharacterSwitchSystem] Could not find a GameObject named 'OutParty' in the scene hierarchy!");
        }

        if (PMembers != null && characterSlots.Length > 0 && characterSlots[0] != null)
        {
            PMembers.currentFrontId = characterSlots[0].characterId;
            PMembers.currentFrontName = characterSlots[0].characterName;
        }

        InitializeUI();
        SelectCharacter(0);

        for (int i = 0; i < charactersInGameNumber; i++)
        {
            if (characterGameObjects[i] != null)
            {
                ToggleMovementScript(characterGameObjects[i], i == 0);
                ToggleNPCScript(characterGameObjects[i], i == 0);

                // 🛠️ LAYER CONTEXT: Nest initial squad assets cleanly under InParty on startup
                if (inPartyParent != null)
                {
                    characterGameObjects[i].transform.SetParent(inPartyParent);
                }
            }
        }

        if (characterSelectionPanel != null)
            characterSelectionPanel.SetActive(false);

        if (leftButton != null) leftButton.onClick.AddListener(Left_Pressed);
        if (upButton != null) upButton.onClick.AddListener(Up_Pressed);
        if (downButton != null) downButton.onClick.AddListener(Down_Pressed);
        if (rightButton != null) rightButton.onClick.AddListener(Right_Pressed);
    }
    public void InitializeUI()
    {
        for (int i = 0; i < holderUI.Length; i++)
        {
            bool active = i < charactersInGameNumber;

            holderUI[i].gameObject.SetActive(active);
            portraitUI[i].gameObject.SetActive(active);

            if (active && characterSlots[i] != null)
            {
                portraitUI[i].sprite = characterSlots[i].characterPortrait;
                SetUnselectedUI(i);
            }
        }
    }

    public void RB_Down()
    {
        if (AMC != null && AMC.masterActionControl == "PartyPanel") return;

        if (AMC.masterActionControl == "idle")
        {
            rbHeld = true;
            characterSelectionPanel.SetActive(true);
            AMC.masterActionControl = "Select Character";
            LockMovementScripts();
            SelectCharacter(0);
            OAF.SelectCharacter();
            Time.timeScale = 0;
        }
    }

    public void RB_Up()
    {
        rbHeld = false;
        characterSelectionPanel.SetActive(false);
        AMC.masterActionControl = "";
        UnlockMovementScripts();

        if (characterSlots[currentCharacterIndex] != null)
        {
            PMembers.currentFrontId = characterSlots[currentCharacterIndex].characterId;
            PMembers.currentFrontName = characterSlots[currentCharacterIndex].characterName;
        }

        OAF.SelectCharacter();
        Time.timeScale = 1;
    }
    public void Left_Pressed()
    {
        if (AMC != null && AMC.masterActionControl == "PartyPanel") return;

        if (AMC.masterActionControl == "Select Character")
        {
            if (!rbHeld) return;
            if (charactersInGameNumber >= 2) SelectCharacter(1);
            if (characterSlots[currentCharacterIndex] != null)
            {
                PMembers.currentFrontId = characterSlots[currentCharacterIndex].characterId;
                PMembers.currentFrontName = characterSlots[currentCharacterIndex].characterName;
            }
            OAF.SelectCharacter();
        }
    }

    public void Up_Pressed()
    {
        if (AMC != null && AMC.masterActionControl == "PartyPanel") return;
        if (AMC.masterActionControl == "Select Character")
        {
            if (!rbHeld) return;
            if (charactersInGameNumber >= 3) SelectCharacter(2);
            if (characterSlots[currentCharacterIndex] != null)
            {
                PMembers.currentFrontId = characterSlots[currentCharacterIndex].characterId;
                PMembers.currentFrontName = characterSlots[currentCharacterIndex].characterName;
            }
            OAF.SelectCharacter();
        }
    }

    public void Down_Pressed()
    {
        if (AMC != null && AMC.masterActionControl == "PartyPanel") return;
        if (AMC.masterActionControl == "Select Character")
        {
            if (!rbHeld) return;
            if (charactersInGameNumber >= 4) SelectCharacter(3);
            if (characterSlots[currentCharacterIndex] != null)
            {
                PMembers.currentFrontId = characterSlots[currentCharacterIndex].characterId;
                PMembers.currentFrontName = characterSlots[currentCharacterIndex].characterName;
            }
            OAF.SelectCharacter();
        }
    }

    public void Right_Pressed()
    {
        if (AMC != null && AMC.masterActionControl == "PartyPanel") return;
        if (AMC.masterActionControl == "Select Character")
        {
            if (!rbHeld) return;
            if (charactersInGameNumber >= 5) SelectCharacter(4);
            if (characterSlots[currentCharacterIndex] != null)
            {
                PMembers.currentFrontId = characterSlots[currentCharacterIndex].characterId;
                PMembers.currentFrontName = characterSlots[currentCharacterIndex].characterName;
            }
            OAF.SelectCharacter();
        }
    }
    void SelectCharacter(int index)
    {
        if (AMC != null && AMC.masterActionControl == "PartyPanel")
        {
            AMC.masterActionControl = "idle";
        }

        int previousCharacter = currentCharacterIndex;
        bool isNewSelection = previousCharacter != index;
        currentCharacterIndex = index;

        CharacterCommands[] allCommands = FindObjectsByType<CharacterCommands>(FindObjectsSortMode.None);
        foreach (var cmd in allCommands)
        {
            cmd.OnCommandButtonResetState();
        }

        AttachAnchorsToCharacter(index);

        for (int i = 0; i < charactersInGameNumber; i++)
        {
            GameObject currentHeroObj = characterGameObjects[i];
            bool isCurrentSelection = (i == index);

            if (isCurrentSelection)
                SetSelectedUI(i);
            else
                SetUnselectedUI(i);

            if (!isMovementLocked)
                ToggleMovementScript(currentHeroObj, isCurrentSelection);

            ToggleNPCScript(currentHeroObj, isCurrentSelection);
            ToggleIKLookTarget(currentHeroObj, isCurrentSelection);

            if (currentHeroObj != null)
            {
                CharacterCommands cmdScript = currentHeroObj.GetComponentInChildren<CharacterCommands>(true);
                ObjectDialogues dialogueScript = currentHeroObj.GetComponentInChildren<ObjectDialogues>(true);

                if (cmdScript != null)
                {
                    cmdScript.enabled = !isCurrentSelection;
                    cmdScript.OnCommandButtonResetState();
                }

                if (dialogueScript != null)
                {
                    dialogueScript.enabled = !isCurrentSelection;
                }
            }
        }

        if (characterSlots[index] != null)
            nameCharacterText.text = characterSlots[index].characterName;

        if (PMembers != null && characterSlots[index] != null)
        {
            PMembers.currentFrontId = characterSlots[index].characterId;
            PMembers.currentFrontName = characterSlots[index].characterName;
        }

        if (isNewSelection)
        {
            Debug.Log("Character Changed: " + previousCharacter + " -> " + index);
            PlayFadeIn();
        }
    }

    void SetSelectedUI(int index)
    {
        holderUI[index].color = Color.black;
        Color portraitColor = portraitUI[index].color;
        portraitColor.a = 1f;
        portraitUI[index].color = portraitColor;
    }

    void SetUnselectedUI(int index)
    {
        holderUI[index].color = Color.white;
        Color portraitColor = portraitUI[index].color;
        portraitColor.a = 0.3f;
        portraitUI[index].color = portraitColor;
    }
    void AttachAnchorsToCharacter(int index)
    {
        if (index < 0 || index >= characterGameObjects.Length) return;
        GameObject selected = characterGameObjects[index];

        if (selected != null)
        {
            Transform armature = FindArmatureByKeyword(selected.transform, armatureKeyword);

            if (armature == null)
            {
                Debug.LogWarning("Armature not found in " + selected.name);
                return;
            }

            transformObjectA.SetParent(armature);
            transformObjectA.localPosition = resetPositionA;
            transformObjectA.localRotation = Quaternion.Euler(resetRotationA);
            transformObjectA.localScale = resetScaleA;

            transformObjectB.SetParent(armature);
            transformObjectB.localPosition = resetPositionB;
            transformObjectB.localRotation = Quaternion.Euler(resetRotationB);
            transformObjectB.localScale = resetScaleB;
        }
    }

    Transform FindArmatureByKeyword(Transform root, string keyword)
    {
        foreach (Transform child in root)
        {
            if (child.name.ToLower().Contains(keyword.ToLower()))
                return child;

            Transform found = FindArmatureByKeyword(child, keyword);
            if (found != null)
                return found;
        }
        return null;
    }

    void ToggleMovementScript(GameObject character, bool enable)
    {
        if (character == null) return;
        var movement = character.GetComponent<PlayerMovement3D>();
        if (movement != null) movement.enabled = enable;
    }

    void ToggleNPCScript(GameObject character, bool disable)
    {
        if (character == null) return;
        NPCFollowPlayer npcmovement = character.GetComponent<NPCFollowPlayer>();
        if (npcmovement != null) npcmovement.enabled = !disable;
    }

    void ToggleIKLookTarget(GameObject character, bool isActive)
    {
        if (character == null) return;
        var ikLook = character.GetComponent<PlayerIKLook>();
        if (ikLook != null)
        {
            ikLook.cameraTransform = isActive ? activeCharacterLookAt : inActiveCharacterLookAt;
        }
    }

    void PlayFadeIn()
    {
        if (fadeAnimator == null || fadeInClip == null) return;
        fadeAnimator.Play(fadeInClip.name, 0, 0f);
        StartCoroutine(WaitForFadeIn());
    }

    IEnumerator WaitForFadeIn()
    {
        yield return new WaitForSeconds(fadeInClip.length);
        UnlockMovementScripts();
    }

    void LockMovementScripts()
    {
        isMovementLocked = true;
        foreach (var character in characterGameObjects)
        {
            if (character == null) continue;
            var movement = character.GetComponent<PlayerMovement3D>();
            if (movement != null) movement.enabled = false;
        }
    }

    void UnlockMovementScripts()
    {
        isMovementLocked = false;
        for (int i = 0; i < charactersInGameNumber; i++)
        {
            var character = characterGameObjects[i];
            if (character == null) continue;
            var movement = character.GetComponent<PlayerMovement3D>();
            if (movement != null) movement.enabled = (i == currentCharacterIndex);
        }
    }
}
