using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour
{
    private static ThirdPersonCamera instance;

    [Header("Targets")]
    public Transform defaultTarget;
    public Transform zoomTarget;
    private Transform currentTarget;

    [Header("Offsets")]
    public Vector3 shoulderOffset = new Vector3(0.5f, 1.5f, -12.46f);

    [Header("Rotation")]
    public float rotationSpeed = 5f;
    private float pitch = 0f;
    private float yaw = 0f;

    [Header("Zoom & Collision")]
    public float zoomDistance = 1.5f;
    public LayerMask outOfBoundsLayer;
    public float smoothTime = 0.1f;
    private Vector3 currentVelocity;

    [Header("Poetic Feedback")]
    public CanvasGroup feedbackCanvas;
    public Text feedbackText;
    public float feedbackFadeDuration = 1f;
    private Coroutine feedbackRoutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        currentTarget = defaultTarget;
    }

    void LateUpdate()
    {
        if (currentTarget == null) return;

        HandleRotation();
        HandlePosition();
    }

    void HandleRotation()
    {
        // --- PC mouse input only
        yaw += Input.GetAxis("Mouse X") * rotationSpeed;
        pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;

        pitch = Mathf.Clamp(pitch, -30f, 60f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void HandlePosition()
    {
        Vector3 targetOffset = transform.rotation * shoulderOffset;
        Vector3 desiredPosition = currentTarget.position + targetOffset;

        Ray ray = new Ray(currentTarget.position, targetOffset.normalized);
        if (Physics.Raycast(ray, out RaycastHit hit, shoulderOffset.magnitude, outOfBoundsLayer))
        {
            if (zoomTarget != null && currentTarget != zoomTarget)
            {
                currentTarget = zoomTarget;
                TriggerPoeticFeedback("The world narrows—your spirit draws close.");
            }
            desiredPosition = currentTarget.position + targetOffset.normalized * zoomDistance;
        }
        else
        {
            if (currentTarget != defaultTarget)
            {
                currentTarget = defaultTarget;
                TriggerPoeticFeedback("Your vision eases—returning to balance.");
            }
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
    }

    void TriggerPoeticFeedback(string message)
    {
        if (feedbackCanvas == null || feedbackText == null) return;

        feedbackText.text = message;
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FadeFeedback());
    }

    IEnumerator FadeFeedback()
    {
        feedbackCanvas.alpha = 1f;
        yield return new WaitForSeconds(feedbackFadeDuration);
        while (feedbackCanvas.alpha > 0f)
        {
            feedbackCanvas.alpha -= Time.deltaTime / feedbackFadeDuration;
            yield return null;
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (defaultTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                defaultTarget = player.transform;
                currentTarget = defaultTarget;
            }
        }
    }

    void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }
}
