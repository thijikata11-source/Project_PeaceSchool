using UnityEngine;

public class ObjectUnParent : MonoBehaviour
{
    [Header("Parent References")]
    public Transform transformParentA; // Parent when Inside
    public Transform transformParentB; // Parent when Outside

    [Header("Locator Reference")]
    public PlayerLocator playerLocator;

    [Header("Custom Local Positions")]
    public Vector3 insideLocalPosition;  // Position when parented to A
    public Vector3 outsideLocalPosition; // Position when parented to B

    private void Update()
    {
        if (playerLocator == null) return;

        // Switch parent based on current location
        if (playerLocator.currentLocation == "Inside")
        {
            if (transform.parent != transformParentA)
            {
                transform.SetParent(transformParentA, false);
                transform.localPosition = insideLocalPosition;
            }
        }
        else if (playerLocator.currentLocation == "Outside")
        {
            if (transform.parent != transformParentB)
            {
                transform.SetParent(transformParentB, false);
                transform.localPosition = outsideLocalPosition;
            }
        }

        // Always follow parent’s transform (rotation/scale)
        if (transform.parent != null)
        {
            transform.localRotation = Quaternion.identity; // reset rotation relative to parent
            transform.localScale = Vector3.one;            // keep consistent scale
        }
    }
}
