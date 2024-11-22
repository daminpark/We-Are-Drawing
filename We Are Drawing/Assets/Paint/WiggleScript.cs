using UnityEngine;

public class WiggleScript : MonoBehaviour
{
    [Header("Position Wiggle Settings")]
    public bool enablePositionWiggle = true;
    public float positionWiggleScale = 0.5f; // Amplitude of position wiggle
    public float positionWiggleSpeed = 1.0f; // Speed of position wiggle

    [Header("Rotation Wiggle Settings")]
    public bool enableRotationWiggle = true;
    public float rotationWiggleScale = 10.0f; // Amplitude of rotation wiggle (in degrees)
    public float rotationWiggleSpeed = 1.0f; // Speed of rotation wiggle

    [Header("Random Seed")]
    public bool useRandomSeed = true; // Enable/disable random seed
    public int seed = 0; // Seed value for consistent randomness

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 positionRandomOffset;
    private Vector3 rotationRandomOffset;

    void Start()
    {
        // Save the initial position and rotation of the object
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // Set up random offsets based on seed
        if (useRandomSeed)
        {
            Random.InitState(seed);
        }
        positionRandomOffset = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));
        rotationRandomOffset = new Vector3(Random.Range(0f, 100f), Random.Range(0f, 100f), Random.Range(0f, 100f));
    }

    void Update()
    {
        // Wiggle position
        if (enablePositionWiggle)
        {
            float offsetX = Mathf.Sin(Time.time * positionWiggleSpeed + positionRandomOffset.x) * positionWiggleScale;
            float offsetY = Mathf.Cos(Time.time * positionWiggleSpeed * 1.1f + positionRandomOffset.y) * positionWiggleScale;
            float offsetZ = Mathf.Sin(Time.time * positionWiggleSpeed * 0.9f + positionRandomOffset.z) * positionWiggleScale;
            transform.position = initialPosition + new Vector3(offsetX, offsetY, offsetZ);
        }

        // Wiggle rotation
        if (enableRotationWiggle)
        {
            float rotX = Mathf.Sin(Time.time * rotationWiggleSpeed + rotationRandomOffset.x) * rotationWiggleScale;
            float rotY = Mathf.Cos(Time.time * rotationWiggleSpeed * 1.2f + rotationRandomOffset.y) * rotationWiggleScale;
            float rotZ = Mathf.Sin(Time.time * rotationWiggleSpeed * 0.8f + rotationRandomOffset.z) * rotationWiggleScale;
            transform.rotation = initialRotation * Quaternion.Euler(rotX, rotY, rotZ);
        }
    }
}
