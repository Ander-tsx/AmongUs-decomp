using System.Collections;
using UnityEngine;

/// <summary>One refuelling panel: hold the button until the tank is full.</summary>
public class RefuelStage : MonoBehaviour
{
    public float RefuelDuration = 3f;
    public VerticalGauge srcGauge;
    public VerticalGauge destGauge;
    public AudioClip RefuelSound;

    SpriteRenderer _red, _green;
    Collider2D _button;
    float _fill;
    bool _done;
    AudioSource _loop;

    public void Begin(MultistageMinigame host)
    {
        _red = transform.Find("Background/engineFuel_buttonBase/redLight")?.GetComponent<SpriteRenderer>();
        _green = transform.Find("Background/engineFuel_buttonBase/GreenLight")?.GetComponent<SpriteRenderer>();
        _button = transform.Find("Background/engineFuel_buttonBase/engineFuel_Button")?.GetComponent<Collider2D>();
        Lights(false);
        destGauge.Set(0f);
        if (srcGauge != null) srcGauge.Set(1f);
        if (host.Bot) RefuelDuration = 0.4f;
    }

    void Lights(bool on)
    {
        if (_red != null) _red.color = on ? new Color(0.4f, 0.1f, 0.1f) : Color.red;
        if (_green != null) _green.color = on ? Color.green : new Color(0.1f, 0.3f, 0.1f);
    }

    public void Tick(MultistageMinigame host)
    {
        if (_done) return;
        bool holding = host.Bot || (host.PointerHeld && _button != null && _button.OverlapPoint(host.PointerPos));
        Lights(holding);
        if (holding)
        {
            _fill += Time.deltaTime / RefuelDuration;
            destGauge.Set(_fill);
            if (srcGauge != null) srcGauge.Set(1f - _fill);
            if (_fill >= 1f) { _done = true; host.StageDone(); }
        }
    }

    bool IsOver()
    {
        var cam = AmongUsLan.UiKit.I.Cam;
        var p = (Vector2)cam.ScreenToWorldPoint(Input.mousePosition);
        return _button.OverlapPoint(p);
    }

    public IEnumerator TestHold(MultistageMinigame host)
    {
        yield return new WaitForSeconds(0.3f);
        var p = (Vector2)_button.bounds.center;
        host.TestHoldAt(p, RefuelDuration + 0.8f);
        yield return new WaitForSeconds(RefuelDuration + 1.2f);
    }
}
