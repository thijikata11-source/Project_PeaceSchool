using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class CommandNavigationalPanel : MonoBehaviour
{
    [System.Serializable]
    public class CommandOption
    {
        public string optionName;
        [Tooltip("Check this box to include this option in the menu loop, uncheck to hide it entirely.")]
        public bool isVisible = true;
        public Image uiImage;
        public TMP_Text textMeshProText;
        public UnityEvent onInteractEvent;
    }

    // 🛠️ FIX: Added the missing system dependency variables
    [Header("System Dependencies")]
    [Tooltip("Leave unassigned to automatically discover the BattleFieldMode script in the scene.")]
    public BattleFieldMode battlefieldMode;

    [Header("Navigation Controls")]
    [Tooltip("Button used to shift selection upwards")]
    public Button upButton;
    [Tooltip("Button used to shift selection downwards")]
    public Button downButton;
    [Tooltip("Button used to activate the currently highlighted option")]
    public Button interactButton;

    [Header("Visual Highlighting Setup")]
    public Color highlightedColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Core Command Options")]
    public CommandOption attackOption;
    public CommandOption defendOption;
    public CommandOption skillsOption;
    public CommandOption itemOption;
    public CommandOption weaponOption;
    public CommandOption runOption;

    [Header("Additional Custom Options (+)")]
    [Tooltip("Use the '+' button in the Inspector to add your own custom navigational UI items here.")]
    public List<CommandOption> additionalOptions = new List<CommandOption>();

    private List<CommandOption> activeNavList = new List<CommandOption>();
    private int currentSelectedIndex = 0;

    private void Awake()
    {
        // 🛠️ FIX: Automatically find BattleFieldMode if left empty in the Inspector
        if (battlefieldMode == null)
        {
            battlefieldMode = FindFirstObjectByType<BattleFieldMode>();
            if (battlefieldMode == null)
            {
                Debug.LogWarning("[CommandNavigationalPanel] BattleFieldMode script could not be found automatically in the scene!");
            }
        }

        SetupButtonListeners();
    }

    private void OnEnable()
    {
        // Re-evaluate visibility states every time the UI panel turns on
        RefreshActiveMenuOptions();
        currentSelectedIndex = 0;
        UpdateVisualHighlighting();
    }

    /// <summary>
    /// Processes which modules should show or hide, and structures a safe loop list.
    /// </summary>
    public void RefreshActiveMenuOptions()
    {
        activeNavList.Clear();

        ProcessOptionVisibility(attackOption);
        ProcessOptionVisibility(defendOption);
        ProcessOptionVisibility(skillsOption);
        ProcessOptionVisibility(itemOption);
        ProcessOptionVisibility(weaponOption);
        ProcessOptionVisibility(runOption);

        if (additionalOptions != null)
        {
            foreach (var customOption in additionalOptions)
            {
                ProcessOptionVisibility(customOption);
            }
        }
    }

    /// <summary>
    /// Evaluates visibility toggles and handles UI container states safely.
    /// </summary>
    private void ProcessOptionVisibility(CommandOption option)
    {
        if (option == null) return;

        // Determine if we have a valid UI layout component to manipulate
        GameObject visualContainer = null;
        if (option.uiImage != null) visualContainer = option.uiImage.gameObject;
        else if (option.textMeshProText != null) visualContainer = option.textMeshProText.gameObject;

        if (option.isVisible)
        {
            if (visualContainer != null) visualContainer.SetActive(true);
            activeNavList.Add(option);
        }
        else
        {
            if (visualContainer != null) visualContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Safely registers input button actions without breaking other script functions.
    /// </summary>
    private void SetupButtonListeners()
    {
        if (upButton != null) upButton.onClick.AddListener(NavigateUp);
        if (downButton != null) downButton.onClick.AddListener(NavigateDown);
        if (interactButton != null) interactButton.onClick.AddListener(ExecuteInteraction);
    }

    public void NavigateUp()
    {
        if (!gameObject.activeInHierarchy || activeNavList.Count == 0) return;

        // SAFEGUARD: Only change menu selections if we are actually viewing the menu phase
        if (battlefieldMode != null && battlefieldMode.currentBattleState != BattleState.CommandMenu) return;

        currentSelectedIndex--;
        if (currentSelectedIndex < 0) currentSelectedIndex = activeNavList.Count - 1;

        UpdateVisualHighlighting();
    }

    public void NavigateDown()
    {
        if (!gameObject.activeInHierarchy || activeNavList.Count == 0) return;

        // SAFEGUARD: Only change menu selections if we are actually viewing the menu phase
        if (battlefieldMode != null && battlefieldMode.currentBattleState != BattleState.CommandMenu) return;

        currentSelectedIndex++;
        if (currentSelectedIndex >= activeNavList.Count) currentSelectedIndex = 0;

        UpdateVisualHighlighting();
    }

    public void ExecuteInteraction()
    {
        if (!gameObject.activeInHierarchy || activeNavList.Count == 0) return;

        CommandOption selected = activeNavList[currentSelectedIndex];
        if (selected != null && selected.onInteractEvent != null)
        {
            Debug.Log($"[CommandNavigationalPanel] Interacted with: {selected.optionName}");
            selected.onInteractEvent.Invoke();
        }
    }

    /// <summary>
    /// Updates colors of visible UI elements and switches text visibility based on selection.
    /// </summary>
    private void UpdateVisualHighlighting()
    {
        for (int i = 0; i < activeNavList.Count; i++)
        {
            CommandOption option = activeNavList[i];
            if (option == null) continue;

            bool isSelected = (i == currentSelectedIndex);
            Color targetColor = isSelected ? highlightedColor : normalColor;

            // Handle background icon coloring
            if (option.uiImage != null)
            {
                option.uiImage.color = targetColor;
            }

            // Text visibility behavior adjustment
            if (option.textMeshProText != null)
            {
                // Only render text mesh components if they match the user's active focus target index
                option.textMeshProText.enabled = isSelected;

                if (isSelected)
                {
                    option.textMeshProText.color = targetColor;
                }
            }
        }
    }

    /// <summary>
    /// Runtime helper method to dynamically show/hide standard options from other management systems.
    /// </summary>
    public void SetOptionVisibilityRuntime(string targetOptionName, bool state)
    {
        if (attackOption != null && attackOption.optionName == targetOptionName) attackOption.isVisible = state;
        else if (defendOption != null && defendOption.optionName == targetOptionName) defendOption.isVisible = state;
        else if (skillsOption != null && skillsOption.optionName == targetOptionName) skillsOption.isVisible = state;
        else if (itemOption != null && itemOption.optionName == targetOptionName) itemOption.isVisible = state;
        else if (weaponOption != null && weaponOption.optionName == targetOptionName) weaponOption.isVisible = state;
        else if (runOption != null && runOption.optionName == targetOptionName) runOption.isVisible = state;
        else
        {
            CommandOption foundExtra = additionalOptions.Find(x => x.optionName == targetOptionName);
            if (foundExtra != null) foundExtra.isVisible = state;
        }

        if (gameObject.activeInHierarchy)
        {
            RefreshActiveMenuOptions();
            if (currentSelectedIndex >= activeNavList.Count) currentSelectedIndex = Mathf.Max(0, activeNavList.Count - 1);
            UpdateVisualHighlighting();
        }
    }
}
