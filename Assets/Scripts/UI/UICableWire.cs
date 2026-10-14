using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UICableWire : MaskableGraphic
{
    [Header("References")]
    [SerializeField] private Slider parentSlider;
    [SerializeField] private RectTransform handleTransform;

    [Header("Cable Layout")]
    [Range(10, 50)][SerializeField] private int vertexCount = 30;
    [SerializeField] private float thickness = 6f;
    [SerializeField] private float maxLoopDrop = 150f;
    [SerializeField] private float loopOffsetDirection = -100f;

    [Header("Idle Sway / Breathe Settings")]
    [SerializeField] private bool enableIdleSway = true;
    [SerializeField] private float swaySpeed = 2f;      // How fast it breathes/sways
    [SerializeField] private float swayAmountX = 15f;    // Horizontal sway distance
    [SerializeField] private float swayAmountY = 10f;    // Vertical breathing depth

    private RectTransform rectTrans;

    protected override void Awake()
    {
        base.Awake();
        rectTrans = GetComponent<RectTransform>();
        if (parentSlider == null) parentSlider = GetComponentInParent<Slider>();

        if (handleTransform == null && parentSlider != null && parentSlider.handleRect != null)
        {
            handleTransform = parentSlider.handleRect;
        }
    }

    void Update()
    {
        // Forces the UI system to redraw the mesh every frame so it can animate
        SetAllDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (parentSlider == null || handleTransform == null || rectTrans == null) return;

        // 1. Calculate positions relative to the Background RectTransform
        Vector2 startPoint = new Vector2(0f, -rectTrans.rect.height * 0.5f);
        Vector2 endPoint = rectTrans.InverseTransformPoint(handleTransform.position);

        // 2. Determine the slack weight based on handle height
        float range = parentSlider.maxValue - parentSlider.minValue;
        float normalizedValue = (range > 0) ? (parentSlider.value - parentSlider.minValue) / range : 0f;
        float slackWeight = 1f - normalizedValue;

        // 3. Calculate Idle Sway offsets using layered math waves
        float offsetX = 0f;
        float offsetY = 0f;

        if (enableIdleSway)
        {
            // Time-based wave calculations, scaled by slackWeight so it stops swaying when pulled perfectly taut
            offsetX = Mathf.Sin(Time.time * swaySpeed) * swayAmountX * slackWeight;

            // Using Cosine for Y so the horizontal and vertical motions form an organic, oval-like breathing path
            offsetY = Mathf.Cos(Time.time * swaySpeed * 1.5f) * swayAmountY * slackWeight;
        }

        // 4. Establish Bezier Control Point (Layering the sway offsets directly here)
        Vector2 controlPoint = new Vector2(
            startPoint.x + (loopOffsetDirection * slackWeight) + offsetX,
            startPoint.y - (maxLoopDrop * slackWeight) + offsetY
        );

        // 5. Generate the path points
        Vector2[] points = new Vector2[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            float t = (float)i / (vertexCount - 1);
            points[i] = CalculateQuadraticBezierPoint(t, startPoint, controlPoint, endPoint);
        }

        // 6. Build a thick 2D UI Mesh out of the points array
        for (int i = 0; i < vertexCount - 1; i++)
        {
            Vector2 current = points[i];
            Vector2 next = points[i + 1];

            Vector2 forward = (next - current).normalized;
            Vector2 normal = new Vector2(-forward.y, forward.x) * (thickness * 0.5f);

            Vector2 v0 = current - normal;
            Vector2 v1 = current + normal;
            Vector2 v2 = next + normal;
            Vector2 v3 = next - normal;

            int vertexIndex = vh.currentVertCount;

            vh.AddVert(v0, color, new Vector2(0f, 0f));
            vh.AddVert(v1, color, new Vector2(1f, 0f));
            vh.AddVert(v2, color, new Vector2(1f, 1f));
            vh.AddVert(v3, color, new Vector2(0f, 1f));

            vh.AddTriangle(vertexIndex, vertexIndex + 1, vertexIndex + 2);
            vh.AddTriangle(vertexIndex, vertexIndex + 2, vertexIndex + 3);
        }
    }

    private Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
}
