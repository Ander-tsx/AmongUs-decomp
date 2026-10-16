using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Unlock Manifolds: press the ten numbered buttons in ascending order.</summary>
public class UnlockManifoldsMinigame : Minigame
{
    public SpriteRenderer[] Buttons;
    public int SystemId;
    public AudioClip PressButtonSound;
    public AudioClip FailSound;

    int[] _number;       // number shown on each button position
    bool[] _lit;
    int _next = 1;
    bool _done;

    protected override void OnBegin()
    {
        Buttons = Enumerable.Range(0, 10).Select(i => Find("Numbers_" + i)).Where(t => t != null).Select(t => t.GetComponent<SpriteRenderer>()).ToArray();
        var sprites = Buttons.Select(b => b.sprite).ToArray();
        // sprite names end with the number (reactorButton01..10): shuffle which sprite sits where
        var order = Enumerable.Range(0, Buttons.Length).OrderBy(_ => Random.value).ToArray();
        _number = new int[Buttons.Length];
        _lit = new bool[Buttons.Length];
        for (int i = 0; i < Buttons.Length; i++)
        {
            Buttons[i].sprite = sprites[order[i]];
            _number[i] = order[i] + 1;
        }
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit == null) return;
        for (int i = 0; i < Buttons.Length; i++) if (hit.transform == Buttons[i].transform) Press(i);
    }

    void Press(int i)
    {
        if (_done || _lit[i]) return;
        if (_number[i] == _next)
        {
            _lit[i] = true;
            Buttons[i].color = new Color(0.55f, 1f, 0.6f);
            Sfx(PressButtonSound);
            _next++;
            if (_next > Buttons.Length) { _done = true; Win(0.7f); }
        }
        else
        {
            Sfx(FailSound);
            _next = 1;
            for (int k = 0; k < _lit.Length; k++) { _lit[k] = false; Buttons[k].color = Color.white; }
        }
    }

    IEnumerator BotSolve()
    {
        for (int n = 1; n <= Buttons.Length; n++)
        {
            yield return new WaitForSeconds(0.08f);
            Press(System.Array.IndexOf(_number, n));
        }
    }
}
