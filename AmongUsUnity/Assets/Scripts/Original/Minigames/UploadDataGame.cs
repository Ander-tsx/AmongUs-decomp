using System.Collections;
using UnityEngine;
using PowerTools;

/// <summary>Download / Upload Data: press the button and wait while the courier carries the file across.</summary>
public class UploadDataGame : Minigame
{
    public SpriteAnim LeftFolder;
    public SpriteAnim RightFolder;
    public AnimationClip FolderOpen;
    public AnimationClip FolderClose;
    public SpriteAnim Runner;
    public HorizontalGauge Gauge;

    TextMesh _status, _label;
    Transform _runner, _status0;
    bool _running, _done;
    float _t;
    Vector3 _from, _to;
    const float Duration = 5f;

    protected override void OnBegin()
    {
        LeftFolder = FindC<SpriteAnim>("LeftFront"); RightFolder = FindC<SpriteAnim>("RightFront"); Runner = FindC<SpriteAnim>("Runner");
        var gt = Find("Status");
        Gauge = gt != null ? gt.GetComponentInChildren<HorizontalGauge>(true) : null;
        _runner = Runner.transform;
        _from = _runner.localPosition;
        _to = new Vector3(1.23f, _from.y, _from.z);
        bool upload = Step >= 1;
        var st = Find("Status");
        if (st != null) st.gameObject.SetActive(true);
        Gauge.Set(0f);
        _status = MakeText(transform, upload ? "Upload to HQ" : "Download from " + Room, 0.3f, Color.white);
        _status.transform.localPosition = new Vector3(0f, 1.2f, -4f);
        // button
        var ui = AmongUsLan.UiKit.I;
        var btn = ui.Sliced("buttonClick", transform, new Vector2(0, -1.2f), new Vector2(1.5f, 0.45f), 120);
        btn.gameObject.layer = AmongUsLan.UiKit.Layer;
        btn.transform.localPosition = new Vector3(0, -1.0f, -3f);
        btn.name = "TransferButton";
        btn.gameObject.AddComponent<BoxCollider2D>().size = new Vector2(1.5f, 0.45f);
        _label = MakeText(btn.transform, upload ? "UPLOAD" : "DOWNLOAD", 0.2f, Color.white);
        if (LeftFolder != null && FolderOpen != null) LeftFolder.Play(FolderOpen);
        if (Bot) StartCoroutine(Run());
    }

    protected override void Tick()
    {
        if (_done) return;
        if (!_running && MouseDown && !Bot)
        {
            var hit = HitAt(Mouse);
            if (hit != null && hit.name == "TransferButton") StartCoroutine(Run());
        }
    }

    IEnumerator Run()
    {
        _running = true;
        _status.text = Step >= 1 ? "Uploading..." : "Downloading...";
        float dur = Bot ? 0.6f : Duration;
        while (_t < dur)
        {
            _t += Time.deltaTime;
            float k = _t / dur;
            _runner.localPosition = Vector3.Lerp(_from, _to, k);
            Gauge.Set(k);
            yield return null;
        }
        if (RightFolder != null && FolderClose != null) RightFolder.Play(FolderClose);
        _status.text = Step >= 1 ? "Upload Complete" : "Download Complete";
        _done = true;
        Win(0.9f);
    }
}
