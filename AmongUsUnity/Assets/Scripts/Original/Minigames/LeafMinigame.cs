using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Clean O2 Filter: drag every leaf out of the filter into the vacuum on the left.</summary>
public class LeafMinigame : Minigame
{
    public AudioClip[] LeaveSounds;
    public AudioClip[] SuckSounds;

    class Leaf { public Transform T; public bool Gone; public float Spin; }
    readonly List<Leaf> _leaves = new List<Leaf>();
    Leaf _held; Vector2 _grab;
    int _left;
    bool _done;
    const float MinX = -1.0f, MaxX = 1.75f, MinY = -1.9f, MaxY = 1.9f, SuckX = -1.55f;

    protected override void OnBegin()
    {
        var prefab = PrefabCatalog.Get("o2_leaf1");
        int n = 8;
        for (int i = 0; i < n && prefab != null; i++)
        {
            var go = Instantiate(prefab, transform);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = AmongUsLan.UiKit.Layer;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += 105;
            var lb = go.GetComponent<LeafBehaviour>();
            var sr = go.GetComponent<SpriteRenderer>();
            if (lb != null && lb.Images != null && lb.Images.Length > 0) sr.sprite = lb.Images[Random.Range(0, lb.Images.Length)];
            var rb = go.GetComponent<Rigidbody2D>(); if (rb != null) Destroy(rb);
            go.transform.localPosition = new Vector3(Random.Range(MinX + 0.2f, MaxX - 0.2f), Random.Range(MinY + 0.2f, MaxY - 0.2f), -4f - i * 0.01f);
            go.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f));
            _leaves.Add(new Leaf { T = go.transform, Spin = Random.Range(-45f, 45f) });
        }
        _left = _leaves.Count;
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown && _held == null)
            foreach (var c in AmongUsLan.UiKit.PointAll(m))
            {
                var l = _leaves.Find(x => !x.Gone && x.T == c.transform);
                if (l != null) { _held = l; _grab = (Vector2)l.T.position - m; Sfx(LeaveSounds); break; }
            }
        if (_held != null)
        {
            if (MouseHeld)
            {
                var p = m + _grab;
                var lp = transform.InverseTransformPoint(new Vector3(p.x, p.y, 0));
                _held.T.localPosition = new Vector3(Mathf.Clamp(lp.x, -2.4f, 2.4f), Mathf.Clamp(lp.y, MinY, MaxY), _held.T.localPosition.z);
                _held.T.Rotate(0, 0, _held.Spin * Time.deltaTime);
                if (_held.T.localPosition.x < SuckX) Suck(_held);
            }
            if (MouseUp) _held = null;
        }
    }

    void Suck(Leaf l)
    {
        if (l.Gone) return;
        l.Gone = true;
        if (_held == l) _held = null;
        Sfx(SuckSounds);
        StartCoroutine(Fly(l));
    }

    IEnumerator Fly(Leaf l)
    {
        float t = 0;
        var from = l.T.localPosition;
        while (t < 1f && l.T != null) { t += Time.deltaTime * 4f; l.T.localPosition = Vector3.Lerp(from, from + new Vector3(-1.5f, 0, 0), t); l.T.localScale *= 0.93f; yield return null; }
        if (l.T != null) Destroy(l.T.gameObject);
        if (--_left <= 0 && !_done) { _done = true; Win(0.5f); }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        foreach (var l in _leaves) { yield return new WaitForSeconds(0.05f); Suck(l); }
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        float until = Time.time + 50f;
        while (!_done && Time.time < until)
        {
            var l = _leaves.Find(x => !x.Gone && x.T != null);
            if (l == null) { yield return null; continue; }
            var to = (Vector2)transform.TransformPoint(new Vector3(SuckX - 0.4f, l.T.localPosition.y, 0));
            yield return StartCoroutine(TDrag(5f, Center(l.T), to));
        }
    }
}
