using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueDecisionManager : MonoBehaviour
{
    [Header("Decision UI")]
    public GameObject decisionPanel;
    public Button decisionButtonPrefab;

    [Header("Canvases to Disable During Decisions")]
    public List<Canvas> canvasesToDisable = new List<Canvas>();

    [Header("Requirements Toggle")]
    public string requirementsMet = "No"; // default

    private CharacterDialogues dialogueSystem;

    void Start()
    {
        dialogueSystem = GetComponent<CharacterDialogues>();
        decisionPanel.SetActive(false);
    }

    // ✅ Called by GlobalTimeSystem.NextDay()
    public void ResetRequirements()
    {
        if (requirementsMet == "Yes")
        {
            requirementsMet = "No";
            Debug.Log($"{gameObject.name} requirementsMet reset to NO at NextDay()");
        }
    }

    public void ShowDecisionOptions(List<DecisionOption> options)
    {
        decisionPanel.SetActive(true);

        foreach (var canvas in canvasesToDisable)
            if (canvas != null) canvas.enabled = false;

        foreach (Transform child in decisionPanel.transform)
            Destroy(child.gameObject);

        foreach (var option in options)
        {
            Button btn = Instantiate(decisionButtonPrefab, decisionPanel.transform);
            btn.transform.SetParent(decisionPanel.transform, false);

            btn.GetComponentInChildren<TMP_Text>().text = dialogueSystem.ParseDialogueText(option.optionText);
            btn.onClick.AddListener(() => OnDecisionSelected(option));
        }
    }

    void OnDecisionSelected(DecisionOption option)
    {
        decisionPanel.SetActive(false);

        foreach (var canvas in canvasesToDisable)
            if (canvas != null) canvas.enabled = true;

        // ✅ First check requirements before applying effects
        if (!RequirementsMet(option))
        {
            if (option.failureLines != null && option.failureLines.Count > 0)
                dialogueSystem.LoadBranch(option.failureLines);
            else
                dialogueSystem.LoadBranch(new List<string> { "You don't meet the requirements." });
            return; // stop here, don’t apply effects
        }

        // ✅ Only apply effects if requirements are met
        foreach (var command in option.effectCommands)
        {
            if (command == "?success")
            {
                requirementsMet = "Yes";
                Debug.Log($"{gameObject.name} requirementsMet flipped to YES");
            }
            else
            {
                ApplyEffect(command);
            }
        }

        if (requirementsMet == "Yes")
        {
            dialogueSystem.OverrideDialogueWithSuccess(option.successLines);
        }
        else
        {
            dialogueSystem.LoadBranch(option.resultingLines);
        }
    }

    bool RequirementsMet(DecisionOption option)
    {
        foreach (var command in option.effectCommands)
        {
            if (string.IsNullOrEmpty(command)) continue;

            string[] parts = command.Split(' ');
            if (parts.Length < 2) continue;

            string operation = parts[0];
            int amount = int.Parse(parts[1]);

            // ✅ Check PlayerGolds before subtracting
            if (operation.Contains("PlayerGolds") && operation.StartsWith("-"))
            {
                if (dialogueSystem.playerProfile.PlayerGolds < amount)
                    return false;
            }
        }
        return true;
    }


    void ApplyEffect(string command)
    {
        if (string.IsNullOrEmpty(command)) return;

        string[] parts = command.Split(' ');
        if (parts.Length < 2) return;

        string operation = parts[0];
        int amount = int.Parse(parts[1]);

        if (dialogueSystem.playerProfile == null) return;

        if (operation.Contains("PlayerGolds"))
        {
            if (operation.StartsWith("+"))
                dialogueSystem.playerProfile.PlayerGolds += amount;
            else if (operation.StartsWith("-"))
                dialogueSystem.playerProfile.PlayerGolds -= amount;
        }
        else if (operation.Contains("PlayerLumbers"))
        {
            if (operation.StartsWith("+"))
                dialogueSystem.playerProfile.PlayerLumbers += amount;
            else if (operation.StartsWith("-"))
                dialogueSystem.playerProfile.PlayerLumbers -= amount;
        }
        else if (operation.Contains("PlayerStamina"))
        {
            if (operation.StartsWith("+"))
                dialogueSystem.playerProfile.PlayerStamina += amount;
            else if (operation.StartsWith("-"))
                dialogueSystem.playerProfile.PlayerStamina -= amount;
        }
        else if (operation.Contains("PlayerHealth"))
        {
            string[] healthStates = { "NearDeath", "Sick", "Fatigue", "Normal" };
            int currentIndex = System.Array.IndexOf(healthStates, dialogueSystem.playerProfile.PlayerHealthStatus);

            if (currentIndex == -1) return;

            if (operation.StartsWith("+"))
                currentIndex = Mathf.Min(currentIndex + amount, healthStates.Length - 1);
            else if (operation.StartsWith("-"))
                currentIndex = Mathf.Max(currentIndex - amount, 0);

            dialogueSystem.playerProfile.PlayerHealthStatus = healthStates[currentIndex];
        }
    }
}
