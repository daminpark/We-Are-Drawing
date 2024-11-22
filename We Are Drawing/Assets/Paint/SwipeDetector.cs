using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
    public OVRHand hand; // Hand used for swipe detection
    public float swipeThreshold = 1.5f;
    public float swipeCooldown = 1f;

    private StrokeManager strokeManager;
    private Vector3 lastHandPosition;
    private float lastSwipeTime = 0;

    private void Start()
    {
#if UNITY_2023_1_OR_NEWER
        strokeManager = FindAnyObjectByType<StrokeManager>();
#else
        strokeManager = FindObjectOfType<StrokeManager>();
#endif
        lastHandPosition = hand.transform.position;
    }

    private void Update()
    {
        DetectSwipe();
    }

    private void DetectSwipe()
    {
        if (Time.time - lastSwipeTime < swipeCooldown)
            return;

        Vector3 currentHandPosition = hand.transform.position;
        Vector3 handVelocity = (currentHandPosition - lastHandPosition) / Time.deltaTime;

        if (handVelocity.x > swipeThreshold)
        {
            strokeManager.UndoLastStroke();
            lastSwipeTime = Time.time;
        }

        lastHandPosition = currentHandPosition;
    }
}
