using UnityEngine;

public class IsometricFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform player;          // Reference to the player
    public Vector3 offset = new Vector3(0, 10, -10); // Default isometric offset
    public float smoothSpeed = 0.125f; // Smoothness of camera movement

    private bool isFollowing = true;  // Flag to control following

    void LateUpdate()
    {
        if (isFollowing && player != null)
        {
            Vector3 desiredPosition = player.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    // Called externally by PlayerCollisionHandler
    public void SetFollowing(bool follow)
    {
        isFollowing = follow;
    }
}
