using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Inspect Sample: start the analysis, then (after the wait) pick the anomalous sample.</summary>
public class SampleMinigame : Minigame
{
    public float TimePerStep = 60f;
    public AudioClip ButtonSound;
    public AudioClip FailSound;
    public AudioClip[] DropSounds;

    SpriteRenderer[] _tubes;
    Transform[] _buttons;
    Transform _confirm;
    TextMesh _text;
    int _odd, _selected = -1;
    bool _done;

    static readonly Color Normal = new Color(0.3f, 0.7f, 1f), Odd = new Color(1f, 0.35f, 0.35f);

    protected override void OnBegin()
    {
        _tubes = GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("medBay_liquid")).OrderBy(r => r.name).ToArray();
        _buttons = GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("medBay_sampleButton")).OrderBy(t => t.name).ToArray();
        _confirm = Find("medBay_buttonConfirm");
        var top = Find("medBay_panelTop");
        _text = MakeText(top != null ? top : transform, Step == 0 ? "Press confirm to start the analysis" : "Select the anomalous sample", 0.28f, Color.white);
        _odd = Random.Range(0, _tubes.Length);
        for (int i = 0; i < _tubes.Length; i++) _tubes[i].color = Step == 0 ? Normal : (i == _odd ? Odd : Normal);
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit == null) return;
        if (_confirm != null && hit.transform == _confirm) Confirm();
        else for (int i = 0; i < _buttons.Length; i++) if (hit.transform == _buttons[i]) { _selected = i; Sfx(ButtonSound); Highlight(); }
    }

    void Highlight()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var sr = _buttons[i].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = i == _selected ? Color.green : Color.white;
        }
    }

    void Confirm()
    {
        if (Step == 0) { Sfx(ButtonSound); _done = true; _text.text = "Analysis started - come back in a minute"; Win(1.0f); return; }
        if (_selected == _odd) { _done = true; _text.text = "Correct"; Win(0.8f); }
        else { Sfx(FailSound); _selected = -1; Highlight(); _odd = Random.Range(0, _tubes.Length); for (int i = 0; i < _tubes.Length; i++) _tubes[i].color = i == _odd ? Odd : Normal; }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        _selected = _odd; Confirm();
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        if (Step >= 1) yield return StartCoroutine(TClick(Center(_buttons[_odd])));
        yield return StartCoroutine(TClick(Center(_confirm)));
    }
}
