using UnityEngine;

/// <summary>Submit Scan: stay at the scanner until the progress bar fills.</summary>
public class MedScanMinigame : Minigame
{
    public HorizontalGauge gauge;
    public float ScanDuration = 10f;
    public float ScanTimer;
    public AudioClip ScanSound;
    public AudioClip TextSound;

    TextMesh _text;
    bool _done;

    protected override void OnBegin()
    {
        _text = MakeText(transform, "Scanning...", 0.4f, new Color(0.6f, 1f, 0.7f));
        _text.transform.localPosition = new Vector3(-0.3f, 0.2f, -3);
        Sfx(ScanSound);
        gauge.Set(0f);
        if (Bot) ScanDuration = 1f;
    }

    protected override void Tick()
    {
        if (_done) return;
        ScanTimer += Time.deltaTime;
        gauge.Set(ScanTimer / ScanDuration);
        if (ScanTimer >= ScanDuration) { _done = true; _text.text = "Scan Complete"; Sfx(TextSound); Win(1f); }
    }
}
