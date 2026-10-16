using UnityEngine;

public class VerticalGauge : GaugeBase
{
    public float value;
    public void Set(float v) { value = v; Apply(v, false); }
}
