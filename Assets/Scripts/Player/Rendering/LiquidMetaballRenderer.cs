using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Renders liquid particles as a smooth, blobby metaball mesh.
/// Uses a render texture approach: particles are rendered as soft circles,
/// then a threshold shader creates the blob effect.
/// Compatible with URP (Universal Render Pipeline).
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class LiquidMetaballRenderer : MonoBehaviour
{
    [Header("Render Settings")]
    [Tooltip("Resolution of the metaball render texture")]
    public int textureResolution = 256;
    
    [Tooltip("Size of the render area in world units")]
    public float renderAreaSize = 10f;
    
    [Tooltip("Material with the metaball shader")]
    public Material metaballMaterial;

    [Header("Blob Settings")]
    [Tooltip("Size of each particle's influence")]
    public float particleBlobSize = 1.5f;
    
    [Tooltip("Threshold for blob edge (0-1)")]
    [Range(0.1f, 0.9f)]
    public float blobThreshold = 0.5f;
    
    [Tooltip("Softness of blob edges")]
    [Range(0.01f, 0.3f)]
    public float edgeSoftness = 0.1f;

    [Header("Gradient Colors")]
    public Color innerColor = new Color(0.2f, 0.5f, 1f, 1f);  // Deep blue
    public Color outerColor = new Color(0.5f, 0.8f, 1f, 0.8f); // Light blue
    public Color edgeColor = new Color(0.8f, 0.95f, 1f, 1f);   // White edge

    // Internal
    private RenderTexture metaballRT;
    private Camera metaballCamera;
    private GameObject cameraObj;
    private List<Transform> particles = new List<Transform>();
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh quadMesh;
    private Material materialInstance;

    // Shader property IDs
    private static readonly int ThresholdProp = Shader.PropertyToID("_Threshold");
    private static readonly int SoftnessProp = Shader.PropertyToID("_Softness");
    private static readonly int InnerColorProp = Shader.PropertyToID("_InnerColor");
    private static readonly int OuterColorProp = Shader.PropertyToID("_OuterColor");
    private static readonly int EdgeColorProp = Shader.PropertyToID("_EdgeColor");
    private static readonly int MainTexProp = Shader.PropertyToID("_MainTex");

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        SetupRenderTexture();
        SetupCamera();
        SetupQuadMesh();
    }

    void SetupRenderTexture()
    {
        // Create RenderTexture with depth buffer for URP compatibility
        metaballRT = new RenderTexture(textureResolution, textureResolution, 24, RenderTextureFormat.ARGB32);
        metaballRT.depthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D24_UNorm_S8_UInt;
        metaballRT.filterMode = FilterMode.Bilinear;
        metaballRT.Create();
    }

    void SetupCamera()
    {
        // Create a camera that renders particles to the render texture
        cameraObj = new GameObject("MetaballCamera");
        cameraObj.transform.SetParent(transform);
        cameraObj.transform.localPosition = new Vector3(0, 0, -10);
        
        metaballCamera = cameraObj.AddComponent<Camera>();
        metaballCamera.orthographic = true;
        metaballCamera.orthographicSize = renderAreaSize / 2f;
        metaballCamera.targetTexture = metaballRT;
        metaballCamera.clearFlags = CameraClearFlags.SolidColor;
        metaballCamera.backgroundColor = new Color(0, 0, 0, 0); // Transparent black
        metaballCamera.cullingMask = LayerMask.GetMask("LiquidParticle");
        metaballCamera.depth = -100; // Render before main camera
        metaballCamera.allowHDR = false;
        metaballCamera.allowMSAA = false;
        
        // For URP, we need to disable post-processing on this camera
        var additionalCameraData = cameraObj.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (additionalCameraData == null)
        {
            additionalCameraData = cameraObj.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        }
        additionalCameraData.renderPostProcessing = false;
        additionalCameraData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
        additionalCameraData.renderShadows = false;
    }

    void SetupQuadMesh()
    {
        // Create a quad mesh to display the metaball render
        quadMesh = new Mesh();
        
        float halfSize = renderAreaSize / 2f;
        
        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-halfSize, -halfSize, 0),
            new Vector3(halfSize, -halfSize, 0),
            new Vector3(-halfSize, halfSize, 0),
            new Vector3(halfSize, halfSize, 0)
        };

        Vector2[] uvs = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };

        int[] triangles = new int[] { 0, 2, 1, 2, 3, 1 };

        quadMesh.vertices = vertices;
        quadMesh.uv = uvs;
        quadMesh.triangles = triangles;
        quadMesh.RecalculateNormals();

        meshFilter.mesh = quadMesh;
    }

    void Start()
    {
        // Setup material
        if (metaballMaterial != null)
        {
            // Create instance to avoid modifying the original
            materialInstance = new Material(metaballMaterial);
            meshRenderer.material = materialInstance;
            UpdateMaterialProperties();
        }
        else
        {
            Debug.LogWarning("LiquidMetaballRenderer: No metaball material assigned! Assign a material with the Matter/LiquidMetaball shader.");
        }
    }

    void LateUpdate()
    {
        if (particles.Count == 0) return;

        // Center the renderer on the particles
        Vector3 center = GetParticlesCenter();
        transform.position = new Vector3(center.x, center.y, 0);

        // Update camera position
        if (metaballCamera != null)
        {
            metaballCamera.transform.position = new Vector3(center.x, center.y, -10);
        }

        UpdateMaterialProperties();
    }

    void UpdateMaterialProperties()
    {
        if (materialInstance == null) return;

        materialInstance.SetTexture(MainTexProp, metaballRT);
        materialInstance.SetFloat(ThresholdProp, blobThreshold);
        materialInstance.SetFloat(SoftnessProp, edgeSoftness);
        materialInstance.SetColor(InnerColorProp, innerColor);
        materialInstance.SetColor(OuterColorProp, outerColor);
        materialInstance.SetColor(EdgeColorProp, edgeColor);
    }

    /// <summary>
    /// Register a particle to be rendered as part of the metaball
    /// </summary>
    public void RegisterParticle(Transform particle)
    {
        if (!particles.Contains(particle))
        {
            particles.Add(particle);
            
            // Scale sprite for blob effect
            particle.localScale = Vector3.one * particleBlobSize;
        }
    }

    /// <summary>
    /// Unregister a particle
    /// </summary>
    public void UnregisterParticle(Transform particle)
    {
        particles.Remove(particle);
    }

    /// <summary>
    /// Clear all particles
    /// </summary>
    public void ClearParticles()
    {
        particles.Clear();
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
        if (metaballRT != null)
        {
            metaballRT.Release();
            Destroy(metaballRT);
        }

        if (cameraObj != null)
        {
            Destroy(cameraObj);
        }

        if (quadMesh != null)
        {
            Destroy(quadMesh);
        }

        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw render area bounds
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(renderAreaSize, renderAreaSize, 0));
    }
}
