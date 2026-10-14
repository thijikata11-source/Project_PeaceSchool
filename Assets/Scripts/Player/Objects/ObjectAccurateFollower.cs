using UnityEngine;

public class ObjectAccurateFollower : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform player;

    [Header("Isometric Camera Setup")]
    public Transform cameraTransform; // Assign your Main Camera or CameraPivot here

    [Header("Offsets")]
    public float frontDistance = 1.0f;
    public float backDistance = 1.0f;

    [Header("Vertical Offset (Y axis)")]
    public float yOffset = 0.0f;

    [Header("Debug Info")]
    public string positionState = "Front";

    private bool bumped = false;

    public GameObject[] characters;
    public PartyMembers PMembers;
    public int currentIdFront;

    public void SelectCharacter()
    {
        currentIdFront = PMembers.currentFrontId;
        player = characters[currentIdFront].transform;
    }

    void LateUpdate()
    {
        if (player == null || cameraTransform == null) return;

        // Get the view direction of the isometric camera projected on the flat XZ plane
        Vector3 viewDir = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;

        // The "Front" direction relative to the screen/camera perspective
        Vector3 frontDirection = viewDir;

        Vector3 targetPosition;

        // Check if there is an OutOfBounds wall physically blocking the target front position
        Vector3 desiredFrontPos = player.position + frontDirection * frontDistance;
        desiredFrontPos.y = player.position.y + yOffset;

        // Raycast from player center towards the front position to check for walls
        Vector3 rayStart = player.position + Vector3.up * (yOffset + 0.5f);
        float checkRadius = 0.3f; // Approximate width of your follower character

        // Bitmask for the OutOfBounds layer
        int layerMask = 1 << LayerMask.NameToLayer("OutOfBounds");

        // If a solid wall is detected blocking the front path, flip to the back
        if (Physics.SphereCast(rayStart, checkRadius, frontDirection, out RaycastHit hit, frontDistance, layerMask))
        {
            bumped = true;
        }
        else
        {
            bumped = false;
        }

        // Apply positioning based on the visibility check
        if (bumped)
        {
            // Position behind the player relative to the camera view
            targetPosition = player.position - frontDirection * backDistance;
            positionState = "Behind";
        }
        else
        {
            // Position in front of the player relative to the camera view
            targetPosition = player.position + frontDirection * frontDistance;
            positionState = "Front";
        }

        targetPosition.y = player.position.y + yOffset;

        // Apply translation and copy player's rotation
        transform.position = targetPosition;
        transform.rotation = player.rotation;
    }
}
