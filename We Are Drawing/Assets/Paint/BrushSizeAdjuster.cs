using UnityEngine;
using System.Collections;

public class BrushSizeAdjuster : MonoBehaviour
{
    [Header("Brush Size Adjustment")]
    public OVRHand leftHand;
    public OVRHand rightHand;
    public float pinchThreshold = 0.8f;
    public float handsCloseThreshold = 0.1f;
    public Material indicatorMaterial;
    public Color indicatorColor = Color.white;

    private GameObject brushSizeIndicator;
    private bool isAdjustingBrushSize = false;
    private float initialHandsDistance;
    private float brushSize = 0.01f; // Default brush size
    private float initialBrushSize;

    private Transform leftThumbTip;
    private Transform leftIndexTip;
    private Transform rightThumbTip;
    private Transform rightIndexTip;

    private void Start()
    {
        CreateBrushSizeIndicator();
        StartCoroutine(InitializeBones());
    }

    private IEnumerator InitializeBones()
    {
        // Initialize left hand bones
        var leftSkeleton = leftHand.GetComponent<OVRSkeleton>();
        while (leftSkeleton == null || !leftSkeleton.IsInitialized)
        {
            yield return null;
            leftSkeleton = leftHand.GetComponent<OVRSkeleton>();
        }

        leftThumbTip = GetBoneTransform(leftSkeleton, OVRSkeleton.BoneId.Hand_ThumbTip);
        leftIndexTip = GetBoneTransform(leftSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);

        // Initialize right hand bones
        var rightSkeleton = rightHand.GetComponent<OVRSkeleton>();
        while (rightSkeleton == null || !rightSkeleton.IsInitialized)
        {
            yield return null;
            rightSkeleton = rightHand.GetComponent<OVRSkeleton>();
        }

        rightThumbTip = GetBoneTransform(rightSkeleton, OVRSkeleton.BoneId.Hand_ThumbTip);
        rightIndexTip = GetBoneTransform(rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip);
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

    private void Update()
    {
        DetectBrushSizeAdjustment();

        if (isAdjustingBrushSize)
        {
            UpdateBrushSizeAdjustment();
        }
    }

    private void CreateBrushSizeIndicator()
    {
        brushSizeIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        brushSizeIndicator.transform.localScale = Vector3.one * brushSize;
        var renderer = brushSizeIndicator.GetComponent<Renderer>();
        renderer.material = new Material(indicatorMaterial);
        Color color = indicatorColor;
        color.a = 0.5f;
        renderer.material.color = color;
        brushSizeIndicator.SetActive(false);
    }

    private void DetectBrushSizeAdjustment()
    {
        if (!leftHand.IsTracked || !rightHand.IsTracked)
            return;

        if (leftThumbTip == null || leftIndexTip == null || rightThumbTip == null || rightIndexTip == null)
            return;

        Vector3 leftPinchPosition = GetPinchPosition(leftThumbTip, leftIndexTip, leftHand);
        Vector3 rightPinchPosition = GetPinchPosition(rightThumbTip, rightIndexTip, rightHand);
        float handsDistance = Vector3.Distance(leftPinchPosition, rightPinchPosition);

        if (!isAdjustingBrushSize)
        {
            if (handsDistance < handsCloseThreshold)
            {
                ShowBrushSizeIndicator(leftPinchPosition, rightPinchPosition);

                float leftPinchStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                float rightPinchStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

                if (leftPinchStrength >= pinchThreshold && rightPinchStrength >= pinchThreshold)
                {
                    StartBrushSizeAdjustment(leftPinchPosition, rightPinchPosition);
                }
            }
            else
            {
                if (brushSizeIndicator.activeSelf)
                    brushSizeIndicator.SetActive(false);
            }
        }
    }

    private void ShowBrushSizeIndicator(Vector3 leftPinchPosition, Vector3 rightPinchPosition)
    {
        Vector3 midpoint = (leftPinchPosition + rightPinchPosition) / 2f;
        brushSizeIndicator.transform.position = midpoint;
        brushSizeIndicator.transform.localScale = Vector3.one * brushSize;
        brushSizeIndicator.SetActive(true);
    }

    private void StartBrushSizeAdjustment(Vector3 leftPinchPosition, Vector3 rightPinchPosition)
    {
        isAdjustingBrushSize = true;
        initialHandsDistance = Vector3.Distance(leftPinchPosition, rightPinchPosition);
        initialBrushSize = brushSize;
    }

    private void UpdateBrushSizeAdjustment()
    {
        if (leftThumbTip == null || leftIndexTip == null || rightThumbTip == null || rightIndexTip == null)
            return;

        float leftPinchStrength = leftHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);
        float rightPinchStrength = rightHand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (leftPinchStrength < pinchThreshold || rightPinchStrength < pinchThreshold)
        {
            isAdjustingBrushSize = false;
            brushSizeIndicator.SetActive(false);
            return;
        }

        Vector3 leftPinchPosition = GetPinchPosition(leftThumbTip, leftIndexTip, leftHand);
        Vector3 rightPinchPosition = GetPinchPosition(rightThumbTip, rightIndexTip, rightHand);
        float currentHandsDistance = Vector3.Distance(leftPinchPosition, rightPinchPosition);
        float distanceDelta = currentHandsDistance - initialHandsDistance;

        brushSize = Mathf.Clamp(initialBrushSize + distanceDelta * 2.0f, 0.001f, 0.1f);

        Vector3 midpoint = (leftPinchPosition + rightPinchPosition) / 2f;
        brushSizeIndicator.transform.position = midpoint;
        brushSizeIndicator.transform.localScale = Vector3.one * brushSize;
    }

    public float GetBrushSize()
    {
        return brushSize;
    }

    public bool IsAdjustingBrushSize()
    {
        return isAdjustingBrushSize;
    }

    private Vector3 GetPinchPosition(Transform thumbTip, Transform indexTip, OVRHand hand)
    {
        if (thumbTip == null || indexTip == null)
        {
            return hand.transform.position;
        }

        return (thumbTip.position + indexTip.position) / 2f;
    }

    private void OnDestroy()
    {
        if (brushSizeIndicator != null)
        {
            Destroy(brushSizeIndicator);
        }
    }
}
