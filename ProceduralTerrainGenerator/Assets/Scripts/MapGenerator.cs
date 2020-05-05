using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColorMap, FalloffMap, Mesh }
    public DrawMode drawMode;

    public NoiseData noiseData;
    public TerrainData terrainData;
    
    [Range(0, 6)]
    public int editorLevelOfDetail;

    public bool autoUpdate;

    // ** CAUTION: Make sure to enter regions in order of increasing height (or else the terrains will not draw correctly) ** 
    public TerrainType[] regions;

    private float[,] _falloffMap;

    private static MapGenerator instance;

    private void Awake()
    {
        _falloffMap = FalloffMapGenerator.GenerateFalloffMap(mapChunkSize);
    }

    private void OnValuesUpdated()
    {
        if (!Application.isPlaying)
            DrawMap();
    }

    public static int mapChunkSize
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<MapGenerator>();

            if (instance.terrainData.applyFlatShading)
                return 95;
            else
                return 239;
        }
    }

    private Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();

    public void RequestMapData(Vector2 center, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MapDataThread(center, callback);
        };

        new Thread(threadStart).Start();
    }

    private void MapDataThread(Vector2 center, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(center);

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
        MeshData meshData = MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetail, terrainData.applyFlatShading);

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
        MapData mapData = GenerateMapData(Vector2.zero);

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

            case DrawMode.FalloffMap:
                mapDisplay.DrawTexture(TextureGenerator.FromHeightMap(FalloffMapGenerator.GenerateFalloffMap(mapChunkSize)));
                break;

            case DrawMode.Mesh:
                mapDisplay.DrawMesh(
                    MeshGenerator.GenerateTerrainMeshData(mapData.heightMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorLevelOfDetail, terrainData.applyFlatShading), 
                    TextureGenerator.FromColorMap(mapData.colorMap, mapChunkSize, mapChunkSize)
                );
                break;
        }
    }

    private MapData GenerateMapData(Vector2 center)
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(noiseData.seed, mapChunkSize + 2, mapChunkSize + 2, noiseData.scale, noiseData.lacunarity, noiseData.persistence, noiseData.octaves, center + noiseData.offset, noiseData.normalizeMode);

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
                if (terrainData.applyFalloffMap)
                    noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _falloffMap[x, y]);

                float currentHeight = noiseMap[x, y];

                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight >= regions[i].height)
                        colorMap[y * width + x] = regions[i].color;
                    else
                        break;
                }
            }
        }

        return colorMap;
    }

    private void OnValidate()
    {
        if (noiseData != null)
        {
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
        }

        if (terrainData != null)
        {
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }

        if (_falloffMap == null)
            _falloffMap = FalloffMapGenerator.GenerateFalloffMap(mapChunkSize);
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