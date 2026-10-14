using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class NavigationHandler : MonoBehaviour
{
    [Header("Navigation Panel References")]
    public GameObject navigationPanel;       // The panel this script controls
    public Image[] menuImages;               // Images/options to navigate
    public Button upButton;                  // Navigate up
    public Button downButton;                // Navigate down
    public Button leftButton;                // Navigate left
    public Button rightButton;               // Navigate right
    public Button interactButton;            // Confirm/Select
    public Button cancelButton;              // Cancel/Close
    public Image cursorImage;                // Cursor image
    public TextMeshProUGUI selectionText;    // Label for current selection

    [Header("Image Labels")]
    public string[] imageLabels;             // Names/descriptions for each image

    [Header("Image Actions")]
    public UnityEvent[] imageEvents;         // Actions per image

    [Header("Buttons to Disable While Active")]
    public List<Button> buttonsToDisable;    // Buttons disabled when panel is active

    [Header("Panel to Open on Cancel")]
    public GameObject panelToOpenOnCancel;   // Another panel to enable when cancel is pressed or when this closes

    private int currentIndex = 0;

    void Start()
    {
        // Hook up navigation
        if (upButton != null) upButton.onClick.AddListener(() => SafeNavigate(-1));
        if (downButton != null) downButton.onClick.AddListener(() => SafeNavigate(1));
        if (leftButton != null) leftButton.onClick.AddListener(() => SafeNavigate(-1));
        if (rightButton != null) rightButton.onClick.AddListener(() => SafeNavigate(1));
        if (interactButton != null) interactButton.onClick.AddListener(SafeInteract);
        if (cancelButton != null) cancelButton.onClick.AddListener(SafeCancel);

        UpdateSelection();
    }

    void OnEnable()
    {
        foreach (var button in buttonsToDisable)
        {
            if (button != null) button.interactable = false;
        }

        currentIndex = 0;
        UpdateSelection();
    }

    void OnDisable()
    {
        foreach (var button in buttonsToDisable)
        {
            if (button != null) button.interactable = true;
        }

        // Exemption: force open another panel when this one closes
        if (panelToOpenOnCancel != null)
        {
            panelToOpenOnCancel.SetActive(true);
        }
    }

    // --- Safe wrappers ---
    private void SafeNavigate(int direction)
    {
        if (navigationPanel != null && navigationPanel.activeInHierarchy)
        {
            Navigate(direction);
        }
    }

    private void SafeInteract()
    {
        if (navigationPanel != null && navigationPanel.activeInHierarchy)
        {
            Interact();
        }
    }

    private void SafeCancel()
    {
        if (navigationPanel != null && navigationPanel.activeInHierarchy)
        {
            CancelNavigation();
        }
    }

    // --- Core logic ---
    private void Navigate(int direction)
    {
        currentIndex += direction;

        if (currentIndex < 0) currentIndex = menuImages.Length - 1;
        if (currentIndex >= menuImages.Length) currentIndex = 0;

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        if (cursorImage != null && currentIndex < menuImages.Length)
        {
            cursorImage.transform.SetParent(menuImages[currentIndex].transform, false);
            cursorImage.rectTransform.anchoredPosition = Vector2.zero;
        }

        if (selectionText != null && imageLabels.Length == menuImages.Length)
        {
            selectionText.text = imageLabels[currentIndex];
        }
    }

    private void Interact()
    {
        Debug.Log("Interacted with image index: " + currentIndex);

        if (imageEvents != null && currentIndex < imageEvents.Length && imageEvents[currentIndex] != null)
        {
            imageEvents[currentIndex].Invoke();
        }
    }

    private void CancelNavigation()
    {
        // Disable this panel UI
        if (navigationPanel != null)
        {
            navigationPanel.SetActive(false);
        }

        // Disable this GameObject entirely
        gameObject.SetActive(false);

        // Enable another panel if assigned
        if (panelToOpenOnCancel != null)
        {
            panelToOpenOnCancel.SetActive(true);
        }
    }
}
