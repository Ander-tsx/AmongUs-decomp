using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Short full-screen effects: meeting banner and the red flash when killed.</summary>
    public partial class Game
    {
        Transform _fx;
        SpriteRenderer _fxRect;
        TextMesh _fxText;
        float _fxUntil, _fxStart;
        Color _fxColor;

        void Fx(string text, Color bg, float seconds, Color textColor)
        {
            if (_ui == null) _ui = UiKit.I;
            if (_fx != null) Destroy(_fx.gameObject);
            _fx = _ui.Group("Fx");
            _fxRect = _ui.Rect(_fx, Vector2.zero, new Vector2(_ui.HalfW * 2 + 2, _ui.HalfH * 2 + 2), bg, 300);
            _fxText = string.IsNullOrEmpty(text) ? null : _ui.Text(text, _fx, Vector2.zero, 0.9f, textColor, TextAnchor.MiddleCenter, 305, true);
            _fxStart = Time.time; _fxUntil = Time.time + seconds; _fxColor = bg;
        }

        void UpdateFx()
        {
            if (_fx == null) return;
            if (Time.time >= _fxUntil) { Destroy(_fx.gameObject); _fx = null; return; }
            float k = Mathf.Clamp01((_fxUntil - Time.time) / Mathf.Max(0.01f, _fxUntil - _fxStart));
            var c = _fxColor; c.a *= k; _fxRect.color = c;
            if (_fxText != null) { var t = _fxText.color; t.a = Mathf.Clamp01(k * 2f); _fxText.color = t; }
        }
    }
}
