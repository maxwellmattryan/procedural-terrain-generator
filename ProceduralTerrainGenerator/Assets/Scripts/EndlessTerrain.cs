using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    private const float _moveDistanceThresholdForChunkUpdate = 25f;
    private const float _sqrMoveDistanceThresholdForChunkUpdate = _moveDistanceThresholdForChunkUpdate * _moveDistanceThresholdForChunkUpdate;

    private const float _scale = 2f;

    public static float maxViewDistance;
    public LODData[] levelOfDetailData;

    public Transform viewer;

    public Material mapMaterial;

    public static Vector2 viewerPosition;
    private static Vector2 _previousViewerPosition;

    private static MapGenerator _mapGenerator;

    private int _chunkSize;
    private int _visibleChunkCount;

    private Dictionary<Vector2, TerrainChunk> _terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();

    private static List<TerrainChunk> _previouslyVisibleTerrainChunks = new List<TerrainChunk>();

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
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / _scale;

        if ((_previousViewerPosition - viewerPosition).sqrMagnitude > _sqrMoveDistanceThresholdForChunkUpdate)
        {
            _previousViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        for (int i = 0; i < _previouslyVisibleTerrainChunks.Count; i++)
            _previouslyVisibleTerrainChunks[i].SetVisibility(false);

        _previouslyVisibleTerrainChunks.Clear();

        int currentChunkXPos = Mathf.RoundToInt(viewerPosition.x / _chunkSize);
        int currentChunkYPos = Mathf.RoundToInt(viewerPosition.y / _chunkSize);

        for(int yOffset = -_visibleChunkCount; yOffset <= _visibleChunkCount; yOffset++)
        {
            for(int xOffset = -_visibleChunkCount; xOffset <= _visibleChunkCount; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkXPos + xOffset, currentChunkYPos + yOffset);

                if(_terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    _terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                else
                    _terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, _chunkSize, levelOfDetailData, transform, mapMaterial));
            }
        }
    }

    public class TerrainChunk
    {
        private Vector2 _position;
        private Bounds _bounds;
        private GameObject _meshObject;

        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private LODData[] _levelOfDetailData;
        private LODMesh[] _levelOfDetailMeshes;
        private LODMesh _collisionLevelOfDetailMesh;

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
            _meshObject.transform.position = positionV3 * _scale;
            _meshObject.transform.parent = parent;
            _meshObject.transform.localScale = Vector3.one * _scale;

            _meshCollider = _meshObject.AddComponent<MeshCollider>();

            _meshFilter = _meshObject.AddComponent<MeshFilter>();

            _meshRenderer = _meshObject.AddComponent<MeshRenderer>();
            _meshRenderer.material = material;

            _levelOfDetailMeshes = new LODMesh[levelOfDetailData.Length];
            for(int i = 0; i < levelOfDetailData.Length; i++)
            {
                _levelOfDetailMeshes[i] = new LODMesh(levelOfDetailData[i].levelOfDetail, UpdateTerrainChunk);

                if(levelOfDetailData[i].useForCollider)
                    _collisionLevelOfDetailMesh = _levelOfDetailMeshes[i];
            }

            _mapGenerator.RequestMapData(_position, OnMapDataReceived);

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

                if (levelOfDetailIndex == 0)
                {
                    if (_collisionLevelOfDetailMesh.hasMesh)
                        _meshCollider.sharedMesh = _collisionLevelOfDetailMesh.mesh;

                    else if (!_collisionLevelOfDetailMesh.hasRequestedMesh)
                        _collisionLevelOfDetailMesh.RequestMesh(_mapData);
                }


                _previouslyVisibleTerrainChunks.Add(this);
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

            Texture2D texture = TextureGenerator.FromColorMap(mapData.colorMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            _meshRenderer.material.SetTexture("_MainTex", texture);

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
        public bool useForCollider;
    }
}
