using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float _moveDistanceThresholdForChunkUpdate = 25f;
    private const float _sqrMoveDistanceThresholdForChunkUpdate = _moveDistanceThresholdForChunkUpdate * _moveDistanceThresholdForChunkUpdate;

    public LODData[] levelOfDetailData;
    public static float maxViewDistance;

    public Transform viewer;

    public Material mapMaterial;

    public static Vector2 viewerPosition;
    private static Vector2 _previousViewerPosition;

    private static MapGenerator _mapGenerator;

    private int _chunkSize;
    private int _visibleChunkCount;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    List<TerrainChunk> previouslyVisibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        _mapGenerator = FindObjectOfType<MapGenerator>();

        maxViewDistance = levelOfDetailData[levelOfDetailData.Length - 1].visibleDistanceThreshold;

        _chunkSize = MapGenerator.mapChunkSize - 1;
        _visibleChunkCount = Mathf.RoundToInt(maxViewDistance / _chunkSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if ((_previousViewerPosition - viewerPosition).sqrMagnitude > _sqrMoveDistanceThresholdForChunkUpdate)
        {
            _previousViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
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
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, levelOfDetailData, transform, mapMaterial));
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

        private LODData[] _levelOfDetailData;
        private LODMesh[] _levelOfDetailMeshes;

        private int _previousLevelOfDetailIndex = -1;

        private MapData _mapData;
        private bool _mapDataReceived;

        public TerrainChunk(Vector2 coordinates, int size, LODData[] levelOfDetailData, Transform parent, Material material)
        {
            _position = coordinates * size;

            _bounds = new Bounds(_position, Vector2.one * size);

            _meshObject = new GameObject("Terrain Chunk");

            _levelOfDetailData = levelOfDetailData;

            Vector3 positionV3 = new Vector3(_position.x, 0f, _position.y);
            _meshObject.transform.position = positionV3;
            _meshObject.transform.parent = parent;

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;

            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _levelOfDetailMeshes = new LODMesh[levelOfDetailData.Length];
            for(int i = 0; i < levelOfDetailData.Length; i++)
                _levelOfDetailMeshes[i] = new LODMesh(levelOfDetailData[i].levelOfDetail, UpdateTerrainChunk);

            _mapGenerator.RequestMapData(OnMapDataReceived);

            SetVisibility(false);
        }

        public void UpdateTerrainChunk()
        {
            if (!_mapDataReceived) return;

            float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(viewerPosition));

            bool isVisible = viewerDistanceFromNearestEdge <= maxViewDistance;
            if (isVisible)
            {
                int levelOfDetailIndex = GetLevelOfDetailIndex(viewerDistanceFromNearestEdge);

                if (levelOfDetailIndex != _previousLevelOfDetailIndex)
                {
                    LODMesh levelOfDetailMesh = _levelOfDetailMeshes[levelOfDetailIndex];
                    if (levelOfDetailMesh.hasMesh)
                    {
                        _previousLevelOfDetailIndex = levelOfDetailIndex;
                        _meshFilter.mesh = levelOfDetailMesh.mesh;
                    }
                    else if (!levelOfDetailMesh.hasRequestedMesh)
                        levelOfDetailMesh.RequestMesh(_mapData);
                }
            }   

            SetVisibility(isVisible);
        }

        private int GetLevelOfDetailIndex(float visibleDistance)
        {
            int levelOfDetailIndex = 0;

            for (int i = 0; i < _levelOfDetailData.Length - 1; i++)
            {
                if (visibleDistance > _levelOfDetailData[i].visibleDistanceThreshold)
                    levelOfDetailIndex = i + 1;
                else
                    break;
            }

            return levelOfDetailIndex;
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
            _mapData = mapData;
            _mapDataReceived = true;

            UpdateTerrainChunk();
        }
    }

    public class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        private int _levelOfDetail;

        System.Action _updateCallback;

        public LODMesh(int levelOfDetail, System.Action updateCallback)
        {
            _levelOfDetail = levelOfDetail;
            _updateCallback = updateCallback;
        }

        public void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            _updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _levelOfDetail, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODData
    {
        public int levelOfDetail;
        public float visibleDistanceThreshold;
    }
}
