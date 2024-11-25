using UnityEngine;
using System.Collections;

public class FollowHeadMovement : MonoBehaviour
{
    public Transform headTransform; // Assign your head transform here
    public float smoothSpeed = 0.125f; // Adjust the smoothness
    public float minDistance = 5.0f; // Distance from head in x/z plane

    private Vector3 targetPosition;

    void Start()
    {
        StartCoroutine(UpdateTextPosition());
    }

    IEnumerator UpdateTextPosition()
    {
        while (true)
        {
            yield return new WaitForSeconds(2.0f); // Update more frequently

            if (headTransform != null)
            {
                // Calculate new target position at exactly minDistance from the head
                Vector3 headPosition = headTransform.position;
                Vector3 direction = headTransform.forward; // Assuming the head's forward direction
                Vector3 potentialTargetPosition = new Vector3(
                    headPosition.x + direction.x * minDistance,
                    headPosition.y, // Maintain horizontal position
                    headPosition.z + direction.z * minDistance
                );

                targetPosition = potentialTargetPosition;
            }
        }
    }

    void Update()
    {
        if (headTransform != null)
        {
            // Smoothly interpolate the position
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);
            // Ensure the text remains horizontal
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.x = 0;
            eulerAngles.z = 0;
            transform.eulerAngles = eulerAngles;
        }
    }
}
