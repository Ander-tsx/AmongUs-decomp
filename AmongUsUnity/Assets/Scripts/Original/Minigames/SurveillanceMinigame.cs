using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Security console: four live camera feeds rendered into the original monitor viewports.</summary>
public class SurveillanceMinigame : Minigame
{
    public Material DefaultMaterial;
    public Material StaticMaterial;

    readonly List<Camera> _cams = new List<Camera>();
    readonly List<RenderTexture> _rts = new List<RenderTexture>();
    Renderer[] _ports;
    bool _down;

    protected override void OnBegin()
    {
        var fill = Find("FillQuad");
        if (fill != null)
        {
            var fr = fill.GetComponent<Renderer>();
            if (fr != null) { fr.sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = Color.black }; fr.sortingOrder = 100; }
            fill.localPosition = new Vector3(fill.localPosition.x, fill.localPosition.y, 30f);
        }
        foreach (var n in GetComponentsInChildren<Transform>(true))
            if (n.name == "Noise") { var nr = n.GetComponent<Renderer>(); if (nr != null) { nr.sharedMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.12f, 0.16f, 0.16f) }; nr.sortingOrder = 119; } }
        _ports = new Renderer[4];
        for (int i = 0; i < 4; i++)
        {
            var t = Find(i == 0 ? "ViewPort" : "ViewPort (" + i + ")");
            _ports[i] = t != null ? t.GetComponent<Renderer>() : null;
        }
        var cams = FindObjectsByType<SurvCamera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).ToArray();
        _down = Arg == "down";
        int mask = ~((1 << AmongUsLan.UiKit.Layer) | (1 << AmongUsLan.FogOfWar.FogLayer));
        for (int i = 0; i < 4 && i < cams.Length; i++)
        {
            var rt = new RenderTexture(512, 512, 16);
            var go = new GameObject("SurvFeed" + i);
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = cams[i].CamSize > 0 ? cams[i].CamSize : 2.35f;
            cam.cullingMask = mask;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.targetTexture = rt;
            cam.depth = -5;
            cam.enabled = !_down;
            go.transform.position = cams[i].transform.position + cams[i].Offset + new Vector3(0, 0, -20);
            _cams.Add(cam); _rts.Add(rt);
            if (_ports[i] != null)
            {
                var m = new Material(Shader.Find("Unlit/Texture")) { mainTexture = _down ? null : rt };
                if (_down) m.color = new Color(0.1f, 0.1f, 0.1f);
                _ports[i].sharedMaterial = m;
                _ports[i].sortingOrder = 120;
            }
        }
        if (_down)
        {
            var t = MakeText(transform, "COMMS OFFLINE", 0.5f, Color.red);
            t.transform.localPosition = new Vector3(0, 0.7f, -8f);
        }
        if (Bot) Invoke(nameof(BotDone), 0.5f);
    }

    void BotDone() { Close(true); }

    protected override void Tick() { }

    void OnDisable() { Cleanup(); }
    void OnDestroyExtra() { Cleanup(); }

    void Cleanup()
    {
        foreach (var c in _cams) if (c != null) Destroy(c.gameObject);
        foreach (var r in _rts) if (r != null) r.Release();
        _cams.Clear(); _rts.Clear();
    }
}
