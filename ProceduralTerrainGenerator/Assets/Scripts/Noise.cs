using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    // TODO: write description comments somewhere (i.e. scale 
    public static float[,] GenerateNoiseMap(int width, int height, float scale, float lacunarity, float persistence, int octaves)
    {
        if (scale <= 0f)
            scale = 0.001f;

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

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
                    float sampleX = x / scale * frequency;
                    float sampleY = y / scale * frequency;

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

        for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);

        return noiseMap;
    }
}
