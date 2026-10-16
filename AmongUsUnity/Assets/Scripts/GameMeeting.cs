using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Meeting ("phone") screen, vote reveal and ejection cutscene built from the original sprites.</summary>
    public partial class Game
    {
        class VoteArea
        {
            public PlayerInfo P;
            public Transform T;
            public SpriteRenderer Base, DeadOverlay, Thumb, Megaphone;
            public UiButton Btn;
            public Transform Confirm;
            public Transform Votes;
        }

        Transform _meet, _eject;
        readonly List<VoteArea> _areas = new List<VoteArea>();
        VoteArea _skipArea;
        TextMesh _meetTimerText, _meetInfoText, _ejectText;
        SpriteRenderer _ejectBody;
        int _selected = -2;                         // -2 none, -1 skip, else player id
        int _resultStage;                           // 0: votes revealed, 1: ejection scene
        float _stageClock;
        Dictionary<int, List<int>> _voteMap = new Dictionary<int, List<int>>();
        int _ejectId = -1;
        string _ejectFull = "";

        const float RevealSeconds = 4.5f, EjectSeconds = 6f;

        void ShowMeeting()
        {
            HideMeeting();
            _selected = -2;
            _ui = _ui ?? UiKit.I;
            float w = _ui.HalfW, h = _ui.HalfH;
            _meet = _ui.Group("Meeting");

            var bg = _ui.Sprite("BG_Skeld", _meet, Vector2.zero, 0);
            bg.transform.localScale = new Vector3(73.5f, 4.49f, 1);
            // phone body: sliced, tinted through the mask shader
            var body = _ui.Sliced("UI_Phone_Body", _meet, Vector2.zero, new Vector2(8.54f, 5.79f), 1);
            body.transform.localScale = new Vector3(1.2f, 1f, 1);
            body.sharedMaterial = Resources.Load<Material>("PlayerMaterial");
            UiKit.Tint(body, new Color(0.42f, 0.5f, 0.6f), new Color(0.2f, 0.25f, 0.33f));
            var glass = _ui.Sliced("baseGlass", _meet, new Vector2(0.01f, 0f), new Vector2(7.45f, 5.2f), 2);
            glass.transform.localScale = new Vector3(1.2f, 1f, 1);
            glass.color = new Color(0.22f, 0.3f, 0.45f, 1f);
            var wifi = _ui.Sprite("UI_Icon_Wifi", _meet, new Vector2(-4.05f, 2.24f), 6, 0.7f);
            var bat = _ui.Sprite("UI_Icon_Battery", _meet, new Vector2(3.87f, 2.17f), 6, 0.7f);

            _meetInfoText = _ui.Text("", _meet, new Vector2(-3.4f, 2.22f), 0.3f, Color.white, TextAnchor.MiddleLeft, 8, true);
            _meetTimerText = _ui.Text("", _meet, new Vector2(3.4f, 2.22f), 0.3f, Color.white, TextAnchor.MiddleRight, 8, true);

            // vote areas
            _areas.Clear();
            var list = _players.Values.OrderBy(p => p.Id).ToList();
            int cols = list.Count > 10 ? 3 : 2;
            float[] xs = cols == 3 ? new[] { -3.05f, 0f, 3.05f } : new[] { -1.55f, 1.55f };
            for (int i = 0; i < list.Count; i++)
            {
                var a = MakeVoteArea(list[i], new Vector2(xs[i % cols], 1.5f - 0.75f * (i / cols)));
                _areas.Add(a);
            }
            // skip button
            _skipArea = new VoteArea { T = _ui.Group("Skip", _meet) };
            _skipArea.T.localPosition = new Vector2(-3.72f, -2.21f);
            var sb = _ui.Sprite("button_skipVote", _skipArea.T, Vector2.zero, 10, 1.6f);
            _skipArea.Base = sb;
            _skipArea.Btn = _ui.Button(sb, () => SelectVote(-1), new Vector2(1.8f, 0.5f));
            _ui.Text(Loc.T("Skip Vote"), _skipArea.T, new Vector2(0, 0), 0.22f, Color.black, TextAnchor.MiddleCenter, 12, true);
            _skipArea.Votes = _ui.Group("votes", _skipArea.T);
            _skipArea.Votes.localPosition = new Vector2(1.2f, 0);
            _skipArea.Megaphone = null;
            _skipArea.Confirm = MakeConfirm(_skipArea.T, new Vector2(1.5f, 0f), -1);
        }

        Transform MakeConfirm(Transform parent, Vector2 pos, int target)
        {
            var g = _ui.Group("confirm", parent);
            g.localPosition = pos;
            var yes = _ui.Sprite("UI_Button_Yes", g, new Vector2(0, 0), 20, 0.55f);
            _ui.Button(yes, () => ConfirmVote(target), new Vector2(0.5f, 0.5f));
            var no = _ui.Sprite("UI_Button_No", g, new Vector2(0.62f, 0), 20, 0.55f);
            _ui.Button(no, () => { _selected = -2; }, new Vector2(0.5f, 0.5f));
            g.gameObject.SetActive(false);
            return g;
        }

        VoteArea MakeVoteArea(PlayerInfo p, Vector2 pos)
        {
            var a = new VoteArea { P = p, T = _ui.Group("Vote " + p.Name, _meet) };
            a.T.localPosition = pos;
            a.Base = _ui.Sliced("votePlayerBase", a.T, Vector2.zero, new Vector2(2.7f, 0.63f), 10);
            a.Btn = _ui.Button(a.Base, () => SelectVote(p.Id), new Vector2(2.7f, 0.63f));
            _ui.PlayerIcon(a.T, new Vector2(-0.95f, -0.18f), 0.4f, p.Color, 12);
            _ui.Text(p.Name, a.T, new Vector2(-0.58f, 0f), 0.26f, Color.black, TextAnchor.MiddleLeft, 12, true);
            a.Thumb = _ui.Sprite("i_voted_thumb", a.T, new Vector2(-0.53f, 0.2f), 14);
            a.Thumb.gameObject.SetActive(false);
            if (p.Id == _meetingCaller || p.Id == _meetingBody)
                a.Megaphone = _ui.Sprite("voteReported", a.T, new Vector2(1.31f, 0.29f), 14, 0.62f);
            a.DeadOverlay = _ui.Sprite("dead_GreyedOut", a.T, Vector2.zero, 16);
            a.DeadOverlay.transform.localScale = new Vector3(0.79f, 1f, 1f);
            var x = _ui.Sprite("deadX_border", a.T, new Vector2(-1.01f, 0f), 17);
            a.DeadOverlay.gameObject.SetActive(!p.Alive);
            x.gameObject.SetActive(!p.Alive);
            a.Confirm = MakeConfirm(a.T, new Vector2(1.55f, 0f), p.Id);
            a.Votes = _ui.Group("votes", a.T);
            a.Votes.localPosition = new Vector2(0.55f, -0.18f);
            return a;
        }

        void SelectVote(int target)
        {
            if (_phase != Phase.Meeting || _me == null || !_me.Alive || _myVote != -2) return;
            if (target >= 0) { var t = Get(target); if (t == null || !t.Alive) return; }
            _selected = target;
        }

        void ConfirmVote(int target)
        {
            if (_phase != Phase.Meeting || _myVote != -2) return;
            _myVote = target;
            _selected = -2;
            Cmd("VOTE|" + target);
        }

        void HideMeeting()
        {
            if (_meet != null) Destroy(_meet.gameObject);
            _meet = null;
            _areas.Clear();
            if (_eject != null) Destroy(_eject.gameObject);
            _eject = null;
        }

        /// <summary>Votes arrive with the result; icons appear on the voted player (or on the skip button).</summary>
        void RevealVotes()
        {
            if (_meet == null) return;
            foreach (var kv in _voteMap)
            {
                var area = kv.Key == -1 ? _skipArea : _areas.FirstOrDefault(a => a.P != null && a.P.Id == kv.Key);
                if (area == null || area.Votes == null) continue;
                for (int i = 0; i < kv.Value.Count; i++)
                {
                    var voter = Get(kv.Value[i]);
                    if (voter == null) continue;
                    _ui.PlayerIcon(area.Votes, new Vector2(i * 0.22f, 0f), 0.16f, voter.Color, 18);
                }
            }
            foreach (var a in _areas) if (a.Thumb != null) a.Thumb.gameObject.SetActive(false);
        }

        void BeginEjectScene()
        {
            if (_meet != null) _meet.gameObject.SetActive(false);
            _eject = _ui.Group("Eject");
            float w = _ui.HalfW, h = _ui.HalfH;
            _ui.Rect(_eject, Vector2.zero, new Vector2(w * 2 + 1, h * 2 + 1), Color.black, 0);
            var rng = new System.Random(7);
            for (int i = 0; i < 90; i++)
                _ui.Rect(_eject, new Vector2((float)(rng.NextDouble() * 2 - 1) * w, (float)(rng.NextDouble() * 2 - 1) * h),
                         Vector2.one * (0.02f + (float)rng.NextDouble() * 0.04f), new Color(1, 1, 1, 0.4f + (float)rng.NextDouble() * 0.6f), 1);
            _ejectBody = null;
            if (_ejectId >= 0 && Get(_ejectId) != null)
            {
                _ejectBody = _ui.PlayerIcon(_eject, new Vector2(-w - 1, 0), 0.9f, Get(_ejectId).Color, 5);
                Play("eject_text");
            }
            _ejectText = _ui.Text("", _eject, new Vector2(0, -1.6f), 0.4f, Color.white, TextAnchor.MiddleCenter, 8, true);
            _ejectFull = _resultText.Replace("\n", " ");
            if (_shotPath != null && System.Environment.GetCommandLineArgs().Contains("-shotOnEject")) _shotAt = Time.timeSinceLevelLoad + 3.2f;
        }

        void UpdateMeetingUi()
        {
            bool inMeeting = _phase == Phase.Meeting;
            bool inResult = _phase == Phase.Result;
            if (!inMeeting && !inResult)
            {
                if (_meet != null || _eject != null) HideMeeting();
                return;
            }
            float w = _ui.HalfW, h = _ui.HalfH;

            if (inMeeting && _meet != null)
            {
                _meetInfoText.text = (_meetingBody >= 0 ? Loc.T("Who died?") : Loc.T("Who is the Impostor?"));
                _meetTimerText.text = Loc.T("Voting Ends: ") + Mathf.Max(0, Mathf.CeilToInt(_meetingTimer)) + "s";
                bool canVote = _me != null && _me.Alive && _myVote == -2;
                foreach (var a in _areas)
                {
                    bool voted = _votedIds.Contains(a.P.Id);
                    a.Thumb.gameObject.SetActive(voted);
                    a.Btn.Interactable = canVote && a.P.Alive;
                    a.Confirm.gameObject.SetActive(canVote && _selected == a.P.Id);
                }
                _skipArea.Btn.Interactable = canVote;
                _skipArea.Confirm.gameObject.SetActive(canVote && _selected == -1);
            }
            else if (inResult)
            {
                _stageClock += Time.deltaTime;
                if (_resultStage == 0 && _stageClock >= RevealSeconds) { _resultStage = 1; _stageClock = 0; BeginEjectScene(); }
                if (_resultStage == 1 && _eject != null)
                {
                    float t = _stageClock / EjectSeconds;
                    if (_ejectBody != null)
                    {
                        _ejectBody.transform.localPosition = new Vector2(Mathf.Lerp(-w - 1f, w + 1f, Mathf.Clamp01(t * 1.4f)), Mathf.Sin(_stageClock * 1.3f) * 0.5f);
                        _ejectBody.transform.localRotation = Quaternion.Euler(0, 0, -_stageClock * 140f);
                        _ejectBody.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 0.5f, Mathf.Clamp01(t * 1.4f));
                    }
                    int chars = Mathf.Min(_ejectFull.Length, Mathf.FloorToInt(Mathf.Max(0, _stageClock - 0.8f) * 18f));
                    _ejectText.text = _ejectFull.Substring(0, chars);
                }
            }
        }
    }
}
