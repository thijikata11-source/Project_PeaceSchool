using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraDistortLoop : MonoBehaviour
{
    public enum DistortionMode
    {
        TransformWobble,
        ProjectionMatrix,
        FieldOfViewPulse,
        ShaderEffect   // NEW MODE
    }

    [Header("General Settings")]
    public DistortionMode mode = DistortionMode.TransformWobble;
    public float frequency = 2f;

    [Header("Transform Wobble Settings")]
    public float positionAmplitude = 0.5f;
    public float rotationAmplitude = 5f;

    [Header("Projection Matrix Settings")]
    public float distortionStrength = 0.05f;

    [Header("Field of View Pulse Settings")]
    public float baseFOV = 60f;
    public float fovAmplitude = 10f;

    [Header("Shader Effect Settings")]
    public Shader distortionShader;   // Drag your shader here
    public float shaderStrength = 0.5f;
    private Material distortionMaterial;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Camera cam;
    private Matrix4x4 originalProjection;

    void Start()
    {
        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            originalProjection = cam.projectionMatrix;
        }

        if (distortionShader != null)
        {
            distortionMaterial = new Material(distortionShader);
        }
    }

    void Update()
    {
        float t = Time.time * frequency;

        switch (mode)
        {
            case DistortionMode.TransformWobble:
                ApplyTransformWobble(t);
                break;

            case DistortionMode.ProjectionMatrix:
                ApplyProjectionDistortion(t);
                break;

            case DistortionMode.FieldOfViewPulse:
                ApplyFOVPulse(t);
                break;

            case DistortionMode.ShaderEffect:
                // Shader is applied in OnRenderImage, just animate property here
                if (distortionMaterial != null)
                {
                    distortionMaterial.SetFloat("_DistortAmount", Mathf.Sin(t) * shaderStrength);
                }
                break;
        }
    }

    void ApplyTransformWobble(float t)
    {
        transform.localPosition = initialPosition + new Vector3(
            Mathf.Sin(t) * positionAmplitude,
            Mathf.Cos(t) * positionAmplitude,
            0f
        );

        transform.localRotation = initialRotation * Quaternion.Euler(
            Mathf.Sin(t) * rotationAmplitude,
            Mathf.Cos(t) * rotationAmplitude,
            0f
        );
    }

    void ApplyProjectionDistortion(float t)
    {
        if (cam == null) return;

        Matrix4x4 mat = originalProjection;
        float skew = Mathf.Sin(t) * distortionStrength;

        mat[0, 1] = skew;   // horizontal skew
        mat[1, 0] = -skew;  // vertical skew

        cam.projectionMatrix = mat;
    }

    void ApplyFOVPulse(float t)
    {
        if (cam == null) return;

        cam.fieldOfView = baseFOV + Mathf.Sin(t) * fovAmplitude;
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (mode == DistortionMode.ShaderEffect && distortionMaterial != null)
        {
            Graphics.Blit(src, dest, distortionMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    void OnDisable()
    {
        if (cam != null)
        {
            cam.projectionMatrix = originalProjection;
            cam.fieldOfView = baseFOV;
        }
        transform.localPosition = initialPosition;
        transform.localRotation = initialRotation;
    }
}
