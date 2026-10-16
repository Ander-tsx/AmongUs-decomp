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
    bool _dragging, _inSlot, _timing, _done, _returning;
    float _startX, _prevX;
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
        _startX = _minX + 0.5f; _prevX = _minX;
        SetLights(false, false);
        Sfx(WalletOut);
        if (Bot) StartCoroutine(BotSolve());
    }

    void SetLights(bool red, bool green)
    {
        if (redLight != null) redLight.color = red ? Color.white : Off;
        if (greenLight != null) greenLight.color = green ? Color.white : Off;
    }

    bool Grabbable(Vector2 m)
    {
        var cb = col.bounds; cb.Expand(new Vector3(0.5f, 0.5f, 10f));
        if (cb.Contains(new Vector3(m.x, m.y, cb.center.z))) return true;
        if (_wallet != null)
            foreach (var r in _wallet.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var wb = r.bounds; wb.Expand(new Vector3(0.2f, 0.2f, 10f));
                if (wb.Contains(new Vector3(m.x, m.y, wb.center.z))) return true;
            }
        return false;
    }

    protected override void Tick()
    {
        if (_done || Bot) return;
        var m = Mouse;
        if (MouseDown && !_dragging && !_returning && Grabbable(m))
        {
            _dragging = true; _inSlot = false; _timing = false;
            var cb = col.bounds;
            bool onCard = cb.Contains(new Vector3(m.x, m.y, cb.center.z));
            _grabDx = onCard ? _card.position.x - m.x : 0f;
            _grabDy = onCard ? _card.position.y - m.y : 0f;
            RaiseCard();
            Sfx(CardMove);
        }
        if (_dragging && MouseHeld)
        {
            var p = _card.position;
            float x = Mathf.Clamp(m.x + _grabDx, _minX, _maxX);
            if (!_inSlot)
            {
                p.x = x;
                p.y = m.y + _grabDy;
                if (p.y >= _slotY - 0.3f) _inSlot = true;
            }
            if (_inSlot)
            {
                p.y = _slotY;
                if (!_timing && _prevX < _startX && x >= _startX) { _timing = true; _t0 = Time.time; }
                if (_timing && x < _startX - 0.05f) _timing = false;
                p.x = x;
                _prevX = x;
                if (_timing && x >= _maxX - 0.05f) Evaluate();
            }
            else _prevX = x;
            _card.position = p;
        }
        if (_dragging && MouseUp) { _dragging = false; if (!_done) StartCoroutine(ReturnCard(false)); }
    }

    void RaiseCard()
    {
        int top = 0;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r.transform != _card) top = Mathf.Max(top, r.sortingOrder);
        foreach (var r in _card.GetComponentsInChildren<Renderer>(true)) r.sortingOrder = top + 1;
        var pz = _card.position; pz.z = transform.position.z - 8f; _card.position = pz;
    }

    void Evaluate()
    {
        _dragging = false;
        dragTime = Time.time - _t0;
        if (dragTime >= AcceptedTime.min * 0.8f && dragTime <= AcceptedTime.max * 1.6f)
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
            _status.text = dragTime < AcceptedTime.min * 0.8f ? "Too Fast. Try Again" : "Too Slow. Try Again";
            Sfx(DenySound);
            StartCoroutine(ReturnCard(true));
        }
    }

    IEnumerator ReturnCard(bool deny)
    {
        _returning = true;
        float t = 0f; var from = _card.position;
        while (t < 1f) { t += Time.deltaTime * 3f; _card.position = Vector3.Lerp(from, _cardHome, t); yield return null; }
        _timing = false; _inSlot = false; _returning = false; _prevX = _minX;
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

    public override bool HasTest => true;
    public override IEnumerator TestPlay()
    {
        yield return new WaitForSeconds(0.3f);
        var start = (Vector2)col.bounds.center;
        var entry = new Vector2(_minX + 0.1f, _slotY + 0.05f);
        var end = new Vector2(_maxX + 0.2f, _slotY);
        // first a slow pull out of the wallet, then a swipe of ~0.5s across the slot
        yield return StartCoroutine(TDrag(8.4f, start, new Vector2(start.x, _slotY + 0.1f), entry, end));
    }
}
