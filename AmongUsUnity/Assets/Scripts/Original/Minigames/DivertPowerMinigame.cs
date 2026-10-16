using System.Collections;
using UnityEngine;

/// <summary>Divert Power (electrical side): pull the slider of the target system all the way up.</summary>
public class DivertPowerMinigame : Minigame
{
    public byte[] SliderOrder;
    public FloatRange SliderY;

    Transform[] _sliders;
    VerticalGauge[] _gauges;
    int _target;
    bool _drag, _done;
    float _grab;

    protected override void OnBegin()
    {
        var names = new[] { "REngineSlider", "LEngineSlider", "WeaponSlider", "ShieldSlider", "NavSlider", "CommsSlider", "LifeSuppSlider", "SecuritySlider" };
        var tanks = new[] { "REngineTank", "LEngineTank", "WeaponsTank", "ShieldsTank", "NavTank", "CommsTank", "LifeSuppTank", "SecurityTank" };
        _sliders = new Transform[8]; _gauges = new VerticalGauge[8];
        for (int i = 0; i < 8; i++) { _sliders[i] = Find(names[i]); _gauges[i] = FindC<VerticalGauge>(tanks[i]); if (_gauges[i] != null) _gauges[i].Set(0f); }
        // slider i belongs to system SliderOrder[i]
        _target = 0;
        for (int i = 0; i < SliderOrder.Length && i < 8; i++) if ((SystemTypes)SliderOrder[i] == Target) _target = i;
        if (Bot) StartCoroutine(BotSolve());
    }

    void SetSlider(int i, float y)
    {
        y = Mathf.Clamp(y, SliderY.min, SliderY.max);
        var lp = _sliders[i].localPosition; lp.y = y; _sliders[i].localPosition = lp;
        if (_gauges[i] != null) _gauges[i].Set(Mathf.InverseLerp(SliderY.min, SliderY.max, y));
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown)
        {
            var hit = HitAt(m);
            if (hit != null && hit.transform == _sliders[_target]) { _drag = true; _grab = _sliders[_target].position.y - m.y; }
        }
        if (_drag && MouseHeld) SetSlider(_target, transform.InverseTransformPoint(new Vector3(0, m.y + _grab, 0)).y);
        if (_drag && MouseUp)
        {
            _drag = false;
            if (_sliders[_target].localPosition.y >= SliderY.max - 0.05f) { _done = true; Win(0.7f); }
        }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        SetSlider(_target, SliderY.max);
        _done = true; Win(0.4f);
    }
}
