using UnityEngine;

public class FloorTile : MonoBehaviour
{
    public enum TileState { Healthy, Contaminated, HeavyContaminated }

    [Header("Tile State")]
    public TileState currentState = TileState.Healthy;

    [Header("Materials")]
    public Material healthyMaterial;
    public Material contaminatedMaterial;
    public Material heavyContaminationMaterial;

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
        if (currentState == TileState.Healthy)
        {
            currentState = TileState.Contaminated;
            previousState = currentState;
            UpdateVisual();
        }
    }

    /// <summary>
    /// Force set to heavy contamination (used by Rooter)
    /// </summary>
    public void HeavyContaminate()
    {
        if (currentState != TileState.HeavyContaminated)
        {
            currentState = TileState.HeavyContaminated;
            previousState = currentState;
            UpdateVisual();
        }
    }

    /// <summary>
    /// Called when player sprays/cleans a tile once.
    /// Heavy -> Contaminated -> Healthy.
    /// Returns true if tile state changed.
    /// </summary>
    public bool SprayOnce()
    {
        if (currentState == TileState.HeavyContaminated)
        {
            currentState = TileState.Contaminated;
            UpdateVisual();
            previousState = currentState;
            return true;
        }
        else if (currentState == TileState.Contaminated)
        {
            currentState = TileState.Healthy;
            UpdateVisual();
            previousState = currentState;
            return true;
        }
        // if already healthy, nothing to do
        return false;
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

        //if (tileRenderer != null && healthyMaterial != null && contaminatedMaterial != null)
        //{
        //    tileRenderer.material = (currentState == TileState.Healthy)
        //        ? healthyMaterial
        //        : contaminatedMaterial;
        //}
        if (tileRenderer != null)
        {
            switch (currentState)
            {
                case TileState.Healthy:
                    if (healthyMaterial != null)
                        tileRenderer.material = healthyMaterial;
                    break;
                case TileState.Contaminated:
                    if (contaminatedMaterial != null)
                        tileRenderer.material = contaminatedMaterial;
                    break;
                case TileState.HeavyContaminated:
                    if (heavyContaminationMaterial != null)
                        tileRenderer.material = heavyContaminationMaterial;
                    break;
            }

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