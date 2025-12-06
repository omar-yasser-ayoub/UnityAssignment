using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders liquid particles as a cohesive blob using Marching Squares algorithm.
/// Particles that are close together merge into one smooth shape.
/// This creates a true metaball effect where the mesh is generated from an implicit field.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class LiquidBlobRenderer : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Size of the grid in world units")]
    public float gridSize = 10f;
    
    [Tooltip("Resolution of the marching squares grid")]
    [Range(20, 100)]
    public int gridResolution = 50;

    [Header("Blob Settings")]
    [Tooltip("Radius of influence for each particle")]
    public float particleRadius = 1.5f;
    
    [Tooltip("Threshold for surface generation (lower = bigger blobs)")]
    [Range(0.01f, 1f)]
    public float surfaceThreshold = 0.2f;
    
    [Tooltip("Falloff power (higher = sharper edges, lower = more blobby)")]
    [Range(1f, 4f)]
    public float falloffPower = 2f;

    [Header("Visual Settings")]
    public Material blobMaterial;
    public Color blobColor = new Color(0.2f, 0.5f, 1f, 1f);
    
    [Header("Sorting Layer")]
    [Tooltip("Sorting layer name for the blob renderer")]
    public string sortingLayerName = "Default";
    
    [Tooltip("Sorting order within the layer")]
    public int sortingOrder = 0;
    
    [Header("Debug")]
    public bool showDebugGizmos = false;

    // Internal
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh blobMesh;
    private List<Transform> particles = new List<Transform>();
    
    // Grid data
    private float[,] scalarField;
    private float cellSize;
    
    // Mesh data - reused to avoid allocations
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color> colors = new List<Color>();

    // Debug
    private float maxFieldValue = 0f;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        blobMesh = new Mesh();
        blobMesh.name = "LiquidBlobMesh";
        blobMesh.MarkDynamic();
        meshFilter.mesh = blobMesh;
        
        scalarField = new float[gridResolution + 1, gridResolution + 1];
        cellSize = gridSize / gridResolution;
    }

    void Start()
    {
        SetupMaterial();
    }

    void SetupMaterial()
    {
        if (blobMaterial != null)
        {
            meshRenderer.material = new Material(blobMaterial);
        }
        else
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = blobColor;
            meshRenderer.material = mat;
        }
        meshRenderer.material.color = blobColor;
        
        // Set sorting layer
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (particles.Count == 0)
        {
            blobMesh.Clear();
            return;
        }

        cellSize = gridSize / gridResolution;
        
        if (scalarField.GetLength(0) != gridResolution + 1)
        {
            scalarField = new float[gridResolution + 1, gridResolution + 1];
        }

        Vector3 center = GetParticlesCenter();
        transform.position = new Vector3(center.x, center.y, 0);

        GenerateBlobMesh(center);
    }

    void GenerateBlobMesh(Vector3 center)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();
        maxFieldValue = 0f;

        CalculateScalarField(center);
        MarchingSquares();

        if (vertices.Count >= 3)
        {
            blobMesh.Clear();
            blobMesh.SetVertices(vertices);
            blobMesh.SetTriangles(triangles, 0);
            blobMesh.SetUVs(0, uvs);
            blobMesh.SetColors(colors);
            blobMesh.RecalculateNormals();
            blobMesh.RecalculateBounds();
        }
        else
        {
            blobMesh.Clear();
        }
    }

    void CalculateScalarField(Vector3 center)
    {
        float halfSize = gridSize / 2f;
        Vector3 gridOrigin = center - new Vector3(halfSize, halfSize, 0);

        for (int y = 0; y <= gridResolution; y++)
        {
            for (int x = 0; x <= gridResolution; x++)
            {
                Vector3 worldPos = gridOrigin + new Vector3(x * cellSize, y * cellSize, 0);
                float value = CalculateFieldValue(worldPos);
                scalarField[x, y] = value;
                
                if (value > maxFieldValue) maxFieldValue = value;
            }
        }
    }

    float CalculateFieldValue(Vector3 position)
    {
        float fieldValue = 0f;

        foreach (var particle in particles)
        {
            if (particle == null) continue;

            Vector2 particlePos = particle.position;
            Vector2 samplePos = new Vector2(position.x, position.y);
            
            float distance = Vector2.Distance(particlePos, samplePos);
            
            if (distance < particleRadius)
            {
                float normalizedDist = distance / particleRadius;
                float contribution = 1f - Mathf.Pow(normalizedDist, falloffPower);
                contribution = Mathf.Max(0f, contribution);
                
                fieldValue += contribution;
            }
        }

        return fieldValue;
    }

    void MarchingSquares()
    {
        float halfSize = gridSize / 2f;

        for (int y = 0; y < gridResolution; y++)
        {
            for (int x = 0; x < gridResolution; x++)
            {
                float bl = scalarField[x, y];
                float br = scalarField[x + 1, y];
                float tr = scalarField[x + 1, y + 1];
                float tl = scalarField[x, y + 1];

                int caseIndex = 0;
                if (bl >= surfaceThreshold) caseIndex |= 1;
                if (br >= surfaceThreshold) caseIndex |= 2;
                if (tr >= surfaceThreshold) caseIndex |= 4;
                if (tl >= surfaceThreshold) caseIndex |= 8;

                if (caseIndex == 0) continue;

                Vector3 cellOrigin = new Vector3(
                    x * cellSize - halfSize,
                    y * cellSize - halfSize,
                    0
                );

                if (caseIndex == 15)
                {
                    float avgValue = (bl + br + tr + tl) / 4f;
                    AddQuad(
                        cellOrigin,
                        cellOrigin + new Vector3(cellSize, 0, 0),
                        cellOrigin + new Vector3(cellSize, cellSize, 0),
                        cellOrigin + new Vector3(0, cellSize, 0),
                        avgValue
                    );
                    continue;
                }

                Vector3[] edgePoints = new Vector3[4];
                
                float t0 = GetInterpolation(bl, br);
                edgePoints[0] = cellOrigin + new Vector3(t0 * cellSize, 0, 0);
                
                float t1 = GetInterpolation(br, tr);
                edgePoints[1] = cellOrigin + new Vector3(cellSize, t1 * cellSize, 0);
                
                float t2 = GetInterpolation(tl, tr);
                edgePoints[2] = cellOrigin + new Vector3(t2 * cellSize, cellSize, 0);
                
                float t3 = GetInterpolation(bl, tl);
                edgePoints[3] = cellOrigin + new Vector3(0, t3 * cellSize, 0);

                GenerateCellTriangles(caseIndex, edgePoints, cellOrigin, bl, br, tr, tl);
            }
        }
    }

    float GetInterpolation(float v1, float v2)
    {
        if (Mathf.Abs(v2 - v1) < 0.0001f) return 0.5f;
        float t = (surfaceThreshold - v1) / (v2 - v1);
        return Mathf.Clamp01(t);
    }

    void GenerateCellTriangles(int caseIndex, Vector3[] edgePoints, Vector3 cellOrigin, 
                                float bl, float br, float tr, float tl)
    {
        Vector3 bottomLeft = cellOrigin;
        Vector3 bottomRight = cellOrigin + new Vector3(cellSize, 0, 0);
        Vector3 topRight = cellOrigin + new Vector3(cellSize, cellSize, 0);
        Vector3 topLeft = cellOrigin + new Vector3(0, cellSize, 0);
        
        float centerValue = (bl + br + tr + tl) / 4f;
        
        switch (caseIndex)
        {
            case 1:
                AddTriangle(bottomLeft, edgePoints[0], edgePoints[3], bl);
                break;
            case 2:
                AddTriangle(edgePoints[0], bottomRight, edgePoints[1], br);
                break;
            case 3:
                AddQuad(bottomLeft, bottomRight, edgePoints[1], edgePoints[3], centerValue);
                break;
            case 4:
                AddTriangle(edgePoints[1], topRight, edgePoints[2], tr);
                break;
            case 5:
                AddTriangle(bottomLeft, edgePoints[0], edgePoints[3], bl);
                AddTriangle(edgePoints[1], topRight, edgePoints[2], tr);
                break;
            case 6:
                AddQuad(edgePoints[0], bottomRight, topRight, edgePoints[2], centerValue);
                break;
            case 7:
                AddPentagon(bottomLeft, bottomRight, topRight, edgePoints[2], edgePoints[3], centerValue);
                break;
            case 8:
                AddTriangle(edgePoints[3], edgePoints[2], topLeft, tl);
                break;
            case 9:
                AddQuad(bottomLeft, edgePoints[0], edgePoints[2], topLeft, centerValue);
                break;
            case 10:
                AddTriangle(edgePoints[0], bottomRight, edgePoints[1], br);
                AddTriangle(edgePoints[3], edgePoints[2], topLeft, tl);
                break;
            case 11:
                AddPentagon(bottomLeft, bottomRight, edgePoints[1], edgePoints[2], topLeft, centerValue);
                break;
            case 12:
                AddQuad(edgePoints[3], edgePoints[1], topRight, topLeft, centerValue);
                break;
            case 13:
                AddPentagon(bottomLeft, edgePoints[0], edgePoints[1], topRight, topLeft, centerValue);
                break;
            case 14:
                AddPentagon(edgePoints[0], bottomRight, topRight, topLeft, edgePoints[3], centerValue);
                break;
        }
    }

    void AddTriangle(Vector3 a, Vector3 b, Vector3 c, float fieldValue)
    {
        int startIndex = vertices.Count;
        
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        
        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);

        colors.Add(blobColor);
        colors.Add(blobColor);
        colors.Add(blobColor);

        float halfSize = gridSize / 2f;
        uvs.Add(new Vector2((a.x + halfSize) / gridSize, (a.y + halfSize) / gridSize));
        uvs.Add(new Vector2((b.x + halfSize) / gridSize, (b.y + halfSize) / gridSize));
        uvs.Add(new Vector2((c.x + halfSize) / gridSize, (c.y + halfSize) / gridSize));
    }

    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float fieldValue)
    {
        AddTriangle(a, b, c, fieldValue);
        AddTriangle(a, c, d, fieldValue);
    }

    void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, float fieldValue)
    {
        AddTriangle(a, b, c, fieldValue);
        AddTriangle(a, c, d, fieldValue);
        AddTriangle(a, d, e, fieldValue);
    }

    public void RegisterParticle(Transform particle)
    {
        if (!particles.Contains(particle))
        {
            particles.Add(particle);
        }
    }

    public void UnregisterParticle(Transform particle)
    {
        particles.Remove(particle);
    }

    public void ClearParticles()
    {
        particles.Clear();
        if (blobMesh != null) blobMesh.Clear();
    }

    Vector3 GetParticlesCenter()
    {
        if (particles.Count == 0) return transform.position;

        Vector3 center = Vector3.zero;
        int validCount = 0;

        foreach (var p in particles)
        {
            if (p != null)
            {
                center += p.position;
                validCount++;
            }
        }

        return validCount > 0 ? center / validCount : transform.position;
    }

    void OnDestroy()
    {
        if (blobMesh != null)
        {
            Destroy(blobMesh);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize, gridSize, 0));
        
        Gizmos.color = Color.yellow;
        foreach (var p in particles)
        {
            if (p != null)
            {
                Gizmos.DrawWireSphere(p.position, particleRadius);
            }
        }
    }
}
