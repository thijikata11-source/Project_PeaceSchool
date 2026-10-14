using UnityEngine;

public class SpinnerObject : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("Rotation Settings")]
    [Tooltip("Select which axis the object should rotate around.")]
    public RotationAxis axis = RotationAxis.Y;

    [Tooltip("Rotation speed in degrees per second.")]
    public int speed = 100;

    void Update()
    {
        Vector3 rotationVector = Vector3.zero;

        switch (axis)
        {
            case RotationAxis.X:
                rotationVector = Vector3.right;
                break;
            case RotationAxis.Y:
                rotationVector = Vector3.up;
                break;
            case RotationAxis.Z:
                rotationVector = Vector3.forward;
                break;
        }

        transform.Rotate(rotationVector * speed * Time.deltaTime);
    }
}
