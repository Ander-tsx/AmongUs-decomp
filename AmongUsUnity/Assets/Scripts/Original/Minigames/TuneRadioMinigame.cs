using System.Collections;
using UnityEngine;

/// <summary>Fix Communications: turn the dial until the two waves match.</summary>
public class TuneRadioMinigame : Minigame
{
    public float Tolerance = 0.175f;
    public float targetAngle = 70.65f;
    public AudioClip StaticSound;
    public AudioClip RadioSound;

    Transform _dial, _dialTop, _dialShad;
    LineRenderer _top, _bottom;
    SpriteRenderer _red, _green;
    float _angle, _target, _matched;
    bool _drag, _done;
    static readonly Color Dim = new Color(0.25f, 0.25f, 0.25f);

    protected override void OnBegin()
    {
        _dial = Find("Dial"); _dialTop = Find("DialTop"); _dialShad = Find("DialShad");
        _top = FindC<LineRenderer>("TopCurve"); _bottom = FindC<LineRenderer>("BottomCurve");
        _red = Find("comms_Light").GetComponent<SpriteRenderer>(); _green = Find("comms_Light (1)").GetComponent<SpriteRenderer>();
        _target = Random.Range(-85f, 85f); if (Mathf.Abs(_target) < 30f) _target = 60f * Mathf.Sign(_target == 0 ? 1 : _target);
        _angle = Mathf.Clamp(-_target, -100f, 100f);
        foreach (var l in new[] { _top, _bottom })
        {
            if (l == null) continue;
            l.useWorldSpace = false; l.material = new Material(Shader.Find("Sprites/Default"));
            l.startColor = l.endColor = new Color(0.4f, 1f, 0.5f); l.startWidth = l.endWidth = 0.04f; l.sortingOrder = 125;
            l.positionCount = 64;
        }
        Sfx(StaticSound);
        if (Bot) StartCoroutine(BotSolve());
        Rotate();
    }

    void Rotate()
    {
        var q = Quaternion.Euler(0, 0, -_angle);
        if (_dialTop != null) _dialTop.localRotation = q;
        if (_dialShad != null) _dialShad.localRotation = q;
    }

    void Wave(LineRenderer l, float freq, float x0, float w, float amp)
    {
        if (l == null) return;
        for (int i = 0; i < l.positionCount; i++)
        {
            float t = i / (float)(l.positionCount - 1);
            l.SetPosition(i, new Vector3(x0 + (t - 0.5f) * w, Mathf.Sin(t * freq * 6.283f + Time.time * 3f) * amp, -1f));
        }
    }

    protected override void Tick()
    {
        if (_done) return;
        Wave(_top, 3f + _target / 25f, 0, 1.8f, 0.22f);
        Wave(_bottom, 3f + _angle / 25f, 0, 1.8f, 0.22f);
        if (!Bot)
        {
            var m = Mouse;
            if (MouseDown && Vector2.Distance(m, _dial.position) < 0.8f) _drag = true;
            if (_drag && MouseHeld)
            {
                var d = m - (Vector2)_dial.position;
                _angle = Mathf.Clamp(Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg, -110f, 110f);
                Rotate();
            }
            if (MouseUp) _drag = false;
        }
        bool match = Mathf.Abs(_angle - _target) < 6f;
        _green.color = match ? Color.white : Dim; _red.color = match ? Dim : Color.white;
        _matched = match ? _matched + Time.deltaTime : 0f;
        if (_matched > 0.8f) { _done = true; Sfx(RadioSound); Win(0.7f); }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        while (Mathf.Abs(_angle - _target) > 1f) { _angle = Mathf.MoveTowards(_angle, _target, Time.deltaTime * 200f); Rotate(); yield return null; }
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        var c = (Vector2)_dial.position;
        float a = _target * Mathf.Deg2Rad;
        yield return StartCoroutine(TDrag(2.5f, c + new Vector2(0, 0.4f), c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 0.6f));
        yield return StartCoroutine(THold(c + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * 0.6f, 1.5f));
    }
}
