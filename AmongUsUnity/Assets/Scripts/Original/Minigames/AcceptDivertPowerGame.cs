using System.Collections;
using UnityEngine;

/// <summary>Divert Power (receiving side): flip the switch.</summary>
public class AcceptDivertPowerGame : Minigame
{
    public AudioClip SwitchSound;
    Transform _switch;
    bool _done;

    protected override void OnBegin()
    {
        _switch = Find("electricity_Receive_switch");
        if (Bot) StartCoroutine(BotSolve());
    }

    protected override void Tick()
    {
        if (_done || Bot || !MouseDown) return;
        var hit = HitAt(Mouse);
        if (hit != null && hit.transform == _switch) Flip();
    }

    void Flip()
    {
        _done = true;
        Sfx(SwitchSound);
        _switch.localScale = new Vector3(_switch.localScale.x, -_switch.localScale.y, _switch.localScale.z);
        Win(0.7f);
    }

    IEnumerator BotSolve() { yield return new WaitForSeconds(0.3f); Flip(); }

    public override bool HasTest => true;
    public override IEnumerator TestPlay() { yield return new WaitForSeconds(0.3f); yield return StartCoroutine(TClick(Center(_switch))); }
}
