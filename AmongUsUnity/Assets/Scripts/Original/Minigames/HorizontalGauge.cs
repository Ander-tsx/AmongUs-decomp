using UnityEngine;

public class HorizontalGauge : GaugeBase
{
    public float Value;
    public SpriteRenderer MaskArea;
    public void Set(float v) { Value = v; Apply(v, true); }
}

