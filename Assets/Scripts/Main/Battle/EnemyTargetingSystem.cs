using System.Collections.Generic;
using UnityEngine;

public class EnemyTargetingSystem : MonoBehaviour
{
    [Header("System Dependencies")]
    public BattleFieldMode battlefieldMode;
    public CommandOptionTriggers commandTriggers;

    private List<GameObject> aliveEnemies = new List<GameObject>();
    private int currentTargetIndex = 0;
    private bool isTargetingActive = false;

    // MULTI-SELECTION SYSTEM FIELDS
    private SkillScriptable activeSkillContext;
    private List<GameObject> manualSelectedTargetsBuffer = new List<GameObject>();
    private int targetsRequiredCount = 1;

    // NEW SELECTION LOCK FLAG: Blocks left/right browsing once the quota is filled
    private bool selectionLimitReached = false;

    private void Awake()
    {
        if (battlefieldMode == null) battlefieldMode = FindFirstObjectByType<BattleFieldMode>();
        if (commandTriggers == null) commandTriggers = FindFirstObjectByType<CommandOptionTriggers>();

        ExitTargetingMode();
    }

    /// <summary>
    /// Expanded setup wrapper that accepts a Skill context to process custom manual target counters.
    /// </summary>
    public void StartSkillTargetingSelection(SkillScriptable skill)
    {
        if (battlefieldMode == null || skill == null) return;

        activeSkillContext = skill;
        manualSelectedTargetsBuffer.Clear();
        selectionLimitReached = false;

        // Separate faction pools dynamically based on skill type attributes
        aliveEnemies = (skill.skillAbility == SkillScriptable.SkillAbilityType.Offensive) ?
            battlefieldMode.GetInstantiatedEnemyUnits() : battlefieldMode.GetActivePlayerUnitsList();

        aliveEnemies.RemoveAll(item => item == null);

        if (aliveEnemies.Count == 0)
        {
            ReturnToSkillsMenu();
            return;
        }

        isTargetingActive = true;
        currentTargetIndex = 0;

        if (skill.skillScope == SkillScriptable.SkillScopeType.ThreeCharacters)
        {
            targetsRequiredCount = Mathf.Min(3, aliveEnemies.Count);
            battlefieldMode.currentBattleState = BattleState.MultiTargetingSelection;
        }
        else if (skill.skillScope == SkillScriptable.SkillScopeType.AllCharacters)
        {
            targetsRequiredCount = aliveEnemies.Count;
            battlefieldMode.currentBattleState = BattleState.MultiTargetingSelection;

            if (MultiTargetIndicatorManager.Instance != null)
            {
                MultiTargetIndicatorManager.Instance.ShowIndicatorsOnTargets(aliveEnemies);
            }
            return;
        }
        else
        {
            targetsRequiredCount = 1;
            battlefieldMode.currentBattleState = BattleState.EnemyTargeting;
        }

        UpdateTargetFocus();
    }

    // Standard attack single target fallback entry point compatibility link
    public void StartTargetingSelection()
    {
        activeSkillContext = null;
        manualSelectedTargetsBuffer.Clear();
        targetsRequiredCount = 1;
        selectionLimitReached = false;

        if (battlefieldMode == null) return;
        aliveEnemies = battlefieldMode.GetInstantiatedEnemyUnits();
        aliveEnemies.RemoveAll(item => item == null);

        if (aliveEnemies.Count == 0) { ReturnToCommandMenu(); return; }

        isTargetingActive = true;
        currentTargetIndex = 0;
        battlefieldMode.currentBattleState = BattleState.EnemyTargeting;
        UpdateTargetFocus();
    }

    public void TargetNext()
    {
        if (!isTargetingActive || aliveEnemies.Count == 0 || selectionLimitReached) return;
        if (activeSkillContext != null && activeSkillContext.skillScope == SkillScriptable.SkillScopeType.AllCharacters) return;

        currentTargetIndex = (currentTargetIndex + 1) % aliveEnemies.Count;
        UpdateTargetFocus();
    }

    public void TargetPrevious()
    {
        if (!isTargetingActive || aliveEnemies.Count == 0 || selectionLimitReached) return;
        if (activeSkillContext != null && activeSkillContext.skillScope == SkillScriptable.SkillScopeType.AllCharacters) return;

        currentTargetIndex--;
        if (currentTargetIndex < 0) currentTargetIndex = aliveEnemies.Count - 1;
        UpdateTargetFocus();
    }

    public void ConfirmTargetSelection()
    {
        if (!isTargetingActive || aliveEnemies.Count == 0) return;

        GameObject focusedTarget = aliveEnemies[currentTargetIndex];

        if (activeSkillContext != null)
        {
            // --- CASE A: ALL friendly characters target scope ---
            if (activeSkillContext.skillScope == SkillScriptable.SkillScopeType.AllCharacters)
            {
                if (commandTriggers != null)
                {
                    commandTriggers.multiTargetPlaceholders = new List<GameObject>(aliveEnemies);
                    commandTriggers.currentTargetPlaceholder = focusedTarget;
                }
                ExitTargetingMode();
                if (commandTriggers != null) commandTriggers.CommitFinalSkillExecution();
                return;
            }

            // --- CASE B: THREE friendly characters target scope ---
            if (activeSkillContext.skillScope == SkillScriptable.SkillScopeType.ThreeCharacters)
            {
                // Sub-case B1: We are still collecting our 3 unique manual targets
                if (!selectionLimitReached)
                {
                    if (!manualSelectedTargetsBuffer.Contains(focusedTarget))
                    {
                        manualSelectedTargetsBuffer.Add(focusedTarget);
                        Debug.Log($"🎯 Target added ({manualSelectedTargetsBuffer.Count}/{targetsRequiredCount}): {focusedTarget.name}");
                    }

                    if (manualSelectedTargetsBuffer.Count >= targetsRequiredCount)
                    {
                        selectionLimitReached = true;
                        Debug.Log("🔒 Target selection quota reached! Press the INTERACT button again to confirm and cast the skill.");
                    }

                    UpdateTargetFocus();
                    return;
                }

                // Sub-case B2: Targets are full, and the player clicked INTERACT again to confirm!
                if (commandTriggers != null)
                {
                    commandTriggers.multiTargetPlaceholders = new List<GameObject>(manualSelectedTargetsBuffer);

                    if (manualSelectedTargetsBuffer.Count > 0)
                    {
                        commandTriggers.currentTargetPlaceholder = manualSelectedTargetsBuffer[0];
                    }
                }
                ExitTargetingMode();
                if (commandTriggers != null) commandTriggers.CommitFinalSkillExecution();
                return;
            }
        }

        // --- CASE C: Standard SINGLE target setup path (Weapon Attack OR Single Target Skill) ---
        if (commandTriggers != null)
        {
            commandTriggers.multiTargetPlaceholders.Clear();
            commandTriggers.currentTargetPlaceholder = focusedTarget;
        }

        ExitTargetingMode();

        // 🛠️ BUG FIX ROUTER: Check if a skill context is active instead of reading battleState, 
        // to prevent single-target friendly support skills from executing a weapon attack command by mistake!
        if (activeSkillContext != null)
        {
            if (commandTriggers != null) commandTriggers.CommitFinalSkillExecution();
        }
        else
        {
            if (commandTriggers != null) commandTriggers.CommitFinalAttackExecution();
        }
    }


    public void CancelTargetingSelection()
    {
        if (selectionLimitReached)
        {
            selectionLimitReached = false;
            if (manualSelectedTargetsBuffer.Count > 0)
            {
                manualSelectedTargetsBuffer.RemoveAt(manualSelectedTargetsBuffer.Count - 1);
            }
            Debug.Log("🔓 Selection unlocked. You can adjust your targets again.");
            UpdateTargetFocus();
            return;
        }

        ExitTargetingMode();

        if (activeSkillContext != null)
        {
            ReturnToSkillsMenu();
        }
        else
        {
            ReturnToCommandMenu();
        }
    }

    private void UpdateTargetFocus()
    {
        if (aliveEnemies.Count == 0 || currentTargetIndex >= aliveEnemies.Count || aliveEnemies[currentTargetIndex] == null) return;

        List<GameObject> indicatorsToShow = new List<GameObject>();
        GameObject hoverPreviewTarget = aliveEnemies[currentTargetIndex];

        if (activeSkillContext != null && activeSkillContext.skillScope == SkillScriptable.SkillScopeType.ThreeCharacters)
        {
            indicatorsToShow.AddRange(manualSelectedTargetsBuffer);

            if (!selectionLimitReached && !indicatorsToShow.Contains(hoverPreviewTarget))
            {
                indicatorsToShow.Add(hoverPreviewTarget);
            }
        }
        else
        {
            indicatorsToShow.Add(hoverPreviewTarget);
        }

        if (MultiTargetIndicatorManager.Instance != null)
        {
            MultiTargetIndicatorManager.Instance.ShowIndicatorsOnTargets(indicatorsToShow);
        }

        if (battlefieldMode != null && battlefieldMode.mainBattleCamera != null)
        {
            battlefieldMode.mainBattleCamera.transform.LookAt(hoverPreviewTarget.transform.position + Vector3.up * 1f);
        }
    }

    private void ExitTargetingMode()
    {
        isTargetingActive = false;
        selectionLimitReached = false;
        manualSelectedTargetsBuffer.Clear();
        if (MultiTargetIndicatorManager.Instance != null) MultiTargetIndicatorManager.Instance.ClearAllIndicators();
    }

    private void ReturnToCommandMenu()
    {
        if (battlefieldMode != null)
        {
            battlefieldMode.currentBattleState = BattleState.CommandMenu;

            // 🛠️ REFERENCE FIX: Route through battlefieldMode dependency safely
            if (battlefieldMode.commandUIPanel != null)
            {
                battlefieldMode.commandUIPanel.SetActive(true);
            }
        }
    }
    private void ReturnToSkillsMenu()
    {
        if (battlefieldMode != null)
        {
            battlefieldMode.currentBattleState = BattleState.SkillsMenu;
            if (battlefieldMode.skillsNavPanel != null) battlefieldMode.skillsNavPanel.gameObject.SetActive(true);
        }
    }
    private void OnDisable()
    {
        ExitTargetingMode();
    }
}