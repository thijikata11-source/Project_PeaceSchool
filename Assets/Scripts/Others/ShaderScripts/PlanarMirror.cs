using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PlanarMirror : MonoBehaviour
{
    [Header("Scene")]
    public Camera mainCamera;
    public Camera mirrorCamera;
    public Transform mirrorPlane; // The quad/plane transform

    [Header("Quality")]
    public RenderTexture reflectionRT;
    public LayerMask reflectionCulling = ~0;
    public bool copyPostFX = true;
    public bool updateEveryFrame = true;

    Vector4 _clipPlane; // world-space plane for oblique projection

    void Awake()
    {
        if (mirrorCamera != null)
        {
            mirrorCamera.enabled = false; // we manually render
            if (reflectionRT != null) mirrorCamera.targetTexture = reflectionRT;
        }
    }

    void OnEnable()
    {
        UpdateOnce();
    }

    void LateUpdate()
    {
        if (updateEveryFrame) RenderReflection();
    }

    public void UpdateOnce()
    {
        if (!updateEveryFrame) RenderReflection();
    }

    void RenderReflection()
    {
        if (mainCamera == null || mirrorCamera == null || mirrorPlane == null) return;
        if (reflectionRT == null) return;

        // 1) Compute mirror plane (world)
        var planeNormal = mirrorPlane.forward;          // assume forward is mirror normal
        var planePoint = mirrorPlane.position;         // a point on the plane
        _clipPlane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z,
                                 -Vector3.Dot(planeNormal, planePoint));

        // 2) Mirror camera transform (reflect main camera across plane)
        MirrorCameraTransform(mainCamera.transform, mirrorCamera.transform, planePoint, planeNormal);

        // 3) Match camera settings
        mirrorCamera.fieldOfView = mainCamera.fieldOfView;
        mirrorCamera.orthographic = mainCamera.orthographic;
        mirrorCamera.orthographicSize = mainCamera.orthographicSize;
        mirrorCamera.aspect = mainCamera.aspect;
        mirrorCamera.cullingMask = reflectionCulling;
        mirrorCamera.clearFlags = CameraClearFlags.Skybox;
        mirrorCamera.allowHDR = mainCamera.allowHDR;
        mirrorCamera.allowMSAA = mainCamera.allowMSAA;

        // 4) Oblique near-clip to prevent rendering behind the mirror
        var viewMatrix = mirrorCamera.worldToCameraMatrix;
        var clipPlaneCameraSpace = CameraSpacePlane(viewMatrix, _clipPlane);
        var proj = mainCamera.projectionMatrix; // start from main to keep FOV/PP compatibility
        proj = CalculateObliqueMatrix(proj, clipPlaneCameraSpace);
        mirrorCamera.projectionMatrix = proj;

        // 5) Optionally copy post-processing/fx settings (renderer-specific)
        // For URP: ensure both cams share same renderer or custom PP pipeline.

        // 6) Render
        mirrorCamera.Render();

        // 7) Assign RT to material (if not already)
        var mr = GetComponent<MeshRenderer>();
        if (mr && mr.sharedMaterial)
            mr.sharedMaterial.SetTexture("_MainTex", reflectionRT); // or "_BaseMap"/"_EmissionMap" by shader
    }

    static void MirrorCameraTransform(Transform src, Transform dst, Vector3 planePoint, Vector3 planeNormal)
    {
        // Reflect position across plane
        Vector3 toPoint = src.position - planePoint;
        Vector3 reflectedPos = src.position - 2f * Vector3.Dot(toPoint, planeNormal) * planeNormal;

        // Reflect forward and up vectors
        Vector3 fwd = src.forward - 2f * Vector3.Dot(src.forward, planeNormal) * planeNormal;
        Vector3 up = src.up - 2f * Vector3.Dot(src.up, planeNormal) * planeNormal;

        dst.position = reflectedPos;
        dst.rotation = Quaternion.LookRotation(fwd, up);
    }

    static Vector4 CameraSpacePlane(Matrix4x4 viewMatrix, Vector4 worldPlane)
    {
        // Transform plane into camera space: (M^-1)^T * plane
        Matrix4x4 invView = viewMatrix.inverse;
        Vector3 n = new Vector3(worldPlane.x, worldPlane.y, worldPlane.z);
        float d = worldPlane.w;
        Vector3 p0 = -n * d; // point on plane

        Vector3 cP0 = invView.MultiplyPoint(p0);
        Vector3 cN = invView.MultiplyVector(n).normalized;
        float cD = -Vector3.Dot(cP0, cN);
        return new Vector4(cN.x, cN.y, cN.z, cD);
    }

    static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 proj, Vector4 clipPlane)
    {
        // Based on Eric Lengyel’s oblique near-plane clipping
        Vector4 q;
        q.x = (Mathf.Sign(clipPlane.x) + proj[8]) / proj[0];
        q.y = (Mathf.Sign(clipPlane.y) + proj[9]) / proj[5];
        q.z = -1.0f;
        q.w = (1.0f + proj[10]) / proj[14];

        Vector4 c = clipPlane * (2.0f / Vector4.Dot(clipPlane, q));
        proj[2] = c.x;
        proj[6] = c.y;
        proj[10] = c.z + 1.0f;
        proj[14] = c.w;
        return proj;
    }
}
