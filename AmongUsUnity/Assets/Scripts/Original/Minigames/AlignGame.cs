using System.Collections;
using UnityEngine;

/// <summary>Align Engine Output: slide the handle until the engine sits on the centre line.</summary>
public class AlignGame : Minigame
{
    public FloatRange YRange;
    public AnimationCurve curve;
    Transform engine;
    public Collider2D col;

    bool _drag, _done;
    float _grabDy;

    protected override void OnBegin()
    {
        engine = Find("engineAlign_engine");
        if (col == null) col = Find("engineAlign_slider").GetComponent<Collider2D>();
        float y = Random.Range(0.9f, 1.9f) * (Random.value < 0.5f ? -1 : 1);
        SetY(y);
        if (Bot) StartCoroutine(BotSolve());
    }

    void SetY(float y)
    {
        y = Mathf.Clamp(y, YRange.min, YRange.max);
        var h = col.transform; var lp = h.localPosition; lp.y = y; h.localPosition = lp;
        var e = engine.localPosition; e.y = y; engine.localPosition = e;
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown && HitAt(m) == col) { _drag = true; _grabDy = col.transform.position.y - m.y; }
        if (_drag && MouseHeld) SetY(transform.InverseTransformPoint(new Vector3(0, m.y + _grabDy, 0)).y);
        if (_drag && MouseUp)
        {
            _drag = false;
            if (Mathf.Abs(col.transform.localPosition.y) < 0.12f) { _done = true; SetY(0); Win(0.6f); }
        }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        float y = col.transform.localPosition.y;
        while (Mathf.Abs(y) > 0.02f) { y = Mathf.MoveTowards(y, 0, Time.deltaTime * 3f); SetY(y); yield return null; }
        _done = true; Win(0.4f);
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        var h = (Vector2)col.bounds.center;
        var mid = (Vector2)transform.TransformPoint(Vector3.zero);
        yield return StartCoroutine(TDrag(2f, h, new Vector2(h.x, mid.y + (h.y - col.transform.position.y))));
    }
}
