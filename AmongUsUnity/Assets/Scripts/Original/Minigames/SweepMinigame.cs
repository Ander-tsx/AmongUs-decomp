using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Calibrate Distributor: stop each rotating plug when it lines up with its contact.</summary>
public class SweepMinigame : Minigame
{
    public SpriteRenderer[] Spinners;
    public SpriteRenderer[] Shadows;
    public SpriteRenderer[] Lights;
    public HorizontalGauge[] Gauges;
    public float SpinRate;
    public AudioClip SpinningSound;
    public AudioClip AcceptSound;
    public AudioClip RejectSound;

    Transform[] _pivots, _buttons;
    float[] _angle;
    int _idx;
    bool _done;

    protected override void OnBegin()
    {
        int n = Spinners.Length;
        _pivots = Spinners.Select(s => s.transform.parent).ToArray();
        _angle = new float[n];
        _buttons = new Transform[n];
        var btns = GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("calibratorButton")).OrderBy(t => t.name).ToArray();
        for (int i = 0; i < n && i < btns.Length; i++) _buttons[i] = btns[i];
        ResetAll();
        if (Bot) StartCoroutine(BotSolve());
    }

    void ResetAll()
    {
        _idx = 0;
        for (int i = 0; i < _angle.Length; i++)
        {
            _angle[i] = Random.Range(70f, 290f);
            if (Lights[i] != null) Lights[i].gameObject.SetActive(false);
            Gauges[i].Set(0.5f);
            Apply(i);
        }
    }

    void Apply(int i)
    {
        _pivots[i].localRotation = Quaternion.Euler(0, 0, _angle[i]);
    }

    float Wrapped(int i) => Mathf.DeltaAngle(_angle[i], 0f);

    protected override void Tick()
    {
        if (_done) return;
        if (_idx < _angle.Length)
        {
            _angle[_idx] = (_angle[_idx] + SpinRate * Time.deltaTime) % 360f;
            Apply(_idx);
            Gauges[_idx].Set(1f - Mathf.Abs(Wrapped(_idx)) / 180f);
        }
        if (MouseDown && !Bot)
        {
            var hit = HitAt(Mouse);
            if (hit != null) for (int j = 0; j < _buttons.Length; j++) if (_buttons[j] != null && hit.transform == _buttons[j]) HitButton(j);
        }
    }

    void HitButton(int j)
    {
        if (_done || _idx >= _angle.Length) return;
        if (j == _idx && Mathf.Abs(Wrapped(_idx)) <= 22f)
        {
            _angle[_idx] = 0f; Apply(_idx);
            Gauges[_idx].Set(1f);
            if (Lights[_idx] != null) Lights[_idx].gameObject.SetActive(true);
            Sfx(AcceptSound);
            _idx++;
            if (_idx >= _angle.Length) { _done = true; Win(0.8f); }
        }
        else { Sfx(RejectSound); ResetAll(); }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        while (!_done)
        {
            if (_idx < _angle.Length && Mathf.Abs(Wrapped(_idx)) <= 10f) HitButton(_idx);
            yield return null;
        }
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        float until = Time.time + 40f;
        while (!_done && Time.time < until)
        {
            if (_idx < _angle.Length && Mathf.Abs(Wrapped(_idx)) <= 8f && _buttons[_idx] != null)
                yield return StartCoroutine(TClick(Center(_buttons[_idx])));
            else yield return null;
        }
    }
}
