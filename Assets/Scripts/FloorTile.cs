using UnityEngine;

public class FloorTile : MonoBehaviour
{
    public enum TileState { Healthy, Contaminated }

    [Header("Tile State")]
    public TileState currentState = TileState.Healthy;

    [Header("Materials")]
    public Material healthyMaterial;
    public Material contaminatedMaterial;

    private Renderer tileRenderer;
    private TileState previousState; // Track changes

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        previousState = currentState;
        UpdateVisual();
    }
    void Update()
    {
        // Check if state changed in Inspector during Play mode
        if (currentState != previousState)
        {
            previousState = currentState;
            UpdateVisual();
        }
        //Debug.Log($"Floor Health: {GetHealthPercentage():F1}%");
    }

    public void Contaminate()
    {
        if (currentState != TileState.Contaminated)
        {
            currentState = TileState.Contaminated;
            previousState = currentState;
            UpdateVisual();
        }
    }

    public void Cleanse()
    {
        if (currentState != TileState.Healthy)
        {
            currentState = TileState.Healthy;
            previousState = currentState;
            UpdateVisual();
        }
    }

    public void UpdateVisual()
    {
        if (tileRenderer == null)
            tileRenderer = GetComponent<Renderer>();

        if (tileRenderer != null && healthyMaterial != null && contaminatedMaterial != null)
        {
            tileRenderer.material = (currentState == TileState.Healthy)
                ? healthyMaterial
                : contaminatedMaterial;
        }
    }

    // This runs when values change in Inspector (even in Play mode!)
    void OnValidate()
    {
        // Update visual when state changes in Inspector
        if (Application.isPlaying)
        {
            UpdateVisual();
        }
    }

    // For debugging - visualize tile bounds
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 0.1f, 1));
    }
}