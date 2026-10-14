using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float Horizontal { get { return (snapX) ? SnapFloat(input.x, AxisOptions.Horizontal) : input.x; } }
    public float Vertical { get { return (snapY) ? SnapFloat(input.y, AxisOptions.Vertical) : input.y; } }
    public Vector2 Direction { get { return new Vector2(Horizontal, Vertical); } }

    public float HandleRange
    {
        get { return handleRange; }
        set { handleRange = Mathf.Max(value, 0f); }
    }

    public float DeadZone
    {
        get { return deadZone; }
        set { deadZone = Mathf.Max(value, 0f); }
    }

    public AxisOptions AxisOptions { get { return axisOptions; } set { axisOptions = value; } }
    public bool SnapX { get { return snapX; } set { snapX = value; } }
    public bool SnapY { get { return snapY; } set { snapY = value; } }

    [SerializeField] private float handleRange = 1f;
    [SerializeField] private float deadZone = 0f;
    [SerializeField] private AxisOptions axisOptions = AxisOptions.Both;
    [SerializeField] private bool snapX = false;
    [SerializeField] private bool snapY = false;

    [SerializeField] protected RectTransform background = null;
    [SerializeField] private RectTransform handle = null;
    private RectTransform baseRect = null;

    private Canvas canvas;
    private Camera cam;

    protected Vector2 input = Vector2.zero;

    // --- GLOW PROPERTIES ---
    [Header("Glow Controls")]
    [SerializeField] private float targetIntensity = 2.5f;
    [SerializeField] private float fadeSpeed = 10.0f;
    private Material joystickMaterial;
    private float currentIntensity = 0f;
    private bool isPressed = false;
    private Vector2 currentDirection = Vector2.up;
    private int intensityID;
    private int directionID;
    // ----------------------------

    protected virtual void Start()
    {
        HandleRange = handleRange;
        DeadZone = deadZone;
        baseRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            Debug.LogError("The Joystick must be placed inside a Canvas");

        Vector2 center = new Vector2(0.5f, 0.5f);
        background.pivot = center;
        handle.anchorMin = center;
        handle.anchorMax = center;
        handle.anchoredPosition = Vector2.zero; // FIX 1: Fixed typo from anchorPosition to anchoredPosition
        handle.pivot = center;

        // --- INITIALIZE GLOW ---
        if (background != null)
        {
            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null && bgImage.material != null)
            {
                bgImage.material = new Material(bgImage.material);
                joystickMaterial = bgImage.material;
            }
        }
        intensityID = Shader.PropertyToID("_GlowIntensity");
        directionID = Shader.PropertyToID("_GlowDirection");
        if (joystickMaterial != null) joystickMaterial.SetFloat(intensityID, 0f);
        // -----------------------
    }

    // --- SMOOTH FADE LOGIC ---
    protected virtual void Update()
    {
        float targetInt = isPressed ? targetIntensity : 0f;
        currentIntensity = Mathf.MoveTowards(currentIntensity, targetInt, fadeSpeed * Time.deltaTime);

        if (joystickMaterial != null)
        {
            joystickMaterial.SetFloat(intensityID, currentIntensity);

            if (isPressed && input.magnitude > 0.01f)
            {
                currentDirection = Vector2.Lerp(currentDirection, input.normalized, Time.deltaTime * 15f);
                joystickMaterial.SetVector(directionID, new Vector4(currentDirection.x, currentDirection.y, 0, 0));
            }
        }
    }
    // -------------------------

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        OnDrag(eventData);
    }

    public virtual void OnDrag(PointerEventData eventData)
    {
        cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
            cam = canvas.worldCamera;

        Vector2 position = RectTransformUtility.WorldToScreenPoint(cam, background.position);
        Vector2 radius = background.sizeDelta / 2f;
        input = (eventData.position - position) / (radius * canvas.scaleFactor);
        FormatInput();
        HandleInput(input.magnitude, input.normalized, radius, cam);
        handle.anchoredPosition = input * radius * handleRange;
    }

    protected virtual void HandleInput(float magnitude, Vector2 direction, Vector2 radius, Camera cam)
    {
        if (magnitude > deadZone)
        {
            if (magnitude > 1f)
                input = direction;
        }
        else
            input = Vector2.zero;
    }

    private void FormatInput()
    {
        if (axisOptions == AxisOptions.Horizontal)
            input = new Vector2(input.x, 0f);
        else if (axisOptions == AxisOptions.Vertical)
            input = new Vector2(0f, input.y);
    }

    private float SnapFloat(float value, AxisOptions axisOption)
    {
        if (value == 0f)
            return value;

        if (axisOptions == AxisOptions.Both)
        {
            float angle = Vector2.Angle(input, Vector2.up);
            if (axisOption == AxisOptions.Horizontal)
            {
                if (angle < 22.5f || angle > 157.5f)
                    return 0f;
                else
                    return (value > 0f) ? 1f : -1f;
            }
            else if (axisOption == AxisOptions.Vertical)
            {
                if (angle > 67.5f && angle < 112.5f)
                    return 0f;
                else
                    return (value > 0f) ? 1f : -1f;
            }
        }
        else
        {
            if (value > 0f)
                return 1f;
            if (value < 0f)
                return -1f;
        }
        return 0f;
    }

    public virtual void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
        input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;
    }

    protected virtual void OnDestroy()
    {
        if (joystickMaterial != null)
        {
            Destroy(joystickMaterial);
        }
    }

    // FIX 2: Restored this helper function required by the other Joystick variants (Floating, Variable, Dynamic)
    protected Vector2 ScreenPointToAnchoredPosition(Vector2 screenPosition)
    {
        Vector2 localPoint = Vector2.zero;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, screenPosition, cam, out localPoint))
        {
            Vector2 pivotOffset = baseRect.pivot - new Vector2(0.5f, 0.5f);
            Vector2 size = baseRect.rect.size;
            return localPoint - (pivotOffset * size);
        }
        return Vector2.zero;
    }
}

public enum AxisOptions { Both, Horizontal, Vertical }
