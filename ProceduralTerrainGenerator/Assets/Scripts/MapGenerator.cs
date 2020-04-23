using System;
using System.Threading;
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
    public int editorLevelOfDetail;

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

    private Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();

        lock(_mapDataThreadInfoQueue)
        {
            _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    private Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate 
        {
            MeshDataThread(mapData, levelOfDetail, callback); ; 
        };

        new Thread(threadStart).Start();
    }

    private void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail);

        lock(_meshDataThreadInfoQueue)
        {
            _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        CheckMapDataThreadInfoQueue();
        CheckMeshDataThreadInfoQueue();
    }

    private void CheckMapDataThreadInfoQueue()
    {
        if (_mapDataThreadInfoQueue.Count > 0)
        {
            for (int i = 0; i < _mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = _mapDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private void CheckMeshDataThreadInfoQueue()
    {
        if(_meshDataThreadInfoQueue.Count > 0)
        {
            for(int i = 0; i < _meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = _meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    public void DrawMap()
    {
        MapData mapData = GenerateMapData();

        MapDisplay mapDisplay = FindObjectOfType<MapDisplay>();
        switch (drawMode)
        {
            default:
            case DrawMode.NoiseMap:
                mapDisplay.DrawTexture(TextureGenerator.FromHeightMap(mapData.heightMap));
                break;

            case DrawMode.ColorMap:
                mapDisplay.DrawTexture(TextureGenerator.FromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize));
                break;

            case DrawMode.Mesh:
                mapDisplay.DrawMesh(
                    MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, meshHeightMultiplier, meshHeightCurve, editorLevelOfDetail), 
                    TextureGenerator.FromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize)
                );
                break;
        }
    }

    private MapData GenerateMapData()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(seed, mapChunkSize, mapChunkSize, scale, lacunarity, persistence, octaves, offset);

        Color[] colorMap = GenerateColorMap(mapChunkSize, mapChunkSize, noiseMap);

        return new MapData(noiseMap, colorMap);
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

    private void OnValidate()
    {
        if (lacunarity < 1f)
            lacunarity = 1f;

        persistence = Mathf.Clamp(persistence, 0f, 1f);

        if (octaves < 1)
            octaves = 1;
    }

    private struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public readonly float[,] heightMap;
    public readonly Color[] colorMap;

    public MapData(float [,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}