using UnityEngine;

public class Paintbucket : MonoBehaviour
{
    [Header("Paintbucket Settings")]
    public Color bucketColor = Color.magenta; // Default color, can be set in Inspector

    private BrushTip brushTip;

    private void Start()
    {
        // Get the BrushTip script from the scene
#if UNITY_2023_1_OR_NEWER
        brushTip = Object.FindAnyObjectByType<BrushTip>();
#else
        brushTip = FindObjectOfType<BrushTip>();
#endif
        if (brushTip == null)
        {
            Debug.LogWarning("BrushTip script not found in the scene.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the collider belongs to the fingertip indicator
        if (other.CompareTag("Player"))
        {
            if (brushTip != null)
            {
                // Determine which hand is interacting based on the fingertip indicator's name
                if (other.gameObject.name.Contains("Left"))
                {
                    brushTip.SetBrushColor(BrushTip.HandSide.Left, bucketColor);
                }
                else if (other.gameObject.name.Contains("Right"))
                {
                    brushTip.SetBrushColor(BrushTip.HandSide.Right, bucketColor);
                }
            }
            else
            {
                Debug.LogWarning("BrushTip script not found in the scene.");
            }
        }
    }
}
