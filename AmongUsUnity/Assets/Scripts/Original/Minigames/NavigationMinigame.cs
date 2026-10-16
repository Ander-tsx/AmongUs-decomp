using System.Collections;
using UnityEngine;

/// <summary>Stabilize Steering: drag the crosshair onto the centre of the graph.</summary>
public class NavigationMinigame : Minigame
{
    public Transform TwoAxisImage;
    public SpriteRenderer CrossHairImage;
    public Collider2D hitbox;

    bool _drag, _done;
    Vector2 _grab;
    float _limit = 2.3f;

    protected override void OnBegin()
    {
        CrossHairImage = Find("CrossHairs").GetComponent<SpriteRenderer>();
        var p = CrossHairImage.transform.localPosition;
        float x = Random.Range(0.9f, 2.0f) * (Random.value < 0.5f ? -1 : 1), y = Random.Range(0.7f, 1.7f) * (Random.value < 0.5f ? -1 : 1);
        CrossHairImage.transform.localPosition = new Vector3(x, y, p.z);
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        var t = CrossHairImage.transform;
        if (MouseDown && Vector2.Distance(m, t.position) < 0.6f) { _drag = true; _grab = (Vector2)t.position - m; }
        if (_drag && MouseHeld)
        {
            var p = m + _grab;
            var lp = transform.InverseTransformPoint(new Vector3(p.x, p.y, t.position.z));
            lp.x = Mathf.Clamp(lp.x, -_limit, _limit); lp.y = Mathf.Clamp(lp.y, -_limit, _limit);
            t.localPosition = new Vector3(lp.x, lp.y, t.localPosition.z);
            if (Mathf.Abs(lp.x) < 0.1f && Mathf.Abs(lp.y) < 0.1f) { _done = true; t.localPosition = new Vector3(0, 0, t.localPosition.z); Win(0.6f); }
        }
        if (MouseUp) _drag = false;
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        var t = CrossHairImage.transform; var from = t.localPosition; float k = 0;
        while (k < 1f) { k += Time.deltaTime * 2f; t.localPosition = Vector3.Lerp(from, new Vector3(0, 0, from.z), k); yield return null; }
        _done = true; Win(0.4f);
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        var t = CrossHairImage.transform;
        yield return StartCoroutine(TDrag(3f, (Vector2)t.position, (Vector2)transform.TransformPoint(Vector3.zero)));
    }
}
