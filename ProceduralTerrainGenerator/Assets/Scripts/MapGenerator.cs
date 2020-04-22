using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, Mesh }
    public DrawMode drawMode;

    [HideInInspector]
    public const int mapChunkSize = 241;

    [Range(0, 6)]
    public int levelOfDetail;

    public int seed;

    public float scale;

    [Tooltip("Affects noise frequency across octaves (default = 2f)")]
    public float lacunarity;

    [Tooltip("Affects noise amplitude across octaves (default = 0.5f")]
    [Range(0f, 1f)]
    public float persistence;

    public int octaves;

    public Vector2 offset;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    public bool autoUpdate;

    // ** CAUTION: Make sure to enter regions in order of increasing height (or else the terrains will not draw correctly) ** 
    public TerrainType[] regions;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(seed, mapChunkSize, mapChunkSize, scale, lacunarity, persistence, octaves, offset);

        Color[] colorMap = GenerateColorMap(mapChunkSize, mapChunkSize, noiseMap);

        DrawMapDisplay(noiseMap, TextureGenerator.FromHeightMap(noiseMap), TextureGenerator.FromColorMap(colorMap, mapChunkSize, mapChunkSize));
    }

    private Color[] GenerateColorMap(int width, int height, float[,] noiseMap)
    {
        Color[] colorMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colorMap[y * width + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return colorMap;
    }

    private void DrawMapDisplay(float[,] noiseMap, Texture2D heightMapTexture, Texture2D colorMapTexture)
    {
        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();

        switch (drawMode)
        {
            default:
            case DrawMode.NoiseMap:
                mapDisplay.DrawTexture(heightMapTexture);
                break;

            case DrawMode.ColorMap:
                mapDisplay.DrawTexture(colorMapTexture);
                break;

            case DrawMode.Mesh:
                mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMeshData(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), colorMapTexture);
                break;
        }
    }

    private void OnValidate()
    {
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