using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Fix Lights: flip every switch up.</summary>
public class SwitchMinigame : Minigame
{
    public Color OnColor = Color.green;
    public Color OffColor = new Color(0.1f, 0.3f, 0.1f);
    public AudioClip FlipSound;

    SpriteRenderer[] _switches, _lights;
    bool[] _on;
    HorizontalGauge _gauge;
    bool _done;

    protected override void OnBegin()
    {
        _switches = new SpriteRenderer[5]; _lights = new SpriteRenderer[5];
        for (int i = 0; i < 5; i++)
        {
            var sw = Find(i == 0 ? "switch" : "switch (" + i + ")");
            _switches[i] = sw.GetComponent<SpriteRenderer>();
            _lights[i] = Find(i == 0 ? "light" : "light (" + i + ")").GetComponent<SpriteRenderer>();
        }
        _gauge = FindC<HorizontalGauge>("MiddleGraph");
        _on = new bool[5];
        do { for (int i = 0; i < 5; i++) _on[i] = Random.value < 0.4f; } while (_on.All(x => x));
        for (int i = 0; i < 5; i++) Paint(i);
        if (Bot) StartCoroutine(BotSolve());
    }

    void Paint(int i)
    {
        _lights[i].color = _on[i] ? OnColor : OffColor;
        var s = _switches[i].transform.localScale;
        _switches[i].transform.localScale = new Vector3(s.x, Mathf.Abs(s.y) * (_on[i] ? 1 : -1), s.z);
        if (_gauge != null) _gauge.Set(_on.Count(x => x) / 5f);
    }

    protected override void Tick()
    {
        if (_done || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit == null) return;
        for (int i = 0; i < 5; i++) if (hit.transform == _switches[i].transform) Flip(i);
    }

    void Flip(int i)
    {
        _on[i] = !_on[i];
        Paint(i);
        Sfx(FlipSound);
        if (_on.All(x => x)) { _done = true; Win(0.6f); }
    }

    IEnumerator BotSolve() { for (int i = 0; i < 5; i++) if (!_on[i]) { yield return new WaitForSeconds(0.08f); Flip(i); } }
}
