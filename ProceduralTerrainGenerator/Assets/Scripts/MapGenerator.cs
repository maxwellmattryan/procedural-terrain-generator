using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public int mapWidth;
    public int mapHeight;

    public float noiseScale;

    public float lacunarity;
    public float persistence;

    public int octaves;

    public bool autoUpdate;

    public void GenerateMap()
    {
        ValidateParameters();

        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, lacunarity, persistence, octaves);

        var display = FindObjectOfType<MapDisplay>();
        display.DrawNoiseMap(noiseMap);
    }

    private void ValidateParameters()
    {
        if (mapWidth <= 0)
            mapWidth = 1;
        if (mapHeight <= 0)
            mapHeight = 1;

        persistence = Mathf.Clamp(persistence, 0f, 1f);
    }
}
