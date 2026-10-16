using System.Collections;
using UnityEngine;

/// <summary>Reset Reactor: hold the hand print. Both reactor consoles must be held at the same time.</summary>
public class ReactorMinigame : Minigame
{
    public AudioClip HandSound;
    Collider2D _hand;
    Transform _sweeper;
    TextMesh _text;
    float _sweep;

    protected override void OnBegin()
    {
        _hand = Find("HandButton").GetComponent<Collider2D>();
        _sweeper = Find("reactorMeltdown_glowBar");
        _text = MakeText(transform, "Hold to stabilize the reactor", 0.28f, Color.white);
        _text.transform.localPosition = new Vector3(0f, 2f, -4f);
    }

    protected override void Tick()
    {
        bool held = Bot || (MouseHeld && _hand.OverlapPoint(Mouse));
        Hold?.Invoke(held);
        _text.text = held ? "Holding..." : "Hold to stabilize the reactor";
        if (held) { _sweep += Time.deltaTime; if (_sweeper != null) _sweeper.localPosition = new Vector3(0, -2.15f + Mathf.PingPong(_sweep * 3f, 4.3f), _sweeper.localPosition.z); }
    }

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(THold((Vector2)_hand.bounds.center, 1.0f));
    }
}
