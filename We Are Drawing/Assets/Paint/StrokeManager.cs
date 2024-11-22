using UnityEngine;
using System.Collections.Generic;

public class StrokeManager : MonoBehaviour
{
    private Dictionary<BrushTip.HandSide, Stroke> currentStrokes = new Dictionary<BrushTip.HandSide, Stroke>();
    private List<GameObject> allStrokes = new List<GameObject>();
    private GameObject strokesParent;
    private float currentScaleFactor = 1.0f;
    private float scaleStep = 0.1f;

    [Header("Brush Settings")]
    public Material brushMaterial; // Assign a material with a shader that supports emission (e.g., Standard Shader)
    public int brushSegmentDetail = 16;
    public float minDistance = 0.005f;

    [Header("Glow Settings")]
    public float emissionIntensity = 1.0f; // Adjust this value to control the glow brightness

    private BrushSizeAdjuster brushSizeAdjuster;

    private void Start()
    {
        strokesParent = new GameObject("Strokes Parent");

#if UNITY_2023_1_OR_NEWER
        brushSizeAdjuster = FindAnyObjectByType<BrushSizeAdjuster>();
#else
        brushSizeAdjuster = FindObjectOfType<BrushSizeAdjuster>();
#endif
    }

    public void AddPointToStroke(BrushTip.HandSide handSide, Vector3 point, Color color, Quaternion handRotation)
    {
        if (!currentStrokes.ContainsKey(handSide))
        {
            StartNewStroke(handSide, point, color, handRotation);
        }
        else
        {
            Stroke stroke = currentStrokes[handSide];
            if (Vector3.Distance(stroke.lastPoint, point) >= minDistance)
            {
                AddTubeSegment(stroke, point, color, handRotation);
                stroke.lastPoint = point;
            }
        }
    }

    public void EndStroke(BrushTip.HandSide handSide)
    {
        if (currentStrokes.ContainsKey(handSide))
        {
            currentStrokes.Remove(handSide);
        }
    }

    private void StartNewStroke(BrushTip.HandSide handSide, Vector3 point, Color color, Quaternion handRotation)
    {
        Stroke stroke = new Stroke();

        GameObject strokeObject = new GameObject("Brush Stroke");
        strokeObject.transform.parent = strokesParent.transform;

        MeshFilter meshFilter = strokeObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = strokeObject.AddComponent<MeshRenderer>();

        // Create a new material instance for this stroke
        Material strokeMaterial = new Material(brushMaterial);
        strokeMaterial.color = color;

        // Enable emission on the material
        strokeMaterial.EnableKeyword("_EMISSION");
        strokeMaterial.SetColor("_EmissionColor", color * emissionIntensity);

        meshRenderer.material = strokeMaterial;

        Mesh mesh = new Mesh
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };
        meshFilter.mesh = mesh;

        stroke.strokeObject = strokeObject;
        stroke.mesh = mesh;
        stroke.vertexCount = 0;
        stroke.lastPoint = point;

        currentStrokes[handSide] = stroke;
        allStrokes.Add(strokeObject);

        // Add the first segment
        AddTubeSegment(stroke, point, color, handRotation);
    }

    private void AddTubeSegment(Stroke stroke, Vector3 point, Color color, Quaternion handRotation)
    {
        float brushSize = brushSizeAdjuster != null ? brushSizeAdjuster.GetBrushSize() : 0.01f;

        int segmentVertices = brushSegmentDetail;
        int segmentTriangles = brushSegmentDetail * 6;

        Vector3[] updatedVertices = new Vector3[stroke.vertexCount + segmentVertices];
        Vector2[] updatedUVs = new Vector2[stroke.vertexCount + segmentVertices];
        int[] updatedTriangles;

        if (stroke.vertexCount > 0)
        {
            stroke.mesh.vertices.CopyTo(updatedVertices, 0);
            stroke.mesh.uv.CopyTo(updatedUVs, 0);
        }

        // Create a circular extrusion around the point
        for (int i = 0; i < segmentVertices; i++)
        {
            float angle = i * (360f / brushSegmentDetail) * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * brushSize * 0.5f;
            float y = Mathf.Sin(angle) * brushSize * 0.5f;
            Vector3 offset = handRotation * new Vector3(x, y, 0f);
            updatedVertices[stroke.vertexCount + i] = stroke.strokeObject.transform.InverseTransformPoint(point + offset);
            updatedUVs[stroke.vertexCount + i] = new Vector2((float)i / segmentVertices, 0);
        }

        if (stroke.vertexCount > 0)
        {
            updatedTriangles = new int[stroke.mesh.triangles.Length + segmentTriangles];
            stroke.mesh.triangles.CopyTo(updatedTriangles, 0);

            for (int i = 0; i < brushSegmentDetail; i++)
            {
                int current = stroke.vertexCount - segmentVertices + i;
                int next = stroke.vertexCount - segmentVertices + (i + 1) % brushSegmentDetail;
                int currentNew = stroke.vertexCount + i;
                int nextNew = stroke.vertexCount + (i + 1) % brushSegmentDetail;

                // First triangle
                updatedTriangles[stroke.mesh.triangles.Length + i * 6 + 0] = current;
                updatedTriangles[stroke.mesh.triangles.Length + i * 6 + 1] = currentNew;
                updatedTriangles[stroke.mesh.triangles.Length + i * 6 + 2] = nextNew;

                // Second triangle
                updatedTriangles[stroke.mesh.triangles.Length + i * 6 + 3] = current;
                updatedTriangles[stroke.mesh.triangles.Length + i * 6 + 4] = nextNew;
                updatedTriangles[stroke.mesh.triangles.Length + i * 6 + 5] = next;
            }
        }
        else
        {
            updatedTriangles = new int[0];
        }

        stroke.mesh.vertices = updatedVertices;
        stroke.mesh.uv = updatedUVs;
        stroke.mesh.triangles = updatedTriangles;

        stroke.mesh.RecalculateNormals();
        stroke.mesh.RecalculateBounds();

        stroke.vertexCount += segmentVertices;
    }

    public void UndoLastStroke()
    {
        if (allStrokes.Count > 0)
        {
            GameObject lastStroke = allStrokes[allStrokes.Count - 1];
            allStrokes.RemoveAt(allStrokes.Count - 1);
            Destroy(lastStroke);
        }
    }

    public void SetDrawingScale(float scaleFactor)
    {
        currentScaleFactor = scaleFactor;
        Vector3 newScale = Vector3.one * scaleFactor;
        strokesParent.transform.localScale = newScale;
    }

    public void IncreaseScale()
    {
        currentScaleFactor += scaleStep;
        SetDrawingScale(currentScaleFactor);
    }

    public void DecreaseScale()
    {
        currentScaleFactor = Mathf.Max(scaleStep, currentScaleFactor - scaleStep);
        SetDrawingScale(currentScaleFactor);
    }

    private class Stroke
    {
        public GameObject strokeObject;
        public Mesh mesh;
        public int vertexCount;
        public Vector3 lastPoint;
    }
}
