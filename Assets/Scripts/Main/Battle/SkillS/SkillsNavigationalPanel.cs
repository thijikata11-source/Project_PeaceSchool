using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillsNavigationalPanel : MonoBehaviour
{
    [System.Serializable]
    public class SkillUIOption
    {
        public GameObject containerObject;
        public Image skillIconImage;
        public TMP_Text skillNameText;
    }

    [Header("System Dependencies")]
    public BattleFieldMode battlefieldMode;
    public CommandOptionTriggers commandTriggers;

    [Header("UI Grid Navigation Structure (Exactly 8 Slots)")]
    [Tooltip("Provide exactly 8 UI display groups corresponding to your skillset ceiling limit.")]
    public List<SkillUIOption> skillSlotsUI = new List<SkillUIOption>(8);

    [Header("Visual Highlighting Setup")]
    public Color highlightedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Locked State Overlay Colors")]
    public Color insufficientResourceColor = new Color(0.25f, 0.25f, 0.25f, 0.6f); // Clean dark grey/black look

    private List<SkillScriptable> runtimeEquippedSkills = new List<SkillScriptable>();
    private List<bool> skillCastableStatusMap = new List<bool>(); // Tracks resource state validation
    private int currentSelectedIndex = 0;

    private void Awake()
    {
        if (battlefieldMode == null) battlefieldMode = FindFirstObjectByType<BattleFieldMode>();
        if (commandTriggers == null) commandTriggers = FindFirstObjectByType<CommandOptionTriggers>();
    }

    private void OnEnable()
    {
        PopulateActiveSkills();
        currentSelectedIndex = 0;
        UpdateVisualHighlighting();
    }

    /// <summary>
    /// Sources equipped ability files directly out from whichever character node is acting.
    /// </summary>
    /// <summary>
    /// Sources equipped ability files directly out from whichever character node is acting.
    /// </summary>
    public void PopulateActiveSkills()
    {
        runtimeEquippedSkills.Clear();
        skillCastableStatusMap.Clear();

        // INITIAL CLEANUP: Hide ALL 8 UI elements by default first
        foreach (var slot in skillSlotsUI)
        {
            if (slot.containerObject != null) slot.containerObject.SetActive(false);
        }

        if (battlefieldMode == null || battlefieldMode.GetCurrentTurnUnit() == null) return;

        GameObject activeCaster = battlefieldMode.GetCurrentTurnUnit();
        CharacterStats casterStats = activeCaster.GetComponent<CharacterStats>();
        TurnbaseAnimationsTrigger activeAnimTrigger = activeCaster.GetComponent<TurnbaseAnimationsTrigger>();

        // 🎨 EXTRACT COLOR PREFERENCES FROM FLOATING TEXT PREFAB DYNAMICALLY
        FloatingDamageText txtScript = null;
        if (battlefieldMode != null && battlefieldMode.damageTextPrefab != null)
        {
            txtScript = battlefieldMode.damageTextPrefab.GetComponent<FloatingDamageText>();
        }

        // Convert customizable inspector palette vectors straight to rich HTML hex rows
        string hpHex = txtScript != null ? ColorUtility.ToHtmlStringRGBA(txtScript.hpCostColor) : "DA1212";
        string manaHex = txtScript != null ? ColorUtility.ToHtmlStringRGBA(txtScript.manaRestoreColor) : "00EAFF";
        string staminaHex = txtScript != null ? ColorUtility.ToHtmlStringRGBA(txtScript.staminaRestoreColor) : "E933FF";
        string energyHex = txtScript != null ? ColorUtility.ToHtmlStringRGBA(txtScript.energyRestoreColor) : "FF3333";

        if (activeAnimTrigger != null && activeAnimTrigger.characterSkillSet != null)
        {
            for (int i = 0; i < activeAnimTrigger.characterSkillSet.Count; i++)
            {
                SkillScriptable skill = activeAnimTrigger.characterSkillSet[i];
                if (skill != null && i < skillSlotsUI.Count)
                {
                    runtimeEquippedSkills.Add(skill);

                    // VALIDATION MATRIX LAYER CHECK: Verify all pool boundaries natively
                    bool isCastable = true;
                    if (casterStats != null)
                    {
                        if (casterStats.CurrentHP <= skill.hpCost ||
                            casterStats.CurrentMana < skill.manaCost ||
                            casterStats.CurrentStamina < skill.staminaCost ||
                            casterStats.CurrentEnergy < skill.energyCost)
                        {
                            isCastable = false;
                        }
                    }
                    skillCastableStatusMap.Add(isCastable);

                    SkillUIOption ui = skillSlotsUI[i];
                    if (ui.containerObject != null) ui.containerObject.SetActive(true);

                    // =========================================================================
                    // 📊 DYNAMIC ADAPTIVE COST TEXT GENERATOR SYSTEM (Omits Zeros)
                    // =========================================================================
                    if (ui.skillNameText != null)
                    {
                        string nameLine = skill.skillName;
                        string costLine = "";

                        // Create a temporary list to hold only the strings of resources that cost > 0
                        List<string> activeCostsList = new List<string>();

                        if (skill.hpCost > 0) activeCostsList.Add($"<color=#{hpHex}>{skill.hpCost} HP</color>");
                        if (skill.manaCost > 0) activeCostsList.Add($"<color=#{manaHex}>{skill.manaCost} Mana</color>");
                        if (skill.staminaCost > 0) activeCostsList.Add($"<color=#{staminaHex}>{skill.staminaCost} Stamina</color>");
                        if (skill.energyCost > 0) activeCostsList.Add($"<color=#{energyHex}>{skill.energyCost} Energy</color>");

                        // Only build a cost sub-line if there is at least one resource required
                        if (activeCostsList.Count > 0)
                        {
                            // string.Join handles automatically placing the commas between elements flawlessly!
                            string combinedCosts = string.Join(", ", activeCostsList);
                            costLine = $"<size=70%>\n({combinedCosts} cost)</size>";
                        }

                        ui.skillNameText.text = nameLine + costLine;
                    }
                    // =========================================================================

                    if (ui.skillIconImage != null && skill.skillIcon != null)
                    {
                        ui.skillIconImage.sprite = skill.skillIcon;
                    }
                }
            }
        }

        // FALLBACK GATING: Close sub-menu completely if no skills are assigned
        if (runtimeEquippedSkills.Count == 0)
        {
            gameObject.SetActive(false);
        }
    }


    public void NavigateUp()
    {
        if (!gameObject.activeInHierarchy || runtimeEquippedSkills.Count == 0) return;
        if (battlefieldMode != null && battlefieldMode.currentBattleState != BattleState.SkillsMenu) return;

        currentSelectedIndex--;
        if (currentSelectedIndex < 0) currentSelectedIndex = runtimeEquippedSkills.Count - 1;

        UpdateVisualHighlighting();
    }

    public void NavigateDown()
    {
        if (!gameObject.activeInHierarchy || runtimeEquippedSkills.Count == 0) return;
        if (battlefieldMode != null && battlefieldMode.currentBattleState != BattleState.SkillsMenu) return;

        currentSelectedIndex++;
        if (currentSelectedIndex >= runtimeEquippedSkills.Count) currentSelectedIndex = 0;

        UpdateVisualHighlighting();
    }

    public void ExecuteSkillInteraction()
    {
        if (!gameObject.activeInHierarchy || runtimeEquippedSkills.Count == 0) return;

        // INTERACTION BLOCKADE LOCKOUT: Reject input if the active index status mapping evaluates false!
        if (currentSelectedIndex >= skillCastableStatusMap.Count || !skillCastableStatusMap[currentSelectedIndex])
        {
            Debug.LogWarning($"🔒 Cannot cast {runtimeEquippedSkills[currentSelectedIndex].skillName}! Insufficient resource pools capacity values.");
            return;
        }

        SkillScriptable chosenSkill = runtimeEquippedSkills[currentSelectedIndex];
        if (chosenSkill != null && commandTriggers != null)
        {
            Debug.Log($"[SkillsNavigationalPanel] Selected Ability: {chosenSkill.skillName}");
            gameObject.SetActive(false);
            commandTriggers.ProcessSkillScopeRouting(chosenSkill);
        }
    }

    private void UpdateVisualHighlighting()
    {
        for (int i = 0; i < runtimeEquippedSkills.Count; i++)
        {
            if (i >= skillSlotsUI.Count) continue;
            SkillUIOption ui = skillSlotsUI[i];
            if (ui.containerObject == null) continue;

            bool isSelected = (i == currentSelectedIndex);
            bool isCastable = (i < skillCastableStatusMap.Count) ? skillCastableStatusMap[i] : true;

            Color targetColor = normalColor;

            // ELEVATED RENDERING PRIORITY SYSTEM
            if (!isCastable)
            {
                targetColor = insufficientResourceColor;
            }
            else if (isSelected)
            {
                targetColor = highlightedColor;
            }

            if (ui.skillIconImage != null) ui.skillIconImage.color = targetColor;

            if (ui.skillNameText != null)
            {
                ui.skillNameText.enabled = isSelected || !isCastable;
                ui.skillNameText.color = targetColor;
            }
        }
    }
}
