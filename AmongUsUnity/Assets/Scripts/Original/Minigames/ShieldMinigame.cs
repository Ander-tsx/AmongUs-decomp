using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Prime Shields: switch every red panel on.</summary>
public class ShieldMinigame : Minigame
{
    public Color OnColor = Color.white;
    public Color OffColor = Color.red;
    public SpriteRenderer[] Shields;
    public SpriteRenderer Gauge;
    public AudioClip FlipSound;

    bool[] _on;
    bool _done;

    protected override void OnBegin()
    {
        Shields = GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("ShieldButton")).OrderBy(r => r.name).ToArray();
        _on = new bool[Shields.Length];
        do { for (int i = 0; i < _on.Length; i++) _on[i] = Random.value < 0.4f; } while (_on.All(x => x) || _on.Count(x => !x) < 3);
        for (int i = 0; i < _on.Length; i++) Paint(i);
        if (Bot) StartCoroutine(BotSolve());
    }

    void Paint(int i) { Shields[i].color = _on[i] ? OnColor : OffColor; }

    protected override void Tick()
    {
        if (_done || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit == null) return;
        for (int i = 0; i < Shields.Length; i++)
            if (hit.transform == Shields[i].transform) { Toggle(i); break; }
    }

    void Toggle(int i)
    {
        _on[i] = !_on[i];
        Paint(i);
        Sfx(FlipSound);
        if (_on.All(x => x)) { _done = true; Win(0.7f); }
    }

    IEnumerator BotSolve()
    {
        for (int i = 0; i < _on.Length; i++) if (!_on[i]) { yield return new WaitForSeconds(0.08f); Toggle(i); }
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        for (int i = 0; i < Shields.Length; i++)
            if (!_on[i]) yield return StartCoroutine(TClick(Center(Shields[i].transform)));
    }
}
