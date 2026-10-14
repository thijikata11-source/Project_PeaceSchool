using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleFieldMode : MonoBehaviour
{
    public bool BattleMode = false;

    [Header("State Flow Management")]
    public BattleState currentBattleState = BattleState.CharacterSelection;
    public EnemyTargetingSystem targetingSystem;

    [Header("Turn & Round Tracking Counters")]
    [SerializeField] private int currentRoundNumber = 1;
    private List<GameObject> combatTurnOrderQueue = new List<GameObject>();
    private int currentTurnQueueIndex = 0;

    // Track chosen units manually within custom selection phases
    private HashSet<GameObject> spentPlayerUnitsThisRound = new HashSet<GameObject>();
    private bool isPlayerPhaseActive = false;

    [Header("Battle Field Swapping Configuration")]
    public List<GameObject> objectsToEnable = new List<GameObject>();
    public List<GameObject> objectsToDisable = new List<GameObject>();

    [Header("System Dependencies")]
    public CharacterSwitchSystem characterSwitchSystem;
    public PartyMembers partyMembers;
    public GameObject commandUIPanel;

    [Tooltip("Assign the ActionMasterControl GameObject or component here")]
    public ActionMasterControl AMC;

    [Header("Standard Camera References")]
    public Camera mainBattleCamera;
    public Transform playerCamLookTarget;
    public Transform enemyCamLookTarget;
    public float cameraIntroZOffset = 4.0f;
    public float cameraBattleTurnZOffset = 3.5f;
    public float cameraBattleTurnXOffset = 0.75f;

    [Header("Player Battle Arena Anchors")]
    public Transform playerPositionA;
    public Transform playerPositionB;
    public Transform playerPositionC;
    public Transform playerPositionD;
    public Transform playerPositionE;

    [Header("Enemy Battle Arena Anchors")]
    public List<Transform> enemyPositions = new List<Transform>(10);
    public Transform bossPosition;

    [Header("Mobile UI Buttons Assignments")]
    public GameObject mobileLeftButton;
    public GameObject mobileRightButton;
    public GameObject mobileSelectButton;
    public GameObject mobileBackButton;

    private List<GameObject> activePlayerUnits = new List<GameObject>();
    private List<GameObject> instantiatedEnemyUnits = new List<GameObject>();
    private GameObject currentTurnUnit;
    private bool isPlayerTurn = false;
    private bool battleActive = false;

    private int currentlyHighlightedCharacterIndex = 0;
    private bool isCharacterConfirmedSelected = false;

    private GameObject battleTriggeringUnit = null;
    private List<GameObject> internalEnemyPrefabsToSpawn = new List<GameObject>();
    private bool internalIsBossEncounter = false;

    [Header("Combat Text Visual Effects Spawning")]
    [Tooltip("Drag your Floating Damage Text Prefab here from your project folder.")]
    public GameObject damageTextPrefab;

    // Drag your new script reference into this empty property window slot inside the inspector
    public SkillsNavigationalPanel skillsNavPanel;

    [Header("UI Routing Links")]
    [Tooltip("Drag your CommandOptionTriggers script reference slot layer here.")]
    public CommandOptionTriggers commandOptionTriggers;

    // 🛠️ BULLETPROOF TIMING TRACKERS FOR ANIMATION EVENTS
    private SkillScriptable activeCastedSkillContext = null;
    private bool currentActionHasHit = true;
    private List<GameObject> animationTargetsCache = new List<GameObject>(); // 🌟 THE FIXED REGISTRY BUFFER




    void Start()
    {
        if (commandUIPanel != null) commandUIPanel.SetActive(false);
        if (mainBattleCamera == null) mainBattleCamera = Camera.main;
    }

    void Update()
    {
        if (BattleMode && !battleActive)
        {
            StartCoroutine(BeginBattleSequence());
        }
    }

    public void InitiateBattleData(GameObject unit, List<GameObject> enemyPrefabs, bool isBoss)
    {
        battleTriggeringUnit = unit;
        internalEnemyPrefabsToSpawn = new List<GameObject>(enemyPrefabs);
        internalIsBossEncounter = isBoss;
    }

    public void InitiateBattleWithTriggeringUnit(GameObject unit)
    {
        battleTriggeringUnit = unit;
    }
    private IEnumerator BeginBattleSequence()
    {
        battleActive = true;
        currentRoundNumber = 1;
        Debug.Log("⚔️ Battle Sequence Initiated.");

        if (AMC != null) AMC.masterActionControl = "BattleMode";

        ExecuteEnvironmentSwap();
        SetUpCombatants();
        SpawnEnemySquad();

        yield return StartCoroutine(PlayCameraIntro());

        StartNewCombatRound();
    }

    private void ExecuteEnvironmentSwap()
    {
        foreach (var obj in objectsToEnable) { if (obj != null) obj.SetActive(true); }
        foreach (var obj in objectsToDisable) { if (obj != null) obj.SetActive(false); }
    }

    private void SetUpCombatants()
    {
        activePlayerUnits.Clear();
        if (characterSwitchSystem == null) return;

        int totalMembers = characterSwitchSystem.charactersInGameNumber;
        List<Transform> arenaAnchors = new List<Transform>();
        if (playerPositionB != null) arenaAnchors.Add(playerPositionB);
        if (playerPositionC != null) arenaAnchors.Add(playerPositionC);
        if (playerPositionD != null) arenaAnchors.Add(playerPositionD);
        if (playerPositionE != null) arenaAnchors.Add(playerPositionE);

        int nonFrontCounter = 0;

        for (int i = 0; i < totalMembers; i++)
        {
            GameObject playerObj = characterSwitchSystem.characterGameObjects[i];
            if (playerObj == null) continue;

            DisableOverworldScriptsOnUnit(playerObj);

            bool isTriggeringLeader = (battleTriggeringUnit != null && playerObj == battleTriggeringUnit);
            if (battleTriggeringUnit == null && i == characterSwitchSystem.currentCharacterIndex) isTriggeringLeader = true;

            if (isTriggeringLeader && playerPositionA != null)
            {
                playerObj.transform.position = playerPositionA.position;
                playerObj.transform.rotation = playerPositionA.rotation;
                activePlayerUnits.Insert(0, playerObj);
            }
            else if (nonFrontCounter < arenaAnchors.Count && arenaAnchors[nonFrontCounter] != null)
            {
                playerObj.transform.position = arenaAnchors[nonFrontCounter].position;
                playerObj.transform.rotation = arenaAnchors[nonFrontCounter].rotation;
                activePlayerUnits.Add(playerObj);
                nonFrontCounter++;
            }

            Animator playerAnimator = playerObj.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
                playerAnimator.SetBool("BattleMode", true);
                playerAnimator.SetTrigger("BattleReady");
            }
        }
    }

    private void DisableOverworldScriptsOnUnit(GameObject obj)
    {
        CharacterController cc = obj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null && (script.GetType().Name == "NPCFollowPlayer" || script.GetType().Name == "ObjectAccurateFollower" || script.GetType().Name == "PlayerIKLook"))
                script.enabled = false;
        }

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb != null) { rb.velocity = Vector3.zero; rb.isKinematic = true; }

        Camera[] prefabCameras = obj.GetComponentsInChildren<Camera>(true);
        foreach (Camera cam in prefabCameras) { if (cam != mainBattleCamera) cam.enabled = false; }
    }

    private void SpawnEnemySquad()
    {
        foreach (var oldEnemy in instantiatedEnemyUnits) { if (oldEnemy != null) Destroy(oldEnemy); }
        instantiatedEnemyUnits.Clear();

        if (internalEnemyPrefabsToSpawn == null || internalEnemyPrefabsToSpawn.Count == 0) return;
        int loopLimit = Mathf.Min(internalEnemyPrefabsToSpawn.Count, 10);

        for (int i = 0; i < loopLimit; i++)
        {
            GameObject prefab = internalEnemyPrefabsToSpawn[i];
            if (prefab == null) continue;

            Transform targetAnchor = (internalIsBossEncounter && i == 0 && bossPosition != null) ? bossPosition : (i < enemyPositions.Count ? enemyPositions[i] : null);

            if (targetAnchor != null)
            {
                GameObject spawnedEnemy = Instantiate(prefab, targetAnchor.position, targetAnchor.rotation);
                instantiatedEnemyUnits.Add(spawnedEnemy);

                Animator enemyAnim = spawnedEnemy.GetComponentInChildren<Animator>();
                if (enemyAnim != null)
                {
                    enemyAnim.SetBool("BattleMode", true);
                    enemyAnim.SetTrigger("BattleReady");
                }
            }
        }
    }
    private void StartNewCombatRound()
    {
        activePlayerUnits.RemoveAll(item => item == null);
        instantiatedEnemyUnits.RemoveAll(item => item == null);

        spentPlayerUnitsThisRound.Clear();

        Debug.Log($"🔄 [Round {currentRoundNumber}] Phase Initiated.");
        EnterPlayerSelectionPhase();
    }

    private void EnterPlayerSelectionPhase()
    {
        isPlayerPhaseActive = true;
        isPlayerTurn = true;
        isCharacterConfirmedSelected = false;
        currentBattleState = BattleState.CharacterSelection;

        // 🛠️ BUG FIX: Check if ANY character is actually capable of acting right now
        int availableActorsCount = 0;
        foreach (GameObject playerObj in activePlayerUnits)
        {
            if (playerObj == null) continue;
            CharacterStats stats = playerObj.GetComponent<CharacterStats>();
            if (!spentPlayerUnitsThisRound.Contains(playerObj) && stats != null && stats.CurrentHP > 0)
            {
                availableActorsCount++;
            }
        }

        // If no one is left alive to take a turn, immediately exit to the enemy phase!
        if (availableActorsCount == 0)
        {
            Debug.Log("🔄 No available living players left to act this round. Forcing Phase Transition.");
            isPlayerPhaseActive = false;
            StartCoroutine(ExecuteEnemyTeamPhase());
            return;
        }

        FindNextAvailableSelectionIndex();

        // AUTOMATED LOCKOUT SAFEGUARD: Check active character skills size parameters to hide the 'Skills' command if 0
        if (activePlayerUnits.Count > 0 && currentlyHighlightedCharacterIndex < activePlayerUnits.Count)
        {
            GameObject upcomingUnit = activePlayerUnits[currentlyHighlightedCharacterIndex];
            CommandNavigationalPanel cmdPanel = commandUIPanel != null ? commandUIPanel.GetComponent<CommandNavigationalPanel>() : null;
            TurnbaseAnimationsTrigger animTrigger = upcomingUnit != null ? upcomingUnit.GetComponent<TurnbaseAnimationsTrigger>() : null;

            if (cmdPanel != null && animTrigger != null)
            {
                int validSkillCount = 0;
                if (animTrigger.characterSkillSet != null)
                {
                    foreach (var skill in animTrigger.characterSkillSet) { if (skill != null) validSkillCount++; }
                }

                if (validSkillCount == 0)
                {
                    Debug.Log($"🔒 {upcomingUnit.name} has no skills. Disabling 'Skills' menu button.");
                    cmdPanel.SetOptionVisibilityRuntime("Skills", false);
                }
                else
                {
                    cmdPanel.SetOptionVisibilityRuntime("Skills", true);
                }
            }
        }

        UpdateSelectionView();
    }

    private void FindNextAvailableSelectionIndex()
    {
        for (int i = 0; i < activePlayerUnits.Count; i++)
        {
            CharacterStats stats = activePlayerUnits[i].GetComponent<CharacterStats>();

            // Only auto-highlight units who haven't acted AND are still alive (HP > 0)
            if (!spentPlayerUnitsThisRound.Contains(activePlayerUnits[i]) && stats != null && stats.CurrentHP > 0)
            {
                currentlyHighlightedCharacterIndex = i;
                return;
            }
        }
    }


    public void OnPressLeftButton()
    {
        if (!BattleMode || !isPlayerTurn) return;

        switch (currentBattleState)
        {
            case BattleState.CharacterSelection:
                int startingIndexLeft = currentlyHighlightedCharacterIndex;
                int safetyCounterLeft = 0; // Emergency break loop tracker

                do
                {
                    currentlyHighlightedCharacterIndex--;
                    if (currentlyHighlightedCharacterIndex < 0) currentlyHighlightedCharacterIndex = activePlayerUnits.Count - 1;

                    CharacterStats stats = activePlayerUnits[currentlyHighlightedCharacterIndex].GetComponent<CharacterStats>();

                    // Keep cycling left if the unit has already acted OR is dead (HP <= 0)
                    if (!spentPlayerUnitsThisRound.Contains(activePlayerUnits[currentlyHighlightedCharacterIndex]) && stats != null && stats.CurrentHP > 0)
                        break;

                    safetyCounterLeft++;
                    // BUG FIX: If we cycled through the entire party size array, break instantly to stop freezing
                    if (safetyCounterLeft >= activePlayerUnits.Count) break;

                } while (currentlyHighlightedCharacterIndex != startingIndexLeft);

                // This now automatically evaluates the smart camera router to face selected support targets!
                UpdateSelectionView();
                break;

            case BattleState.SkillsMenu:
                if (skillsNavPanel != null) skillsNavPanel.NavigateUp();
                break;

            case BattleState.EnemyTargeting:
            case BattleState.MultiTargetingSelection:
                if (targetingSystem != null) targetingSystem.TargetPrevious();
                break;
        }
    }

    public void OnPressRightButton()
    {
        if (!BattleMode || !isPlayerTurn) return;

        switch (currentBattleState)
        {
            case BattleState.CharacterSelection:
                int startingIndexRight = currentlyHighlightedCharacterIndex;
                int safetyCounterRight = 0; // Emergency break loop tracker

                do
                {
                    currentlyHighlightedCharacterIndex++;
                    if (currentlyHighlightedCharacterIndex >= activePlayerUnits.Count) currentlyHighlightedCharacterIndex = 0;

                    CharacterStats stats = activePlayerUnits[currentlyHighlightedCharacterIndex].GetComponent<CharacterStats>();

                    // Keep cycling right if the unit has already acted OR is dead (HP <= 0)
                    if (!spentPlayerUnitsThisRound.Contains(activePlayerUnits[currentlyHighlightedCharacterIndex]) && stats != null && stats.CurrentHP > 0)
                        break;

                    safetyCounterRight++;
                    // BUG FIX: If we cycled through the entire party size array, break instantly to stop freezing
                    if (safetyCounterRight >= activePlayerUnits.Count) break;

                } while (currentlyHighlightedCharacterIndex != startingIndexRight);

                // This now automatically evaluates the smart camera router to face selected support targets!
                UpdateSelectionView();
                break;

            case BattleState.SkillsMenu:
                if (skillsNavPanel != null) skillsNavPanel.NavigateDown();
                break;

            case BattleState.EnemyTargeting:
            case BattleState.MultiTargetingSelection:
                if (targetingSystem != null) targetingSystem.TargetNext();
                break;
        }
    }




    public void EndCurrentTurnCycle()
    {
        if (isPlayerPhaseActive)
        {
            if (currentTurnUnit != null)
            {
                spentPlayerUnitsThisRound.Add(currentTurnUnit);
                Debug.Log($"🔒 Turn ended for: {currentTurnUnit.name}. Added to spent actions registry.");
            }

            // 🛠️ BUG FIX: Count how many characters are ACTIVELY capable of taking a turn (Alive)
            int totalLivingPlayers = 0;
            foreach (GameObject playerObj in activePlayerUnits)
            {
                if (playerObj == null) continue;
                CharacterStats stats = playerObj.GetComponent<CharacterStats>();
                if (stats != null && stats.CurrentHP > 0)
                {
                    totalLivingPlayers++;
                }
            }

            // Count how many of those living players have spent their turn
            int spentLivingPlayersCount = 0;
            foreach (GameObject spentObj in spentPlayerUnitsThisRound)
            {
                if (spentObj == null) continue;
                CharacterStats stats = spentObj.GetComponent<CharacterStats>();
                if (stats != null && stats.CurrentHP > 0)
                {
                    spentLivingPlayersCount++;
                }
            }

            // Proceed to next selection if there are still living characters left to choose from
            if (spentLivingPlayersCount < totalLivingPlayers)
            {
                EnterPlayerSelectionPhase();
            }
            else
            {
                // All living characters have acted; transition cleanly to the enemy phase!
                isPlayerPhaseActive = false;
                StartCoroutine(ExecuteEnemyTeamPhase());
            }
        }
    }


    private IEnumerator ExecuteEnemyTeamPhase()
    {
        isPlayerTurn = false;
        currentBattleState = BattleState.AnimationPlaying;
        if (commandUIPanel != null) commandUIPanel.SetActive(false);

        Debug.Log("🔴 Enemy AI Action Phase Started.");
        instantiatedEnemyUnits.RemoveAll(item => item == null);

        foreach (GameObject enemy in instantiatedEnemyUnits)
        {
            if (enemy == null) continue;

            currentTurnUnit = enemy;
            Debug.Log($"Enemy Acting: {enemy.name}");
            PositionCameraBehindUnit(enemy);

            yield return new WaitForSeconds(0.8f);

            // 🛠️ DYNAMIC LIVING POOL FILTERING
            // Create a temporary, clean list of only player units who are actively alive
            List<GameObject> validLivingTargets = new List<GameObject>();

            foreach (GameObject playerObj in activePlayerUnits)
            {
                if (playerObj == null) continue;

                CharacterStats playerStats = playerObj.GetComponent<CharacterStats>();
                // Only include the character if they have a stats component and their HP is above 0
                if (playerStats != null && playerStats.CurrentHP > 0)
                {
                    validLivingTargets.Add(playerObj);
                }
            }

            // 🎯 INTELLECTUAL TARGET ALLOCATION
            if (validLivingTargets.Count > 0)
            {
                // Choose a random target EXCLUSIVELY from the surviving party members pool
                GameObject targetPlayer = validLivingTargets[Random.Range(0, validLivingTargets.Count)];

                // Keep your camera indices cleanly updated pointing back to the core tracking registry
                currentlyHighlightedCharacterIndex = activePlayerUnits.IndexOf(targetPlayer);

                // Read the range toggle helper field ahead of time without triggering the clip early
                EnemyAnimationsTrigger enemyAnim = enemy.GetComponent<EnemyAnimationsTrigger>();
                bool enemyIsRanged = false;
                if (enemyAnim != null && enemyAnim.attackClip != null)
                {
                    enemyIsRanged = enemyAnim.attackClip.isRangedAction;
                }

                // Pass the movement command cleanly to attack the selected living player
                yield return StartCoroutine(PlayAttackAnimationSequence(enemy, targetPlayer, enemyIsRanged));
            }
            else
            {
                // 💀 GAME OVER CONDITION: All party members have fallen!
                Debug.LogWarning("❌ All player party members are down! Enemies have no valid targets remaining.");
                // Optional: Insert your custom trigger here to open a Game Over UI screen or force a defeat menu canvas
                break;
            }
        }

        currentRoundNumber++;
        StartNewCombatRound();
    }





    public void OnPressSelectButton()
    {
        if (!BattleMode || !isPlayerTurn) return;

        switch (currentBattleState)
        {
            case BattleState.CharacterSelection:
                GameObject selectedUnit = activePlayerUnits[currentlyHighlightedCharacterIndex];
                if (spentPlayerUnitsThisRound.Contains(selectedUnit)) return;
                isCharacterConfirmedSelected = true;
                currentTurnUnit = selectedUnit;
                currentBattleState = BattleState.CommandMenu;
                StartCoroutine(DelayedCommandMenuActivation());
                break;

            case BattleState.CommandMenu:
                CommandNavigationalPanel cmdPanel = commandUIPanel != null ? commandUIPanel.GetComponent<CommandNavigationalPanel>() : null;
                if (cmdPanel != null) cmdPanel.ExecuteInteraction();
                break;

            case BattleState.SkillsMenu:
                // 🛠️ SECURED CHECK: Verify pool constraints via panel script helper validation state arrays before execution
                if (skillsNavPanel != null)
                {
                    skillsNavPanel.ExecuteSkillInteraction();
                }
                break;

            case BattleState.EnemyTargeting:
            case BattleState.MultiTargetingSelection:
                // SMART TARGETING ROUTER BUG FIX:
                // Check if the open targeting session is handling a manual cursor or a SelfOnly shortcut!
                CommandOptionTriggers foundTriggers = FindFirstObjectByType<CommandOptionTriggers>();
                bool isSelfOnlySkillActive = false;

                if (foundTriggers != null)
                {
                    // Use Reflection to safely read the private activeSelectedSkillContext field inside CommandOptionTriggers
                    System.Reflection.FieldInfo skillContextField = typeof(CommandOptionTriggers).GetField("activeSelectedSkillContext", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (skillContextField != null)
                    {
                        SkillScriptable activeSkill = skillContextField.GetValue(foundTriggers) as SkillScriptable;
                        if (activeSkill != null && activeSkill.skillScope == SkillScriptable.SkillScopeType.SelfOnly)
                        {
                            isSelfOnlySkillActive = true;
                        }
                    }
                }

                // If a SelfOnly skill is active, instantly execute it on the caster!
                if (isSelfOnlySkillActive && foundTriggers != null)
                {
                    Debug.Log($"💖 SelfOnly Skill confirmed! Casting instantly on: {currentTurnUnit.name}");
                    foundTriggers.CommitFinalSkillExecution();
                }
                // Otherwise, safely pass control to the cursor management script for 1, 3, or All targets
                else if (targetingSystem != null && targetingSystem.enabled && targetingSystem.gameObject.activeInHierarchy)
                {
                    targetingSystem.ConfirmTargetSelection();
                }
                break;
        }
    }



    public void OnPressBackButton()
    {
        if (!BattleMode || !isPlayerTurn) return;

        switch (currentBattleState)
        {
            case BattleState.CommandMenu:
                isCharacterConfirmedSelected = false;
                currentTurnUnit = null;
                currentBattleState = BattleState.CharacterSelection;
                if (commandUIPanel != null) commandUIPanel.SetActive(false);
                UpdateSelectionView();
                break;

            case BattleState.SkillsMenu:
                if (skillsNavPanel != null) skillsNavPanel.gameObject.SetActive(false);
                currentBattleState = BattleState.CommandMenu;
                if (commandUIPanel != null) commandUIPanel.SetActive(true);
                break;

            case BattleState.EnemyTargeting:
            case BattleState.MultiTargetingSelection:
                // REFERENCE FIX: Dynamically find the component instead of checking an undeclared field
                if (targetingSystem != null)
                {
                    targetingSystem.CancelTargetingSelection();
                }
                else
                {
                    CommandOptionTriggers foundTriggers = FindFirstObjectByType<CommandOptionTriggers>();
                    if (foundTriggers != null)
                    {
                        foundTriggers.CancelSelfOnlyTargeting();
                    }
                }

                // 🛠️ RESET VIEW: Snap camera back behind the character when backing out of targeting selection
                if (currentTurnUnit != null)
                {
                    PositionCameraBehindUnit(currentTurnUnit);
                }
                break;


        }
    }


    private IEnumerator DelayedCommandMenuActivation()
    {
        yield return new WaitForEndOfFrame();
        if (commandUIPanel != null) commandUIPanel.SetActive(true);
    }


    private void UpdateSelectionView()
    {
        if (activePlayerUnits.Count == 0) return;
        GameObject targetedUnit = activePlayerUnits[currentlyHighlightedCharacterIndex];

        // 🛠️ SMART CAMERA TARGET ROUTER
        if (commandOptionTriggers != null && commandOptionTriggers.skillsUIPanel != null && commandOptionTriggers.skillsUIPanel.activeSelf)
        {
            // If the skills panel is active, we are actively picking targets for a support/friendly skill
            PositionCameraInFrontOfUnit(targetedUnit);
            Debug.Log($"🎬 Target selection phase active: Framed camera in FRONT of friendly unit: {targetedUnit.name}");
        }
        else
        {
            // Standard exploration selection perspective path baseline
            PositionCameraBehindUnit(targetedUnit);
        }
    }

    public List<GameObject> GetInstantiatedEnemyUnits() => instantiatedEnemyUnits;
    private IEnumerator PlayCameraIntro()
    {
        if (mainBattleCamera == null || playerCamLookTarget == null || enemyCamLookTarget == null) yield break;
        float duration = 2.0f; float elapsed = 0f;
        Vector3 startPos = playerCamLookTarget.position - playerCamLookTarget.forward * cameraIntroZOffset + Vector3.up * 2f;
        Vector3 endPos = enemyCamLookTarget.position + enemyCamLookTarget.forward * cameraIntroZOffset + Vector3.up * 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; float t = elapsed / duration;
            mainBattleCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainBattleCamera.transform.LookAt(enemyCamLookTarget.position);
            yield return null;
        }
    }

    private void PositionCameraBehindUnit(GameObject unit)
    {
        if (mainBattleCamera == null || unit == null) return;
        Vector3 behindBasePos = unit.transform.position - (unit.transform.forward * cameraBattleTurnZOffset) + (Vector3.up * 1.8f);
        Vector3 finalOffsetPos = behindBasePos + (unit.transform.right * cameraBattleTurnXOffset);
        mainBattleCamera.transform.position = finalOffsetPos;
        mainBattleCamera.transform.rotation = Quaternion.LookRotation(unit.transform.forward + (Vector3.down * 0.1f));
    }

    /// <summary>
    /// Swaps camera perspective to stand in front of a friendly unit, looking back at them during supportive actions.
    /// </summary>
    /// <summary>
    /// Swaps camera perspective to stand in front of a friendly unit, looking back at them during supportive actions.
    /// </summary>
    public void PositionCameraInFrontOfUnit(GameObject unit)
    {
        if (mainBattleCamera == null || unit == null) return;

        Vector3 inFrontBasePos = unit.transform.position + (unit.transform.forward * cameraBattleTurnZOffset) + (Vector3.up * 1.8f);
        Vector3 finalOffsetPos = inFrontBasePos - (unit.transform.right * cameraBattleTurnXOffset);

        mainBattleCamera.transform.position = finalOffsetPos;
        mainBattleCamera.transform.LookAt(unit.transform.position + Vector3.up * 1.0f);
    }



    public void OnPlayerAttackCommand(GameObject targetPlaceholder, bool isRanged)
    {
        currentBattleState = BattleState.AnimationPlaying;
        if (commandUIPanel != null) commandUIPanel.SetActive(false);
        StartCoroutine(PlayerAttackSequence(targetPlaceholder, isRanged));
    }

    private IEnumerator PlayerAttackSequence(GameObject targetPlaceholder, bool isRanged)
    {
        yield return StartCoroutine(PlayAttackAnimationSequence(currentTurnUnit, targetPlaceholder, isRanged));
        EndCurrentTurnCycle();
    }

    private IEnumerator PlayAttackAnimationSequence(GameObject attacker, GameObject target, bool isRanged)
    {
        // Cache initial parameters cleanly
        Vector3 originalAttackerPosition = attacker.transform.position;
        Quaternion originalAttackerRotation = attacker.transform.rotation;
        Quaternion originalDefenderRotation = target != null ? target.transform.rotation : Quaternion.identity;

        float speed = 14f;

        // Support skill exception check. Skip flat plane look-at pivot entirely!
        bool isSupportSkill = (activeCastedSkillContext != null && activeCastedSkillContext.skillAbility == SkillScriptable.SkillAbilityType.Support);

        if (target != null && !isSupportSkill)
        {
            Vector3 targetPositionOnPlane = new Vector3(target.transform.position.x, attacker.transform.position.y, target.transform.position.z);
            attacker.transform.LookAt(targetPositionOnPlane);

            Vector3 attackerPositionOnPlane = new Vector3(attacker.transform.position.x, target.transform.position.y, attacker.transform.position.z);
            target.transform.LookAt(attackerPositionOnPlane);
        }

        if (!isRanged)
        {
            float targetModelRadius = 0.6f;

            if (target != null)
            {
                Collider targetCollider = target.GetComponent<Collider>();
                if (targetCollider == null) targetCollider = target.GetComponentInChildren<Collider>();

                if (targetCollider != null)
                {
                    targetModelRadius = Mathf.Max(targetCollider.bounds.extents.x, targetCollider.bounds.extents.z);
                }
            }

            float comfortPadding = isPlayerTurn ? 0.4f : 0.8f;

            Vector3 targetStrikingDistance = target != null ?
                target.transform.position - (target.transform.position - attacker.transform.position).normalized * (targetModelRadius + comfortPadding) :
                attacker.transform.position + attacker.transform.forward * 3f;

            while (Vector3.Distance(attacker.transform.position, targetStrikingDistance) > 0.05f)
            {
                attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, targetStrikingDistance, speed * Time.deltaTime);

                if (target != null && !isSupportSkill)
                    attacker.transform.LookAt(new Vector3(target.transform.position.x, attacker.transform.position.y, target.transform.position.z));

                if (mainBattleCamera != null)
                {
                    Vector3 camTargetPosition = attacker.transform.position - (attacker.transform.forward * cameraBattleTurnZOffset) + (Vector3.up * 1.8f) + (attacker.transform.right * cameraBattleTurnXOffset);
                    mainBattleCamera.transform.position = Vector3.Lerp(mainBattleCamera.transform.position, camTargetPosition, 10f * Time.deltaTime);
                    if (target != null && !isSupportSkill) mainBattleCamera.transform.LookAt(target.transform.position + Vector3.up * 1f);
                }
                yield return null;
            }
        }
        else
        {
            // Ranged camera staging phase (Only update rotation angle look-at vectors if it isn't an exploration support skill)
            if (mainBattleCamera != null && target != null && !isSupportSkill)
            {
                mainBattleCamera.transform.LookAt(target.transform.position + Vector3.up * 1f);
            }
            yield return new WaitForSeconds(0.2f);
        }

        EnemyAnimationsTrigger enemyAnimTrigger = attacker.GetComponent<EnemyAnimationsTrigger>();
        Animator playerAnimator = attacker.GetComponentInChildren<Animator>();

        if (!isPlayerTurn && enemyAnimTrigger != null && enemyAnimTrigger.attackClip != null && enemyAnimTrigger.attackClip.clip != null)
        {
            enemyAnimTrigger.TriggerAttackAnimation();
            Debug.Log($"🎬 Enemy arrived at destination. Playing custom clip: {enemyAnimTrigger.attackClip.clip.name}");
        }
        else if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("Attack");
        }

        yield return new WaitForSeconds(0.15f);

        // =========================================================================
        // 🛠️ DUPLICATE NUMBER BUG FIX GATING LAYER
        // =========================================================================
        // ONLY apply standard weapon attack damage calculations if we aren't currently 
        // processing a special skill scriptable object configuration loop!
        if (activeCastedSkillContext == null)
        {
            if (isPlayerTurn)
            {
                ProcessDamageExecution(attacker, target);
            }
            else
            {
                ProcessEnemyDamageExecution(attacker, target);
            }
        }
        else
        {
            Debug.Log("🛡️ Standard basic attack damage calculations skipped: Special skill cast asset is active.");
        }
        // =========================================================================

        if (MultiTargetIndicatorManager.Instance != null)
        {
            MultiTargetIndicatorManager.Instance.ClearAllIndicators();
        }

        yield return new WaitForSeconds(0.45f);

        if (!isRanged)
        {
            while (Vector3.Distance(attacker.transform.position, originalAttackerPosition) > 0.1f)
            {
                attacker.transform.position = Vector3.MoveTowards(attacker.transform.position, originalAttackerPosition, speed * Time.deltaTime);

                if (mainBattleCamera != null && !isSupportSkill)
                {
                    Vector3 camTargetPosition = attacker.transform.position - (attacker.transform.forward * cameraBattleTurnZOffset) + (Vector3.up * 1.8f) + (attacker.transform.right * cameraBattleTurnXOffset);
                    mainBattleCamera.transform.position = Vector3.Lerp(mainBattleCamera.transform.position, camTargetPosition, 10f * Time.deltaTime);
                    mainBattleCamera.transform.rotation = Quaternion.Lerp(mainBattleCamera.transform.rotation, Quaternion.LookRotation(attacker.transform.forward + (Vector3.down * 0.1f)), 10f * Time.deltaTime);
                }
                yield return null;
            }
        }

        attacker.transform.position = originalAttackerPosition;
        attacker.transform.rotation = originalAttackerRotation;

        if (target != null)
        {
            target.transform.rotation = originalDefenderRotation;
        }

        if (!isSupportSkill)
        {
            PositionCameraBehindUnit(attacker);
        }
    }





    public void OnPlayerSkillCommand(GameObject targetPlaceholder, SkillScriptable skillData)
    {
        currentBattleState = BattleState.AnimationPlaying;
        if (commandUIPanel != null) commandUIPanel.SetActive(false);
        if (skillsNavPanel != null) skillsNavPanel.gameObject.SetActive(false);

        // 🔒 LOCK CONTEXT IMMEDIATELY: Save a dedicated freeze copy for the animation event
        activeCastedSkillContext = skillData;
        animationTargetsCache.Clear();

        if (skillData != null)
        {
            if (skillData.skillScope == SkillScriptable.SkillScopeType.AllCharacters)
            {
                if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Offensive)
                    animationTargetsCache.AddRange(instantiatedEnemyUnits);
                else
                    animationTargetsCache.AddRange(activePlayerUnits);
            }
            else if (skillData.skillScope == SkillScriptable.SkillScopeType.SelfOnly)
            {
                if (currentTurnUnit != null) animationTargetsCache.Add(currentTurnUnit);
            }
            else
            {
                // Pull exactly what was targeted right now before UI records change
                CommandOptionTriggers UIFields = FindFirstObjectByType<CommandOptionTriggers>();
                if (UIFields != null)
                {
                    if (UIFields.multiTargetPlaceholders != null && UIFields.multiTargetPlaceholders.Count > 0)
                    {
                        foreach (var tgt in UIFields.multiTargetPlaceholders)
                        {
                            if (tgt != null && !animationTargetsCache.Contains(tgt))
                                animationTargetsCache.Add(tgt);
                        }
                    }
                    else if (UIFields.currentTargetPlaceholder != null)
                    {
                        animationTargetsCache.Add(UIFields.currentTargetPlaceholder);
                    }
                }

                // Fallback baseline safety check
                if (animationTargetsCache.Count == 0 && targetPlaceholder != null)
                {
                    animationTargetsCache.Add(targetPlaceholder);
                }
            }
        }

        StartCoroutine(PlayerSkillSequence(targetPlaceholder, skillData));
    }


    private IEnumerator PlayerSkillSequence(GameObject targetPlaceholder, SkillScriptable skillData)
    {
        if (skillData == null) yield break;

        // 🎥 PHASE 1: Cast Skill Animation (Camera behind player caster)
        if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Support)
        {
            Debug.Log($"🎬 Support skill cast initiated. Camera framing behind casting player: {currentTurnUnit.name}");
            PositionCameraBehindUnit(currentTurnUnit);
        }

        // Run up or frame action zoom setup 
        yield return StartCoroutine(PlayAttackAnimationSequence(currentTurnUnit, targetPlaceholder, skillData.isRangedSkill));

        // 🎥 PHASE 2: While casting skill (Camera snaps to front of target player/players)
        if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Support)
        {
            GameObject primaryTarget = targetPlaceholder;
            if (animationTargetsCache.Count > 0 && animationTargetsCache[0] != null)
            {
                primaryTarget = animationTargetsCache[0];
            }

            if (primaryTarget != null)
            {
                PositionCameraInFrontOfUnit(primaryTarget);
            }
        }

        // Programmatically trigger the combined impact pipeline instantly
        ExecuteAnimationHitImpactEvent();

        // 🎥 PHASE 3: Hold viewpoint tracking posture on target, then release control loop
        yield return new WaitForSeconds(1.2f);

        activeCastedSkillContext = null;
        animationTargetsCache.Clear();
        EndCurrentTurnCycle();
    }






    public void ProcessSkillExecution(GameObject attackerObj, SkillScriptable skillData, GameObject primaryDefenderObj)
    {
        if (attackerObj == null || skillData == null) return;

        // 🛠️ CACHE SECURITY LINK: Lock this skill context so the animation receiver can see it
        activeCastedSkillContext = skillData;
        currentActionHasHit = true; // Baseline default flag

        CharacterStats attackerStats = attackerObj.GetComponent<CharacterStats>();
        if (attackerStats == null) return;

        // 1. RESOURCE CONSUMPTION: Deduct resource pooling values straight from live variables
        attackerStats.CurrentHP = Mathf.Max(0, attackerStats.CurrentHP - skillData.hpCost);
        attackerStats.CurrentMana = Mathf.Max(0, attackerStats.CurrentMana - skillData.manaCost);
        attackerStats.CurrentStamina = Mathf.Max(0, attackerStats.CurrentStamina - skillData.staminaCost);
        attackerStats.CurrentEnergy = Mathf.Max(0, attackerStats.CurrentEnergy - skillData.energyCost);
        Debug.Log($"✨ {attackerStats.Name} used {skillData.skillName}! Spent: {skillData.hpCost} HP / {skillData.manaCost} MP.");

        // 🎬 HEALTH GUARD: If the HP cost drops the caster to 0, trigger their Down animation state instantly!
        if (attackerStats.CurrentHP <= 0)
        {
            Animator casterAnimator = attackerObj.GetComponentInChildren<Animator>();
            if (casterAnimator != null)
            {
                casterAnimator.SetBool("BattleModeDown", true);
                Debug.Log($"💀 {attackerStats.Name} spent their remaining life force to cast! Transitioning to Down animation state.");
            }
        }

        // 2. TARGET MATRICES GENERATION POOLING
        List<GameObject> targetedUnitsPool = new List<GameObject>();

        if (skillData.skillScope == SkillScriptable.SkillScopeType.AllCharacters)
        {
            // If the skill hits everyone, add your entire active player party team list to the pool
            targetedUnitsPool.AddRange(activePlayerUnits);
        }
        else if (skillData.skillScope == SkillScriptable.SkillScopeType.SelfOnly)
        {
            targetedUnitsPool.Add(attackerObj);
        }
        else
        {
            // Default baseline fallback single-target assignment slot
            if (primaryDefenderObj != null) targetedUnitsPool.Add(primaryDefenderObj);
        }

        // 3. SKILL PATH ROUTING CALCULATIONS LOOP
        foreach (GameObject targetedObj in targetedUnitsPool)
        {
            if (targetedObj == null) continue;

            if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Support)
            {
                // ROUTE A: Apply restorative resource calculations to characters
                ExecuteSupportSkillMath(skillData, targetedObj);
            }
            else if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Offensive)
            {
                // ROUTE B: Apply elemental magical or physical attack reductions to enemies
                ExecuteOffensiveSkillMath(attackerStats, skillData, targetedObj);
            }
        }
    }



    // 🌟 REVERTED TO BE SILENT: Calculations only, no automated/delayed popups here anymore!
    private void ExecuteSupportSkillMath(SkillScriptable skill, GameObject targetPlayerObj)
    {
        CharacterStats targetStats = targetPlayerObj.GetComponent<CharacterStats>();
        if (targetStats == null) return;

        bool wasDeadBeforeHeal = targetStats.CurrentHP <= 0;

        targetStats.CurrentHP = Mathf.Min(targetStats.MaxHP, targetStats.CurrentHP + skill.healHP);
        targetStats.CurrentMana = Mathf.Min(targetStats.MaxMana, targetStats.CurrentMana + skill.restoreMana);
        targetStats.CurrentStamina = Mathf.Min(targetStats.MaxStamina, targetStats.CurrentStamina + skill.restoreStamina);
        targetStats.CurrentEnergy = Mathf.Min(targetStats.MaxEnergy, targetStats.CurrentEnergy + skill.restoreEnergy);

        if (wasDeadBeforeHeal && targetStats.CurrentHP > 0)
        {
            Animator playerAnimator = targetPlayerObj.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.SetBool("BattleModeDown", false);
                Debug.Log($"💖 {targetStats.Name} was revived!");
            }
        }
    }

    // 🌟 REVERTED TO BE SILENT: Calculations only, no automated/delayed popups here anymore!
    private void ExecuteOffensiveSkillMath(CharacterStats attackerStats, SkillScriptable skill, GameObject targetEnemyObj)
    {
        Enemy targetEnemy = targetEnemyObj.GetComponent<Enemy>();
        if (targetEnemy == null) return;

        CharacterStats enemyStats = targetEnemyObj.GetComponent<CharacterStats>();
        CharacterStats.ElementType enemyStrong = CharacterStats.ElementType.None;
        CharacterStats.ElementType enemyWeak = CharacterStats.ElementType.None;

        if (enemyStats != null)
        {
            enemyStrong = enemyStats.strongWith;
            enemyWeak = enemyStats.weakWith;
        }

        int finalMagicDamage = BattleRulesElements.CalculateMagicalDamage(
            attackerStats.MagicAttack + skill.skillDamage,
            (int)targetEnemy.baseStats.enemyDefense,
            skill.skillElement,
            enemyStrong,
            enemyWeak
        );

        targetEnemy.TakeDamage(finalMagicDamage);

        if (targetEnemyObj == null || targetEnemy == null)
        {
            if (instantiatedEnemyUnits.Contains(targetEnemyObj)) instantiatedEnemyUnits.Remove(targetEnemyObj);
        }
        else
        {
            EnemyAnimationsTrigger enemyAnimTrigger = targetEnemyObj.GetComponent<EnemyAnimationsTrigger>();
            if (enemyAnimTrigger != null && enemyAnimTrigger.hurtClip != null && enemyAnimTrigger.hurtClip.clip != null)
            {
                enemyAnimTrigger.TriggerHurtAnimation();
            }
        }
    }

    /// <summary>
    /// 🌟 NEW: Triggered explicitly via Animation Event 'OnPopUpTextTrigger' to drop numbers frame-accurately!
    /// </summary>
    public void ExecuteAnimationTextPopUpEvent()
    {
    
    }


    // 🌟 HELPER COROUTINE TO DELAY TEXT SPLASH
    private IEnumerator DelayedSpawnPopUpText(string content, bool isCrit, bool isMiss, Vector3 worldPos, float delayTime)
    {
        yield return new WaitForSeconds(delayTime);
        SpawnPopUpText(content, isCrit, isMiss, worldPos);
    }



    public void ForceTestVictory() => StartCoroutine(HandleVictorySequence());

    private IEnumerator HandleVictorySequence()
    {
        foreach (var playerObj in activePlayerUnits)
        {
            if (playerObj != null)
            {
                Animator playerAnimator = playerObj.GetComponentInChildren<Animator>();
                if (playerAnimator != null) playerAnimator.SetTrigger("Victory");
            }
        }

        // 🛠️ FIX: References index [0] to point precisely to the leader's transform coordinates instead of reading a generic List layout
        if (mainBattleCamera != null && activePlayerUnits.Count > 0 && activePlayerUnits[0] != null)
        {
            Vector3 victoryCamPos = activePlayerUnits[0].transform.position + activePlayerUnits[0].transform.forward * 3.5f + Vector3.up * 1.5f;
            mainBattleCamera.transform.position = victoryCamPos;
            mainBattleCamera.transform.LookAt(activePlayerUnits[0].transform.position + Vector3.up * 1f);
        }
        yield return new WaitForSeconds(3.0f);
        ResetOverworldState();
    }
    private void ResetOverworldState()
    {
        BattleMode = false; battleActive = false; isPlayerTurn = false; isCharacterConfirmedSelected = false; battleTriggeringUnit = null;
        isPlayerPhaseActive = false;

        foreach (var enemy in instantiatedEnemyUnits) { if (enemy != null) Destroy(enemy); }
        instantiatedEnemyUnits.Clear();
        combatTurnOrderQueue.Clear();
        spentPlayerUnitsThisRound.Clear();

        if (commandUIPanel != null) commandUIPanel.SetActive(false);
        if (AMC != null) AMC.masterActionControl = "Overworld";

        foreach (var obj in objectsToEnable) { if (obj != null) obj.SetActive(false); }
        foreach (var obj in objectsToDisable) { if (obj != null) obj.SetActive(true); }

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            Camera[] prefabCameras = player.GetComponentsInChildren<Camera>(true);
            foreach (Camera cam in prefabCameras) { if (cam != mainBattleCamera) cam.enabled = true; }

            MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null && (script.GetType().Name == "NPCFollowPlayer" || script.GetType().Name == "ObjectAccurateFollower" || script.GetType().Name == "PlayerIKLook" || script.GetType().Name == "PlayerMovement3D"))
                    script.enabled = true;
            }

            MonoBehaviour[] childScripts = player.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour childScript in childScripts)
            {
                if (childScript != null && (childScript.GetType().Name == "CinemachineBrain" || childScript.GetType().Name == "CinemachineVirtualCamera"))
                    childScript.enabled = true;
            }

            AudioListener listener = player.GetComponentInChildren<AudioListener>();
            if (listener != null) listener.enabled = true;

            Animator playerAnimator = player.GetComponentInChildren<Animator>();
            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
                playerAnimator.SetBool("BattleMode", false);

                // 🛠️ CLEAN UP ON EXIT: Force release the down flag when transitioning back to exploration
                playerAnimator.SetBool("BattleModeDown", false);
            }

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = true;
        }
    }




    /// <summary>
    /// Loops through single or grouped active target inputs to calculate individual reductions and drop individual floating text popups.
    /// </summary>
    /// <summary>
    /// Loops through single or grouped active target inputs to calculate individual reductions and drop individual floating text popups.
    /// </summary>
    /// <summary>
    /// Loops through grouped target inputs to calculate individual damage math and clone floating text structures.
    /// </summary>
    public void ProcessDamageExecution(GameObject attackerObj, GameObject defenderObj)
    {
        if (attackerObj == null) return;

        // 🛠️ REDIRECT OUT TO INDEPENDENT COMPONENT SYSTEM
        CharacterStats attackerStatsComp = attackerObj.GetComponent<CharacterStats>();

        if (attackerStatsComp == null)
        {
            Debug.LogError($"❌ {attackerObj.name} is missing a CharacterStats component container script!");
            return;
        }

        CommandOptionTriggers UIFields = FindFirstObjectByType<CommandOptionTriggers>();

        // CLONING PIPELINE MATRIX: If a multi-target action is active, loop through every single element individually
        if (UIFields != null && UIFields.multiTargetPlaceholders != null && UIFields.multiTargetPlaceholders.Count > 0)
        {
            Debug.Log("[BattleFieldMode] Processing cloned text matrices for " + UIFields.multiTargetPlaceholders.Count + " total targets.");

            for (int i = 0; i < UIFields.multiTargetPlaceholders.Count; i++)
            {
                GameObject targetUnit = UIFields.multiTargetPlaceholders[i];
                if (targetUnit == null) continue;

                // Pass the component straight forward down into the calculation loop
                ExecuteSingleUnitDamageMathFromComponent(attackerStatsComp, targetUnit);
            }
        }
        else
        {
            // Standard single-target baseline execution path fallback
            if (defenderObj != null)
            {
                ExecuteSingleUnitDamageMathFromComponent(attackerStatsComp, defenderObj);
            }
        }
    }

    /// <summary>
    /// Processes base mitigation formulas using data directly from the CharacterStats component.
    /// </summary>
    private void ExecuteSingleUnitDamageMathFromComponent(CharacterStats attackerStats, GameObject targetObj)
    {
        Enemy targetEnemy = targetObj.GetComponent<Enemy>();
        if (targetEnemy == null) return;

        // =========================================================================
        // 🛠️ SMART HEIGHT LOOKUP PIPELINE (Fixes Enemy Feet Floating Text Bug)
        // =========================================================================
        Vector3 spawnPosition = targetObj.transform.position;
        Collider col = targetObj.GetComponent<Collider>();
        if (col == null) col = targetObj.GetComponentInChildren<Collider>(); // Scan child bones safely

        // Check if the collider exists AND has an actual height size
        if (col != null && col.bounds.size.y > 0.1f)
        {
            spawnPosition += Vector3.up * (col.bounds.size.y + 0.2f);
        }
        else
        {
            // 🛠️ FIX: Forcefully lifts the popup to exactly your preferred 2.22+ value!
            spawnPosition += Vector3.up * 2.22f;
        }
        // =========================================================================

        // 1. Evaluate hit or miss status per enemy unit node
        bool hasHit = BattleRulesStats.CheckIfAttackHits(
            attackerStats.Accuracy,
            targetEnemy.baseStats.enemyAgility,
            targetEnemy.baseStats.enemyEvasion
        );

        if (!hasHit)
        {
            Debug.Log("💨 Attack missed target: " + targetEnemy.currentName);
            EnemyAnimationsTrigger enemyAnim = targetObj.GetComponent<EnemyAnimationsTrigger>();
            if (enemyAnim != null && enemyAnim.evadeClip != null && enemyAnim.evadeClip.clip != null)
            {
                enemyAnim.TriggerEvadeAnimation();
            }
            SpawnPopUpText("MISS", false, true, spawnPosition);
            return;
        }

        // 2. Evaluate critical rolls
        bool isCrit = BattleRulesStats.CheckIfCriticalHit(attackerStats.Critical);

        // 3. Compute structural damage output offset by this enemy's defense stat
        int finalDamageOutput = BattleRulesStats.CalculatePhysicalDamage(
            attackerStats.PhysicalAttack,
            (int)targetEnemy.baseStats.enemyDefense,
            isCrit
        );

        // 4. Subtract health points from the target
        targetEnemy.TakeDamage(finalDamageOutput);

        if (targetObj == null || targetEnemy == null)
        {
            Debug.Log("💀 Enemy has been defeated and removed from the battlefield registry.");
            if (instantiatedEnemyUnits.Contains(targetObj))
            {
                instantiatedEnemyUnits.Remove(targetObj);
            }
        }
        else
        {
            EnemyAnimationsTrigger enemyAnimTrigger = targetObj.GetComponent<EnemyAnimationsTrigger>();
            if (enemyAnimTrigger != null && enemyAnimTrigger.hurtClip != null && enemyAnimTrigger.hurtClip.clip != null)
            {
                enemyAnimTrigger.TriggerHurtAnimation();
            }
        }

        string textDisplay = isCrit ? "CRIT!\n" + finalDamageOutput : finalDamageOutput.ToString();
        SpawnPopUpText(textDisplay, isCrit, false, spawnPosition);
    }


    public void ProcessEnemyDamageExecution(GameObject enemyAttackerObj, GameObject playerDefenderObj)
    {
        if (enemyAttackerObj == null || playerDefenderObj == null) return;

        Enemy attackerEnemy = enemyAttackerObj.GetComponent<Enemy>();
        CharacterStats defenderStatsComp = playerDefenderObj.GetComponent<CharacterStats>();

        if (attackerEnemy == null || defenderStatsComp == null) return;

        // =========================================================================
        // 🛠️ SMART HEIGHT LOOKUP PIPELINE (Fixes Character 1 Feet Floating Text Bug)
        // =========================================================================
        Vector3 spawnPosition = playerDefenderObj.transform.position;
        Collider col = playerDefenderObj.GetComponent<Collider>();
        if (col == null) col = playerDefenderObj.GetComponentInChildren<Collider>(); // Scan child bones safely

        // Check if the collider exists AND has an actual height size
        if (col != null && col.bounds.size.y > 0.1f)
        {
            spawnPosition += Vector3.up * (col.bounds.size.y + 0.2f);
        }
        else
        {
            // 🛠️ FIX: Forcefully lifts the popup to exactly your preferred 2.22+ value!
            spawnPosition += Vector3.up * 2.22f;
        }
        // =========================================================================

        bool hasHit = BattleRulesStats.CheckIfAttackHits(
            attackerEnemy.baseStats.enemyAccuracy,
            defenderStatsComp.Agility,
            defenderStatsComp.Dodge
        );

        if (!hasHit)
        {
            Debug.Log($"💨 {attackerEnemy.currentName}'s attack missed {defenderStatsComp.Name}!");
            TurnbaseAnimationsTrigger playerAnim = playerDefenderObj.GetComponent<TurnbaseAnimationsTrigger>();
            if (playerAnim != null && playerAnim.evadeClip != null && playerAnim.evadeClip.clip != null)
            {
                playerAnim.TriggerEvadeAnimation();
            }
            SpawnPopUpText("MISS", false, true, spawnPosition);
            return;
        }

        bool isCrit = BattleRulesStats.CheckIfCriticalHit(attackerEnemy.baseStats.enemyCritical);

        int finalDamageOutput = BattleRulesStats.CalculatePhysicalDamage(
            (int)attackerEnemy.baseStats.enemyAttackDamage,
            defenderStatsComp.PhysicalDefense,
            isCrit
        );

        // MODIFIES INDEPENDENT MONOBEHAVIOUR FIELDS DIRECTLY
        defenderStatsComp.CurrentHP = Mathf.Max(0, defenderStatsComp.CurrentHP - finalDamageOutput);
        Debug.Log($"🔴 {defenderStatsComp.Name} took {finalDamageOutput} damage! Remaining HP: {defenderStatsComp.CurrentHP}");

        // 🎬 NATIVE ANIMATOR PARAMETER PIPELINE
        Animator playerAnimatorComponent = playerDefenderObj.GetComponentInChildren<Animator>();
        TurnbaseAnimationsTrigger playerAnimTrigger = playerDefenderObj.GetComponent<TurnbaseAnimationsTrigger>();

        if (playerAnimatorComponent != null)
        {
            if (defenderStatsComp.CurrentHP <= 0)
            {
                playerAnimatorComponent.SetBool("BattleModeDown", true);
                Debug.Log($"💀 {defenderStatsComp.Name} knocked out. Animator parameter 'BattleModeDown' set to true.");
            }
            else
            {
                playerAnimatorComponent.SetBool("BattleModeDown", false);

                if (playerAnimTrigger != null && playerAnimTrigger.hurtClip != null && playerAnimTrigger.hurtClip.clip != null)
                {
                    playerAnimTrigger.TriggerHurtAnimation();
                }
            }
        }

        string textDisplay = isCrit ? $"CRIT!\n{finalDamageOutput}" : finalDamageOutput.ToString();
        SpawnPopUpText(textDisplay, isCrit, false, spawnPosition);
    }




    /// <summary>
    /// Safely plays the hit flinch animation, holds for its duration, then returns the character back to active idle.
    /// </summary>
    private IEnumerator ExecuteHurtToIdleTransition(TurnbaseAnimationsTrigger animTrigger)
    {
        if (animTrigger == null) yield break;

        // 1. Play the flinch/hurt asset clip instantly on impact
        animTrigger.TriggerHurtAnimation();

        // 2. Fetch the length of the raw hurt clip file asset dynamically (default fallback to 0.4s if unreadable)
        float waitDuration = 0.4f;
        if (animTrigger.hurtClip != null && animTrigger.hurtClip.clip != null)
        {
            waitDuration = animTrigger.hurtClip.clip.length;
        }

        // Wait it out cleanly so it doesn't clip frames mid-swing
        yield return new WaitForSeconds(waitDuration);

        // 3. Re-verify health status and return them to the default standing stance
       
    }

    /// <summary>
    /// Core combined execution pipeline. Spawns visual particles first, delays for visual peak, 
    /// then applies math formulas, flinch nodes, and numeric text popups sequentially.
    /// </summary>
    public void ExecuteAnimationHitImpactEvent()
    {
        Debug.Log("💥 Combined Impact VFX & Damage Execution Triggered.");

        if (activeCastedSkillContext == null && skillsNavPanel != null && skillsNavPanel.gameObject.activeInHierarchy)
        {
            System.Reflection.FieldInfo skillsField = typeof(SkillsNavigationalPanel).GetField("runtimeEquippedSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (skillsField != null)
            {
                List<SkillScriptable> skillsList = skillsField.GetValue(skillsNavPanel) as List<SkillScriptable>;
                System.Reflection.FieldInfo indexField = typeof(SkillsNavigationalPanel).GetField("currentSelectedIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (skillsList != null && indexField != null)
                {
                    int index = (int)indexField.GetValue(skillsNavPanel);
                    if (index >= 0 && index < skillsList.Count)
                    {
                        activeCastedSkillContext = skillsList[index];
                    }
                }
            }
        }

        if (activeCastedSkillContext == null) return;

        // Read targets straight from the safe bulletproof cache buffer
        List<GameObject> dynamicTargetsList = new List<GameObject>(animationTargetsCache);

        // 🎬 1. VISUAL EFFECTS SPARK PHASE
        if (activeCastedSkillContext.skillEffectPrefab != null && dynamicTargetsList.Count > 0)
        {
            foreach (GameObject targetObj in dynamicTargetsList)
            {
                if (targetObj == null) continue;

                Vector3 groundSpawnPosition = targetObj.transform.position;
                groundSpawnPosition.y += 0.05f;

                GameObject instantiatedEffect = Instantiate(activeCastedSkillContext.skillEffectPrefab, groundSpawnPosition, Quaternion.identity);
                Destroy(instantiatedEffect, 3.5f);
            }
        }

        // 🎬 2. SEQUENTIAL COROUTINE HANDSHAKE
        // We pass the collected data downwards into a delayed process thread.
        // This ensures the math formulas, hurt animations, and float text pop up AFTER the VFX detonate!
        StartCoroutine(ExecuteDelayedDamageAndPopUpTextPipeline(dynamicTargetsList, activeCastedSkillContext));
    }

    /// <summary>
    /// Delays math execution slightly so numbers and death triggers hit right on top of the visual effects.
    /// </summary>
    private IEnumerator ExecuteDelayedDamageAndPopUpTextPipeline(List<GameObject> targets, SkillScriptable skillData)
    {
        // ⏳ CHANGE THIS DELAY WINDOW: How long (in seconds) the particles play before numbers show and enemies get hurt.
        // 0.25 seconds is perfect for fast impact elements. Increase to 0.5f if you want a longer wind-up.
        float customImpactBeatDelay = 0.25f;

        // If the skill has no VFX prefab, skip the delay entirely so text pops instantly when clicked!
        if (skillData.skillEffectPrefab == null) customImpactBeatDelay = 0f;

        yield return new WaitForSeconds(customImpactBeatDelay);

        if (currentTurnUnit == null || skillData == null) yield break;

        CharacterStats attackerStats = currentTurnUnit.GetComponent<CharacterStats>();
        if (attackerStats == null) yield break;

        // Deduct resource costs from live casting variables safely
        attackerStats.CurrentHP = Mathf.Max(0, attackerStats.CurrentHP - skillData.hpCost);
        attackerStats.CurrentMana = Mathf.Max(0, attackerStats.CurrentMana - skillData.manaCost);
        attackerStats.CurrentStamina = Mathf.Max(0, attackerStats.CurrentStamina - skillData.staminaCost);
        attackerStats.CurrentEnergy = Mathf.Max(0, attackerStats.CurrentEnergy - skillData.energyCost);

        if (attackerStats.CurrentHP <= 0)
        {
            Animator casterAnimator = currentTurnUnit.GetComponentInChildren<Animator>();
            if (casterAnimator != null) casterAnimator.SetBool("BattleModeDown", true);
        }

        // Process actual damage deductions and floating numbers simultaneously 
        foreach (GameObject targetObj in targets)
        {
            if (targetObj == null) continue;

            // Height lookup formulas for numbers anchoring
            Vector3 textSpawnPos = targetObj.transform.position;
            Collider col = targetObj.GetComponent<Collider>();
            if (col == null) col = targetObj.GetComponentInChildren<Collider>();
            if (col != null && col.bounds.size.y > 0.1f)
                textSpawnPos += Vector3.up * (col.bounds.size.y + 0.2f);
            else
                textSpawnPos += Vector3.up * 2.22f;

            // --- PROCESS SUPPORT MATH & TEXT ---
            if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Support)
            {
                CharacterStats targetStats = targetObj.GetComponent<CharacterStats>();
                if (targetStats != null)
                {
                    bool wasDeadBeforeHeal = targetStats.CurrentHP <= 0;

                    targetStats.CurrentHP = Mathf.Min(targetStats.MaxHP, targetStats.CurrentHP + skillData.healHP);
                    targetStats.CurrentMana = Mathf.Min(targetStats.MaxMana, targetStats.CurrentMana + skillData.restoreMana);
                    targetStats.CurrentStamina = Mathf.Min(targetStats.MaxStamina, targetStats.CurrentStamina + skillData.restoreStamina);
                    targetStats.CurrentEnergy = Mathf.Min(targetStats.MaxEnergy, targetStats.CurrentEnergy + skillData.restoreEnergy);

                    if (wasDeadBeforeHeal && targetStats.CurrentHP > 0)
                    {
                        Animator targetAnim = targetObj.GetComponentInChildren<Animator>();
                        if (targetAnim != null) targetAnim.SetBool("BattleModeDown", false);
                    }

                    System.Text.StringBuilder supportText = new System.Text.StringBuilder();
                    if (skillData.healHP > 0) supportText.AppendLine($"<color=#33DA4D>+{skillData.healHP} HP</color>");
                    if (skillData.restoreMana > 0) supportText.AppendLine($"<color=#00EAFF>+{skillData.restoreMana} Mana</color>");
                    if (skillData.restoreStamina > 0) supportText.AppendLine($"<color=#FFB700>+{skillData.restoreStamina} Stamina</color>");
                    if (skillData.restoreEnergy > 0) supportText.AppendLine($"<color=#E933FF>+{skillData.restoreEnergy} Energy</color>");

                    string finalCombinedText = supportText.ToString().TrimEnd();
                    if (!string.IsNullOrEmpty(finalCombinedText))
                    {
                        SpawnPopUpText(finalCombinedText, false, false, textSpawnPos);
                    }
                }
            }
            // --- PROCESS OFFENSIVE MATH & TEXT & HURT ANIMS ---
            else if (skillData.skillAbility == SkillScriptable.SkillAbilityType.Offensive)
            {
                Enemy targetEnemy = targetObj.GetComponent<Enemy>();
                if (targetEnemy != null)
                {
                    CharacterStats enemyStats = targetObj.GetComponent<CharacterStats>();
                    CharacterStats.ElementType enemyStrong = enemyStats != null ? enemyStats.strongWith : CharacterStats.ElementType.None;
                    CharacterStats.ElementType enemyWeak = enemyStats != null ? enemyStats.weakWith : CharacterStats.ElementType.None;

                    int finalMagicDamage = BattleRulesElements.CalculateMagicalDamage(
                        attackerStats.MagicAttack + skillData.skillDamage,
                        (int)targetEnemy.baseStats.enemyDefense,
                        skillData.skillElement,
                        enemyStrong,
                        enemyWeak
                    );

                    // Subtract health points AFTER the delay, syncing death checks perfectly with the explosion!
                    targetEnemy.TakeDamage(finalMagicDamage);

                    if (targetObj == null || targetEnemy == null)
                    {
                        if (instantiatedEnemyUnits.Contains(targetObj)) instantiatedEnemyUnits.Remove(targetObj);
                    }
                    else
                    {
                        EnemyAnimationsTrigger enemyAnimTrigger = targetObj.GetComponent<EnemyAnimationsTrigger>();
                        if (enemyAnimTrigger != null && enemyAnimTrigger.hurtClip != null && enemyAnimTrigger.hurtClip.clip != null)
                        {
                            enemyAnimTrigger.TriggerHurtAnimation();
                        }
                    }

                    SpawnPopUpText(finalMagicDamage.ToString(), false, false, textSpawnPos);
                }
            }
        }
    }







    private void SpawnPopUpText(string content, bool isCrit, bool isMiss, Vector3 worldPos)
    {
        if (damageTextPrefab == null)
        {
            Debug.LogWarning("[BattleFieldMode] DamageTextPrefab field tracking is unassigned inside the inspector slots.");
            return;
        }

        GameObject spawnedTextObj = Instantiate(damageTextPrefab, worldPos, Quaternion.identity);

        spawnedTextObj.transform.position = worldPos;

        FloatingDamageText floatingScript = spawnedTextObj.GetComponent<FloatingDamageText>();
        if (floatingScript != null)
        {
            floatingScript.InitializeDisplay(content, isCrit, isMiss);
        }
    }


    public GameObject GetCurrentTurnUnit() => currentTurnUnit;

    public List<GameObject> GetActivePlayerUnitsList()
    {
        activePlayerUnits.RemoveAll(item => item == null);
        return activePlayerUnits;
    }
}

