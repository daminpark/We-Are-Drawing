using UnityEngine;
using System.Collections;

public class BrushTip : MonoBehaviour
{
    [Header("Hand Tracking")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public float pinchThreshold = 0.8f;

    [Header("Brush Colors")]
    public Color leftHandColor = Color.red;
    public Color rightHandColor = Color.blue;

    [Header("Painting Settings")]
    public bool paintingAllowed = true; // Added option to allow or prohibit painting

    // Reference to the BrushSizeAdjuster
    private BrushSizeAdjuster brushSizeAdjuster;

    // Reference to the StrokeManager
    private StrokeManager strokeManager;

    // Fingertip indicators
    private GameObject leftIndexFingerTipIndicator;
    private GameObject rightIndexFingerTipIndicator;

    // Fingertip transforms
    private Transform leftIndexTip;
    private Transform rightIndexTip;

    private void Start()
    {
        // Find required components
#if UNITY_2023_1_OR_NEWER
        brushSizeAdjuster = FindAnyObjectByType<BrushSizeAdjuster>();
        strokeManager = FindAnyObjectByType<StrokeManager>();
#else
        brushSizeAdjuster = FindObjectOfType<BrushSizeAdjuster>();
        strokeManager = FindObjectOfType<StrokeManager>();
#endif

        StartCoroutine(InitializeFingertipTransforms());
    }

    private void Update()
    {
        // Do not allow drawing if brush size is being adjusted
        if (brushSizeAdjuster != null && brushSizeAdjuster.IsAdjustingBrushSize())
            return;

        // Update fingertip indicators positions
        UpdateFingertipIndicators();

        // Update visibility of fingertip indicators based on paintingAllowed
        UpdateFingertipIndicatorVisibility();

        // Only handle drawing if painting is allowed
        if (paintingAllowed)
        {
            // Handle drawing with the left hand
            HandleDrawing(leftHand, leftIndexTip, HandSide.Left, leftHandColor);

            // Handle drawing with the right hand
            HandleDrawing(rightHand, rightIndexTip, HandSide.Right, rightHandColor);
        }
        else
        {
            // Ensure any ongoing strokes are ended
            if (strokeManager != null)
            {
                strokeManager.EndStroke(HandSide.Left);
                strokeManager.EndStroke(HandSide.Right);
            }
        }
    }

    private void HandleDrawing(OVRHand hand, Transform indexTip, HandSide handSide, Color handColor)
    {
        if (hand == null || !hand.IsTracked)
            return;

        // Detect index finger pinch
        float pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (pinchStrength >= pinchThreshold)
        {
            Vector3 pinchPosition = GetPinchPosition(hand, indexTip);
            strokeManager.AddPointToStroke(handSide, pinchPosition, handColor, hand.transform.rotation);
        }
        else
        {
            strokeManager.EndStroke(handSide);
        }
    }

    private Vector3 GetPinchPosition(OVRHand hand, Transform indexTip)
    {
        if (indexTip == null)
            return hand.transform.position;

        var skeleton = hand.GetComponent<OVRSkeleton>();
        if (skeleton == null || skeleton.Bones.Count == 0)
            return hand.transform.position;

        var thumbTipTransform = GetBoneTransform(skeleton, OVRSkeleton.BoneId.Hand_ThumbTip);
        if (thumbTipTransform == null)
            return indexTip.position;

        var thumbTipPosition = thumbTipTransform.position;
        var indexFingerTip = indexTip.position;

        return (thumbTipPosition + indexFingerTip) / 2f;
    }

    public void SetBrushColor(HandSide handSide, Color newColor)
    {
        if (handSide == HandSide.Left)
        {
            leftHandColor = newColor;
            UpdateFingertipIndicatorColor(HandSide.Left, newColor);
        }
        else if (handSide == HandSide.Right)
        {
            rightHandColor = newColor;
            UpdateFingertipIndicatorColor(HandSide.Right, newColor);
        }
    }

    private void UpdateFingertipIndicatorColor(HandSide handSide, Color newColor)
    {
        if (handSide == HandSide.Left && leftIndexFingerTipIndicator != null)
        {
            leftIndexFingerTipIndicator.GetComponent<Renderer>().material.color = newColor;
        }
        else if (handSide == HandSide.Right && rightIndexFingerTipIndicator != null)
        {
            rightIndexFingerTipIndicator.GetComponent<Renderer>().material.color = newColor;
        }
    }

    private IEnumerator InitializeFingertipTransforms()
    {
        // Initialize left hand fingertip transform
        if (leftHand != null)
        {
            var leftSkeleton = leftHand.GetComponent<OVRSkeleton>();
            while (leftSkeleton == null || !leftSkeleton.IsInitialized)
            {
                yield return null;
                leftSkeleton = leftHand.GetComponent<OVRSkeleton>();
            }
            leftIndexTip = GetBoneTransform(leftSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        }

        // Initialize right hand fingertip transform
        if (rightHand != null)
        {
            var rightSkeleton = rightHand.GetComponent<OVRSkeleton>();
            while (rightSkeleton == null || !rightSkeleton.IsInitialized)
            {
                yield return null;
                rightSkeleton = rightHand.GetComponent<OVRSkeleton>();
            }
            rightIndexTip = GetBoneTransform(rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        }

        // Create fingertip indicators after initialization
        CreateFingertipIndicators();
    }

    private Transform GetBoneTransform(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId)
    {
        foreach (var bone in skeleton.Bones)
        {
            if (bone.Id == boneId)
                return bone.Transform;
        }
        return null;
    }

    private void CreateFingertipIndicators()
    {
        // Left hand index fingertip indicator
        if (leftIndexTip != null)
        {
            leftIndexFingerTipIndicator = CreateFingertipIndicator("LeftFingertipIndicator", leftHandColor);
        }

        // Right hand index fingertip indicator
        if (rightIndexTip != null)
        {
            rightIndexFingerTipIndicator = CreateFingertipIndicator("RightFingertipIndicator", rightHandColor);
        }
    }

    private GameObject CreateFingertipIndicator(string name, Color color)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.localScale = Vector3.one * 0.005f;
        indicator.name = name;
        Material indicatorMaterial = new Material(Shader.Find("Unlit/Color"));
        indicatorMaterial.color = color;
        indicator.GetComponent<Renderer>().material = indicatorMaterial;
        indicator.tag = "Player"; // Tagged as "Player" to work with LevelSelector
        // Add a small collider
        SphereCollider collider = indicator.GetComponent<SphereCollider>();
        collider.isTrigger = false; // Ensure collider is not a trigger
        collider.radius = 0.0025f;
        // Add Rigidbody
        Rigidbody rb = indicator.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        return indicator;
    }

    private void UpdateFingertipIndicators()
    {
        if (leftIndexTip != null && leftIndexFingerTipIndicator != null)
        {
            leftIndexFingerTipIndicator.transform.position = leftIndexTip.position;
        }
        if (rightIndexTip != null && rightIndexFingerTipIndicator != null)
        {
            rightIndexFingerTipIndicator.transform.position = rightIndexTip.position;
        }
    }

    private void UpdateFingertipIndicatorVisibility()
    {
        // Hide or show the fingertip indicators based on paintingAllowed
        if (leftIndexFingerTipIndicator != null)
        {
            var renderer = leftIndexFingerTipIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = paintingAllowed;
            }
        }

        if (rightIndexFingerTipIndicator != null)
        {
            var renderer = rightIndexFingerTipIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = paintingAllowed;
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up fingertip indicators
        if (leftIndexFingerTipIndicator != null)
            Destroy(leftIndexFingerTipIndicator);
        if (rightIndexFingerTipIndicator != null)
            Destroy(rightIndexFingerTipIndicator);
    }

    public enum HandSide
    {
        Left,
        Right
    }
}
