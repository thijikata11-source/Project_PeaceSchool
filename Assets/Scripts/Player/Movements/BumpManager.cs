using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class BumpManager : MonoBehaviour
{
    private CharacterController controller;
    private PlayerMovement3D playerMovement;

    private bool forwardBlocked = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement3D>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Wall"))
        {
            Vector3 localNormal = transform.InverseTransformDirection(hit.normal);

            // Proper front-facing check
            if (localNormal.z < -0.01f)
            {
                forwardBlocked = true;
                //Debug.Log("Forward/Diagonal blocked by wall!");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            forwardBlocked = false;
            //Debug.Log("Exited wall collider → forward unblocked");
        }
    }

    void LateUpdate()
    {
        if (forwardBlocked)
        {
            Vector3 velocity = controller.velocity;
            Vector3 localVel = transform.InverseTransformDirection(velocity);

            // Cancel forward AND diagonal (X + Z together)
            if (localVel.z > 0.01f)
            {
                // Stop ALL forward-related motion
                Vector3 cancelMove = new Vector3(localVel.x, 0f, localVel.z);
                controller.Move(-transform.TransformDirection(cancelMove) * Time.deltaTime);

                if (playerMovement.animator != null)
                {
                    playerMovement.animator.SetFloat("MoveZ", 0f);
                    playerMovement.animator.SetFloat("MoveX", 0f);
                    playerMovement.animator.SetFloat("Speed", 0f);
                }
            }
        }

        // Reset block if moving backward
        Vector3 currentVel = controller.velocity;
        Vector3 localCurrentVel = transform.InverseTransformDirection(currentVel);

        if (localCurrentVel.z < 0f)
        {
            forwardBlocked = false;
        }
    }
}
