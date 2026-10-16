using System.Collections;
using UnityEngine;

/// <summary>Admin "Swipe Card": pull the card out of the wallet and slide it through the reader at the right speed.</summary>
public class CardSlideGame : Minigame
{
    public FloatRange AcceptedTime;
    public Collider2D col;
    public SpriteRenderer redLight;
    public SpriteRenderer greenLight;
    public AudioClip AcceptSound;
    public AudioClip DenySound;
    public AudioClip[] CardMove;
    public AudioClip WalletOut;
    public float dragTime;

    Transform _card, _wallet;
    TextMesh _status;
    Vector3 _cardHome;
    float _slotY, _minX, _maxX, _grabDx, _grabDy;
    bool _dragging, _inSlot, _timing, _done;
    float _t0;

    static readonly Color Off = new Color(0.45f, 0.45f, 0.45f, 1f);

    protected override void OnBegin()
    {
        _card = col.transform;
        _cardHome = _card.position;
        _wallet = Find("admin_sliderBottom/admin_Wallet");
        var bar = Find("admin_sliderTop/admin_textBar");
        _status = MakeText(bar != null ? bar : transform, "Please Insert Card", 0.34f, new Color(0.45f, 1f, 0.6f));
        var sliderBottom = Find("admin_sliderBottom");
        _slotY = sliderBottom != null ? sliderBottom.position.y - 0.05f : 0f;
        _minX = transform.position.x - 2.0f;
        _maxX = transform.position.x + 2.0f;
        SetLights(false, false);
        Sfx(WalletOut);
        if (Bot) StartCoroutine(BotSolve());
    }

    void SetLights(bool red, bool green)
    {
        if (redLight != null) redLight.color = red ? Color.white : Off;
        if (greenLight != null) greenLight.color = green ? Color.white : Off;
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown && !_dragging && HitAt(m) == col)
        {
            _dragging = true; _inSlot = false; _timing = false;
            _grabDx = _card.position.x - m.x; _grabDy = _card.position.y - m.y;
            Sfx(CardMove);
        }
        if (_dragging && MouseHeld)
        {
            var p = _card.position;
            if (!_inSlot)
            {
                p.x = Mathf.Clamp(m.x + _grabDx, _minX, _maxX);
                p.y = m.y + _grabDy;
                if (p.y >= _slotY - 0.15f) { _inSlot = true; }
            }
            if (_inSlot)
            {
                p.y = _slotY;
                p.x = Mathf.Clamp(m.x + _grabDx, _minX, _maxX);
                if (!_timing && p.x > _minX + 0.25f && p.x < _minX + 0.6f) { _timing = true; _t0 = Time.time; }
                if (_timing && p.x >= _maxX - 0.05f) Evaluate();
            }
            _card.position = p;
        }
        if (_dragging && MouseUp) { _dragging = false; if (!_done) StartCoroutine(ReturnCard(false)); }
    }

    void Evaluate()
    {
        _dragging = false;
        dragTime = Time.time - _t0;
        if (AcceptedTime.Contains(dragTime))
        {
            _done = true;
            SetLights(false, true);
            _status.text = "Thank You!";
            Sfx(AcceptSound);
            Win(0.9f);
        }
        else
        {
            SetLights(true, false);
            _status.text = dragTime < AcceptedTime.min ? "Too Fast. Try Again" : "Too Slow. Try Again";
            Sfx(DenySound);
            StartCoroutine(ReturnCard(true));
        }
    }

    IEnumerator ReturnCard(bool deny)
    {
        float t = 0f; var from = _card.position;
        while (t < 1f) { t += Time.deltaTime * 3f; _card.position = Vector3.Lerp(from, _cardHome, t); yield return null; }
        _timing = false; _inSlot = false;
        if (deny) { yield return new WaitForSeconds(0.6f); SetLights(false, false); _status.text = "Please Insert Card"; }
    }

    IEnumerator BotSolve()
    {
        yield return new WaitForSeconds(0.3f);
        float dur = (AcceptedTime.min + AcceptedTime.max) * 0.5f;
        var start = new Vector3(_minX + 0.4f, _slotY, _card.position.z);
        _card.position = start;
        _t0 = Time.time;
        float t = 0f;
        while (t < dur) { t += Time.deltaTime; _card.position = new Vector3(Mathf.Lerp(_minX + 0.4f, _maxX, t / dur), _slotY, start.z); yield return null; }
        _t0 = Time.time - dur;
        Evaluate();
    }
}
