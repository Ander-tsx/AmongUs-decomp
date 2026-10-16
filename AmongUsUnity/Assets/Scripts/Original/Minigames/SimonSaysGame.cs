using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Start Reactor: repeat the growing pattern shown on the left screen on the right keypad.</summary>
public class SimonSaysGame : Minigame
{
    public SpriteRenderer[] LeftSide;
    public SpriteRenderer[] Buttons;
    public SpriteRenderer[] LeftLights;
    public SpriteRenderer[] RightLights;
    public AudioClip ButtonPressSound;
    public AudioClip FailSound;

    readonly List<int> _sequence = new List<int>();
    int _stage, _input;
    bool _showing, _done;

    static readonly Color Dark = new Color(0.06f, 0.06f, 0.08f), Unlit = new Color(0.45f, 0.45f, 0.45f), Lit = new Color(0.45f, 1f, 0.5f);

    protected override void OnBegin()
    {
        var all = GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("blank")).ToArray();
        bool Under(Transform t, string root) { for (var p = t; p != null; p = p.parent) if (p.name == root) return true; return false; }
        LeftSide = all.Where(r => Under(r.transform, "Background")).OrderBy(r => r.name).ToArray();
        Buttons = all.Where(r => Under(r.transform, "BackgroundRight")).OrderBy(r => r.name).ToArray();
        LeftLights = GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("sslights") && Under(r.transform, "Background")).OrderBy(r => r.name).ToArray();
        RightLights = GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name.StartsWith("sslights") && Under(r.transform, "BackgroundRight")).OrderBy(r => r.name).ToArray();
        foreach (var r in LeftSide) r.color = Dark;
        _sequence.Clear();
        for (int i = 0; i < 5; i++) _sequence.Add(Random.Range(0, LeftSide.Length));
        UpdateLights();
        StartCoroutine(Show(0.6f));
    }

    void UpdateLights()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < LeftLights.Length) LeftLights[i].color = i <= _stage ? Lit : Unlit;
            if (i < RightLights.Length) RightLights[i].color = i < _input ? Lit : Unlit;
        }
    }

    IEnumerator Show(float delay)
    {
        _showing = true; _input = 0;
        UpdateLights();
        yield return new WaitForSeconds(delay);
        for (int i = 0; i <= _stage; i++)
        {
            LeftSide[_sequence[i]].color = Color.white;
            yield return new WaitForSeconds(0.45f);
            LeftSide[_sequence[i]].color = Dark;
            yield return new WaitForSeconds(0.2f);
        }
        _showing = false;
        if (Bot) StartCoroutine(BotInput());
    }

    protected override void Tick()
    {
        if (_done || _showing || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit == null) return;
        for (int i = 0; i < Buttons.Length; i++) if (hit.transform == Buttons[i].transform) Press(i);
    }

    void Press(int i)
    {
        if (_done || _showing) return;
        Sfx(ButtonPressSound);
        StartCoroutine(Flash(Buttons[i]));
        if (_sequence[_input] == i)
        {
            _input++;
            UpdateLights();
            if (_input > _stage)
            {
                _stage++;
                if (_stage >= 5) { _done = true; Win(0.8f); }
                else StartCoroutine(Show(0.8f));
            }
        }
        else
        {
            Sfx(FailSound);
            _stage = 0; _input = 0;
            StartCoroutine(Show(1.0f));
        }
    }

    IEnumerator Flash(SpriteRenderer r) { r.color = Color.green; yield return new WaitForSeconds(0.15f); r.color = Color.white; }

    IEnumerator BotInput()
    {
        for (int k = 0; k <= _stage; k++) { yield return new WaitForSeconds(0.08f); if (_done) yield break; Press(_sequence[k]); }
    }
}
