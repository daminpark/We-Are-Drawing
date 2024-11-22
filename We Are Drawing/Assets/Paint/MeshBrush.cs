using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class MeshBrush : MonoBehaviour
{
    [Header("Hand Tracking")]
    public OVRHand hand;
    public float pinchThreshold = 0.8f;
    public float minDistance = 0.01f; // Minimum distance between points to consider

    [Header("Brush Settings")]
    public Material brushMaterial; // Assign a material in the Inspector
    public Color brushColor = Color.white;

    private bool isDrawing = false;
    private List<Vector3> drawnPoints = new List<Vector3>();
    private GameObject currentMeshObject;
    private Mesh currentMesh;

    private Transform indexTip;

    // Plane for projection
    private Plane drawingPlane;
    private Vector3 planeNormal;
    private Vector3 planeOrigin;

    private void Start()
    {
        // Initialize fingertip transform
        StartCoroutine(InitializeFingertipTransform());
    }

    private IEnumerator InitializeFingertipTransform()
    {
        if (hand != null)
        {
            var skeleton = hand.GetComponent<OVRSkeleton>();
            while (skeleton == null || !skeleton.IsInitialized)
            {
                yield return null;
                skeleton = hand.GetComponent<OVRSkeleton>();
            }

            indexTip = GetBoneTransform(skeleton, OVRSkeleton.BoneId.Hand_IndexTip);
        }
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
        if (hand == null || !hand.IsTracked || indexTip == null)
            return;

        float pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

        if (pinchStrength >= pinchThreshold)
        {
            if (!isDrawing)
            {
                StartDrawing();
            }
            else
            {
                CollectPoint();
            }
        }
        else
        {
            if (isDrawing)
            {
                FinishDrawing();
            }
        }
    }

    private void StartDrawing()
    {
        isDrawing = true;
        drawnPoints.Clear();

        // Create a new GameObject for the mesh
        currentMeshObject = new GameObject("Drawn Mesh");
        currentMeshObject.transform.position = Vector3.zero;
        currentMeshObject.transform.rotation = Quaternion.identity;

        MeshRenderer meshRenderer = currentMeshObject.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = currentMeshObject.AddComponent<MeshFilter>();

        // Create a new material instance
        Material materialInstance = new Material(brushMaterial);
        materialInstance.color = brushColor;
        meshRenderer.material = materialInstance;

        currentMesh = new Mesh();
        meshFilter.mesh = currentMesh;

        CollectPoint(); // Collect the starting point

        // Define the drawing plane based on initial drawing direction
        planeOrigin = indexTip.position;
        planeNormal = hand.PointerPose.forward;
        drawingPlane = new Plane(planeNormal, planeOrigin);
    }

    private void CollectPoint()
    {
        Vector3 point = indexTip.position;

        // Only add the point if it's far enough from the last point
        if (drawnPoints.Count == 0 || Vector3.Distance(point, drawnPoints[drawnPoints.Count - 1]) >= minDistance)
        {
            drawnPoints.Add(point);
            UpdateMesh();
        }
    }

    private void FinishDrawing()
    {
        isDrawing = false;
        // Optionally, perform final mesh adjustments here
    }

    private void UpdateMesh()
    {
        if (drawnPoints.Count < 3)
            return;

        // Project points onto the plane
        List<Vector3> projectedPoints3D = new List<Vector3>();
        List<Vector2> projectedPoints2D = new List<Vector2>();

        // Create a coordinate system on the plane
        Vector3 planeRight = Vector3.Cross(planeNormal, Vector3.up);
        if (planeRight.sqrMagnitude < 0.001f)
            planeRight = Vector3.Cross(planeNormal, Vector3.forward);

        planeRight.Normalize();
        Vector3 planeUp = Vector3.Cross(planeNormal, planeRight);

        foreach (var point in drawnPoints)
        {
            Vector3 localPoint = point - planeOrigin;
            float x = Vector3.Dot(localPoint, planeRight);
            float y = Vector3.Dot(localPoint, planeUp);
            projectedPoints2D.Add(new Vector2(x, y));
            projectedPoints3D.Add(point);
        }

        // Sort points to form a polygon
        List<int> sortedIndices = SortPoints(projectedPoints2D);

        // Triangulate the polygon
        List<int> triangles = EarClippingTriangulation(projectedPoints2D, sortedIndices);

        // Build the mesh
        currentMesh.Clear();
        Vector3[] vertices = new Vector3[sortedIndices.Count];
        for (int i = 0; i < sortedIndices.Count; i++)
        {
            vertices[i] = projectedPoints3D[sortedIndices[i]];
        }

        currentMesh.vertices = vertices;
        currentMesh.triangles = triangles.ToArray();
        currentMesh.RecalculateNormals();
    }

    private List<int> SortPoints(List<Vector2> points)
    {
        // Compute the centroid
        Vector2 centroid = Vector2.zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        centroid /= points.Count;

        // Sort points based on angle from centroid
        List<int> indices = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            indices.Add(i);
        }

        indices.Sort((a, b) =>
        {
            Vector2 dirA = points[a] - centroid;
            Vector2 dirB = points[b] - centroid;
            float angleA = Mathf.Atan2(dirA.y, dirA.x);
            float angleB = Mathf.Atan2(dirB.y, dirB.x);
            return angleA.CompareTo(angleB);
        });

        return indices;
    }

    private List<int> EarClippingTriangulation(List<Vector2> points, List<int> indices)
    {
        List<int> triangles = new List<int>();
        List<int> polygon = new List<int>(indices);

        while (polygon.Count >= 3)
        {
            bool earFound = false;
            for (int i = 0; i < polygon.Count; i++)
            {
                int i0 = polygon[(i + polygon.Count - 1) % polygon.Count];
                int i1 = polygon[i];
                int i2 = polygon[(i + 1) % polygon.Count];

                if (IsEar(points, polygon, i))
                {
                    triangles.Add(i0);
                    triangles.Add(i1);
                    triangles.Add(i2);
                    polygon.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound)
            {
                // No ear found, possible degenerate polygon
                break;
            }
        }

        return triangles;
    }

    private bool IsEar(List<Vector2> points, List<int> polygon, int i)
    {
        int prev = (i + polygon.Count - 1) % polygon.Count;
        int next = (i + 1) % polygon.Count;

        int i0 = polygon[prev];
        int i1 = polygon[i];
        int i2 = polygon[next];

        Vector2 a = points[i0];
        Vector2 b = points[i1];
        Vector2 c = points[i2];

        if (Area(a, b, c) >= 0)
            return false;

        // Check if any other point is inside the triangle
        for (int j = 0; j < polygon.Count; j++)
        {
            if (j == prev || j == i || j == next)
                continue;

            int idx = polygon[j];
            Vector2 p = points[idx];

            if (PointInTriangle(p, a, b, c))
                return false;
        }

        return true;
    }

    private float Area(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float area = Area(a, b, c);
        float area1 = Area(p, b, c);
        float area2 = Area(a, p, c);
        float area3 = Area(a, b, p);

        return Mathf.Abs(area - (area1 + area2 + area3)) < 0.01f;
    }
}
