using System;
using UnityEngine;

[Serializable]
public struct FloatRange
{
    public float min, max;
    public bool Contains(float v) => v >= min && v <= max;
    public float Lerp(float t) => Mathf.Lerp(min, max, t);
    public float Random() => UnityEngine.Random.Range(min, max);
}
