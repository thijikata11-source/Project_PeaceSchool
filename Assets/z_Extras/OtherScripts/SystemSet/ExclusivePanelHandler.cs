using UnityEngine;

public class ExclusivePanelHandler : MonoBehaviour
{
    [Header("Other Panel to Control")]
    public GameObject otherPanel;   // Drag the other GameObject here in Inspector

    void OnEnable()
    {
        // When this GameObject becomes active, disable the other
        if (otherPanel != null)
        {
            otherPanel.SetActive(false);
        }
    }

    void OnDisable()
    {
        // Optional: when this GameObject is disabled, re-enable the other
        if (otherPanel != null)
        {
            otherPanel.SetActive(true);
        }
    }
}
