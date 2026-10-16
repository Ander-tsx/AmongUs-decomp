using System.Collections;
using UnityEngine;

/// <summary>Empty Garbage: pull the lever down and hold it while the grinder runs.</summary>
public class EmptyGarbageMinigame : Minigame
{
    public FloatRange HandleRange;
    public AudioClip LeverDown;
    public AudioClip LeverUp;
    public AudioClip GrinderStart;
    public AudioClip GrinderLoop;

    Transform _handle;
    bool _drag, _running, _done;
    float _grab;

    protected override void OnBegin()
    {
        _handle = Find("garbage_leverHandle");
        SetHandle(HandleRange.max);
        if (Bot) StartCoroutine(Run());
    }

    void SetHandle(float y)
    {
        y = Mathf.Clamp(y, HandleRange.min, HandleRange.max);
        var lp = _handle.localPosition; lp.y = y; _handle.localPosition = lp;
    }

    protected override void Tick()
    {
        if (_done || _running || Bot) return;
        var m = Mouse;
        if (MouseDown && HitAt(m) == _handle.GetComponent<Collider2D>()) { _drag = true; _grab = _handle.position.y - m.y; Sfx(LeverDown); }
        if (_drag && MouseHeld) SetHandle(transform.InverseTransformPoint(new Vector3(0, m.y + _grab, 0)).y - 0f);
        if (_drag && MouseUp)
        {
            _drag = false;
            if (_handle.localPosition.y <= HandleRange.min + 0.08f) StartCoroutine(Run());
            else { SetHandle(HandleRange.max); Sfx(LeverUp); }
        }
    }

    IEnumerator Run()
    {
        _running = true;
        SetHandle(HandleRange.min);
        Sfx(GrinderStart);
        var pivot = Find("ShakePivot");
        float t = 0, dur = Bot ? 0.5f : 2.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (pivot != null) pivot.localPosition = new Vector3(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f), 0);
            yield return null;
        }
        if (pivot != null) pivot.localPosition = Vector3.zero;
        SetHandle(HandleRange.max);
        _done = true;
        Win(0.4f);
    }
}
