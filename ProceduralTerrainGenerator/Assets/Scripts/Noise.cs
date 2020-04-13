using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    // TODO: write description comments somewhere
    public static float[,] GenerateNoiseMap(int seed, int width, int height, float scale, float lacunarity, float persistence, int octaves, Vector2 offset)
    {
        if (scale <= 0f)
            scale = 0.001f;

        System.Random pseudoRNG = new System.Random(seed);

        Vector2[] octaveOffsets = new Vector2[octaves];
        for(int i = 0; i < octaves; i++)
        {
            float offsetX = pseudoRNG.Next(-100000, 100000) + offset.x;
            float offsetY = pseudoRNG.Next(-100000, 100000) + offset.y;

            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        float[,] noiseMap = new float[width, height];

        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                float frequency = 1f;
                float amplitude = 1f;

                float noiseHeight = 0f;

                for(int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x * frequency;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y * frequency;

                    // height will never decrease unless this value is remapped between [-1f, 1f]
                    float noiseValue = Mathf.PerlinNoise(sampleX, sampleY) * 2f - 1f;

                    noiseHeight += noiseValue * amplitude;

                    frequency *= lacunarity;
                    amplitude *= persistence;
                }

                if (maxNoiseHeight < noiseHeight)
                    maxNoiseHeight = noiseHeight;
                else if (minNoiseHeight > noiseHeight)
                    minNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }

        // normalize back to [0f, 1f]
        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);

        return noiseMap;
    }
}
