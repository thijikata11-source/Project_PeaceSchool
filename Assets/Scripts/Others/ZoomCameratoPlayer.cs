using UnityEngine;
using UnityEngine.UI;

public class ZoomCameratoPlayer : MonoBehaviour
{
    [Header("Camera Target")]
    public Camera targetCamera;   // Assign in Inspector

    public GameObject targetObjectPivot;

    [Header("Slider Control")]
    public Slider zoomSlider;     // Assign in Inspector

    [Header("Zoom Settings")]
    public float minZoomSize = 3.16f;
    public float maxZoomSize = 1.27f;

    [Header("Rotation Settings")]
    public float minRotationX = 0f;
    public float maxRotationX = -16.86f;

    [Header("Pivot Rotation Settings")]
    public float PminRotationX = 30f;
    public float PmaxRotationX = 20f;


    void Start()
    {
        if (zoomSlider != null)
        {
            // Ensure slider normalized range (0–1)
            zoomSlider.minValue = 0f;
            zoomSlider.maxValue = 1f;
            zoomSlider.onValueChanged.AddListener(OnZoomSliderChanged);
        }
    }

    void OnZoomSliderChanged(float value)
    {
        if (targetCamera == null) return;

        // Interpolate zoom size
        float zoomSize = Mathf.Lerp(minZoomSize, maxZoomSize, value);
        targetCamera.orthographicSize = zoomSize;

        // Interpolate rotation X
        float rotationX = Mathf.Lerp(minRotationX, maxRotationX, value);
        Vector3 currentEuler = targetCamera.transform.eulerAngles;
        currentEuler.x = rotationX;
        targetCamera.transform.eulerAngles = currentEuler;


        // Interpolate rotation X
        float pRotationX = Mathf.Lerp(PminRotationX, PmaxRotationX, value);
        Vector3 pCurrentEuler = targetObjectPivot.transform.eulerAngles;
        pCurrentEuler.x = pRotationX;
        targetObjectPivot.transform.eulerAngles = pCurrentEuler;
    }
}
