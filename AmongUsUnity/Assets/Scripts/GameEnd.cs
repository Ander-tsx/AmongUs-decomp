using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Victory / defeat screen with the winning team's line-up.</summary>
    public partial class Game
    {
        Transform _end;
        float _endShown;

        void ShowEnd()
        {
            HideEnd();
            if (_ui == null) _ui = UiKit.I;
            _endShown = Time.time;
            _end = _ui.Group("End");
            float w = _ui.HalfW, h = _ui.HalfH;
            bool impWin = _endWinner == 1;
            bool iWon = _me != null && (_me.Impostor == impWin);
            _ui.Rect(_end, Vector2.zero, new Vector2(w * 2 + 2, h * 2 + 2), Color.black, 200);
            var col = iWon ? new Color(0.35f, 0.75f, 1f) : new Color(0.9f, 0.15f, 0.15f);
            _ui.Text(iWon ? Loc.T("VICTORY") : Loc.T("DEFEAT"), _end, new Vector2(0, 1.6f), 1.3f, col, TextAnchor.MiddleCenter, 205, true);
            _ui.Text(_endText, _end, new Vector2(0, 0.75f), 0.4f, Color.white, TextAnchor.MiddleCenter, 205, true);

            var winners = _players.Values.Where(p => p.Impostor == impWin).OrderBy(p => p.Id).ToList();
            float spacing = Mathf.Min(1.2f, (w * 2 - 1.5f) / Mathf.Max(1, winners.Count));
            for (int i = 0; i < winners.Count; i++)
            {
                float x = (i - (winners.Count - 1) / 2f) * spacing;
                _ui.PlayerIcon(_end, new Vector2(x, -0.55f), 0.7f, winners[i].Color, 204);
                _ui.Text(winners[i].Name, _end, new Vector2(x, -1.55f), 0.2f, impWin ? new Color(1f, 0.35f, 0.35f) : Color.white, TextAnchor.MiddleCenter, 206, true);
            }
            if (_isHost)
            {
                var b = _ui.Sliced("buttonClick", _end, new Vector2(0, -2.5f), new Vector2(2.8f, 0.55f), 206);
                _ui.Button(b, () => Emit("LOBBYBACK"), new Vector2(2.8f, 0.55f));
                _ui.Text(Loc.T("Back to Lobby"), _end, new Vector2(0, -2.5f), 0.24f, Color.white, TextAnchor.MiddleCenter, 208, true);
            }
            else _ui.Text(Loc.T("Waiting for the host..."), _end, new Vector2(0, -2.5f), 0.24f, new Color(0.8f, 0.8f, 0.9f), TextAnchor.MiddleCenter, 208, true);
        }

        void HideEnd() { if (_end != null) Destroy(_end.gameObject); _end = null; }

        void UpdateEnd()
        {
            if (_phase != Phase.Ended) { if (_end != null) HideEnd(); return; }
            if (_end == null) ShowEnd();
        }
    }
}
