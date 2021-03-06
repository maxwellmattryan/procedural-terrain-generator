﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    // this returns MeshData (instead of Mesh) because we need the individual data points that allow multi-threading to be possible
    public static MeshData GenerateTerrainMeshData(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail, bool applyFlatShading)
    {
        // each thread has its own heightCurve with this statement (so no wild discrepancies as a result)
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int borderedSize = heightMap.GetLength(0);

        int meshSimplificationIncrement = levelOfDetail == 0 ? 1 : levelOfDetail * 2;

        int meshSizeSimplified = borderedSize - 2 * meshSimplificationIncrement;
        int meshSizeUnsimplified = borderedSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

        int verticesPerLine = (meshSizeSimplified - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, applyFlatShading);

        int[,] vertexIndicesMap = new int[borderedSize, borderedSize];

        int borderVertexIndex = -1;
        int meshVertexIndex = 0;

        for(int y = 0; y < borderedSize; y += meshSimplificationIncrement)
            for(int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }    

        for(int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for(int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];

                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSizeSimplified, (y - meshSimplificationIncrement) / (float)meshSizeSimplified);

                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                if (x < borderedSize - 1 && y < borderedSize - 1)
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }

                vertexIndex++;
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }
}

public class MeshData
{
    private Vector3[] _vertices;
    private int[] _triangles;
    private Vector2[] _uvMaps;
    private Vector3[] _bakedNormals;

    private int _triangleIndex;

    private Vector3[] _borderVertices;
    private int[] _borderTriangles;

    private int _borderTriangleIndex;

    private bool _applyFlatShading;

    public MeshData(int verticesPerLine, bool applyFlatShading)
    {
        _vertices = new Vector3[verticesPerLine * verticesPerLine];
        _triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        _uvMaps = new Vector2[verticesPerLine * verticesPerLine];

        // * 4 because of the sides and + 4 because of the corners
        _borderVertices = new Vector3[verticesPerLine * 4 + 4];
        _borderTriangles = new int[verticesPerLine * 24];

        _applyFlatShading = applyFlatShading;
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
            _borderVertices[-vertexIndex - 1] = vertexPosition;

        else
        {
            _vertices[vertexIndex] = vertexPosition;
            _uvMaps[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if(a < 0 || b < 0 || c < 0)
        {
            _borderTriangles[_borderTriangleIndex] = a;
            _borderTriangles[_borderTriangleIndex + 1] = b;
            _borderTriangles[_borderTriangleIndex + 2] = c;

            _borderTriangleIndex += 3;
        }
        else
        {
            _triangles[_triangleIndex] = a;
            _triangles[_triangleIndex + 1] = b;
            _triangles[_triangleIndex + 2] = c;

            _triangleIndex += 3;
        }
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = _vertices,
            triangles = _triangles,
            uv = _uvMaps
        };

        if (_applyFlatShading)
            mesh.RecalculateNormals();
        else
            mesh.SetNormals(_bakedNormals);

        return mesh;
    }

    private void BakeNormals()
    {
        _bakedNormals = CalculateNormals();
    }

    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[_vertices.Length];

        int triangleCount = _triangles.Length / 3;

        for(int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;

            int vertexIndexA = _triangles[normalTriangleIndex];
            int vertexIndexB = _triangles[normalTriangleIndex + 1];
            int vertexIndexC = _triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = _borderTriangles.Length / 3;

        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;

            int vertexIndexA = _borderTriangles[normalTriangleIndex];
            int vertexIndexB = _borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = _borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

            if (vertexIndexA >= 0)
                vertexNormals[vertexIndexA] += triangleNormal;
            if (vertexIndexB >= 0)
                vertexNormals[vertexIndexB] += triangleNormal;
            if (vertexIndexC >= 0)
                vertexNormals[vertexIndexC] += triangleNormal;
        }

        foreach (Vector3 vertex in vertexNormals)
            vertex.Normalize();

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = indexA < 0 ? _borderVertices[-indexA - 1] : _vertices[indexA];
        Vector3 pointB = indexB < 0 ? _borderVertices[-indexB - 1] : _vertices[indexB];
        Vector3 pointC = indexC < 0 ? _borderVertices[-indexC - 1] : _vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    private void ApplyFlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[_triangles.Length];
        Vector2[] fladShadedUVs = new Vector2[_triangles.Length];

        for(int i = 0; i < _triangles.Length; i++)
        {
            flatShadedVertices[i] = _vertices[_triangles[i]];
            fladShadedUVs[i] = _uvMaps[_triangles[i]];

            _triangles[i] = i;
        }

        _vertices = flatShadedVertices;
        _uvMaps = fladShadedUVs;
    }

    public void ProcessMesh()
    {
        if (_applyFlatShading)
            ApplyFlatShading();
        else
            BakeNormals();
    }
}