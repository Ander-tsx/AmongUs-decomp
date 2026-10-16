using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>LAN text chat for the lobby and meetings (dead players only talk to other dead players).</summary>
    public partial class Game
    {
        struct ChatMsg { public int Id; public bool Dead; public string Text; }

        readonly List<ChatMsg> _chat = new List<ChatMsg>();
        Transform _chatBtnGroup, _chatUi;
        SpriteRenderer _chatBtn;
        TextMesh[] _chatLines;
        TextMesh _chatInput;
        bool _chatOpen;
        string _chatBuf = "";
        const int ChatMaxLines = 14, ChatWrap = 34;

        bool ChatAvailable => _me != null && (_phase == Phase.Lobby || _phase == Phase.Meeting);

        void BuildChat()
        {
            _chatBtnGroup = _ui.Group("ChatButton");
            _chatBtn = _ui.Sprite("BUTTON-ChatInactive", _chatBtnGroup, new Vector2(_ui.HalfW - 0.55f, _ui.HalfH - 0.55f), 210, 0.8f);
            _ui.Button(_chatBtn, ToggleChat, new Vector2(0.9f, 0.9f));
            _chatBtnGroup.gameObject.SetActive(false);
        }

        void ToggleChat()
        {
            if (!ChatAvailable && !_chatOpen) return;
            _chatOpen = !_chatOpen;
            _chatBuf = "";
            if (_chatOpen) BuildChatPanel();
            else if (_chatUi != null) { Destroy(_chatUi.gameObject); _chatUi = null; }
        }

        void BuildChatPanel()
        {
            if (_chatUi != null) Destroy(_chatUi.gameObject);
            _chatUi = _ui.Group("Chat");
            float w = _ui.HalfW, h = _ui.HalfH;
            float cx = w - 2.2f;
            _ui.Rect(_chatUi, new Vector2(cx, 0), new Vector2(4.2f, h * 2 - 1.6f), new Color(0.04f, 0.06f, 0.12f, 0.92f), 208);
            _chatLines = new TextMesh[ChatMaxLines];
            for (int i = 0; i < ChatMaxLines; i++)
                _chatLines[i] = _ui.Text("", _chatUi, new Vector2(cx - 2.0f, h - 1.2f - i * 0.3f), 0.17f, Color.white, TextAnchor.MiddleLeft, 212);
            _ui.Rect(_chatUi, new Vector2(cx, -h + 1.0f), new Vector2(3.9f, 0.4f), new Color(1, 1, 1, 0.92f), 209);
            _chatInput = _ui.Text("", _chatUi, new Vector2(cx - 1.9f, -h + 1.0f), 0.2f, Color.black, TextAnchor.MiddleLeft, 213, true);
            RefreshChat();
        }

        void RefreshChat()
        {
            if (_chatLines == null) return;
            var lines = new List<string>();
            foreach (var m in _chat)
            {
                if (m.Dead && _me != null && _me.Alive) continue;
                var p = Get(m.Id);
                string name = p != null ? p.Name : "?";
                var c = p != null ? Palette.Body(p.Color) : Color.white;
                string head = "<color=#" + ColorUtility.ToHtmlStringRGB(c) + ">" + name + (m.Dead ? " (x)" : "") + ":</color> ";
                string text = m.Text;
                bool first = true;
                while (text.Length > 0)
                {
                    int take = Mathf.Min(ChatWrap - (first ? name.Length + 2 : 0), text.Length);
                    if (take < text.Length) { int sp = text.LastIndexOf(' ', take - 1, take); if (sp > 8) take = sp + 1; }
                    lines.Add((first ? head : "     ") + text.Substring(0, take));
                    text = text.Substring(take);
                    first = false;
                }
            }
            var shown = lines.Skip(Mathf.Max(0, lines.Count - ChatMaxLines)).ToList();
            for (int i = 0; i < ChatMaxLines; i++) _chatLines[i].text = i < shown.Count ? shown[i] : "";
        }

        void UpdateChat()
        {
            bool avail = ChatAvailable;
            if (_chatBtnGroup != null)
            {
                _chatBtnGroup.gameObject.SetActive(avail);
                if (avail) _chatBtn.transform.localPosition = new Vector2(_ui.HalfW - 0.55f, _ui.HalfH - 0.55f - (_phase == Phase.Lobby ? 0f : 0f));
            }
            if (!avail && _chatOpen) { _chatOpen = false; if (_chatUi != null) Destroy(_chatUi.gameObject); _chatUi = null; }
            if (!_chatOpen)
            {
                if (avail && Input.GetKeyDown(KeyCode.Return) && _focus == 0 && _wardrobe == null && _settingsUi == null) ToggleChat();
                return;
            }
            foreach (var ch in Input.inputString)
            {
                if (ch == '\b') { if (_chatBuf.Length > 0) _chatBuf = _chatBuf.Substring(0, _chatBuf.Length - 1); }
                else if (ch == '\n' || ch == '\r') { SendChat(); }
                else if (ch >= 32 && ch != 127 && _chatBuf.Length < 100) _chatBuf += ch;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) { ToggleChat(); return; }
            if (_chatInput != null) _chatInput.text = _chatBuf + ((int)(Time.time * 2) % 2 == 0 ? "|" : "");
        }

        void SendChat()
        {
            var t = _chatBuf.Trim();
            _chatBuf = "";
            if (t.Length == 0) return;
            Cmd("CHAT|" + t.Replace("|", "/"));
        }

        void HostChat(int from, string text)
        {
            var p = Get(from);
            if (p == null || (_phase != Phase.Lobby && _phase != Phase.Meeting) || string.IsNullOrWhiteSpace(text)) return;
            if (text.Length > 100) text = text.Substring(0, 100);
            Emit("CHATMSG|" + from + "|" + (p.Alive ? 0 : 1) + "|" + text.Replace("|", "/"));
        }

        void ApplyChat(string[] a)
        {
            _chat.Add(new ChatMsg { Id = int.Parse(a[1]), Dead = a[2] == "1", Text = a.Length > 3 ? a[3] : "" });
            if (_chat.Count > 80) _chat.RemoveAt(0);
            if (_chatOpen) RefreshChat();
            else if (a[1] != _myId.ToString()) Play("Panel_TempButton", 0.4f);
        }
    }
}
