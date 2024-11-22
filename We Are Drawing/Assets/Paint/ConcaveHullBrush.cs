using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ConcaveHullBrush : MonoBehaviour
{
    [Header("Hand Tracking")]
    public OVRHand hand;
    public float pinchThreshold = 0.8f;
    public float minDistance = 0.01f;

    [Header("Brush Settings")]
    public Material brushMaterial;
    public Color brushColor = Color.white;

    private bool isDrawing = false;
    private List<Vector3> drawnPoints = new List<Vector3>();
    private GameObject currentMeshObject;
    private Mesh currentMesh;

    private Transform indexTip;

    private void Start()
    {
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
            if (indexTip == null)
            {
                Debug.LogError("Index Tip Transform not found.");
            }
            else
            {
                Debug.Log("Index Tip Transform initialized.");
            }
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
        Debug.Log("Started Drawing");

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

        // Collect the starting point
        CollectPoint();
    }

    private void CollectPoint()
    {
        Vector3 point = indexTip.position;

        // Only add the point if it's far enough from the last point
        if (drawnPoints.Count == 0 || Vector3.Distance(point, drawnPoints[drawnPoints.Count - 1]) >= minDistance)
        {
            drawnPoints.Add(point);
            UpdateMesh();
            Debug.Log($"Collected Point: {point}");
        }
    }

    private void FinishDrawing()
    {
        isDrawing = false;
        Debug.Log("Finished Drawing");
        // Optionally, perform final mesh adjustments here
    }

    private void UpdateMesh()
    {
        if (drawnPoints.Count < 3)
        {
            Debug.Log("Not enough points to create a mesh.");
            return;
        }

        // Compute the concave hull
        Mesh mesh = GenerateConcaveHullMesh(drawnPoints);

        if (mesh != null)
        {
            currentMesh.vertices = mesh.vertices;
            currentMesh.triangles = mesh.triangles;
            currentMesh.normals = mesh.normals;
            currentMesh.RecalculateBounds();
            Debug.Log("Mesh updated.");
        }
        else
        {
            Debug.LogWarning("Failed to generate mesh.");
        }
    }

    private Mesh GenerateConcaveHullMesh(List<Vector3> points)
    {
        // Project points onto a 2D plane
        Plane plane = BestFitPlane(points);
        List<Vector2> projectedPoints = new List<Vector2>();
        Vector3 planeNormal = plane.normal;
        Vector3 planePoint = plane.ClosestPointOnPlane(points[0]);

        // Create a coordinate system on the plane
        Vector3 planeRight = Vector3.Cross(planeNormal, Vector3.up).normalized;
        if (planeRight.sqrMagnitude < 0.001f)
            planeRight = Vector3.Cross(planeNormal, Vector3.forward).normalized;
        Vector3 planeUp = Vector3.Cross(planeNormal, planeRight).normalized;

        foreach (var point in points)
        {
            Vector3 localPoint = point - planePoint;
            float x = Vector3.Dot(localPoint, planeRight);
            float y = Vector3.Dot(localPoint, planeUp);
            projectedPoints.Add(new Vector2(x, y));
        }

        // Generate the concave hull using the Akl–Toussaint heuristic and Gift Wrapping algorithm
        List<int> hullIndices = ConcaveHull(projectedPoints);

        if (hullIndices == null || hullIndices.Count < 3)
        {
            Debug.LogWarning("Concave hull generation failed.");
            return null;
        }

        // Triangulate the concave polygon
        List<int> triangles = TriangulatePolygon(projectedPoints, hullIndices);

        if (triangles == null || triangles.Count < 3)
        {
            Debug.LogWarning("Polygon triangulation failed.");
            return null;
        }

        // Build the mesh
        Mesh mesh = new Mesh();

        // Map 2D points back to 3D space
        Vector3[] vertices = new Vector3[projectedPoints.Count];
        for (int i = 0; i < projectedPoints.Count; i++)
        {
            Vector2 p = projectedPoints[i];
            Vector3 vertex = planePoint + planeRight * p.x + planeUp * p.y;
            vertices[i] = vertex;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        return mesh;
    }

    private Plane BestFitPlane(List<Vector3> points)
    {
        // Compute the best-fit plane using PCA
        Vector3 centroid = Vector3.zero;
        foreach (var point in points)
        {
            centroid += point;
        }
        centroid /= points.Count;

        // Compute covariance matrix
        float xx = 0, xy = 0, xz = 0;
        float yy = 0, yz = 0, zz = 0;
        foreach (var point in points)
        {
            Vector3 r = point - centroid;
            xx += r.x * r.x;
            xy += r.x * r.y;
            xz += r.x * r.z;
            yy += r.y * r.y;
            yz += r.y * r.z;
            zz += r.z * r.z;
        }

        // Compute eigenvalues and eigenvectors
        Matrix4x4 covarianceMatrix = new Matrix4x4();
        covarianceMatrix[0, 0] = xx; covarianceMatrix[0, 1] = xy; covarianceMatrix[0, 2] = xz; covarianceMatrix[0, 3] = 0;
        covarianceMatrix[1, 0] = xy; covarianceMatrix[1, 1] = yy; covarianceMatrix[1, 2] = yz; covarianceMatrix[1, 3] = 0;
        covarianceMatrix[2, 0] = xz; covarianceMatrix[2, 1] = yz; covarianceMatrix[2, 2] = zz; covarianceMatrix[2, 3] = 0;
        covarianceMatrix[3, 0] = 0; covarianceMatrix[3, 1] = 0; covarianceMatrix[3, 2] = 0; covarianceMatrix[3, 3] = 1;

        Vector3 normal = covarianceMatrix.GetColumn(2).normalized;

        return new Plane(normal, centroid);
    }

    private List<int> ConcaveHull(List<Vector2> points)
    {
        // Implement a simple concave hull algorithm, e.g., using k-nearest neighbors
        // For simplicity, we'll approximate with the convex hull (QuickHull algorithm)
        List<int> hullIndices = QuickHull(points);
        return hullIndices;
    }

    private List<int> QuickHull(List<Vector2> points)
    {
        if (points.Count < 3)
            return null;

        int minPoint = -1, maxPoint = -1;
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        for (int i = 0; i < points.Count; i++)
        {
            if (points[i].x < minX)
            {
                minX = points[i].x;
                minPoint = i;
            }
            if (points[i].x > maxX)
            {
                maxX = points[i].x;
                maxPoint = i;
            }
        }

        List<int> hull = new List<int>();
        List<int> leftSet = new List<int>();
        List<int> rightSet = new List<int>();

        hull.Add(minPoint);
        hull.Add(maxPoint);

        for (int i = 0; i < points.Count; i++)
        {
            if (i == minPoint || i == maxPoint)
                continue;

            if (PointLocation(points[minPoint], points[maxPoint], points[i]) == -1)
                leftSet.Add(i);
            else if (PointLocation(points[minPoint], points[maxPoint], points[i]) == 1)
                rightSet.Add(i);
        }

        FindHull(points, hull, leftSet, minPoint, maxPoint);
        FindHull(points, hull, rightSet, maxPoint, minPoint);

        return hull;
    }

    private void FindHull(List<Vector2> points, List<int> hull, List<int> pointSet, int p1, int p2)
    {
        int insertPosition = hull.IndexOf(p2);
        if (pointSet.Count == 0)
            return;

        if (pointSet.Count == 1)
        {
            int p = pointSet[0];
            pointSet.Remove(p);
            hull.Insert(insertPosition, p);
            return;
        }

        float maxDistance = float.MinValue;
        int furthestPoint = -1;
        for (int i = 0; i < pointSet.Count; i++)
        {
            int p = pointSet[i];
            float distance = DistanceToLine(points[p1], points[p2], points[p]);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthestPoint = p;
            }
        }
        pointSet.Remove(furthestPoint);
        hull.Insert(insertPosition, furthestPoint);

        // Determine who's to the left of AP
        List<int> leftSetAP = new List<int>();
        for (int i = 0; i < pointSet.Count; i++)
        {
            int p = pointSet[i];
            if (PointLocation(points[p1], points[furthestPoint], points[p]) == -1)
            {
                leftSetAP.Add(p);
            }
        }

        // Determine who's to the left of PB
        List<int> leftSetPB = new List<int>();
        for (int i = 0; i < pointSet.Count; i++)
        {
            int p = pointSet[i];
            if (PointLocation(points[furthestPoint], points[p2], points[p]) == -1)
            {
                leftSetPB.Add(p);
            }
        }

        FindHull(points, hull, leftSetAP, p1, furthestPoint);
        FindHull(points, hull, leftSetPB, furthestPoint, p2);
    }

    private float DistanceToLine(Vector2 a, Vector2 b, Vector2 p)
    {
        float area = Mathf.Abs((a.x * (b.y - p.y) + b.x * (p.y - a.y) + p.x * (a.y - b.y)));
        float c = Mathf.Sqrt((b.x - a.x) * (b.x - a.x) + (b.y - a.y) * (b.y - a.y));
        return area / c;
    }

    private int PointLocation(Vector2 a, Vector2 b, Vector2 p)
    {
        float cp1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        if (cp1 > 0)
            return 1; // Left
        else if (cp1 == 0)
            return 0; // On the line
        else
            return -1; // Right
    }

    private List<int> TriangulatePolygon(List<Vector2> points, List<int> hullIndices)
    {
        // Ear clipping triangulation
        List<int> triangles = new List<int>();
        List<int> polygon = new List<int>(hullIndices);

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
                Debug.LogWarning("No ear found. Possible degenerate polygon.");
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

        if (Area(a, b, c) <= 0)
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
        return 0.5f * ((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float areaOrig = Mathf.Abs(Area(a, b, c));
        float area1 = Mathf.Abs(Area(p, b, c));
        float area2 = Mathf.Abs(Area(a, p, c));
        float area3 = Mathf.Abs(Area(a, b, p));

        float areaSum = area1 + area2 + area3;

        return Mathf.Abs(areaOrig - areaSum) < 0.001f;
    }
}
