using UnityEngine;

public class CameraOcclusionColor : MonoBehaviour
{
    public Camera cam;              // Assign your main camera
    public Color blockedColor = Color.black;
    private SkinnedMeshRenderer rend;
    private Color originalColor;

    void Start()
    {
        rend = GetComponent<SkinnedMeshRenderer>();
        originalColor = rend.material.GetColor("_Color");

        if (cam == null)
            cam = Camera.main;
    }

    void Update()
    {
        Vector3 dir = transform.position - cam.transform.position;
        Ray ray = new Ray(cam.transform.position, dir.normalized);
        float dist = dir.magnitude;

        if (Physics.Raycast(ray, out RaycastHit hit, dist))
        {
            // If the hit object is not THIS object, it's blocked
            if (hit.transform != transform)
                rend.material.SetColor("_Color", blockedColor);
            else
                rend.material.SetColor("_Color", originalColor);
        }
        else
        {
            // Nothing in between → visible
            rend.material.SetColor("_Color", originalColor);
        }
    }
}
