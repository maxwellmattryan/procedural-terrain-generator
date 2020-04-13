using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap }
    public DrawMode drawMode;

    public int seed;

    public int mapWidth;
    public int mapHeight;

    public float scale;

    [Tooltip("Affects noise frequency across octaves (default = 2f)")]
    public float lacunarity;

    [Tooltip("Affects noise amplitude across octaves (default = 0.5f")]
    [Range(0f, 1f)]
    public float persistence;

    public int octaves;

    public Vector2 offset;

    public bool autoUpdate;

    // ** CAUTION: Make sure to enter regions in order of increasing height (or else the terrains will not draw correctly) ** 
    public TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(seed, mapWidth, mapHeight, scale, lacunarity, persistence, octaves, offset);

        Color[] colorMap = new Color[mapWidth * mapHeight];
        for(int y = 0; y < mapHeight; y++)
        {
            for(int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];

                for(int i = 0; i < regions.Length; i++)
                {
                    if(currentHeight <= regions[i].height)
                    {
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        switch(drawMode)
        {
            default:
            case DrawMode.NoiseMap:
                mapDisplay.DrawTexture(TextureGenerator.FromHeightMap(noiseMap));
                break;

            case DrawMode.ColorMap:
                mapDisplay.DrawTexture(TextureGenerator.FromColorMap(colorMap, mapWidth, mapHeight));
                break;
        }
    }

    private void OnValidate()
    {
        if (mapWidth < 1)
            mapWidth = 1;
        if (mapHeight < 1)
            mapHeight = 1;

        if (lacunarity < 1f)
            lacunarity = 1f;

        persistence = Mathf.Clamp(persistence, 0f, 1f);

        if (octaves < 1)
            octaves = 1;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}
