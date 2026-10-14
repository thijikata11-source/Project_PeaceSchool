using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIDirectionalGlow : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    private Image joystickImage;
    private Material joystickMaterial;
    private RectTransform rectTransform;

    [Header("Glow Settings")]
    public float targetIntensity = 2.5f; // Max brightness when pressed
    public float fadeSpeed = 10.0f;       // How fast it fades in/out

    private float currentIntensity = 0f;
    private bool isPressed = false;
    private Vector2 currentDirection = Vector2.up;
    private Vector2 targetDirection = Vector2.up;

    // Cached Property IDs for maximum runtime performance
    private int intensityID;
    private int directionID;

    void Start()
    {
        joystickImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (joystickImage != null && joystickImage.material != null)
        {
            // Instantiates a unique material instance so joysticks don't share identical visual data
            joystickImage.material = new Material(joystickImage.material);
            joystickMaterial = joystickImage.material;
        }

        intensityID = Shader.PropertyToID("_GlowIntensity");
        directionID = Shader.PropertyToID("_GlowDirection");

        joystickMaterial.SetFloat(intensityID, 0f);
    }

    void Update()
    {
        // 1. Smoothly fade the intensity in or out
        float intensityTarget = isPressed ? targetIntensity : 0f;
        currentIntensity = Mathf.MoveTowards(currentIntensity, intensityTarget, fadeSpeed * Time.deltaTime);
        joystickMaterial.SetFloat(intensityID, currentIntensity);

        // 2. Smoothly rotate the glow direction vector to follow the dragging finger smoothly
        if (isPressed)
        {
            currentDirection = Vector2.Lerp(currentDirection, targetDirection, Time.deltaTime * 15f);
            joystickMaterial.SetVector(directionID, new Vector4(currentDirection.x, currentDirection.y, 0, 0));
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        UpdateGlowDirection(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateGlowDirection(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    private void UpdateGlowDirection(PointerEventData eventData)
    {
        // Calculate where the mouse/finger is relative to the absolute center of the joystick Rect
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            if (localPoint.magnitude > 0.01f)
            {
                targetDirection = localPoint.normalized;
            }
        }
    }

    void OnDestroy()
    {
        if (joystickMaterial != null)
        {
            Destroy(joystickMaterial);
        }
    }
}
