using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class ConfirmationNavigationHandler : MonoBehaviour
{
    [Header("Navigation Panel References")]
    public GameObject navigationPanel;   // must be the same canvas/panel you toggle in BedInteract
    public Image[] menuImages;
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button interactButton;
    public Button cancelButton;
    public Image cursorImage;
    public TextMeshProUGUI selectionText;

    [Header("Image Labels")]
    public string[] imageLabels;

    [Header("Image Actions")]
    public UnityEvent[] imageEvents; // assign BedInteract.ConfirmAction / CancelAction here

    [Header("GameObjects to Disable While Active")]
    public List<GameObject> objectsToDisable;

    [Header("GameObjects to Enable While Active")]
    public List<GameObject> objectsToEnable;

    [Header("Confirmation Texts")]
    public TextMeshProUGUI confirmationText1;
    public TextMeshProUGUI confirmationText2;
    public TextMeshProUGUI confirmationText3;

    [Header("Confirmation Values")]
    [TextArea] public string textValue1;
    [TextArea] public string textValue2;
    [TextArea] public string textValue3;

    private int currentIndex = 0;

    void Start()
    {
        if (upButton != null) upButton.onClick.AddListener(() => SafeNavigate(-1));
        if (downButton != null) downButton.onClick.AddListener(() => SafeNavigate(1));
        if (leftButton != null) leftButton.onClick.AddListener(() => SafeNavigate(-1));
        if (rightButton != null) rightButton.onClick.AddListener(() => SafeNavigate(1));

        if (interactButton != null) interactButton.onClick.AddListener(SafeInteract);

        if (cancelButton != null) cancelButton.onClick.AddListener(() =>
        {
            if (IsPanelActive())
            {
                Debug.Log("[ConfirmationNav] Cancel pressed.");
                if (imageEvents != null && imageEvents.Length > 1 && imageEvents[1] != null)
                    imageEvents[1].Invoke();
            }
            else
            {
                Debug.Log("[ConfirmationNav] Cancel ignored. Panel inactive.");
            }
        });

        UpdateSelection();
        UpdateConfirmationTexts();
    }

    void OnEnable()
    {
        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);

        foreach (var obj in objectsToEnable)
            if (obj != null) obj.SetActive(true);

        currentIndex = 0;
        UpdateSelection();
        UpdateConfirmationTexts();
    }

    void OnDisable()
    {
        foreach (var obj in objectsToDisable)
            if (obj != null) obj.SetActive(true);

        foreach (var obj in objectsToEnable)
            if (obj != null) obj.SetActive(false);
    }

    private bool IsPanelActive()
    {
        return navigationPanel != null && navigationPanel.activeInHierarchy;
    }

    private void SafeNavigate(int direction)
    {
        if (IsPanelActive())
            Navigate(direction);
        else
            Debug.Log("[ConfirmationNav] Navigate ignored. Panel inactive.");
    }

    private void SafeInteract()
    {
        if (IsPanelActive())
            Interact();
        else
            Debug.Log("[ConfirmationNav] Interact ignored. Panel inactive.");
    }

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
            selectionText.text = imageLabels[currentIndex];
    }

    private void Interact()
    {
        Debug.Log("[ConfirmationNav] Interacted with image index: " + currentIndex);

        if (imageEvents != null && currentIndex < imageEvents.Length && imageEvents[currentIndex] != null)
        {
            imageEvents[currentIndex].Invoke(); // triggers ConfirmAction or CancelAction
        }
    }

    private void UpdateConfirmationTexts()
    {
        if (confirmationText1 != null) confirmationText1.text = textValue1;
        if (confirmationText2 != null) confirmationText2.text = textValue2;
        if (confirmationText3 != null) confirmationText3.text = textValue3;
    }
}
