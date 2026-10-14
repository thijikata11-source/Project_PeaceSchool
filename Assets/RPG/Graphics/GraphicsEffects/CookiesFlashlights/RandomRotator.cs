using UnityEngine;

public class RandomRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Base rotation speed multiplier.")]
    public float rotationSpeed = 30f;

    [Tooltip("How often (in seconds) to pick a new random direction.")]
    public float directionChangeInterval = 2f;

    [Tooltip("Randomize speed each interval as well.")]
    public bool randomizeSpeed = true;

    [Tooltip("Minimum speed when randomizing.")]
    public float minSpeed = 10f;

    [Tooltip("Maximum speed when randomizing.")]
    public float maxSpeed = 100f;

    private Vector3 currentDirection;
    private float timer;

    void Start()
    {
        PickNewDirection();
    }

    void Update()
    {
        // Rotate object
        transform.Rotate(currentDirection * rotationSpeed * Time.deltaTime, Space.Self);

        // Timer for direction change
        timer += Time.deltaTime;
        if (timer >= directionChangeInterval)
        {
            PickNewDirection();
            timer = 0f;
        }
    }

    void PickNewDirection()
    {
        // Random unit vector for direction
        currentDirection = Random.onUnitSphere;

        // Optionally randomize speed
        if (randomizeSpeed)
        {
            rotationSpeed = Random.Range(minSpeed, maxSpeed);
        }
    }
}
