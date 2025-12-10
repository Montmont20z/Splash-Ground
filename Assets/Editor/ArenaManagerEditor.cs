//#if UNITY_EDITOR
//using UnityEngine;
//using UnityEditor;

//[CustomEditor(typeof(ArenaManager))]
//public class ArenaManagerEditor : Editor
//{
//    private ArenaManager arena;
//    private bool paintMode = false;
//    private bool eraseMode = false;
//    private int brushSize = 1;
    
//    void OnEnable()
//    {
//        arena = (ArenaManager)target;
//    }
    
//    public override void OnInspectorGUI()
//    {
//        DrawDefaultInspector();
        
//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Arena Editor Tools", EditorStyles.boldLabel);
        
//        // Generate button
//        if (GUILayout.Button("Generate Arena", GUILayout.Height(30)))
//        {
//            arena.GenerateArena();
//            EditorUtility.SetDirty(arena);
//        }
        
//        EditorGUILayout.Space();
        
//        // Custom shape editor
//        if (arena.shape == ArenaManager.ArenaShape.Custom)
//        {
//            EditorGUILayout.HelpBox(
//                "Custom Shape Mode: Use Scene View to paint tiles.\n" +
//                "Hold Shift + Left Click: Paint tiles\n" +
//                "Hold Shift + Right Click: Erase tiles",
//                MessageType.Info
//            );
            
//            EditorGUILayout.BeginHorizontal();
            
//            paintMode = GUILayout.Toggle(paintMode, "Paint Mode", "Button");
//            eraseMode = GUILayout.Toggle(eraseMode, "Erase Mode", "Button");
            
//            if (paintMode) eraseMode = false;
//            if (eraseMode) paintMode = false;
            
//            EditorGUILayout.EndHorizontal();
            
//            brushSize = EditorGUILayout.IntSlider("Brush Size", brushSize, 1, 5);
            
//            EditorGUILayout.BeginHorizontal();
            
//            if (GUILayout.Button("Clear All"))
//            {
//                ClearCustomShape();
//            }
            
//            if (GUILayout.Button("Fill All"))
//            {
//                FillCustomShape();
//            }
            
//            EditorGUILayout.EndHorizontal();
            
//            if (GUILayout.Button("Save Custom Shape"))
//            {
//                SaveCustomShape();
//            }
            
//            if (GUILayout.Button("Load Custom Shape"))
//            {
//                LoadCustomShape();
//            }
//        }
        
//        EditorGUILayout.Space();
        
//        // Quick size presets
//        EditorGUILayout.LabelField("Quick Size Presets", EditorStyles.boldLabel);
//        EditorGUILayout.BeginHorizontal();
        
//        if (GUILayout.Button("20x20\nSmall"))
//        {
//            SetArenaSize(20, 20);
//        }
//        if (GUILayout.Button("50x50\nMedium"))
//        {
//            SetArenaSize(50, 50);
//        }
//        if (GUILayout.Button("100x100\nLarge"))
//        {
//            SetArenaSize(100, 100);
//        }
//        if (GUILayout.Button("200x200\nHuge"))
//        {
//            SetArenaSize(200, 200);
//        }
        
//        EditorGUILayout.EndHorizontal();
        
//        // Shape presets
//        EditorGUILayout.Space();
//        EditorGUILayout.LabelField("Shape Presets", EditorStyles.boldLabel);
//        EditorGUILayout.BeginHorizontal();
        
//        if (GUILayout.Button("Square"))
//        {
//            SetArenaShape(ArenaManager.ArenaShape.Square);
//        }
//        if (GUILayout.Button("Circle"))
//        {
//            SetArenaShape(ArenaManager.ArenaShape.Circle);
//        }
//        if (GUILayout.Button("Custom"))
//        {
//            SetArenaShape(ArenaManager.ArenaShape.Custom);
//            InitializeCustomShape();
//        }
        
//        EditorGUILayout.EndHorizontal();
//    }
    
//    void OnSceneGUI()
//    {
//        if (arena.shape != ArenaManager.ArenaShape.Custom)
//            return;
        
//        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        
//        Event e = Event.current;
        
//        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
//        {
//            if (e.shift && (e.button == 0 || e.button == 1))
//            {
//                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
//                RaycastHit hit;
                
//                if (Physics.Raycast(ray, out hit))
//                {
//                    Vector3 hitPoint = hit.point;
//                    int x = Mathf.FloorToInt(hitPoint.x / arena.tileSize);
//                    int z = Mathf.FloorToInt(hitPoint.z / arena.tileSize);
                    
//                    bool shouldPaint = e.button == 0; // Left click = paint
//                    PaintCustomShapeTile(x, z, shouldPaint);
                    
//                    e.Use();
//                    EditorUtility.SetDirty(arena);
//                }
//            }
//        }
        
//        // Draw custom shape grid
//        DrawCustomShapeGrid();
//    }
    
//    void DrawCustomShapeGrid()
//    {
//        if (arena.customShape == null)
//            return;
        
//        Handles.color = Color.yellow;
        
//        for (int x = 0; x <= arena.gridWidth; x++)
//        {
//            Vector3 start = new Vector3(x * arena.tileSize, 0, 0);
//            Vector3 end = new Vector3(x * arena.tileSize, 0, arena.gridHeight * arena.tileSize);
//            Handles.DrawLine(start, end);
//        }
        
//        for (int z = 0; z <= arena.gridHeight; z++)
//        {
//            Vector3 start = new Vector3(0, 0, z * arena.tileSize);
//            Vector3 end = new Vector3(arena.gridWidth * arena.tileSize, 0, z * arena.tileSize);
//            Handles.DrawLine(start, end);
//        }
        
//        // Draw filled tiles
//        Handles.color = new Color(0, 1, 0, 0.3f);
        
//        for (int x = 0; x < arena.gridWidth; x++)
//        {
//            for (int z = 0; z < arena.gridHeight; z++)
//            {
//                if (arena.customShape[x, z])
//                {
//                    Vector3 center = new Vector3(
//                        (x + 0.5f) * arena.tileSize,
//                        0,
//                        (z + 0.5f) * arena.tileSize
//                    );
                    
//                    Handles.DrawSolidDisc(center, Vector3.up, arena.tileSize * 0.4f);
//                }
//            }
//        }
//    }
    
//    void PaintCustomShapeTile(int x, int z, bool paint)
//    {
//        if (arena.customShape == null)
//            InitializeCustomShape();
        
//        for (int bx = -brushSize + 1; bx < brushSize; bx++)
//        {
//            for (int bz = -brushSize + 1; bz < brushSize; bz++)
//            {
//                int tx = x + bx;
//                int tz = z + bz;
                
//                if (tx >= 0 && tx < arena.gridWidth && tz >= 0 && tz < arena.gridHeight)
//                {
//                    arena.customShape[tx, tz] = paint;
//                }
//            }
//        }
        
//        SceneView.RepaintAll();
//    }
    
//    void InitializeCustomShape()
//    {
//        arena.customShape = new bool[arena.gridWidth, arena.gridHeight];
//        // Start with all false
//        for (int x = 0; x < arena.gridWidth; x++)
//        {
//            for (int z = 0; z < arena.gridHeight; z++)
//            {
//                arena.customShape[x, z] = false;
//            }
//        }
//    }
    
//    void ClearCustomShape()
//    {
//        if (arena.customShape == null)
//            InitializeCustomShape();
        
//        for (int x = 0; x < arena.gridWidth; x++)
//        {
//            for (int z = 0; z < arena.gridHeight; z++)
//            {
//                arena.customShape[x, z] = false;
//            }
//        }
        
//        EditorUtility.SetDirty(arena);
//        SceneView.RepaintAll();
//    }
    
//    void FillCustomShape()
//    {
//        if (arena.customShape == null)
//            InitializeCustomShape();
        
//        for (int x = 0; x < arena.gridWidth; x++)
//        {
//            for (int z = 0; z < arena.gridHeight; z++)
//            {
//                arena.customShape[x, z] = true;
//            }
//        }
        
//        EditorUtility.SetDirty(arena);
//        SceneView.RepaintAll();
//    }
    
//    void SetArenaSize(int width, int height)
//    {
//        Undo.RecordObject(arena, "Change Arena Size");
//        arena.gridWidth = width;
//        arena.gridHeight = height;
//        EditorUtility.SetDirty(arena);
//    }
    
//    void SetArenaShape(ArenaManager.ArenaShape shape)
//    {
//        Undo.RecordObject(arena, "Change Arena Shape");
//        arena.shape = shape;
//        EditorUtility.SetDirty(arena);
//    }
    
//    void SaveCustomShape()
//    {
//        string path = EditorUtility.SaveFilePanel(
//            "Save Custom Shape",
//            Application.dataPath,
//            "ArenaShape.asset",
//            "asset"
//        );
        
//        if (!string.IsNullOrEmpty(path))
//        {
//            // Save logic here
//            Debug.Log($"Saved custom shape to {path}");
//        }
//    }
    
//    void LoadCustomShape()
//    {
//        string path = EditorUtility.OpenFilePanel(
//            "Load Custom Shape",
//            Application.dataPath,
//            "asset"
//        );
        
//        if (!string.IsNullOrEmpty(path))
//        {
//            // Load logic here
//            Debug.Log($"Loaded custom shape from {path}");
//        }
//    }
//}
//#endif