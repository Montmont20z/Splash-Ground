using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public enum LevelTileType { Empty = 0, Floor = 1, TowerSlot = 2 }

[Serializable]
public struct TileDatum
{
    public LevelTileType type;
    public bool initiallyContaminated;
}

[CreateAssetMenu(menuName = "SplashGround/Level Asset", fileName = "NewLevelMap")]
public class LevelAsset : ScriptableObject
{
    public int width = 20;
    public int height = 20;

    // Flattened row-major storage: index = y*width + x
    public TileDatum[] tiles;

    // Extra designer fields
    public List<Vector2Int> enemySpawns = new List<Vector2Int>();
    public List<Vector2Int> mainPath = new List<Vector2Int>();

    public void EnsureSize()
    {
        if (width < 1) width = 1;
        if (height < 1) height = 1;
        int size = width * height;
        if (tiles == null || tiles.Length != size)
        {
            tiles = new TileDatum[size];
            for (int i = 0; i < size; i++)
            {
                tiles[i].type = LevelTileType.Floor;
                tiles[i].initiallyContaminated = false;
            }
        }
    }

    public TileDatum GetTile(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return default;
        return tiles[y * width + x];
    }

    public void SetTile(int x, int y, TileDatum d)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        tiles[y * width + x] = d;
    }
}
