using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class CommandOptionTriggers : MonoBehaviour
{
    [Header("System Dependencies")]
    public BattleFieldMode battlefieldMode;
    public EnemyTargetingSystem targetingSystem;
    public GameObject skillsUIPanel; // Drag your skills sub-menu panel object canvas here

    [Header("Targeting Storage Slots")]
    public GameObject currentTargetPlaceholder;
    public List<GameObject> multiTargetPlaceholders = new List<GameObject>();

    private SkillScriptable activeSelectedSkillContext;

    private void Awake()
    {
        if (battlefieldMode == null) battlefieldMode = FindFirstObjectByType<BattleFieldMode>();
        if (targetingSystem == null) targetingSystem = FindFirstObjectByType<EnemyTargetingSystem>();
    }

    public void ExecuteSkills()
    {
        Debug.Log("✨ Skills command menu opened.");
        if (battlefieldMode != null && skillsUIPanel != null)
        {
            if (battlefieldMode.commandUIPanel != null) battlefieldMode.commandUIPanel.SetActive(false);

            battlefieldMode.currentBattleState = BattleState.SkillsMenu;
            skillsUIPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[CommandOptionTriggers] Cannot open skills! 'battlefieldMode' or 'skillsUIPanel' fields are unassigned.");
        }
    }

    /// <summary>
    /// Phase 1: Set up target requirements, turn on multi-sigils for groups, and wait for confirmation inputs.
    /// </summary>
    public void ProcessSkillScopeRouting(SkillScriptable skill)
    {
        activeSelectedSkillContext = skill;
        multiTargetPlaceholders.Clear();
        currentTargetPlaceholder = null;

        GameObject activeCaster = battlefieldMode.GetCurrentTurnUnit();

        // 🎬 INSTANT CAMERA OVERRIDE: Face the character immediately if selecting a support skill!
        if (battlefieldMode != null && skill != null && skill.skillAbility == SkillScriptable.SkillAbilityType.Support)
        {
            Debug.Log($"🎬 Support skill selected. Forcing camera in front of character: {activeCaster.name}");
            battlefieldMode.PositionCameraInFrontOfUnit(activeCaster);
        }

        switch (skill.skillScope)
        {
            case SkillScriptable.SkillScopeType.SelfOnly:
                // SelfOnly requires no manual selection, but stays completely cancellable before confirmation!
                currentTargetPlaceholder = activeCaster;
                multiTargetPlaceholders.Add(activeCaster);

                if (MultiTargetIndicatorManager.Instance != null)
                    MultiTargetIndicatorManager.Instance.ShowIndicatorsOnTargets(multiTargetPlaceholders);

                // Set state context to targeting so pressing BACK cancels safely, and SELECT fires the skill!
                battlefieldMode.currentBattleState = BattleState.EnemyTargeting;
                break;

            case SkillScriptable.SkillScopeType.OneCharacter:
            case SkillScriptable.SkillScopeType.ThreeCharacters:
            case SkillScriptable.SkillScopeType.AllCharacters:
                // REDIRECT ALL TARGETING POOLS: Pass directly to our updated multi-selection engine!
                if (targetingSystem != null)
                {
                    targetingSystem.StartSkillTargetingSelection(skill);
                }
                break;
        }
    }


    public void CommitFinalSkillExecution()
    {
        if (activeSelectedSkillContext == null) return;

        // Retrieve the animation script attached to the active character taking their turn
        TurnbaseAnimationsTrigger activeCharacterAnim = GetCurrentTurnUnitAnimations();

        if (activeCharacterAnim != null)
        {
            // Plays the 'HealShield' animation state node inside your Animator Controller via PlayableGraph
            activeCharacterAnim.TriggerDynamicSkillExecution(activeSelectedSkillContext.skillAnimationName);
        }

        if (battlefieldMode != null)
        {
            // Passes targeting data forward to execute the HP healing calculations
            battlefieldMode.OnPlayerSkillCommand(currentTargetPlaceholder, activeSelectedSkillContext);
            activeSelectedSkillContext = null;
        }
    }



    /// <summary>
    /// Explicit direct cancel fallback hook invoked if a player backs out of a SelfOnly menu confirmation layer.
    /// </summary>
    public void CancelSelfOnlyTargeting()
    {
        activeSelectedSkillContext = null;
        multiTargetPlaceholders.Clear();
        currentTargetPlaceholder = null;
        if (MultiTargetIndicatorManager.Instance != null) MultiTargetIndicatorManager.Instance.ClearAllIndicators();

        if (battlefieldMode != null)
        {
            battlefieldMode.currentBattleState = BattleState.SkillsMenu;
            if (battlefieldMode.skillsNavPanel != null) battlefieldMode.skillsNavPanel.gameObject.SetActive(true);
        }
    }


    public void ExecuteAttack()
    {
        Debug.Log("⚔️ Attack command pressed. Entering target selection...");
        if (targetingSystem != null)
        {
            if (battlefieldMode != null && battlefieldMode.commandUIPanel != null) battlefieldMode.commandUIPanel.SetActive(false);
            targetingSystem.StartTargetingSelection();
        }
        else
        {
            CommitFinalAttackExecution();
        }
    }

    public void CommitFinalAttackExecution()
    {
        bool rangedConfig = false;
        TurnbaseAnimationsTrigger activeCharacterAnim = GetCurrentTurnUnitAnimations();
        if (activeCharacterAnim != null) rangedConfig = activeCharacterAnim.TriggerAttackAnimation();

        if (battlefieldMode != null)
        {
            battlefieldMode.OnPlayerAttackCommand(currentTargetPlaceholder, rangedConfig);
        }
    }

    public void ExecuteDefend()
    {
        Debug.Log("🛡️ Defend command triggered.");
        TurnbaseAnimationsTrigger activeCharacterAnim = GetCurrentTurnUnitAnimations();
        if (activeCharacterAnim != null) activeCharacterAnim.TriggerDefendAnimation();
        if (battlefieldMode != null) battlefieldMode.EndCurrentTurnCycle();
    }

    private TurnbaseAnimationsTrigger GetCurrentTurnUnitAnimations()
    {
        if (battlefieldMode == null) return null;
        FieldInfo turnUnitField = typeof(BattleFieldMode).GetField("currentTurnUnit", BindingFlags.NonPublic | BindingFlags.Instance);
        if (turnUnitField != null)
        {
            GameObject currentUnit = turnUnitField.GetValue(battlefieldMode) as GameObject;
            if (currentUnit != null) return currentUnit.GetComponent<TurnbaseAnimationsTrigger>();
        }
        return null;
    }
}
