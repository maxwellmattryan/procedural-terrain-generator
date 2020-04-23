using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public enum NormalizeMode { Local, Global }
    public static float[,] GenerateNoiseMap(int seed, int width, int height, float scale, float lacunarity, float persistence, int octaves, Vector2 offset, NormalizeMode normalizeMode)
    {
        if (scale <= 0f)
            scale = 0.001f;

        System.Random pseudoRNG = new System.Random(seed);

        float maxGlobalNoiseHeight = 0f;
        
        float frequency = 1f;
        float amplitude = 1f;

        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offsetX = pseudoRNG.Next(-100000, 100000) + offset.x;
            float offsetY = pseudoRNG.Next(-100000, 100000) - offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxGlobalNoiseHeight += amplitude;
            amplitude *= persistence;
        }

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        float[,] noiseMap = new float[width, height];

        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                frequency = 1f;
                amplitude = 1f;

                float noiseHeight = 0f;

                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    // height will never decrease unless this value is remapped between [-1f, 1f]
                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

                    noiseHeight += noiseValue * amplitude;

                    frequency *= lacunarity;
                    amplitude *= persistence;
                }

                if (maxLocalNoiseHeight < noiseHeight)
                    maxLocalNoiseHeight = noiseHeight;
                else if (minLocalNoiseHeight > noiseHeight)
                    minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // normalize back to [0f, 1f]
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                if (normalizeMode == NormalizeMode.Local)
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                else
                {
                    float normalizedHeight = (noiseMap[x, y] + 1) / maxGlobalNoiseHeight;
                    noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0f, float.MaxValue);
                }
            }

        return noiseMap;
    }
}
