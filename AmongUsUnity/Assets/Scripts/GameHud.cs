using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>In-game HUD built from the original sprites (use / report / kill buttons, task bar and list).</summary>
    public partial class Game
    {
        UiKit _ui;
        Transform _hud;
        SpriteRenderer _useBtn, _reportBtn, _killBtn, _barFill;
        UiButton _useUi, _reportUi, _killUi;
        TextMesh _killText, _taskText, _roleText, _useProgressText;
        SpriteRenderer _useBarBg, _useBarFill;

        static readonly Color Disabled = new Color(1f, 1f, 1f, 0.35f);

        void BuildHud()
        {
            _ui = UiKit.Create(_cam);
            _hud = _ui.Group("HUD");
            float w = _ui.HalfW, h = _ui.HalfH;

            // bottom-right buttons
            _useBtn = _ui.Sprite("use", _hud, new Vector2(w - 0.75f, -h + 0.75f), 5, 0.9f);
            _useUi = _ui.Button(_useBtn, UseNearest, new Vector2(1.1f, 1.1f));
            _reportBtn = _ui.Sprite("report", _hud, new Vector2(w - 0.75f, -h + 1.95f), 5, 0.9f);
            _reportUi = _ui.Button(_reportBtn, ReportNearBody, new Vector2(1.1f, 1.1f));
            _killBtn = _ui.Sprite("kill", _hud, new Vector2(w - 2.0f, -h + 0.75f), 5, 0.9f);
            _killUi = _ui.Button(_killBtn, TryKill, new Vector2(1.1f, 1.1f));
            _killText = _ui.Text("", _hud, new Vector2(w - 2.0f, -h + 0.75f), 0.55f, Color.white, TextAnchor.MiddleCenter, 8, true);

            // progress bar + task list (top-left)
            var bg = _ui.Rect(_hud, new Vector2(-w + 2.15f, h - 0.35f), new Vector2(3.9f, 0.42f), new Color(0.05f, 0.1f, 0.12f, 0.9f), 1);
            _barFill = _ui.Rect(_hud, new Vector2(-w + 0.25f, h - 0.35f), new Vector2(0.01f, 0.32f), new Color(0.2f, 0.85f, 0.35f), 2);
            _ui.Text("TOTAL TASKS COMPLETED", _hud, new Vector2(-w + 0.35f, h - 0.35f), 0.2f, Color.white, TextAnchor.MiddleLeft, 3, true);
            _ui.Rect(_hud, new Vector2(-w + 1.9f, h - 1.9f), new Vector2(3.4f, 2.4f), new Color(0, 0, 0, 0.35f), 0);
            _taskText = _ui.Text("", _hud, new Vector2(-w + 0.3f, h - 0.85f), 0.2f, Color.white, TextAnchor.UpperLeft, 3);
            _roleText = _ui.Text("", _hud, new Vector2(0, h - 0.45f), 0.3f, Color.white, TextAnchor.MiddleCenter, 3, true);

            // task use progress (bottom-centre)
            _useBarBg = _ui.Rect(_hud, new Vector2(0, -h + 0.5f), new Vector2(2.6f, 0.3f), new Color(0, 0, 0, 0.7f), 4);
            _useBarFill = _ui.Rect(_hud, new Vector2(-1.25f, -h + 0.5f), new Vector2(0.01f, 0.22f), new Color(0.95f, 0.85f, 0.2f), 5);
            _useProgressText = _ui.Text("", _hud, new Vector2(0, -h + 0.9f), 0.25f, Color.white, TextAnchor.MiddleCenter, 6, true);
            BuildSabotageHud();
            _hud.gameObject.SetActive(false);
        }

        void ReportNearBody()
        {
            if (_phase != Phase.Playing || _me == null || !_me.Alive) return;
            var body = _bodies.Where(kv => Vector2.Distance(kv.Value.transform.position, _me.Pos) <= ReportRange)
                              .Select(kv => (int?)kv.Key).FirstOrDefault();
            if (body.HasValue) Cmd("REPORT|" + body.Value);
        }

        void TryKill()
        {
            if (_phase != Phase.Playing || _me == null || !_me.Impostor || !_me.Alive || _killCd > 0f) return;
            var target = NearestPlayer(KillRange, p => p.Alive && !p.Impostor && p != _me);
            if (target != null) { Cmd("KILL|" + target.Id); _killCd = KillCooldown; }
        }

        bool BodyInRange => _me != null && _bodies.Any(kv => Vector2.Distance(kv.Value.transform.position, _me.Pos) <= ReportRange);

        void UpdateHud()
        {
            bool show = _phase == Phase.Playing && _me != null && !IntroActive;
            if (_hud.gameObject.activeSelf != show) _hud.gameObject.SetActive(show);
            if (!show) return;

            float w = _ui.HalfW, h = _ui.HalfH;
            // keep anchors correct if the window aspect changes
            _useBtn.transform.localPosition = new Vector2(w - 0.75f, -h + 0.75f);
            _reportBtn.transform.localPosition = new Vector2(w - 0.75f, -h + 1.95f);

            bool imp = _me.Impostor;
            bool canUseTask = !imp && NearestTask(out _) >= 0;
            bool atButton = _me.Alive && !_me.UsedButton && Vector2.Distance(WorldMap.EmergencyButtonPos, _me.Pos) <= WorldMap.EmergencyButtonRange;
            _useUi.SetTint(canUseTask || atButton ? Color.white : Disabled);
            _reportUi.SetTint(_me.Alive && BodyInRange ? Color.white : Disabled);

            _killBtn.gameObject.SetActive(imp);
            _killText.gameObject.SetActive(imp);
            if (imp)
            {
                bool inRange = _me.Alive && NearestPlayer(KillRange, p => p.Alive && !p.Impostor && p != _me) != null;
                _killUi.SetTint(_killCd <= 0f && inRange ? Color.white : Disabled);
                _killText.text = _killCd > 0f ? Mathf.CeilToInt(_killCd).ToString() : "";
            }

            // task bar
            float frac = _tasksTotal > 0 ? Mathf.Clamp01((float)_tasksDone / _tasksTotal) : 0f;
            float full = 3.7f;
            _barFill.transform.localScale = new Vector3(Mathf.Max(0.01f, full * frac), 0.32f, 1);
            _barFill.transform.localPosition = new Vector2(-w + 0.25f + full * frac / 2f, h - 0.35f);

            // task list / impostor info
            if (!imp)
            {
                var lines = _myTasks.Select(t =>
                {
                    string c = t.Complete ? "<color=#4cff6a>" : "<color=#ffffff>";
                    return t.Title + " (" + t.RoomName(t.Current.Room) + ")" + (t.Steps.Length > 1 ? " " + t.Done + "/" + t.Steps.Length : "");
                });
                _taskText.text = string.Join("\n", lines);
                _roleText.text = _me.Alive ? "" : "YOU ARE DEAD";
            }
            else
            {
                string partners = string.Join(", ", _players.Values.Where(p => p.Impostor && p != _me).Select(p => p.Name));
                _taskText.text = "Fake Tasks:\n" + string.Join("\n", ShipFakeTasks());
                _roleText.text = (partners.Length > 0 ? "Impostors: " + partners : "");
            }

            // use progress
            bool running = _taskRunning >= 0;
            _useBarBg.gameObject.SetActive(running);
            _useBarFill.gameObject.SetActive(running);
            _useProgressText.gameObject.SetActive(running);
            if (running)
            {
                float f = Mathf.Clamp01(_taskHold / TaskHoldSeconds);
                _useBarFill.transform.localScale = new Vector3(Mathf.Max(0.01f, 2.5f * f), 0.22f, 1);
                _useBarFill.transform.localPosition = new Vector2(-1.25f + 2.5f * f / 2f, -h + 0.5f);
                _useProgressText.text = _myTasks[_taskRunning].Title;
            }
        }

        string[] ShipFakeTasks() => new[] { "Fix Wiring (Electrical)", "Swipe Card (Admin)" };
    }
}
