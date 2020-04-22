using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float maxViewDistance = 450f;

    public Transform viewer;

    public Material mapMaterial;

    public static Vector2 viewerPosition;

    private static MapGenerator _mapGenerator;

    private int _chunkSize;
    private int _visibleChunkCount;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    List<TerrainChunk> previouslyVisibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        _mapGenerator = FindObjectOfType<MapGenerator>();

        _chunkSize = MapGenerator.mapChunkSize - 1;
        _visibleChunkCount = Mathf.RoundToInt(maxViewDistance / _chunkSize);
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        UpdateVisibleChunks();
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < previouslyVisibleTerrainChunks.Count; i++)
            previouslyVisibleTerrainChunks[i].SetVisibility(false);

        previouslyVisibleTerrainChunks.Clear();

        int currentChunkXPos = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
        int currentChunkYPos = Mathf.RoundToInt(viewerPosition.y / _chunkSize);

        for(int yOffset = -_visibleChunkCount; yOffset <= _visibleChunkCount; yOffset++)
        {
            for(int xOffset = -_visibleChunkCount; xOffset <= _visibleChunkCount; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkXPos + xOffset, currentChunkYPos + yOffset);

                if(terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();

                    if (terrainChunkDictionary[viewedChunkCoord].IsVisible())
                        previouslyVisibleTerrainChunks.Add(terrainChunkDictionary[viewedChunkCoord]);
                }
                else
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, transform, mapMaterial));
            }
        }
    }

    public class TerrainChunk
    {
        private Vector2 _position;
        private Bounds _bounds;
        private GameObject _meshObject;

        private MeshRenderer _meshRenderer;
        private MeshFilter _meshFilter;

        public TerrainChunk(Vector2 coordinates, int size, Transform parent, Material material)
        {
            _position = coordinates * size;

            _bounds = new Bounds(_position, Vector2.one * size);

            _meshObject = new GameObject("Terrain Chunk");

            Vector3 positionV3 = new Vector3(_position.x, 0f, _position.y);
            _meshObject.transform.position = positionV3;
            _meshObject.transform.parent = parent;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _mapGenerator.RequestMapData(OnMapDataReceived);

            SetVisibility(false);
        }

        public void UpdateTerrainChunk()
        {
            float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));

            bool isVisible = viewerDistanceFromNearestEdge <= maxViewDistance;
            SetVisibility(isVisible);
        }

        public void SetVisibility(bool isVisible)
        {
            _meshObject.SetActive(isVisible);
        }

        public bool IsVisible()
        {
            return _meshObject.activeSelf;
        }

        private void OnMapDataReceived(MapData mapData)
        {
            _mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        private void OnMeshDataReceived(MeshData meshData)
        {
            _meshFilter.mesh = meshData.CreateMesh();
        }
    }
}
