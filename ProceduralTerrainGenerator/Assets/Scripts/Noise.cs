using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    // TODO: write description comments somewhere (i.e. scale 
    public static float[,] GenerateNoiseMap(int width, int height, float scale)
    {
        if (scale <= 0f)
            scale = 0.001f;

        float[,] noiseMap = new float[width, height];

        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            { 
                float sampleX = x / scale;
                float sampleY = y / scale;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                noiseMap[x, y] = noiseValue;
            }
        }

        return noiseMap;
    }
}
