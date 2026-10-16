using System.Collections;
using UnityEngine;

/// <summary>Chart Course: drag the ship along the plotted path to the destination star.</summary>
public class CourseMinigame : Minigame
{
    public Transform Ship;
    public Transform Destination;
    public int NumPoints = 5;
    public FloatRange XRange;
    public FloatRange YRange;

    LineRenderer _line;
    Vector2[] _pts;
    Vector3 _home;
    int _next;
    bool _drag, _done;

    protected override void OnBegin()
    {
        Ship = Find("Ship"); Destination = Find("Destination");
        _home = Ship.localPosition;
        _line = FindC<LineRenderer>("PathLine");
        _pts = new Vector2[NumPoints + 2];
        _pts[0] = _home;
        _pts[_pts.Length - 1] = Destination.localPosition;
        for (int i = 1; i <= NumPoints; i++)
        {
            float t = i / (float)(NumPoints + 1);
            _pts[i] = new Vector2(Mathf.Lerp(_home.x, Destination.localPosition.x, t), Random.Range(YRange.min, YRange.max));
        }
        if (_line != null)
        {
            _line.useWorldSpace = false;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startColor = _line.endColor = new Color(0.35f, 1f, 0.55f, 0.9f);
            _line.startWidth = _line.endWidth = 0.05f;
            _line.sortingOrder = 112;
            _line.positionCount = _pts.Length;
            for (int i = 0; i < _pts.Length; i++) _line.SetPosition(i, new Vector3(_pts[i].x, _pts[i].y, -1f));
        }
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown && Vector2.Distance(m, Ship.position) < 0.55f) { _drag = true; _next = 1; }
        if (_drag && MouseHeld)
        {
            var lp = transform.InverseTransformPoint(new Vector3(m.x, m.y, 0));
            Ship.localPosition = new Vector3(lp.x, lp.y, Ship.localPosition.z);
            if (_next < _pts.Length && Vector2.Distance(new Vector2(lp.x, lp.y), _pts[_next]) < 0.35f) _next++;
            if (_next >= _pts.Length) { _done = true; Ship.localPosition = new Vector3(_pts[_pts.Length - 1].x, _pts[_pts.Length - 1].y, Ship.localPosition.z); Win(0.6f); }
        }
        if (MouseUp && _drag && !_done) { _drag = false; Ship.localPosition = _home; _next = 1; }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        for (int i = 1; i < _pts.Length; i++) { yield return new WaitForSeconds(0.05f); Ship.localPosition = new Vector3(_pts[i].x, _pts[i].y, Ship.localPosition.z); }
        _done = true; Win(0.4f);
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        var path = new System.Collections.Generic.List<Vector2> { (Vector2)Ship.position };
        for (int i = 1; i < _pts.Length; i++) path.Add((Vector2)transform.TransformPoint(new Vector3(_pts[i].x, _pts[i].y, 0)));
        yield return StartCoroutine(TDrag(3f, path.ToArray()));
    }
}
