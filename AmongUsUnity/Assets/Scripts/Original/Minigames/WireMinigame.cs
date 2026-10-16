using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>Fix Wiring: drag each coloured wire from the left to the socket of the same colour on the right.</summary>
public class WireMinigame : Minigame
{
    public Sprite[] Symbols;
    public Wire[] LeftNodes;
    public WireNode[] RightNodes;
    public SpriteRenderer[] LeftLights;
    public SpriteRenderer[] RightLights;
    public AudioClip[] WireSounds;

    static readonly Color[] Colors =
    {
        new Color32(255, 0, 0, 255), new Color32(0, 38, 255, 255), new Color32(255, 235, 4, 255), new Color32(255, 0, 255, 255),
    };
    static readonly Color Off = new Color(0.4f, 0.4f, 0.4f, 1f), On = new Color(1f, 1f, 0.35f, 1f);

    int[] _expected;                 // left index -> right index with the same colour
    int[] _actual;                   // left index -> right index it is plugged into (-1 none)
    Vector3[] _headHome;
    Wire _held;
    int _heldIdx = -1;
    bool _done;

    protected override void OnBegin()
    {
        int n = LeftNodes.Length;
        var left = Enumerable.Range(0, n).OrderBy(_ => Random.value).ToArray();
        var right = Enumerable.Range(0, n).OrderBy(_ => Random.value).ToArray();
        _expected = new int[n]; _actual = Enumerable.Repeat(-1, n).ToArray(); _headHome = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            LeftNodes[i].WireId = (sbyte)left[i];
            RightNodes[i].WireId = (sbyte)right[i];
            Tint(LeftNodes[i], Colors[left[i]]);
            foreach (var r in RightNodes[i].WireColors) r.color = Colors[right[i]];
            if (RightNodes[i].BaseSymbol != null) RightNodes[i].BaseSymbol.gameObject.SetActive(false);
            if (LeftNodes[i].SymbolBase != null) LeftNodes[i].SymbolBase.gameObject.SetActive(false);
            _headHome[i] = LeftNodes[i].hitbox.transform.localPosition;
            LeftLights[i].color = Off; RightLights[i].color = Off;
            UpdateWire(i, LeftNodes[i].hitbox.transform.position);
        }
        for (int i = 0; i < n; i++) _expected[i] = System.Array.FindIndex(RightNodes, r => r.WireId == LeftNodes[i].WireId);
        if (Bot) StartCoroutine(BotSolve());
    }

    static void Tint(Wire w, Color c)
    {
        foreach (var r in new[] { w.Liner, w.ColorBase, w.ColorEnd, w.WireTip })
            if (r != null) r.color = c;
        // the head carries its coloured tip as a child sprite
        foreach (var r in w.hitbox.GetComponentsInChildren<SpriteRenderer>(true)) if (r.name.StartsWith("electricity_wiresBase")) r.color = c;
    }

    /// <summary>Stretch the cord from the wire's base to the plug head.</summary>
    void UpdateWire(int i, Vector3 headWorld)
    {
        var w = LeftNodes[i];
        var head = w.hitbox.transform;
        head.position = new Vector3(headWorld.x, headWorld.y, head.position.z);
        Vector2 from = (Vector2)w.transform.position + new Vector2(0.4f, 0f);
        Vector2 to = head.position;
        var d = to - from;
        float len = Mathf.Max(0.05f, d.magnitude);
        var liner = w.Liner.transform;
        liner.position = new Vector3((from.x + to.x) / 2f, (from.y + to.y) / 2f, liner.position.z);
        liner.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        liner.localScale = Vector3.one;
        w.Liner.drawMode = SpriteDrawMode.Sliced;
        w.Liner.size = new Vector2(len, 0.17f);
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown && _held == null)
        {
            var hit = HitAt(m);
            for (int i = 0; i < LeftNodes.Length && hit != null; i++)
                if (LeftNodes[i].hitbox == hit && _actual[i] < 0) { _held = LeftNodes[i]; _heldIdx = i; Sfx(WireSounds != null && WireSounds.Length > 0 ? WireSounds[0] : null); }
        }
        if (_held != null)
        {
            if (MouseHeld) UpdateWire(_heldIdx, new Vector3(Mathf.Max(m.x, _held.transform.position.x + 0.55f), m.y, 0));
            if (MouseUp)
            {
                int target = -1;
                var hit = HitAt(m);
                for (int j = 0; j < RightNodes.Length && hit != null; j++) if (RightNodes[j].hitbox == hit) target = j;
                if (target >= 0 && RightNodes[target].WireId == _held.WireId && !_actual.Contains(target)) Connect(_heldIdx, target);
                else Retract(_heldIdx);
                _held = null; _heldIdx = -1;
            }
        }
    }

    void Connect(int i, int j)
    {
        _actual[i] = j;
        var tgt = RightNodes[j].transform.position + new Vector3(-0.25f, 0f, 0f);
        UpdateWire(i, tgt);
        LeftLights[i].color = On; RightLights[j].color = On;
        Sfx(WireSounds != null && WireSounds.Length > 1 ? WireSounds[1] : null);
        if (_actual.All(a => a >= 0)) { _done = true; Win(0.8f); }
    }

    void Retract(int i)
    {
        var w = LeftNodes[i];
        UpdateWire(i, w.transform.TransformPoint(_headHome[i]));
        Sfx(WireSounds != null && WireSounds.Length > 2 ? WireSounds[2] : null);
    }

    IEnumerator BotSolve()
    {
        for (int i = 0; i < LeftNodes.Length; i++)
        {
            yield return new WaitForSeconds(0.1f);
            Connect(i, _expected[i]);
        }
    }
}
