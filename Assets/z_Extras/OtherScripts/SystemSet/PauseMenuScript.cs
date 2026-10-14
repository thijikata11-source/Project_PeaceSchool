using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Collections.Generic;

public class PauseMenuScript : MonoBehaviour
{
    [Header("Pause Menu References")]
    public GameObject pauseMenuCanvas;
    public Image[] menuImages;
    public Button leftButton;
    public Button rightButton;
    public Button interactButton;
    public Button togglePauseButton;
    public Button cancelButton;
    public Image cursorImage;
    public TextMeshProUGUI selectionText;

    [Header("Image Labels")]
    public string[] imageLabels;

    [Header("Image Actions")]
    public UnityEvent[] imageEvents;

    [Header("Scripts to Disable While Paused")]
    public List<MonoBehaviour> scriptsToDisable;

    [Header("GameObjects to Disable While Paused")]
    public List<GameObject> gameObjectsToDisable; // NEW LIST

    private int currentIndex = 0;
    private bool isPaused = false;

    void Start()
    {
        pauseMenuCanvas.SetActive(false);

        leftButton.onClick.AddListener(() => Navigate(-1));
        rightButton.onClick.AddListener(() => Navigate(1));
        interactButton.onClick.AddListener(Interact);
        togglePauseButton.onClick.AddListener(TogglePauseMenu);
        cancelButton.onClick.AddListener(ClosePauseMenu);

        UpdateSelection();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ClosePauseMenu();
            else TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        isPaused = !isPaused;
        pauseMenuCanvas.SetActive(isPaused);

        if (isPaused)
        {
            //Time.timeScale = 0f;
            currentIndex = 0;
            UpdateSelection();
            SetScriptsEnabled(false);
            SetGameObjectsEnabled(false); // NEW
        }
        else
        {
            //Time.timeScale = 1f;
            SetScriptsEnabled(true);
            SetGameObjectsEnabled(true); // NEW
        }
    }

    private void ClosePauseMenu()
    {
        isPaused = false;
        pauseMenuCanvas.SetActive(false);
        //Time.timeScale = 1f;
        SetScriptsEnabled(true);
        SetGameObjectsEnabled(true); // NEW
    }

    private void Navigate(int direction)
    {
        if (!isPaused) return;

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
        if (!isPaused) return;

        Debug.Log("Interacted with image index: " + currentIndex);

        if (imageEvents != null && currentIndex < imageEvents.Length && imageEvents[currentIndex] != null)
        {
            imageEvents[currentIndex].Invoke();
        }
    }

    private void SetScriptsEnabled(bool enabled)
    {
        foreach (var script in scriptsToDisable)
        {
            if (script != null) script.enabled = enabled;
        }
    }

    private void SetGameObjectsEnabled(bool enabled) // NEW METHOD
    {
        foreach (var obj in gameObjectsToDisable)
        {
            if (obj != null) obj.SetActive(enabled);
        }
    }
}
