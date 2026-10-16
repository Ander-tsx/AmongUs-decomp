using System.Collections;
using UnityEngine;

/// <summary>Container for multi-part minigames (Refuel): shows the stage that belongs to the current task step.</summary>
public class MultistageMinigame : Minigame
{
    public RefuelStage[] Stages;
    RefuelStage _active;

    protected override void OnBegin()
    {
        int idx = Mathf.Clamp(Step, 0, Stages.Length - 1);
        _active = Stages[idx];
        foreach (var s in Stages) if (s != null) s.gameObject.SetActive(false);
        _active.gameObject.SetActive(true);
        _active.Begin(this);
    }

    protected override void Tick() { if (_active != null) _active.Tick(this); }

    public void TestHoldAt(Vector2 p, float seconds) { StartCoroutine(THold(p, seconds)); }
    public bool PointerHeld => MouseHeld;
    public Vector2 PointerPos => Mouse;

    public void StageDone() { Win(0.8f); }

    public override bool HasTest => true;
    public override IEnumerator TestPlay() { yield return StartCoroutine(_active.TestHold(this)); }
}
