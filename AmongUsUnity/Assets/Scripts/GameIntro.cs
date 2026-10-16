using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Role reveal at the start of a round ("Crewmate" / "Impostor" with the team line-up).</summary>
    public partial class Game
    {
        Transform _intro;
        float _introUntil;
        const float IntroSeconds = 5f;

        bool IntroActive => Time.time < _introUntil;

        void ShowIntro()
        {
            if (_intro != null) Destroy(_intro.gameObject);
            if (_me == null) return;
            if (_ui == null) _ui = UiKit.I;
            _introUntil = Time.time + IntroSeconds;
            _intro = _ui.Group("Intro");
            float w = _ui.HalfW, h = _ui.HalfH;
            bool imp = _me.Impostor;
            _ui.Rect(_intro, Vector2.zero, new Vector2(w * 2 + 2, h * 2 + 2), Color.black, 200);
            var bar = imp ? new Color(0.78f, 0.07f, 0.07f) : new Color(0.33f, 0.62f, 0.82f);
            _ui.Rect(_intro, new Vector2(0, -0.7f), new Vector2(w * 2 + 2, 2.2f), bar, 201);
            var title = _ui.Text(imp ? Loc.T("Impostor") : Loc.T("Crewmate"), _intro, new Vector2(0, 1.05f), 1.2f, imp ? new Color(0.9f, 0.1f, 0.1f) : Color.white, TextAnchor.MiddleCenter, 205, true);
            string sub = imp
                ? (_impostorCount > 1 ? Loc.T("Kill all Crewmates with your fellow Impostors") : Loc.T("Sabotage and kill everyone"))
                : (_impostorCount == 1 ? Loc.T("There is 1 Impostor among us") : Loc.F("There are {0} Impostors among us", _impostorCount));
            _ui.Text(sub, _intro, new Vector2(0, 0.25f), 0.38f, Color.white, TextAnchor.MiddleCenter, 205, true);

            var lineup = imp ? _players.Values.Where(p => p.Impostor).ToList() : _players.Values.ToList();
            lineup = lineup.OrderBy(p => p == _me ? 0 : 1).ThenBy(p => p.Id).ToList();
            float spacing = 0.85f;
            for (int i = 0; i < lineup.Count; i++)
            {
                // me in the middle, others alternating to each side
                int slot = i == 0 ? 0 : ((i + 1) / 2) * (i % 2 == 1 ? 1 : -1);
                float scale = i == 0 ? 0.85f : 0.62f;
                var icon = _ui.PlayerIcon(_intro, new Vector2(slot * spacing, -0.85f - (i == 0 ? 0.15f : 0f)), scale, lineup[i].Color, 204 + (i == 0 ? 1 : 0));
                _ui.Text(lineup[i].Name, _intro, new Vector2(slot * spacing, -1.95f), 0.2f, imp ? new Color(1f, 0.3f, 0.3f) : Color.white, TextAnchor.MiddleCenter, 206, true);
            }
        }

        void UpdateIntro()
        {
            if (_intro == null) return;
            if (!IntroActive || _phase != Phase.Playing) { Destroy(_intro.gameObject); _intro = null; return; }
            float left = _introUntil - Time.time;
            float a = Mathf.Clamp01(left / 0.6f);
            foreach (var sr in _intro.GetComponentsInChildren<SpriteRenderer>(true)) { var c = sr.color; c.a = a; sr.color = c; }
            foreach (var tm in _intro.GetComponentsInChildren<TextMesh>(true)) { var c = tm.color; c.a = a; tm.color = c; }
        }
    }
}
