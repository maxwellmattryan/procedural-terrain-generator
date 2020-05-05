using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData
{
    public float uniformScale = 2.5f;

    public bool applyFlatShading;

    public bool applyFalloffMap;

    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;
}
