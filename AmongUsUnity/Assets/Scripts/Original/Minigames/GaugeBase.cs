using UnityEngine;

/// <summary>
/// Gauges in the original are a SpriteMask that reveals a fill sprite. Here the fill (the child sprite that
/// interacts with the mask) is scaled directly, anchored at its left (horizontal) or bottom (vertical) edge.
/// </summary>
public abstract class GaugeBase : MonoBehaviour
{
    public float MaxValue = 1f;
    public float maskScale = 1f;
    public SpriteMask Mask;

    SpriteRenderer _fill;
    Vector3 _scale0, _pos0;
    Vector2 _size;
    bool _init;

    void Init()
    {
        if (_init) return;
        _init = true;
        foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
            if (r.maskInteraction != SpriteMaskInteraction.None) { _fill = r; break; }
        if (_fill == null && Mask != null) _fill = null;
        if (_fill == null) return;
        _fill.maskInteraction = SpriteMaskInteraction.None;
        if (Mask != null) Mask.enabled = false;
        _scale0 = _fill.transform.localScale; _pos0 = _fill.transform.localPosition;
        _size = _fill.sprite != null ? (Vector2)_fill.sprite.bounds.size : Vector2.one;
    }

    protected void Apply(float v, bool horizontal)
    {
        Init();
        if (_fill == null) return;
        v = Mathf.Clamp01(v / MaxValue);
        var s = _scale0; var p = _pos0;
        if (horizontal)
        {
            float full = _scale0.x * _size.x, w = full * v;
            s.x = Mathf.Max(0.0001f, _scale0.x * v);
            p.x = _pos0.x - full / 2f + w / 2f;
        }
        else
        {
            float full = _scale0.y * _size.y, h = full * v;
            s.y = Mathf.Max(0.0001f, _scale0.y * v);
            p.y = _pos0.y - full / 2f + h / 2f;
        }
        _fill.transform.localScale = s; _fill.transform.localPosition = p;
    }

    public void SetColor(Color c) { Init(); if (_fill != null) _fill.color = c; }
}

