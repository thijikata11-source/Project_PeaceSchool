using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Needed for Pointer events

[RequireComponent(typeof(Collider))] // Keeps physics boundaries structurally aligned
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement3D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 9f;
    public float rotationLerp = 10f;

    [Header("Fall Settings")]
    public float gravity = -60f;
    public float groundedSnap = -2f;

    [Header("References")]
    public Animator animator;
    public Transform cameraTransform;

    [Header("Mobile Joystick")]
    public FixedJoystick joystick;

    [Header("Mobile Buttons")]
    public Button sprintButton;

    [Header("Mobile Movement Buttons (WASD)")]
    public Button forwardButton;
    public Button backButton;
    public Button leftButton;
    public Button rightButton;

    [Header("Mobile Diagonal Buttons")]
    public Button upRightButton;
    public Button upLeftButton;
    public Button downRightButton;
    public Button downLeftButton;

    private CharacterController controller;
    private Vector3 moveInput;
    private float verticalVelocity;
    private bool isSprinting;

    public ActionMasterControl AMC;

    // Held states for WASD buttons
    private bool forwardHeld, backHeld, leftHeld, rightHeld;

    // Held states for Diagonal buttons
    private bool upRightHeld, upLeftHeld, downRightHeld, downLeftHeld;

    public CharacterController Controller => controller;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Sprint button setup
        if (sprintButton != null)
        {
            EventTrigger trigger = sprintButton.gameObject.AddComponent<EventTrigger>();

            var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entryDown.callback.AddListener((data) => { OnSprintButtonDown(); });
            trigger.triggers.Add(entryDown);

            var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            entryUp.callback.AddListener((data) => { OnSprintButtonUp(); });
            trigger.triggers.Add(entryUp);
        }

        // WASD buttons setup
        SetupMovementButton(forwardButton, () => forwardHeld = true, () => forwardHeld = false);
        SetupMovementButton(backButton, () => backHeld = true, () => backHeld = false);
        SetupMovementButton(leftButton, () => leftHeld = true, () => leftHeld = false);
        SetupMovementButton(rightButton, () => rightHeld = true, () => rightHeld = false);

        // Diagonal buttons setup
        SetupMovementButton(upRightButton, () => upRightHeld = true, () => upRightHeld = false);
        SetupMovementButton(upLeftButton, () => upLeftHeld = true, () => upLeftHeld = false);
        SetupMovementButton(downRightButton, () => downRightHeld = true, () => downRightHeld = false);
        SetupMovementButton(downLeftButton, () => downLeftHeld = true, () => downLeftHeld = false);
    }

    private void SetupMovementButton(Button btn, System.Action onDown, System.Action onUp)
    {
        if (btn == null) return;
        EventTrigger trigger = btn.gameObject.AddComponent<EventTrigger>();

        var entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((data) => { onDown(); });
        trigger.triggers.Add(entryDown);

        var entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener((data) => { onUp(); });
        trigger.triggers.Add(entryUp);
    }

    // 🛠️ FIX: Public cleanup initialization to clear vectors and button locks completely
    public void ResetMovementStates()
    {
        moveInput = Vector3.zero;
        isSprinting = false;

        forwardHeld = false;
        backHeld = false;
        leftHeld = false;
        rightHeld = false;

        upRightHeld = false;
        upLeftHeld = false;
        downRightHeld = false;
        downLeftHeld = false;

        if (AMC != null)
        {
            AMC.masterActionControl = "idle";
        }
    }

    void Update()
    {
        // 🛠️ CRITICAL FIX 1: If an external system takes control, stop this script immediately!
        if (AMC != null && AMC.masterActionControl != "idle" &&
            AMC.masterActionControl != "walking" &&
            AMC.masterActionControl != "running" &&
            !string.IsNullOrEmpty(AMC.masterActionControl))
        {
            // Reset character velocity and animator velocity smoothly so they don't slide/freeze mid-air
            if (animator != null)
            {
                animator.SetFloat("Speed", 0f);
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveZ", 0f);
                animator.SetBool("isSprinting", false);
            }
            return; // Exit out of the update frame entirely
        }

        // --- Input (PC fallback only)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // --- JOYSTICK OVERRIDE ---
        if (joystick != null && (joystick.Horizontal != 0f || joystick.Vertical != 0f))
        {
            h = joystick.Horizontal;
            v = joystick.Vertical;
        }

        // Mobile straight button overrides
        if (forwardHeld) v = 1f;
        if (backHeld) v = -1f;
        if (leftHeld) h = -1f;
        if (rightHeld) h = 1f;

        // Mobile diagonal button overrides
        if (upRightHeld) { v = 1f; h = 1f; }
        if (upLeftHeld) { v = 1f; h = -1f; }
        if (downRightHeld) { v = -1f; h = 1f; }
        if (downLeftHeld) { v = -1f; h = -1f; }

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        // Camera-relative movement
        Vector3 camForward = Vector3.Scale(cameraTransform.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = cameraTransform.right;
        moveInput = (camForward * inputDir.z + camRight * inputDir.x).normalized;

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Animator parameters
        Vector3 localMove = transform.InverseTransformDirection(moveInput);
        if (animator != null)
        {
            animator.SetFloat("MoveX", localMove.x);
            animator.SetFloat("MoveZ", localMove.z);
            animator.SetFloat("Speed", moveInput.magnitude);
            animator.SetBool("isSprinting", isSprinting);
        }

        // --- Ground check
        bool isGrounded = controller.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedSnap;

        // --- Gravity
        verticalVelocity += gravity * Time.deltaTime;

        // --- Rotation
        Vector3 flatDir = new Vector3(moveInput.x, 0, moveInput.z);
        if (flatDir.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationLerp * Time.deltaTime);
        }

        // --- Move
        Vector3 horizontal = moveInput * currentSpeed;
        Vector3 motion = new Vector3(horizontal.x, verticalVelocity, horizontal.z) * Time.deltaTime;
        controller.Move(motion);

        // 🛠️ CRITICAL FIX 2: Only write to masterActionControl if we are in core tracking loops
        if (AMC != null)
        {
            if (moveInput.magnitude > 0.1f)
            {
                AMC.masterActionControl = isSprinting ? "running" : "walking";
            }
            else
            {
                AMC.masterActionControl = "idle";
            }
        }
    }

    public void OnSprintButtonDown() => isSprinting = true;
    public void OnSprintButtonUp() => isSprinting = false;
}
