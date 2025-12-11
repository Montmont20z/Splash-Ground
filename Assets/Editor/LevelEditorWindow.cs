#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelEditorWindow : EditorWindow
{
    int width = 20;
    int height = 20;

    // Changed: TowerSlot removed; use Healthy + Contaminated
    enum BrushType { Empty = 0, Healthy = 1, Contaminated = 2 }
    BrushType brush = BrushType.Healthy;

    // core grid data (row-major: [x, z])
    int[,] mapData;

    // visual settings
    float cellSize = 1f;
    Vector3 gridOrigin = Vector3.zero; // where grid starts in world space

    // Optional loaded LevelAsset to edit directly
    LevelAsset editingAsset;

    [MenuItem("Tools/Level Editor")]
    public static void OpenWindow() => GetWindow<LevelEditorWindow>("Level Editor");

    void OnEnable()
    {
        EnsureMapInitialized();
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void EnsureMapInitialized()
    {
        if (mapData == null || mapData.GetLength(0) != width || mapData.GetLength(1) != height)
        {
            mapData = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int z = 0; z < height; z++)
                    mapData[x, z] = (int)BrushType.Healthy;
        }
    }

    void OnGUI()
    {
        if (mapData == null) EnsureMapInitialized();

        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);

        int newWidth = EditorGUILayout.IntField("Grid Width", width);
        int newHeight = EditorGUILayout.IntField("Grid Height", height);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        gridOrigin = EditorGUILayout.Vector3Field("Grid Origin", gridOrigin);

        if (newWidth != width || newHeight != height)
        {
            int[,] newData = new int[newWidth, newHeight];
            for (int x = 0; x < newWidth; x++)
                for (int z = 0; z < newHeight; z++)
                    newData[x, z] = (x < width && z < height) ? mapData[x, z] : (int)BrushType.Healthy;
            width = newWidth;
            height = newHeight;
            mapData = newData;
        }

        brush = (BrushType)EditorGUILayout.EnumPopup("Brush", brush);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Healthy")) FillAll((int)BrushType.Healthy);
        if (GUILayout.Button("Fill Contaminated")) FillAll((int)BrushType.Contaminated);
        if (GUILayout.Button("Clear (Empty)")) FillAll((int)BrushType.Empty);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // ScriptableObject handling
        EditorGUILayout.LabelField("ScriptableObject (LevelAsset)", EditorStyles.boldLabel);
        editingAsset = (LevelAsset)EditorGUILayout.ObjectField("Editing LevelAsset", editingAsset, typeof(LevelAsset), false);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save to LevelAsset")) SaveToLevelAsset();
        if (GUILayout.Button("Load from LevelAsset")) LoadFromLevelAsset();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // JSON options (keep if you still want JSON export)
        EditorGUILayout.LabelField("JSON", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Map (JSON)")) SaveMapJson();
        if (GUILayout.Button("Load Map (JSON)")) LoadMapJson();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox("Paint in Scene view: left-click tiles to paint. Use Load/Save to work with LevelAsset or JSON.", MessageType.Info);
    }

    void FillAll(int value)
    {
        EnsureMapInitialized();
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                mapData[x, z] = value;
        SceneView.RepaintAll();
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (mapData == null) EnsureMapInitialized();

        Event e = Event.current;
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 cellCenter = gridOrigin + new Vector3((x + 0.5f) * cellSize, 0f, (z + 0.5f) * cellSize);
                Vector3 half = new Vector3(cellSize * 0.5f, 0f, cellSize * 0.5f);
                Vector3[] verts = new Vector3[]
                {
                    cellCenter + new Vector3(-half.x, 0, -half.z),
                    cellCenter + new Vector3(-half.x, 0, half.z),
                    cellCenter + new Vector3(half.x, 0, half.z),
                    cellCenter + new Vector3(half.x, 0, -half.z)
                };

                int state = mapData[x, z];
                Color fill = Color.clear;
                Color outline = Color.grey;
                switch (state)
                {
                    case (int)BrushType.Empty: fill = new Color(0, 0, 0, 0); outline = Color.grey; break;
                    case (int)BrushType.Healthy: fill = new Color(0, 1f, 0, 0.15f); outline = Color.green; break;
                    case (int)BrushType.Contaminated: fill = new Color(1f, 0, 0, 0.15f); outline = Color.red; break;
                }

                Handles.DrawSolidRectangleWithOutline(verts, fill, outline);
            }
        }

        // Paint on click
        if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            // Prefer raycast to colliders if present
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                Vector3 localPos = hit.point - gridOrigin;
                int gx = Mathf.FloorToInt(localPos.x / cellSize);
                int gz = Mathf.FloorToInt(localPos.z / cellSize);
                if (gx >= 0 && gx < width && gz >= 0 && gz < height)
                {
                    mapData[gx, gz] = (int)brush;
                    e.Use();
                    SceneView.RepaintAll();
                }
            }
            else
            {
                Plane ground = new Plane(Vector3.up, 0f);
                if (ground.Raycast(ray, out float enter))
                {
                    Vector3 worldPoint = ray.GetPoint(enter);
                    Vector3 localPos = worldPoint - gridOrigin;
                    int gx = Mathf.FloorToInt(localPos.x / cellSize);
                    int gz = Mathf.FloorToInt(localPos.z / cellSize);
                    if (gx >= 0 && gx < width && gz >= 0 && gz < height)
                    {
                        mapData[gx, gz] = (int)brush;
                        e.Use();
                        SceneView.RepaintAll();
                    }
                }
            }
        }
    }

    // --- Save/Load LevelAsset ---
    void SaveToLevelAsset()
    {
        EnsureMapInitialized();
        string path = EditorUtility.SaveFilePanelInProject("Save Level Asset", "NewLevelMap", "asset", "Create LevelAsset in project");
        if (string.IsNullOrEmpty(path)) return;

        LevelAsset asset = ScriptableObject.CreateInstance<LevelAsset>();
        asset.width = width;
        asset.height = height;
        asset.EnsureSize();

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                int val = mapData[x, z];
                TileDatum td = new TileDatum();

                // Save type as Floor for healthy/contaminated and use initiallyContaminated flag
                if (val == (int)BrushType.Empty)
                {
                    td.type = LevelTileType.Empty;
                    td.initiallyContaminated = false;
                }
                else // Healthy or Contaminated -> use Floor type and set contamination flag accordingly
                {
                    td.type = LevelTileType.Floor;
                    td.initiallyContaminated = (val == (int)BrushType.Contaminated);
                }

                asset.tiles[z * width + x] = td;
            }

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log($"Saved LevelAsset to {path}");
    }

    void LoadFromLevelAsset()
    {
        if (editingAsset == null)
        {
            EditorUtility.DisplayDialog("No LevelAsset", "Assign a LevelAsset in the field before loading.", "OK");
            return;
        }

        editingAsset.EnsureSize();
        width = editingAsset.width;
        height = editingAsset.height;
        mapData = new int[width, height];

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                TileDatum td = editingAsset.tiles[z * width + x];

                // Map LevelAsset representation into editor brushes:
                // - Empty -> Empty
                // - Floor & initiallyContaminated=false -> Healthy
                // - Floor & initiallyContaminated=true -> Contaminated
                if (td.type == LevelTileType.Empty)
                {
                    mapData[x, z] = (int)BrushType.Empty;
                }
                else // Floor (or other non-empty) treated as floor tile
                {
                    mapData[x, z] = td.initiallyContaminated ? (int)BrushType.Contaminated : (int)BrushType.Healthy;
                }
            }

        SceneView.RepaintAll();
        Debug.Log("Loaded LevelAsset into editor.");
    }

    // --- JSON handling (same as earlier) ---
    [System.Serializable]
    class MapDataSerializable
    {
        public int width;
        public int height;
        public int[] tiles; // flattened row-major: idx = z*width + x
    }

    void SaveMapJson()
    {
        EnsureMapInitialized();
        string path = EditorUtility.SaveFilePanel("Save map JSON", "", "map.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        MapDataSerializable s = new MapDataSerializable();
        s.width = width; s.height = height;
        s.tiles = new int[width * height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                s.tiles[z * width + x] = mapData[x, z];

        string json = JsonUtility.ToJson(s, true);
        File.WriteAllText(path, json);
        Debug.Log($"Map saved to: {path}");
        AssetDatabase.Refresh();
    }

    void LoadMapJson()
    {
        string path = EditorUtility.OpenFilePanel("Load map JSON", "", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = File.ReadAllText(path);
        MapDataSerializable s = JsonUtility.FromJson<MapDataSerializable>(json);
        if (s == null || s.tiles == null || s.tiles.Length != s.width * s.height)
        {
            Debug.LogError("Invalid JSON map.");
            return;
        }

        width = s.width; height = s.height;
        mapData = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                mapData[x, z] = s.tiles[z * width + x];

        SceneView.RepaintAll();
        Debug.Log("Loaded JSON into editor.");
    }
}
#endif
