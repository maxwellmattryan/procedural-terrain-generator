using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffMapGenerator
{
    public static float [,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for(int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float x = i / (float) size * 2f - 1f;
                float y = j / (float) size * 2f - 1f;

                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = EvaluateCurvedValue(value);
            }
        }

        return map;
    }

    public static float EvaluateCurvedValue(float value)
    {
        float a = 2f;
        float b = 5f;

        float result = Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));

        return result;
    }
}
