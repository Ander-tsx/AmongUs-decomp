using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Main menu built with the original logo/sprites: name field, host button, LAN game list and direct IP.</summary>
    public partial class Game
    {
        TextMesh _nameText, _ipText, _menuStatus, _menuHint;
        readonly List<Transform> _gameRows = new List<Transform>();
        Transform _gamesGroup;
        int _focus;                 // 0 none, 1 name, 2 ip
        float _refreshAt;
        SpriteRenderer _nameBox, _ipBox;
        Transform _langBtn;

        void BuildMenuControls()
        {
            float h = _ui.HalfH;
            _ui.Rect(_menuUi, new Vector2(0, -0.35f), new Vector2(6.6f, 4.5f), new Color(0f, 0f, 0.08f, 0.8f), 1);

            _ui.Text(Loc.T("Name"), _menuUi, new Vector2(-2.95f, 1.35f), 0.24f, Color.white, TextAnchor.MiddleLeft, 6, true);
            _nameBox = _ui.Rect(_menuUi, new Vector2(-0.6f, 1.35f), new Vector2(2.3f, 0.42f), new Color(1, 1, 1, 0.92f), 3);
            _ui.Button(_nameBox, () => _focus = 1, new Vector2(1f, 1f)).GetComponent<BoxCollider2D>().size = Vector2.one;
            _nameText = _ui.Text(_nameField, _menuUi, new Vector2(-1.7f, 1.35f), 0.26f, Color.black, TextAnchor.MiddleLeft, 6, true);
            MenuButton(Loc.T("Create Game"), new Vector2(1.9f, 1.35f), new Vector2(2.1f, 0.5f), () => { _nameField = Clean(_nameField); HostGame(); });

            _ui.Text(Loc.T("Games on the local network"), _menuUi, new Vector2(-2.95f, 0.6f), 0.24f, Color.white, TextAnchor.MiddleLeft, 6, true);
            _gamesGroup = _ui.Group("Games", _menuUi);

            _ui.Text(Loc.T("Code"), _menuUi, new Vector2(-2.95f, -1.55f), 0.24f, Color.white, TextAnchor.MiddleLeft, 6, true);
            _ipBox = _ui.Rect(_menuUi, new Vector2(-1.1f, -1.55f), new Vector2(2.3f, 0.42f), new Color(1, 1, 1, 0.92f), 3);
            _ui.Button(_ipBox, () => _focus = 2, new Vector2(1f, 1f)).GetComponent<BoxCollider2D>().size = Vector2.one;
            _ipText = _ui.Text(_ipField, _menuUi, new Vector2(-2.2f, -1.55f), 0.26f, Color.black, TextAnchor.MiddleLeft, 6, true);
            MenuButton(Loc.T("Join with code"), new Vector2(1.7f, -1.55f), new Vector2(2.4f, 0.5f), () => { _nameField = Clean(_nameField); JoinGame(_ipField.Trim()); });

            _menuHint = _ui.Text(Loc.T("Move: WASD/arrows   Use: E   Kill: Q   Report: R   Vent: V"), _menuUi, new Vector2(0, -2.35f), 0.17f, new Color(0.8f, 0.85f, 1f), TextAnchor.MiddleCenter, 6);
            _menuStatus = _ui.Text("", _menuUi, new Vector2(0, -2.7f), 0.2f, new Color(1f, 0.6f, 0.4f), TextAnchor.MiddleCenter, 6, true);

            var lang = MenuButton(Loc.Spanish ? "ES" : "EN", new Vector2(_ui.HalfW - 0.6f, h - 0.4f), new Vector2(0.7f, 0.4f), () => { Loc.Spanish = !Loc.Spanish; RebuildMenu(); });
            _langBtn = lang;
        }

        Transform MenuButton(string label, Vector2 pos, Vector2 size, System.Action act)
        {
            var sr = _ui.Sliced("buttonClick", _menuUi, pos, size, 4);
            _ui.Button(sr, act, size);
            _ui.Text(label, sr.transform.parent, pos, 0.22f, Color.white, TextAnchor.MiddleCenter, 7, true);
            return sr.transform;
        }

        void RebuildMenu()
        {
            if (_menuUi != null) Destroy(_menuUi.gameObject);
            _menuUi = _ui.Group("MenuUI");
            _ui.Rect(_menuUi, Vector2.zero, new Vector2(_ui.HalfW * 2 + 2, _ui.HalfH * 2 + 2), new Color(0f, 0f, 0.05f, 0.55f), 0);
            _ui.Text(Loc.T("TITLE"), _menuUi, new Vector2(0, _ui.HalfH - 0.7f), 0.62f, Color.white, TextAnchor.MiddleCenter, 5, true);
            BuildMenuControls();
            _gameRows.Clear();
            _refreshAt = 0;
        }

        void UpdateMenuControls()
        {
            if (_nameText == null) return;
            // text input
            if (_focus != 0)
            {
                foreach (var ch in Input.inputString)
                {
                    ref string target = ref _nameField;
                    if (_focus == 2) { HandleChar(ref _ipField, char.ToUpperInvariant(ch), 20); }
                    else HandleChar(ref _nameField, ch, 10);
                }
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Tab)) _focus = _focus == 1 ? 2 : 0;
            }
            _nameText.text = _nameField + (_focus == 1 && (int)(Time.time * 2) % 2 == 0 ? "|" : "");
            _ipText.text = _ipField + (_focus == 2 && (int)(Time.time * 2) % 2 == 0 ? "|" : "");
            _nameBox.color = _focus == 1 ? Color.white : new Color(1, 1, 1, 0.8f);
            _ipBox.color = _focus == 2 ? Color.white : new Color(1, 1, 1, 0.8f);
            _menuStatus.text = _status;

            if (Time.time >= _refreshAt)
            {
                _refreshAt = Time.time + 0.7f;
                foreach (var r in _gameRows) if (r != null) Destroy(r.gameObject);
                _gameRows.Clear();
                var found = _discovery.Snapshot();
                if (found.Count == 0)
                {
                    var t = _ui.Text(Loc.T("Searching..."), _gamesGroup, new Vector2(-2.95f, 0.2f), 0.2f, new Color(0.7f, 0.7f, 0.8f), TextAnchor.MiddleLeft, 6);
                    _gameRows.Add(t.transform);
                }
                for (int i = 0; i < found.Count && i < 4; i++)
                {
                    var f = found[i];
                    float y = 0.2f - i * 0.5f;
                    var g = _ui.Group("row", _gamesGroup);
                    _ui.Text(f.Name + "  (" + f.Players + " " + Loc.T("players") + ")  " + f.Ip, g, new Vector2(-2.95f, y), 0.2f, Color.white, TextAnchor.MiddleLeft, 6, true);
                    var jb = _ui.Sliced("buttonClick", g, new Vector2(2.3f, y), new Vector2(1.3f, 0.4f), 4);
                    var ip = f.Ip;
                    _ui.Button(jb, () => { _nameField = Clean(_nameField); JoinGame(ip); }, new Vector2(1.3f, 0.4f));
                    _ui.Text(Loc.T("Join"), g, new Vector2(2.3f, y), 0.2f, Color.white, TextAnchor.MiddleCenter, 7, true);
                    _gameRows.Add(g);
                }
            }
        }

        static void HandleChar(ref string s, char c, int max)
        {
            if (c == '\b') { if (s.Length > 0) s = s.Substring(0, s.Length - 1); }
            else if (c >= 32 && c != 127 && s.Length < max) s += c;
        }
    }
}
