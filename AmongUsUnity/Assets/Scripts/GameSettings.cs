using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Host-configurable rules (the original "Game Settings" panel of the lobby, trimmed to what this build supports).</summary>
    public class Settings
    {
        public int Impostors = 0;               // 0 = automatic (1 up to 6 players, 2 from 7)
        public float Speed = 1f;                // player speed multiplier
        public float KillCooldown = 20f;
        public int KillDistance = 1;            // 0 short, 1 medium, 2 long
        public float CrewVision = 1f;
        public float ImpostorVision = 1.5f;
        public int CommonTasks = 1, ShortTasks = 2, LongTasks = 1;
        public int Emergencies = 1;             // emergency meetings per player
        public float VotingTime = 45f;
        public bool Anonymous = false;

        public static readonly float[] KillRanges = { 1.0f, 1.8f, 2.5f };

        public string Encode() => string.Join(",", new[]
        {
            Impostors.ToString(), F(Speed), F(KillCooldown), KillDistance.ToString(), F(CrewVision), F(ImpostorVision),
            CommonTasks.ToString(), ShortTasks.ToString(), LongTasks.ToString(), Emergencies.ToString(), F(VotingTime), Anonymous ? "1" : "0",
        });

        static string F(float f) => f.ToString("0.##", CultureInfo.InvariantCulture);
        static float P(string s) => float.Parse(s, CultureInfo.InvariantCulture);

        public static Settings Decode(string s)
        {
            var a = s.Split(',');
            return new Settings
            {
                Impostors = int.Parse(a[0]), Speed = P(a[1]), KillCooldown = P(a[2]), KillDistance = int.Parse(a[3]), CrewVision = P(a[4]), ImpostorVision = P(a[5]),
                CommonTasks = int.Parse(a[6]), ShortTasks = int.Parse(a[7]), LongTasks = int.Parse(a[8]), Emergencies = int.Parse(a[9]), VotingTime = P(a[10]), Anonymous = a[11] == "1",
            };
        }

        public int ImpostorCount(int players) => Impostors > 0 ? Mathf.Min(Impostors, Mathf.Max(1, (players - 1) / 2)) : (players >= 7 ? 2 : (players >= 2 ? 1 : 0));
    }

    public partial class Game
    {
        Settings _cfg = new Settings();
        Transform _settingsUi;
        readonly List<TextMesh> _settingTexts = new List<TextMesh>();

        Vector2 SettingsPanelPos => LobbyOffset + new Vector2(-1.09f, 2.46f);
        bool NearSettingsPanel => _phase == Phase.Lobby && _me != null && Vector2.Distance(_me.Pos, SettingsPanelPos) < 1.5f;

        struct Row { public string Label; public Func<string> Value; public Action<int> Change; }

        List<Row> Rows() => new List<Row>
        {
            new Row { Label = Loc.T("Impostors"), Value = () => _cfg.Impostors == 0 ? "Auto" : _cfg.Impostors.ToString(), Change = d => _cfg.Impostors = Mathf.Clamp(_cfg.Impostors + d, 0, 3) },
            new Row { Label = Loc.T("Player speed"), Value = () => "x" + _cfg.Speed.ToString("0.0", CultureInfo.InvariantCulture), Change = d => _cfg.Speed = Mathf.Clamp(_cfg.Speed + d * 0.25f, 0.5f, 3f) },
            new Row { Label = Loc.T("Kill cooldown"), Value = () => _cfg.KillCooldown.ToString("0") + "s", Change = d => _cfg.KillCooldown = Mathf.Clamp(_cfg.KillCooldown + d * 2.5f, 10f, 60f) },
            new Row { Label = Loc.T("Kill distance"), Value = () => new[] { Loc.T("Short"), Loc.T("Normal"), Loc.T("Long") }[_cfg.KillDistance], Change = d => _cfg.KillDistance = Mathf.Clamp(_cfg.KillDistance + d, 0, 2) },
            new Row { Label = Loc.T("Crew vision"), Value = () => "x" + _cfg.CrewVision.ToString("0.00", CultureInfo.InvariantCulture), Change = d => _cfg.CrewVision = Mathf.Clamp(_cfg.CrewVision + d * 0.25f, 0.25f, 5f) },
            new Row { Label = Loc.T("Impostor vision"), Value = () => "x" + _cfg.ImpostorVision.ToString("0.00", CultureInfo.InvariantCulture), Change = d => _cfg.ImpostorVision = Mathf.Clamp(_cfg.ImpostorVision + d * 0.25f, 0.25f, 5f) },
            new Row { Label = Loc.T("Common tasks"), Value = () => _cfg.CommonTasks.ToString(), Change = d => _cfg.CommonTasks = Mathf.Clamp(_cfg.CommonTasks + d, 0, 2) },
            new Row { Label = Loc.T("Short tasks"), Value = () => _cfg.ShortTasks.ToString(), Change = d => _cfg.ShortTasks = Mathf.Clamp(_cfg.ShortTasks + d, 0, 5) },
            new Row { Label = Loc.T("Long tasks"), Value = () => _cfg.LongTasks.ToString(), Change = d => _cfg.LongTasks = Mathf.Clamp(_cfg.LongTasks + d, 0, 3) },
            new Row { Label = Loc.T("Emergency meetings"), Value = () => _cfg.Emergencies.ToString(), Change = d => _cfg.Emergencies = Mathf.Clamp(_cfg.Emergencies + d, 0, 9) },
            new Row { Label = Loc.T("Voting time"), Value = () => _cfg.VotingTime.ToString("0") + "s", Change = d => _cfg.VotingTime = Mathf.Clamp(_cfg.VotingTime + d * 15f, 15f, 300f) },
            new Row { Label = Loc.T("Anonymous votes"), Value = () => _cfg.Anonymous ? Loc.T("On") : Loc.T("Off"), Change = d => _cfg.Anonymous = !_cfg.Anonymous },
        };

        void ToggleSettings()
        {
            if (_settingsUi != null) { Destroy(_settingsUi.gameObject); _settingsUi = null; _settingTexts.Clear(); return; }
            if (_ui == null) _ui = UiKit.I;
            _settingsUi = _ui.Group("Settings");
            var rows = Rows();
            float h = rows.Count * 0.38f + 1.1f;
            _ui.Rect(_settingsUi, Vector2.zero, new Vector2(6.4f, h), new Color(0.05f, 0.07f, 0.12f, 0.96f), 150);
            _ui.Text(Loc.T("Game Settings"), _settingsUi, new Vector2(0, h / 2 - 0.4f), 0.34f, Color.white, TextAnchor.MiddleCenter, 152, true);
            for (int i = 0; i < rows.Count; i++)
            {
                var r = rows[i];
                float y = h / 2 - 0.95f - i * 0.38f;
                _ui.Text(r.Label, _settingsUi, new Vector2(-2.9f, y), 0.2f, Color.white, TextAnchor.MiddleLeft, 152, true);
                var vt = _ui.Text(r.Value(), _settingsUi, new Vector2(1.55f, y), 0.2f, new Color(1f, 0.9f, 0.3f), TextAnchor.MiddleCenter, 152, true);
                _settingTexts.Add(vt);
                if (_isHost)
                {
                    int idx = i;
                    var minus = _ui.Sliced("buttonClick", _settingsUi, new Vector2(0.65f, y), new Vector2(0.4f, 0.3f), 153);
                    _ui.Button(minus, () => ChangeSetting(idx, -1), new Vector2(0.4f, 0.3f));
                    _ui.Text("-", _settingsUi, new Vector2(0.65f, y), 0.2f, Color.white, TextAnchor.MiddleCenter, 155, true);
                    var plus = _ui.Sliced("buttonClick", _settingsUi, new Vector2(2.45f, y), new Vector2(0.4f, 0.3f), 153);
                    _ui.Button(plus, () => ChangeSetting(idx, 1), new Vector2(0.4f, 0.3f));
                    _ui.Text("+", _settingsUi, new Vector2(2.45f, y), 0.2f, Color.white, TextAnchor.MiddleCenter, 155, true);
                }
            }
            var close = _ui.Sliced("buttonClick", _settingsUi, new Vector2(0, -h / 2 + 0.3f), new Vector2(1.4f, 0.4f), 153);
            _ui.Button(close, ToggleSettings, new Vector2(1.4f, 0.4f));
            _ui.Text(Loc.T("Close"), _settingsUi, new Vector2(0, -h / 2 + 0.3f), 0.2f, Color.white, TextAnchor.MiddleCenter, 155, true);
        }

        void ChangeSetting(int idx, int dir)
        {
            if (!_isHost) return;
            Rows()[idx].Change(dir);
            Emit("CFG|" + _cfg.Encode());
        }

        void RefreshSettingsUi()
        {
            if (_settingsUi == null) return;
            var rows = Rows();
            for (int i = 0; i < rows.Count && i < _settingTexts.Count; i++) _settingTexts[i].text = rows[i].Value();
        }

        void ApplyCfg(string data)
        {
            _cfg = Settings.Decode(data);
            RefreshSettingsUi();
        }
    }
}
