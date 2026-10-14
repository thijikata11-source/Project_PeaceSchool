using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIKeySimulator : MonoBehaviour
{
    [Header("Assign your UI Buttons (controls)")]
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button enterButton;

    void Awake()
    {
        // Bind navigation buttons
        BindNavButton(upButton, MoveSelectionUp);
        BindNavButton(downButton, MoveSelectionDown);
        BindNavButton(leftButton, MoveSelectionLeft);
        BindNavButton(rightButton, MoveSelectionRight);

        // Bind Enter button
        BindNavButton(enterButton, SubmitSelection);

        // Ensure something is selected at start
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            Selectable first = GameObject.FindObjectOfType<Selectable>();
            if (first != null)
                EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }

    private void BindNavButton(Button button, System.Action action)
    {
        if (button == null) return;

        EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((_) => {
            action();
            // IMPORTANT: don’t let control button take focus
            // Keep focus on the menu’s currently selected object
        });
        trigger.triggers.Add(entryDown);
    }

    // Navigation helpers
    private void MoveSelectionUp()
    {
        MoveTo(EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>()?.FindSelectableOnUp());
    }

    private void MoveSelectionDown()
    {
        MoveTo(EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>()?.FindSelectableOnDown());
    }

    private void MoveSelectionLeft()
    {
        MoveTo(EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>()?.FindSelectableOnLeft());
    }

    private void MoveSelectionRight()
    {
        MoveTo(EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>()?.FindSelectableOnRight());
    }

    private void SubmitSelection()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null)
        {
            var button = selected.GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke(); // trigger the button’s action
        }
    }

    private void MoveTo(Selectable target)
    {
        if (target != null)
            EventSystem.current.SetSelectedGameObject(target.gameObject);
    }
}
