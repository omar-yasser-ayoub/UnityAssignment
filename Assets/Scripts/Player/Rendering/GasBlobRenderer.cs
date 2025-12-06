using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders gas particles as a wispy, semi-transparent cloud using Marching Squares algorithm.
/// Similar to LiquidBlobRenderer but with softer edges and lighter appearance.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class GasBlobRenderer : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Size of the grid in world units")]
    public float gridSize = 12f;
    
    [Tooltip("Resolution of the marching squares grid")]
    [Range(20, 100)]
    public int gridResolution = 40;

    [Header("Gas Blob Settings")]
    [Tooltip("Radius of influence for each particle (larger = puffier)")]
    public float particleRadius = 2f;
    
    [Tooltip("Threshold for surface generation (lower = bigger, puffier clouds)")]
    [Range(0.01f, 0.5f)]
    public float surfaceThreshold = 0.1f;
    
    [Tooltip("Falloff power (lower = softer edges, more diffuse)")]
    [Range(0.5f, 3f)]
    public float falloffPower = 1.2f;

    [Header("Visual Settings")]
    public Material gasMaterial;
    public Color gasColor = new Color(0.9f, 0.95f, 1f, 0.5f);
    public Color gasEdgeColor = new Color(1f, 1f, 1f, 0.3f);
    
    [Tooltip("Enable gradient from center to edge")]
    public bool useGradient = true;

    [Header("Sorting Layer")]
    [Tooltip("Sorting layer name for the gas renderer")]
    public string sortingLayerName = "Default";
    
    [Tooltip("Sorting order within the layer")]
    public int sortingOrder = 0;

    [Header("Animation")]
    [Tooltip("Enable wobble animation for organic movement")]
    public bool enableWobble = true;
    
    [Tooltip("Speed of the wobble animation")]
    public float wobbleSpeed = 2f;
    
    [Tooltip("Intensity of the wobble")]
    public float wobbleIntensity = 0.1f;

    [Header("Debug")]
    public bool showDebugGizmos = false;

    // Internal
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh gasMesh;
    private List<Transform> particles = new List<Transform>();
    
    // Grid data
    private float[,] scalarField;
    private float cellSize;
    
    // Mesh data - reused to avoid allocations
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private List<Color> colors = new List<Color>();

    // Animation
    private float wobbleTime = 0f;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        gasMesh = new Mesh();
        gasMesh.name = "GasBlobMesh";
        gasMesh.MarkDynamic();
        meshFilter.mesh = gasMesh;
        
        scalarField = new float[gridResolution + 1, gridResolution + 1];
        cellSize = gridSize / gridResolution;
    }

    void Start()
    {
        SetupMaterial();
    }

    void SetupMaterial()
    {
        if (gasMaterial != null)
        {
            meshRenderer.material = new Material(gasMaterial);
        }
        else
        {
            // Create a transparent material for gas
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = gasColor;
            meshRenderer.material = mat;
        }
        meshRenderer.material.color = gasColor;
        
        // Set render queue to transparent
        meshRenderer.material.renderQueue = 3000;
        
        // Set sorting layer
        meshRenderer.sortingLayerName = sortingLayerName;
        meshRenderer.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (particles.Count == 0)
        {
            gasMesh.Clear();
            return;
        }

        // Update wobble animation
        if (enableWobble)
        {
            wobbleTime += Time.deltaTime * wobbleSpeed;
        }

        cellSize = gridSize / gridResolution;
        
        if (scalarField.GetLength(0) != gridResolution + 1)
        {
            scalarField = new float[gridResolution + 1, gridResolution + 1];
        }

        Vector3 center = GetParticlesCenter();
        transform.position = new Vector3(center.x, center.y, 0.1f); // Slightly behind for layering

        GenerateGasMesh(center);
    }

    void GenerateGasMesh(Vector3 center)
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        colors.Clear();

        CalculateScalarField(center);
        MarchingSquares(center);

        if (vertices.Count >= 3)
        {
            gasMesh.Clear();
            gasMesh.SetVertices(vertices);
            gasMesh.SetTriangles(triangles, 0);
            gasMesh.SetUVs(0, uvs);
            gasMesh.SetColors(colors);
            gasMesh.RecalculateNormals();
            gasMesh.RecalculateBounds();
        }
        else
        {
            gasMesh.Clear();
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
                
                // Add wobble effect to sample position
                if (enableWobble)
                {
                    float wobbleX = Mathf.Sin(wobbleTime + y * 0.5f) * wobbleIntensity;
                    float wobbleY = Mathf.Cos(wobbleTime * 0.7f + x * 0.5f) * wobbleIntensity;
                    worldPos += new Vector3(wobbleX, wobbleY, 0);
                }
                
                float value = CalculateFieldValue(worldPos);
                scalarField[x, y] = value;
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
                
                // Softer falloff for gas (more gradual fade)
                float contribution = 1f - Mathf.Pow(normalizedDist, falloffPower);
                contribution = Mathf.Max(0f, contribution);
                
                // Add some noise for organic look
                contribution *= 1f + Mathf.Sin(distance * 10f + wobbleTime) * 0.1f;
                
                fieldValue += contribution;
            }
        }

        return fieldValue;
    }

    void MarchingSquares(Vector3 center)
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

                float avgValue = (bl + br + tr + tl) / 4f;

                if (caseIndex == 15)
                {
                    AddQuad(
                        cellOrigin,
                        cellOrigin + new Vector3(cellSize, 0, 0),
                        cellOrigin + new Vector3(cellSize, cellSize, 0),
                        cellOrigin + new Vector3(0, cellSize, 0),
                        avgValue,
                        center
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

                GenerateCellTriangles(caseIndex, edgePoints, cellOrigin, bl, br, tr, tl, center);
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
                                float bl, float br, float tr, float tl, Vector3 center)
    {
        Vector3 bottomLeft = cellOrigin;
        Vector3 bottomRight = cellOrigin + new Vector3(cellSize, 0, 0);
        Vector3 topRight = cellOrigin + new Vector3(cellSize, cellSize, 0);
        Vector3 topLeft = cellOrigin + new Vector3(0, cellSize, 0);
        
        float centerValue = (bl + br + tr + tl) / 4f;
        
        switch (caseIndex)
        {
            case 1:
                AddTriangle(bottomLeft, edgePoints[0], edgePoints[3], bl, center);
                break;
            case 2:
                AddTriangle(edgePoints[0], bottomRight, edgePoints[1], br, center);
                break;
            case 3:
                AddQuad(bottomLeft, bottomRight, edgePoints[1], edgePoints[3], centerValue, center);
                break;
            case 4:
                AddTriangle(edgePoints[1], topRight, edgePoints[2], tr, center);
                break;
            case 5:
                AddTriangle(bottomLeft, edgePoints[0], edgePoints[3], bl, center);
                AddTriangle(edgePoints[1], topRight, edgePoints[2], tr, center);
                break;
            case 6:
                AddQuad(edgePoints[0], bottomRight, topRight, edgePoints[2], centerValue, center);
                break;
            case 7:
                AddPentagon(bottomLeft, bottomRight, topRight, edgePoints[2], edgePoints[3], centerValue, center);
                break;
            case 8:
                AddTriangle(edgePoints[3], edgePoints[2], topLeft, tl, center);
                break;
            case 9:
                AddQuad(bottomLeft, edgePoints[0], edgePoints[2], topLeft, centerValue, center);
                break;
            case 10:
                AddTriangle(edgePoints[0], bottomRight, edgePoints[1], br, center);
                AddTriangle(edgePoints[3], edgePoints[2], topLeft, tl, center);
                break;
            case 11:
                AddPentagon(bottomLeft, bottomRight, edgePoints[1], edgePoints[2], topLeft, centerValue, center);
                break;
            case 12:
                AddQuad(edgePoints[3], edgePoints[1], topRight, topLeft, centerValue, center);
                break;
            case 13:
                AddPentagon(bottomLeft, edgePoints[0], edgePoints[1], topRight, topLeft, centerValue, center);
                break;
            case 14:
                AddPentagon(edgePoints[0], bottomRight, topRight, topLeft, edgePoints[3], centerValue, center);
                break;
        }
    }

    Color GetVertexColor(Vector3 vertexPos, float fieldValue, Vector3 center)
    {
        if (!useGradient)
        {
            return gasColor;
        }

        // Calculate distance from center for gradient
        float distFromCenter = Vector3.Distance(vertexPos + transform.position, center + transform.position);
        float maxDist = gridSize * 0.4f;
        float t = Mathf.Clamp01(distFromCenter / maxDist);
        
        // Fade alpha based on distance from center and field value
        Color innerColor = gasColor;
        Color edgeColor = gasEdgeColor;
        
        Color result = Color.Lerp(innerColor, edgeColor, t);
        
        // Also fade based on field value (stronger field = more opaque)
        float alphaMultiplier = Mathf.Clamp01(fieldValue / (surfaceThreshold * 2f));
        result.a *= Mathf.Lerp(0.3f, 1f, alphaMultiplier);
        
        return result;
    }

    void AddTriangle(Vector3 a, Vector3 b, Vector3 c, float fieldValue, Vector3 center)
    {
        int startIndex = vertices.Count;
        
        vertices.Add(a);
        vertices.Add(b);
        vertices.Add(c);
        
        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);

        // Per-vertex colors for gradient effect
        colors.Add(GetVertexColor(a, fieldValue, center));
        colors.Add(GetVertexColor(b, fieldValue, center));
        colors.Add(GetVertexColor(c, fieldValue, center));

        float halfSize = gridSize / 2f;
        uvs.Add(new Vector2((a.x + halfSize) / gridSize, (a.y + halfSize) / gridSize));
        uvs.Add(new Vector2((b.x + halfSize) / gridSize, (b.y + halfSize) / gridSize));
        uvs.Add(new Vector2((c.x + halfSize) / gridSize, (c.y + halfSize) / gridSize));
    }

    void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float fieldValue, Vector3 center)
    {
        AddTriangle(a, b, c, fieldValue, center);
        AddTriangle(a, c, d, fieldValue, center);
    }

    void AddPentagon(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 e, float fieldValue, Vector3 center)
    {
        AddTriangle(a, b, c, fieldValue, center);
        AddTriangle(a, c, d, fieldValue, center);
        AddTriangle(a, d, e, fieldValue, center);
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
        if (gasMesh != null) gasMesh.Clear();
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
        if (gasMesh != null)
        {
            Destroy(gasMesh);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;
        
        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize, gridSize, 0));
        
        Gizmos.color = new Color(0.8f, 0.8f, 0.9f, 0.5f);
        foreach (var p in particles)
        {
            if (p != null)
            {
                Gizmos.DrawWireSphere(p.position, particleRadius);
            }
        }
    }
}
