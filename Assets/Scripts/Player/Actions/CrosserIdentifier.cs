using UnityEngine;
using UnityEngine.UI;

public class CrosserIdentifier : MonoBehaviour
{
    [Header("Detection Settings")]
    public string[] detectableTags = { "Objects", "NPC", "Entrance", "Exit", "Shops" };
    public float detectionRange = 20f;
    public float stickyRange = 2f; // distance threshold for sticky lock
    public Transform character;
    public Transform cameraTransform;

    [Header("UI Settings")]
    public Image targetIndicator;
    public float fadeSpeed = 5f;

    [Header("Debug / Inspector")]
    [SerializeField] private GameObject lockedObject;

    private Color indicatorColor;

    void Start()
    {
        if (targetIndicator != null)
            indicatorColor = targetIndicator.color;
    }

    void Update()
    {
        DetectNearestObject();
        HandleCrosserFade();
    }

    void DetectNearestObject()
    {
        Vector3 camForward = cameraTransform.forward;
        Vector3 charForward = character.forward;
        float alignment = Vector3.Dot(camForward.normalized, charForward.normalized);

        // If already locked and within sticky range, keep it locked regardless of alignment
        if (lockedObject != null)
        {
            float distToLocked = Vector3.Distance(character.position, lockedObject.transform.position);
            if (distToLocked <= stickyRange)
            {
                return; // keep current lock
            }
        }

        // Otherwise, require facing alignment
        if (alignment < 0.5f)
        {
            lockedObject = null;
            return;
        }

        Ray ray = new Ray(cameraTransform.position, camForward);
        RaycastHit[] hits = Physics.RaycastAll(ray, detectionRange);

        GameObject nearest = null;
        float nearestDist = Mathf.Infinity;

        foreach (RaycastHit hit in hits)
        {
            if (IsDetectable(hit.collider.gameObject))
            {
                float dist = Vector3.Distance(cameraTransform.position, hit.point);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = hit.collider.gameObject;
                }
            }
        }

        if (nearest != null)
        {
            if (lockedObject == null)
            {
                lockedObject = nearest;
            }
            else
            {
                float currentDist = Vector3.Distance(cameraTransform.position, lockedObject.transform.position);
                float newDist = Vector3.Distance(cameraTransform.position, nearest.transform.position);

                // Switch lock if new object is closer, unless current is sticky
                if (newDist < currentDist)
                {
                    lockedObject = nearest;
                }
            }
        }
        else
        {
            lockedObject = null;
        }
    }

    bool IsDetectable(GameObject obj)
    {
        foreach (string tag in detectableTags)
        {
            if (obj.CompareTag(tag))
                return true;
        }
        return false;
    }

    void HandleCrosserFade()
    {
        if (targetIndicator == null) return;

        float targetAlpha = (lockedObject != null) ? 1f : 0f;
        indicatorColor.a = Mathf.Lerp(indicatorColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        targetIndicator.color = indicatorColor;
    }
}
