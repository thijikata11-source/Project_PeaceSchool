using UnityEngine;

public class PlayerCollisionHandler : MonoBehaviour
{
    private IsometricFollow cameraFollow;

    void Start()
    {
        // Find the camera script
        cameraFollow = Camera.main.GetComponent<IsometricFollow>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("StopFollow"))
        {
            cameraFollow.SetFollowing(false);
        }
        if (collision.gameObject.CompareTag("StartFollow"))
        {
            cameraFollow.SetFollowing(true);
        }

    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("StopFollow"))
        {
            cameraFollow.SetFollowing(true);
        }
    }





    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("StopFollow"))
        {
            cameraFollow.SetFollowing(false);
        }
        if (other.gameObject.CompareTag("StartFollow"))
        {
            cameraFollow.SetFollowing(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("StopFollow"))
        {
            cameraFollow.SetFollowing(true);
        }
    }
}
