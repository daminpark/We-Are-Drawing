using UnityEngine;

public class MirrorScript : MonoBehaviour
{
    // Public references to the CenterEyeAnchor and MirrorCamera
    public Transform CenterEyeAnchor; // Assign the CenterEyeAnchor in the Inspector
    public Camera MirrorCamera; // Assign the MirrorCamera in the Inspector

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Optionally, add any setup logic here if needed
    }

    // Update is called once per frame
    void Update()
    {
        // Ensure CenterEyeAnchor and MirrorCamera are assigned
        if (CenterEyeAnchor != null && MirrorCamera != null)
        {
            // Create a mirrored position for the camera along the X-axis
            Vector3 MirrorCameraPos = CenterEyeAnchor.position;
            MirrorCameraPos.x = -MirrorCameraPos.x; // Flip across the X-axis if it's a flat mirror

            // Set the MirrorCamera position and rotation
            MirrorCamera.transform.position = MirrorCameraPos;
            MirrorCamera.transform.rotation = CenterEyeAnchor.rotation;
        }
        else
        {
            Debug.LogWarning("CenterEyeAnchor or MirrorCamera is not assigned.");
        }
    }
}
