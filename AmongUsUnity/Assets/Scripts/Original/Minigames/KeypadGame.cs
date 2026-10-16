using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Restore Oxygen: type the code shown on the sticky note.</summary>
public class KeypadGame : Minigame
{
    public AudioClip PressSound;
    public AudioClip FailSound;

    Transform[] _keys;      // index 0..9 = digit
    Transform _clear, _ok;
    TextMesh _display;
    string _typed = "";
    string _code;
    bool _done;

    protected override void OnBegin()
    {
        _code = string.IsNullOrEmpty(Arg) ? "12345" : Arg;
        _keys = new Transform[10];
        for (int d = 1; d <= 9; d++) _keys[d] = Find("reactorButton0" + d);
        _keys[0] = Find("reactorButton10");
        _clear = Find("admin_keypad_x"); _ok = Find("admin_keypad_check");
        _display = MakeText(transform, "", 0.5f, new Color(0.6f, 1f, 0.7f));
        _display.transform.localPosition = new Vector3(0f, 1.75f, -4f);
        var note = Find("StickyNote");
        var nt = MakeText(note != null ? note : transform, _code, 0.3f, Color.black);
        nt.transform.localPosition = new Vector3(0f, -0.1f, -3f);
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit == null) return;
        for (int d = 0; d < 10; d++) if (_keys[d] != null && hit.transform == _keys[d]) Press(d);
        if (hit.transform == _clear) { _typed = ""; _display.text = ""; Sfx(PressSound); }
        if (hit.transform == _ok) Submit();
    }

    void Press(int d)
    {
        if (_typed.Length >= 5) return;
        _typed += d; _display.text = _typed; Sfx(PressSound);
    }

    void Submit()
    {
        if (_typed == _code) { _done = true; _display.text = "OK"; Win(0.6f); }
        else { Sfx(FailSound); _typed = ""; _display.text = "ERR"; }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        foreach (var ch in _code) { Press(ch - '0'); yield return new WaitForSeconds(0.05f); }
        Submit();
    }
}
