using System.Collections.Generic;
using UnityEngine;

public class WallDissolveController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera used for wall checks and viewport position. If empty, Camera.main is used.")]
    public Camera cameraReference;

    [Header("Detection")]
    [Tooltip("Layer mask for walls that should dissolve between the player and camera.")]
    public LayerMask wallMask = 0;

    [Tooltip("How often to refresh the wall raycast. 1 = every frame.")]
    [Range(1, 10)] public int raycastInterval = 1;

    [Header("Transition")]
    [Tooltip("Seconds for the dissolve hole to open or close.")]
    public float transitionDuration = 0.3f;

    [Header("Shader Property Names")]
    [SerializeField] private string sizePropertyName = "_Size";
    [SerializeField] private string positionPropertyName = "_Position";

    private int sizePropertyID;
    private int positionPropertyID;
    private int frameCounter;
    private MaterialPropertyBlock propertyBlock;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private readonly HashSet<Renderer> activeWalls = new HashSet<Renderer>();
    private readonly Dictionary<Renderer, float> wallSizes = new Dictionary<Renderer, float>();
    private readonly List<Renderer> trackedWalls = new List<Renderer>();
    private readonly List<Renderer> wallsToRemove = new List<Renderer>();

    void Awake()
    {
        sizePropertyID = Shader.PropertyToID(sizePropertyName);
        positionPropertyID = Shader.PropertyToID(positionPropertyName);
        propertyBlock = new MaterialPropertyBlock();

        if (cameraReference == null)
            cameraReference = Camera.main;
    }

    void Update()
    {
        if (cameraReference == null)
            return;

        Vector3 viewportPosition = cameraReference.WorldToViewportPoint(transform.position);
        Vector4 shaderPosition = new Vector4(viewportPosition.x, viewportPosition.y, 0f, 0f);

        frameCounter++;
        if (frameCounter >= raycastInterval)
        {
            frameCounter = 0;
            RecomputeActiveWalls();
        }

        UpdateWallDissolve(shaderPosition);
    }

    void RecomputeActiveWalls()
    {
        activeWalls.Clear();

        Vector3 from = transform.position;
        Vector3 to = cameraReference.transform.position;
        Vector3 direction = to - from;
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return;

        int hitCount = Physics.RaycastNonAlloc(from, direction / distance, hitBuffer, distance, wallMask);
        for (int i = 0; i < hitCount; i++)
        {
            Renderer wallRenderer = hitBuffer[i].collider.GetComponent<Renderer>();
            if (wallRenderer == null)
                wallRenderer = hitBuffer[i].collider.GetComponentInParent<Renderer>();

            if (wallRenderer == null)
                continue;

            activeWalls.Add(wallRenderer);

            if (!wallSizes.ContainsKey(wallRenderer))
                wallSizes.Add(wallRenderer, 0f);
        }
    }

    void UpdateWallDissolve(Vector4 shaderPosition)
    {
        float speed = 1f / Mathf.Max(transitionDuration, 0.001f);

        trackedWalls.Clear();
        foreach (Renderer wallRenderer in wallSizes.Keys)
            trackedWalls.Add(wallRenderer);

        wallsToRemove.Clear();

        for (int i = 0; i < trackedWalls.Count; i++)
        {
            Renderer wallRenderer = trackedWalls[i];
            if (wallRenderer == null)
            {
                wallsToRemove.Add(wallRenderer);
                continue;
            }

            float targetSize = activeWalls.Contains(wallRenderer) ? 1f : 0f;
            float currentSize = wallSizes[wallRenderer];
            float nextSize = Mathf.MoveTowards(currentSize, targetSize, speed * Time.deltaTime);
            wallSizes[wallRenderer] = nextSize;

            wallRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(sizePropertyID, nextSize);
            propertyBlock.SetVector(positionPropertyID, shaderPosition);
            wallRenderer.SetPropertyBlock(propertyBlock);

            if (nextSize <= 0.0001f && !activeWalls.Contains(wallRenderer))
                wallsToRemove.Add(wallRenderer);
        }

        for (int i = 0; i < wallsToRemove.Count; i++)
        {
            Renderer wallRenderer = wallsToRemove[i];
            if (wallRenderer != null)
                ResetWall(wallRenderer);

            wallSizes.Remove(wallRenderer);
        }
    }

    void ResetWall(Renderer wallRenderer)
    {
        wallRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(sizePropertyID, 0f);
        wallRenderer.SetPropertyBlock(propertyBlock);
    }

    void OnDisable()
    {
        if (propertyBlock == null)
            return;

        foreach (Renderer wallRenderer in wallSizes.Keys)
        {
            if (wallRenderer != null)
                ResetWall(wallRenderer);
        }

        activeWalls.Clear();
        wallSizes.Clear();
        trackedWalls.Clear();
        wallsToRemove.Clear();
    }
}
