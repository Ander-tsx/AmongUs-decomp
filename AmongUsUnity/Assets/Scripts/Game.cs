using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AmongUsLan
{
    public enum Phase { Menu, Lobby, Playing, Meeting, Result, Ended }

    public class PlayerInfo
    {
        public int Id;
        public string Name = "";
        public int Color;
        public bool Alive = true;
        public bool Impostor;           // host knows all; clients know only themselves (and partners if impostor)
        public Vector2 Pos, TargetPos;
        public bool Flip, Moving;
        public GameObject Go;
        public SpriteRenderer Renderer;
        public Rigidbody2D Body;
        public PlayerVisual Visual;
        public readonly List<GameTask> Tasks = new List<GameTask>();   // host: authoritative copy
        public int Vote = -2;           // -2 none, -1 skip, otherwise player id
        public bool UsedButton;
        public int InVent = -1;
        public int EmergencyUses;
    }

    /// <summary>
    /// Host-authoritative Among Us over LAN. Host is player id 0 and runs the rules; clients send commands
    /// (POS/KILL/TASK/REPORT/VOTE) and mirror the events the host broadcasts. The world is the original Skeld.
    /// </summary>
    public partial class Game : MonoBehaviour
    {
        // ---- tuning (original Player prefab / default game settings) ----
        const float BaseSpeed = 2.5f, ReportRange = 1.5f;
        const float BodyColliderRadius = 0.2233912f, BodyColliderOffsetY = -0.3636f;   // root-local (root scale 0.7)
        const float TaskHoldSeconds = 1.0f, ResultSeconds = 11.5f, NetRate = 1f / 15f;
        float MeetingSeconds => _cfg.VotingTime;

        // ---- state ----
        Phase _phase = Phase.Menu;
        readonly Dictionary<int, PlayerInfo> _players = new Dictionary<int, PlayerInfo>();
        PlayerInfo _me;
        int _myId = -1;
        bool _isHost;
        LanPeer _peer;
        LanDiscovery _discovery;
        string _status = "";

        // round state
        int _tasksDone, _tasksTotal, _impostorCount;
        readonly List<GameTask> _myTasks = new List<GameTask>();
        readonly Dictionary<int, GameObject> _bodies = new Dictionary<int, GameObject>();
        float _killCd, _taskHold, _meetingTimer, _resultTimer, _netTimer;
        string _resultText = "", _endText = "";
        int _myVote = -2;
        readonly HashSet<int> _votedIds = new HashSet<int>();
        int _meetingCaller = -1, _meetingBody = -1;
        int _endWinner = -1;
        string _endImpostors = "";
        float KillRange => Settings.KillRanges[Mathf.Clamp(_cfg.KillDistance, 0, 2)];
        float KillCooldown { get => _cfg.KillCooldown; set => _cfg.KillCooldown = value; }
        float MoveSpeed => BaseSpeed * _cfg.Speed;

        // UI / presentation
        string _nameField = "Player";
        string _ipField = "127.0.0.1";
        Camera _cam;
        AudioSource _audio;
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        float _stepTimer;
        Texture2D _white;
        FogOfWar _fog;
        const float CrewVision = 3.5f, ImpostorVision = 5.25f;

        // bot / automated testing (command-line driven)
        bool _bot, _autoHost, _autoJoin, _botTeleport;
        string _botSab;
        float _botDirTimer;
        Vector2 _botDir;
        float _autoTimer;
        bool _meetDone, _mgOpened, _sabMenuShown, _ventDemo, _mapShown;
        string _shotPath;
        float _shotAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EarlySettings()
        {
            DateHide.SeasonalEnabled = !Environment.GetCommandLineArgs().Contains("-noseasonal");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<Game>() != null) return;
            new GameObject("Game").AddComponent<Game>();
        }

        void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
            WorldMap.Init();
            InitVents();
            InitAmbience();
            BuildLobby();
            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.orthographicSize = 3f;
            _cam.backgroundColor = Color.black;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.transform.position = new Vector3(LobbyOffset.x, LobbyOffset.y + 0.5f, -10);
            _audio = gameObject.AddComponent<AudioSource>();
            foreach (var c in Resources.LoadAll<AudioClip>("Audio")) _clips[c.name] = c;
            _white = Texture2D.whiteTexture;
            Physics2D.queriesStartInColliders = false;
            Physics2D.queriesHitTriggers = false;
            _fog = FogOfWar.Create(_cam);
            BuildHud();
            BuildVentHud();
            BuildLobbyHud();
            BuildMenuUi();

            var args = Environment.GetCommandLineArgs();
            _bot = args.Contains("-bot");
            _botTeleport = args.Contains("-teleport");
            if (args.Contains("-slowkill")) KillCooldown = 60f;
            _autoHost = args.Contains("-autohost");
            _autoJoin = args.Contains("-autojoin");
            string Arg(string n) { int i = Array.IndexOf(args, n); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
            _botSab = Arg("-botsab");
            if (Arg("-name") != null) _nameField = Arg("-name");
            if (Arg("-ip") != null) _ipField = Arg("-ip");
            _shotPath = Arg("-shot");
            if (Arg("-shotAfter") != null) _shotAt = float.Parse(Arg("-shotAfter"), CultureInfo.InvariantCulture);
            if (_shotPath != null && _shotAt <= 0) _shotAt = 6f;

            _discovery = new LanDiscovery();
            _discovery.StartListening();
        }

        void OnDestroy() { Shutdown(); }
        void OnApplicationQuit() { Shutdown(); }

        void Shutdown()
        {
            _peer?.Dispose();
            _discovery?.Dispose();
        }

        // =====================================================================================
        // Networking glue
        // =====================================================================================

        void Play(string clip, float volume = 1f)
        {
            if (_clips.TryGetValue(clip, out var c)) _audio.PlayOneShot(c, volume);
        }

        void HostGame()
        {
            try { _peer = LanPeer.StartHost(); }
            catch (Exception e) { _status = "No se pudo crear partida: " + e.Message; return; }
            _isHost = true;
            _myId = 0;
            _discovery.StartAdvertising(_nameField);
            _me = AddPlayer(0, _nameField, 0);
            SetPhase(Phase.Lobby);
            BroadcastLobby();
            Debug.Log("[LAN] hosting as " + _nameField);
        }

        void JoinGame(string ip)
        {
            try { _peer = LanPeer.Connect(ip); }
            catch (Exception e) { _status = e.Message; return; }
            _isHost = false;
            _peer.Send(0, "HELLO|" + Clean(_nameField));
            _status = Loc.T("Connecting...");
            Debug.Log("[LAN] joining " + ip);
        }

        static string Clean(string s) => (s ?? "").Replace("|", "").Replace(";", "").Replace(",", "").Replace(":", "").Replace("~", "").Trim();

        // Send a command: host handles it directly, clients forward it.
        void Cmd(string msg)
        {
            if (_isHost) HostHandle(0, msg); else _peer?.Send(0, msg);
        }

        // Host emits a shared event to everyone (including itself).
        void Emit(string msg)
        {
            _peer.Broadcast(msg);
            ApplyEvent(msg);
        }

        void EmitTo(int id, string msg)
        {
            if (id == 0) ApplyEvent(msg); else _peer.Send(id, msg);
        }

        void SetPhase(Phase p)
        {
            _phase = p;
            Debug.Log("[LAN] phase=" + p);
        }

        PlayerInfo AddPlayer(int id, string name, int color)
        {
            var pi = new PlayerInfo { Id = id, Name = name, Color = color };
            pi.Pos = pi.TargetPos = LobbySpawn(id);
            _players[id] = pi;
            return pi;
        }

        void EnsureAvatar(PlayerInfo p)
        {
            if (p.Go != null) return;
            p.Go = Instantiate(Resources.Load<GameObject>("Player"));
            p.Go.name = "Player " + p.Name;
            p.Visual = p.Go.GetComponent<PlayerVisual>();
            p.Renderer = p.Visual.Body;
            p.Visual.SetColor(Palette.Body(p.Color), Palette.Shade(p.Color), Palette.Visor);
            if (p.Id == _myId)
            {
                p.Body = p.Go.AddComponent<Rigidbody2D>();
                p.Body.gravityScale = 0;
                p.Body.freezeRotation = true;
                p.Body.interpolation = RigidbodyInterpolation2D.Interpolate;
                var col = p.Go.AddComponent<CircleCollider2D>();
                col.radius = BodyColliderRadius;
                col.offset = new Vector2(0, BodyColliderOffsetY);
            }
            p.Go.transform.position = p.Pos;
        }

        // =====================================================================================
        // Host: command handling
        // =====================================================================================

        int FreeColor()
        {
            var used = new HashSet<int>(_players.Values.Select(p => p.Color));
            for (int i = 0; i < Palette.Colors.Length; i++) if (!used.Contains(i)) return i;
            return 0;
        }

        void BroadcastLobby()
        {
            _discovery.SetPlayers(_players.Count);
            var sb = new StringBuilder("LOBBY|");
            foreach (var p in _players.Values) sb.Append(p.Id).Append(',').Append(p.Name).Append(',').Append(p.Color).Append(';');
            Emit(sb.ToString());
        }

        void HostHandle(int from, string msg)
        {
            var a = msg.Split('|');
            switch (a[0])
            {
                case "CONNECT":
                    break;
                case "HELLO":
                    if (_phase != Phase.Lobby) { _peer.Send(from, "REFUSED|Game in progress"); break; }
                    if (_players.Count >= Palette.Colors.Length) { _peer.Send(from, "REFUSED|Room full"); break; }
                    AddPlayer(from, Clean(a.Length > 1 ? a[1] : "Player"), FreeColor());
                    _peer.Send(from, "WELCOME|" + from);
                    _peer.Send(from, "CFG|" + _cfg.Encode());
                    BroadcastLobby();
                    break;
                case "DISCONNECT":
                    if (_players.Remove(from))
                    {
                        if (_phase == Phase.Lobby) BroadcastLobby();
                        else { Emit("LEAVE|" + from); HostCheckWin(); }
                    }
                    break;
                case "POS":
                    if (_players.TryGetValue(from, out var pp) && a.Length >= 5)
                    {
                        pp.TargetPos = new Vector2(F(a[1]), F(a[2]));
                        pp.Flip = a[3] == "1";
                        pp.Moving = a[4] == "1";
                    }
                    break;
                case "KILL": HostKill(from, int.Parse(a[1])); break;
                case "TASK": HostTask(from, int.Parse(a[1])); break;
                case "REPORT": HostReport(from, int.Parse(a[1])); break;
                case "VOTE": HostVote(from, int.Parse(a[1])); break;
                case "SABOTAGE": HostSabotage(from, a[1], a.Length > 2 ? a[2] : ""); break;
                case "FIX": HostFix(from, a[1], int.Parse(a[2])); break;
                case "HOLD": HostHold(from, int.Parse(a[1]), a[2] == "1"); break;
                case "VENT": HostVent(from, a[1], int.Parse(a[2])); break;
                case "COLOR": HostColor(from, int.Parse(a[1])); break;
            }
        }

        static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
        static string S(float f) => f.ToString("0.###", CultureInfo.InvariantCulture);

        void HostStartGame()
        {
            if (_phase != Phase.Lobby) return;
            var ids = _players.Keys.OrderBy(i => i).ToList();
            int n = ids.Count;
            _impostorCount = _cfg.ImpostorCount(n);
            var shuffled = ids.OrderBy(_ => UnityEngine.Random.value).ToList();
            var imps = new HashSet<int>(shuffled.Take(_impostorCount));
            var rng = new System.Random();
            _tasksDone = 0;
            _tasksTotal = 0;
            foreach (var p in _players.Values)
            {
                p.Alive = true;
                p.Impostor = imps.Contains(p.Id);
                p.Tasks.Clear();
                p.Vote = -2;
                p.UsedButton = false;
                p.EmergencyUses = 0;
                if (!p.Impostor)
                {
                    p.Tasks.AddRange(TaskCatalog.Generate(rng, _cfg.CommonTasks, _cfg.ShortTasks, _cfg.LongTasks));
                    _tasksTotal += p.Tasks.Sum(t => t.Steps.Length);
                }
            }
            string impCsv = string.Join(",", imps);
            foreach (var p in _players.Values)
            {
                string tasks = string.Join("~", p.Tasks.Select(t => t.Encode()));
                EmitTo(p.Id, "START|" + (p.Impostor ? impCsv : "") + "|" + tasks + "|" + _tasksTotal + "|" + _impostorCount);
            }
        }

        PlayerInfo Get(int id) => _players.TryGetValue(id, out var p) ? p : null;

        void HostKill(int killer, int victim)
        {
            if (_phase != Phase.Playing) return;
            var k = Get(killer); var v = Get(victim);
            if (k == null || v == null || !k.Impostor || !k.Alive || !v.Alive || v.Impostor) return;
            if (Vector2.Distance(k.TargetPos, v.TargetPos) > KillRange + 1.5f) return;
            v.Alive = false;
            Emit("KILLED|" + victim + "|" + S(v.TargetPos.x) + "|" + S(v.TargetPos.y));
            HostCheckWin();
        }

        void HostTask(int who, int taskIdx)
        {
            if (_phase != Phase.Playing) return;
            var p = Get(who);
            if (p == null || p.Impostor || taskIdx < 0 || taskIdx >= p.Tasks.Count || p.Tasks[taskIdx].Complete) return;
            p.Tasks[taskIdx].Done++;
            _tasksDone++;
            EmitTo(who, "TASKOK|" + taskIdx);
            Emit("PROG|" + _tasksDone + "|" + _tasksTotal);
            HostCheckWin();
        }

        void HostReport(int who, int bodyOwner)
        {
            if (_phase != Phase.Playing) return;
            var p = Get(who);
            if (p == null || !p.Alive) return;
            if (bodyOwner >= 0)
            {
                if (!_bodies.ContainsKey(bodyOwner)) return;
            }
            else
            {
                if (p.UsedButton || Vector2.Distance(p.TargetPos, WorldMap.EmergencyButtonPos) > WorldMap.EmergencyButtonRange + 1f) return;
                p.EmergencyUses++;
                if (p.EmergencyUses >= _cfg.Emergencies) p.UsedButton = true;
            }
            foreach (var q in _players.Values) q.Vote = -2;
            Emit("MEET|" + who + "|" + bodyOwner + "|" + (int)MeetingSeconds);
        }

        void HostVote(int voter, int target)
        {
            if (_phase != Phase.Meeting) return;
            var p = Get(voter);
            if (p == null || !p.Alive || p.Vote != -2) return;
            if (target >= 0 && (Get(target) == null || !Get(target).Alive)) return;
            p.Vote = target;
            Emit("VOTED|" + voter);
            if (_players.Values.Where(x => x.Alive).All(x => x.Vote != -2)) HostTally();
        }

        void HostTally()
        {
            var counts = new Dictionary<int, int>();
            foreach (var p in _players.Values.Where(x => x.Alive && x.Vote != -2))
                counts[p.Vote] = counts.TryGetValue(p.Vote, out var c) ? c + 1 : 1;
            int eject = -1;
            if (counts.Count > 0)
            {
                int best = counts.Values.Max();
                var top = counts.Where(kv => kv.Value == best).Select(kv => kv.Key).ToList();
                if (top.Count == 1 && top[0] >= 0) eject = top[0];
            }
            bool wasImp = false;
            if (eject >= 0)
            {
                var e = Get(eject);
                wasImp = e.Impostor;
                e.Alive = false;
            }
            int impLeft = _players.Values.Count(x => x.Impostor && x.Alive);
            string votes = string.Join(";", _players.Values.Where(x => x.Vote != -2).Select(x => x.Id + ">" + x.Vote));
            Emit("RESULT|" + eject + "|" + (wasImp ? 1 : 0) + "|" + impLeft + "|" + votes);
        }

        // Returns true when the game ended.
        bool HostCheckWin()
        {
            if (_phase != Phase.Playing && _phase != Phase.Result) return false;
            int imp = _players.Values.Count(p => p.Impostor && p.Alive);
            int crew = _players.Values.Count(p => !p.Impostor && p.Alive);
            int winner = -1;
            if (_impostorCount > 0 && imp == 0) winner = 0;
            else if (_tasksTotal > 0 && _tasksDone >= _tasksTotal) winner = 0;
            else if (_impostorCount > 0 && imp >= crew) winner = 1;
            if (winner < 0) return false;
            HostEnd(winner);
            return true;
        }

        void HostEnd(int winner)
        {
            string impNames = string.Join(", ", _players.Values.Where(p => p.Impostor).Select(p => p.Name));
            Emit("END|" + winner + "|" + impNames);
        }

        // =====================================================================================
        // Client + host: event application
        // =====================================================================================

        void ApplyEvent(string msg)
        {
            var a = msg.Split('|');
            switch (a[0])
            {
                case "REFUSED":
                    _status = Loc.T(a[1]);
                    _peer?.Dispose(); _peer = null;
                    break;
                case "WELCOME":
                    _myId = int.Parse(a[1]);
                    SetPhase(Phase.Lobby);
                    break;
                case "LOBBY":
                    ApplyLobby(a[1]);
                    break;
                case "START":
                    ApplyStart(a);
                    break;
                case "STATE":
                    ApplyState(a[1]);
                    break;
                case "KILLED":
                    {
                        int id = int.Parse(a[1]);
                        var v = Get(id);
                        if (v != null)
                        {
                            v.Alive = false;
                            SpawnBody(id, new Vector2(F(a[2]), F(a[3])));
                        }
                        Play("impostor_kill");
                        break;
                    }
                case "TASKOK":
                    {
                        int idx = int.Parse(a[1]);
                        if (idx >= 0 && idx < _myTasks.Count)
                        {
                            var tk = _myTasks[idx];
                            tk.Done++;
                            if (tk.Type == TaskTypes.InspectSample && tk.Done == 1) tk.NotBefore = Time.time + (_bot ? 2f : 60f);
                        }
                        Play(_myTasks.Count > idx && _myTasks[idx].Complete ? "task_Complete" : "task_Inprogress");
                        break;
                    }
                case "PROG":
                    _tasksDone = int.Parse(a[1]); _tasksTotal = int.Parse(a[2]);
                    Debug.Log("[LAN] tasks " + _tasksDone + "/" + _tasksTotal);
                    break;
                case "LEAVE":
                    {
                        int id = int.Parse(a[1]);
                        if (_players.TryGetValue(id, out var gone))
                        {
                            if (gone.Go != null) Destroy(gone.Go);
                            _players.Remove(id);
                        }
                        break;
                    }
                case "MEET":
                    _meetingCaller = int.Parse(a[1]);
                    _meetingBody = int.Parse(a[2]);
                    _meetingTimer = float.Parse(a[3], CultureInfo.InvariantCulture);
                    _myVote = -2;
                    _votedIds.Clear();
                    foreach (var b in _bodies.Values) Destroy(b);
                    _bodies.Clear();
                    foreach (var p in _players.Values) p.Vote = -2;
                    if (_me != null && _me.Body != null) _me.Body.linearVelocity = Vector2.zero;
                    _taskHold = 0;
                    if (Minigame.Instance != null) Minigame.Instance.Close(false);
                    foreach (var kv in _closedDoors) if (kv.Key != null) kv.Key.SetDoorway(true);
                    _closedDoors.Clear();
                    LeaveAllVents();
                    SetPhase(Phase.Meeting);
                    ShowMeeting();
                    Play(_meetingBody >= 0 ? "Report_BodyFound" : "alarm_emergencymeeting");
                    break;
                case "VOTED":
                    _votedIds.Add(int.Parse(a[1]));
                    break;
                case "RESULT":
                    {
                        int eject = int.Parse(a[1]);
                        bool wasImp = a[2] == "1";
                        int impLeft = int.Parse(a[3]);
                        if (eject >= 0 && _players.TryGetValue(eject, out var e))
                        {
                            e.Alive = false;
                            _resultText = e.Name + (wasImp ? Loc.T(" was an Impostor.") : Loc.T(" was not an Impostor."));
                        }
                        else _resultText = Loc.T("No one was ejected.");
                        if (_impostorCount > 0) _resultText += "\n" + impLeft + (impLeft == 1 ? Loc.T(" Impostor remains.") : Loc.T(" Impostors remain."));
                        _resultTimer = ResultSeconds;
                        _voteMap.Clear();
                        if (a.Length > 4)
                            foreach (var v in a[4].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                var vv = v.Split('>');
                                int voter = int.Parse(vv[0]), target = int.Parse(vv[1]);
                                if (!_voteMap.TryGetValue(target, out var lst)) _voteMap[target] = lst = new List<int>();
                                lst.Add(voter);
                            }
                        _ejectId = eject;
                        _resultStage = 0; _stageClock = 0;
                        SetPhase(Phase.Result);
                        RevealVotes();
                        Play("EjectSplash");
                        break;
                    }
                case "RESUME":
                    HideMeeting();
                    ResetPositions();
                    _killCd = Mathf.Max(_killCd, 10f);
                    SetPhase(Phase.Playing);
                    break;
                case "END":
                    _endWinner = int.Parse(a[1]);
                    Debug.Log("[LAN] END winner=" + (_endWinner == 0 ? "crew" : "impostors"));
                    _endImpostors = a.Length > 2 ? a[2] : "";
                    HideMeeting();
                    ResetSabotage();
                    _endText = _endWinner == 0 ? Loc.T("Crewmates Win") : Loc.T("Impostors Win");
                    SetPhase(Phase.Ended);
                    break;
                case "VENT":
                    ApplyVent(a);
                    break;
                case "CFG":
                    ApplyCfg(a[1]);
                    break;
                case "COUNT":
                    Play("Panel_TempButton");
                    break;
                case "SAB":
                case "SABPROG":
                case "SABFIXED":
                case "DOORS":
                    ApplySabEvent(a);
                    break;
                case "LOBBYBACK":
                    ResetSabotage();
                    HideMeeting();
                    ClearRound();
                    ResetLobbyPositions();
                    SetPhase(Phase.Lobby);
                    break;
            }
        }

        void ApplyLobby(string data)
        {
            var seen = new HashSet<int>();
            foreach (var entry in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var f = entry.Split(',');
                int id = int.Parse(f[0]);
                seen.Add(id);
                if (!_players.TryGetValue(id, out var p)) p = AddPlayer(id, f[1], int.Parse(f[2]));
                p.Name = f[1];
                int col = int.Parse(f[2]);
                p.Color = col;
                if (p.Visual != null) p.Visual.SetColor(Palette.Body(col), Palette.Shade(col), Palette.Visor);
            }
            foreach (var id in _players.Keys.Where(i => !seen.Contains(i)).ToList())
            {
                if (_players[id].Go != null) Destroy(_players[id].Go);
                _players.Remove(id);
            }
            if (_myId >= 0 && _players.TryGetValue(_myId, out var me)) _me = me;
        }

        void ApplyStart(string[] a)
        {
            var imps = a[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
            _myTasks.Clear();
            foreach (var t in a[2].Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries)) _myTasks.Add(GameTask.Decode(t));
            _tasksTotal = int.Parse(a[3]);
            _impostorCount = int.Parse(a[4]);
            _tasksDone = 0;
            ClearRound();
            // The host already holds the authoritative roles; clients only learn their own (and partners').
            if (!_isHost)
            {
                foreach (var p in _players.Values) { p.Alive = true; p.Impostor = false; p.UsedButton = false; }
                foreach (var i in imps) if (_players.TryGetValue(i, out var ip)) ip.Impostor = true;
            }
            if (Environment.GetCommandLineArgs().Contains("-forceimp") && _me != null) { _me.Impostor = true; _impostorCount = Mathf.Max(_impostorCount, 1); }
            ResetPositions();
            ResetSabotage();
            _killCd = 10f;
            SetPhase(Phase.Playing);
            ShowIntro();
            Play(_me != null && _me.Impostor ? "RoleReveal_Impostor" : "RoleReveal_Crewmate");
        }

        void ApplyState(string data)
        {
            foreach (var entry in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var f = entry.Split(':');
                int id = int.Parse(f[0]);
                if (!_players.TryGetValue(id, out var p) || id == _myId) continue;
                p.TargetPos = new Vector2(F(f[1]), F(f[2]));
                p.Flip = f[3] == "1";
                p.Moving = f[4] == "1";
                p.Alive = f[5] == "1";
            }
        }

        void ResetPositions()
        {
            int i = 0, n = _players.Count;
            foreach (var p in _players.Values.OrderBy(x => x.Id))
            {
                p.Pos = p.TargetPos = WorldMap.SpawnPos(i++, n);
                if (p.Go != null) p.Go.transform.position = p.Pos;
            }
        }

        void ClearRound()
        {
            foreach (var b in _bodies.Values) Destroy(b);
            _bodies.Clear();
        }

        void SpawnBody(int owner, Vector2 pos)
        {
            if (_bodies.ContainsKey(owner)) Destroy(_bodies[owner]);
            var p = Get(owner);
            var go = Instantiate(Resources.Load<GameObject>("DeadBody"));
            go.name = "Body " + owner;
            go.transform.position = new Vector3(pos.x, pos.y, 0);
            int ci = p != null ? p.Color : 0;
            var block = new MaterialPropertyBlock();
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.GetPropertyBlock(block);
                block.SetColor("_BodyColor", Palette.Body(ci));
                block.SetColor("_BackColor", Palette.Shade(ci));
                block.SetColor("_VisorColor", Palette.Visor);
                r.SetPropertyBlock(block);
            }
            _bodies[owner] = go;
        }

        // =====================================================================================
        // Update loop
        // =====================================================================================

        void Update()
        {
            PumpNetwork();
            RunAutomation();

            foreach (var p in _players.Values) SyncAvatar(p);

            if (_isHost) HostTick();

            if (_phase == Phase.Lobby || _phase == Phase.Playing) LocalControl();
            else if (_me?.Body != null) _me.Body.linearVelocity = Vector2.zero;

            if (_phase == Phase.Meeting && !_isHost) _meetingTimer -= Time.deltaTime;
            if (_phase == Phase.Result && !_isHost) _resultTimer -= Time.deltaTime;

            UpdateHighlights();
            UpdateFog();
            UpdateHud();
            UpdateSabotage();
            UpdateAmbience();
            UpdateMap();
            UpdateLobbyHud();
            UpdateLobbyAudio();
            UpdateMenuUi();
            UpdateVentHud();
            UpdateVentInput();
            UpdateIntro();
            UpdateMeetingUi();
            FollowCamera();
        }

        void UpdateFog()
        {
            _fog.Active = _phase == Phase.Playing && _me != null && _me.Alive && !IntroActive;
            _fog.Target = _me != null ? _me.Go.transform : null;
            _fog.Radius = _me != null && _me.Impostor ? ImpostorVision * _cfg.ImpostorVision / 1.5f : CrewVision * _cfg.CrewVision * (_sab == SabKind.Lights ? 0.3f : 1f);
        }

        // Task consoles are outlined yellow; the one in reach turns white (as in the original).
        readonly MaterialPropertyBlock _mpb = new MaterialPropertyBlock();
        readonly HashSet<Console> _lit = new HashSet<Console>();
        static readonly Color TaskYellow = new Color(1f, 0.92f, 0.016f, 1f);

        void SetOutline(Console c, bool on, Color col)
        {
            var r = c.Image;
            if (r == null) return;
            r.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_Outline", on ? 1f : 0f);
            _mpb.SetColor("_OutlineColor", col);
            r.SetPropertyBlock(_mpb);
        }

        void UpdateHighlights()
        {
            var want = new Dictionary<Console, Color>();
            if (_phase == Phase.Playing && _me != null && !_me.Impostor)
            {
                foreach (var t in _myTasks)
                {
                    if (t.Complete) continue;
                    var c = WorldMap.FindConsole(t.Current.Console, t.Current.Room);
                    if (c != null) want[c] = TaskYellow;
                }
                NearestTask(out var near);
                if (near != null) want[near] = Color.white;
            }
            foreach (var c in _lit.Where(c => !want.ContainsKey(c)).ToList()) { SetOutline(c, false, Color.white); _lit.Remove(c); }
            foreach (var kv in want) { SetOutline(kv.Key, true, kv.Value); _lit.Add(kv.Key); }
        }

        void LateUpdate()
        {
            if (_shotPath != null && Time.timeSinceLevelLoad > _shotAt)
            {
                ScreenCapture.CaptureScreenshot(_shotPath);
                _shotPath = null;
                Invoke(nameof(QuitNow), 1.0f);
            }
        }

        void QuitNow() { Application.Quit(); }

        void PumpNetwork()
        {
            if (_peer == null) return;
            while (_peer.Inbox.TryDequeue(out var kv))
            {
                try
                {
                    if (_isHost) HostHandle(kv.Key, kv.Value);
                    else if (kv.Value == "DISCONNECT")
                    {
                        _status = Loc.T("Connection lost");
                        LeaveToMenu();
                        return;
                    }
                    else ApplyEvent(kv.Value);
                }
                catch (Exception e) { Debug.LogWarning("[LAN] bad message '" + kv.Value + "': " + e.Message); }
            }
        }

        void LeaveToMenu()
        {
            _peer?.Dispose(); _peer = null;
            foreach (var p in _players.Values) if (p.Go != null) Destroy(p.Go);
            _players.Clear();
            ClearRound();
            _me = null; _myId = -1; _isHost = false;
            SetPhase(Phase.Menu);
        }

        void HostTick()
        {
            HostSabTick();
            if (_phase == Phase.Meeting)
            {
                _meetingTimer -= Time.deltaTime;
                if (_meetingTimer <= 0f) HostTally();
            }
            else if (_phase == Phase.Result)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0f)
                {
                    if (!HostCheckWin()) Emit("RESUME");
                }
            }

            if (_me != null) _me.TargetPos = _me.Pos;
            _netTimer -= Time.deltaTime;
            if (_netTimer <= 0f && _players.Count > 1 && (_phase == Phase.Lobby || _phase == Phase.Playing))
            {
                _netTimer = NetRate;
                var sb = new StringBuilder("STATE|");
                foreach (var p in _players.Values)
                    sb.Append(p.Id).Append(':').Append(S(p.TargetPos.x)).Append(':').Append(S(p.TargetPos.y)).Append(':')
                      .Append(p.Flip ? 1 : 0).Append(':').Append(p.Moving ? 1 : 0).Append(':').Append(p.Alive ? 1 : 0).Append(';');
                _peer.Broadcast(sb.ToString());
            }
        }

        void SyncAvatar(PlayerInfo p)
        {
            EnsureAvatar(p);
            bool visible = _phase != Phase.Menu && p.InVent < 0 && (p.Alive || (_me != null && !_me.Alive));
            if (_phase == Phase.Meeting || _phase == Phase.Result) visible = false;
            p.Renderer.enabled = visible;
            p.Visual.SetState(p.Moving && p.Alive || p.Moving, !p.Alive, p.Flip);
            p.Renderer.color = p.Alive ? Color.white : new Color(1, 1, 1, 0.55f);
            p.Renderer.sortingOrder = 100 - Mathf.RoundToInt(p.Go.transform.position.y * 10f);
            if (p.Id == _myId)
            {
                if (p.Body != null)
                {
                    p.Pos = p.Go.transform.position;
                    var col = p.Go.GetComponent<CircleCollider2D>();
                    if (col != null) col.enabled = p.Alive;
                }
            }
            else
            {
                p.Pos = Vector2.Lerp(p.Pos, p.TargetPos, 1f - Mathf.Exp(-14f * Time.deltaTime));
                p.Go.transform.position = p.Pos;
            }
        }

        // ---- tasks ----

        /// <summary>The nearest console the local player can use for one of its pending task steps.</summary>
        int NearestTask(out Console console)
        {
            console = null;
            int best = -1; float bd = float.MaxValue;
            for (int i = 0; i < _myTasks.Count; i++)
            {
                var t = _myTasks[i];
                if (t.Complete || Time.time < t.NotBefore) continue;
                var c = WorldMap.FindConsole(t.Current.Console, t.Current.Room);
                if (c == null) continue;
                float d = Vector2.Distance(c.transform.position, _me.Pos);
                float range = Mathf.Max(c.usableDistance, 0.8f);
                if (d <= range && d < bd) { bd = d; best = i; console = c; }
            }
            return best;
        }

        void LocalControl()
        {
            if (_me == null || _me.Body == null) return;
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (_bot) input = BotInput();
            if (Minigame.Instance != null || IntroActive || _myVent != null) input = Vector2.zero;
            input = Vector2.ClampMagnitude(input, 1f);
            _me.Body.linearVelocity = input * MoveSpeed * (_me.Alive ? 1f : 1.2f);
            _me.Moving = input.sqrMagnitude > 0.01f;
            if (Mathf.Abs(input.x) > 0.05f) _me.Flip = input.x < 0;

            if (_me.Moving && _me.Alive)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    _stepTimer = 0.36f;
                    var fc = FootstepAt(_me.Pos);
                    if (fc != null) _audio.PlayOneShot(fc, 0.45f); else Play("FootstepMetal0" + UnityEngine.Random.Range(1, 5), 0.4f);
                }
            }

            if (!_isHost)
            {
                _netTimer -= Time.deltaTime;
                if (_netTimer <= 0f)
                {
                    _netTimer = NetRate;
                    _peer?.Send(0, "POS|" + S(_me.Pos.x) + "|" + S(_me.Pos.y) + "|" + (_me.Flip ? 1 : 0) + "|" + (_me.Moving ? 1 : 0));
                }
            }

            if (_phase != Phase.Playing) return;

            if (_killCd > 0f) _killCd -= Time.deltaTime;

            if (_me.Impostor && _me.Alive && _killCd <= 0f && (Input.GetKeyDown(KeyCode.Q) || _bot))
            {
                var target = NearestPlayer(KillRange, p => p.Alive && !p.Impostor && p != _me);
                if (target != null) { Cmd("KILL|" + target.Id); _killCd = KillCooldown; }
            }

            if (_me.Alive && (Input.GetKeyDown(KeyCode.R) || (_bot && !_me.Impostor)))
            {
                var body = _bodies.Where(kv => Vector2.Distance(kv.Value.transform.position, _me.Pos) <= ReportRange)
                                  .Select(kv => (int?)kv.Key).FirstOrDefault();
                if (body.HasValue) Cmd("REPORT|" + body.Value);
            }
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || (_bot && _taskRunning < 0)) UseNearest();
            UpdateTaskProgress();
        }

        // ---- use button: nearest task console, otherwise the emergency button ----

        int _taskRunning = -1;

        public void UseNearest()
        {
            if (_phase != Phase.Playing || _me == null || _taskRunning >= 0 || Minigame.Instance != null) return;
            if (TrySabotageFix()) return;
            if (!_me.Impostor)
            {
                int idx = NearestTask(out _);
                if (idx >= 0)
                {
                    if (OpenTaskMinigame(idx)) return;
                    _taskRunning = idx; _taskHold = 0; return;
                }
            }
            var sv = WorldMap.SurvConsole;
            if (sv != null && Vector2.Distance(sv.position, _me.Pos) <= 1.6f)
            {
                var m = Minigame.Open("SurvMinigame", ok => { }, 0, SystemTypes.Security, "SurvConsole", _bot, SystemTypes.Hallway, _sab == SabKind.Comms ? "down" : null);
                if (m != null) { if (_me.Body != null) _me.Body.linearVelocity = Vector2.zero; return; }
            }
            var adm = WorldMap.AdminMap;
            if (adm != null && !_bot && Vector2.Distance(adm.position, _me.Pos) <= 1.5f) { ToggleMap(MapMode.Count); return; }
            if (_bot && !Environment.GetCommandLineArgs().Contains("-meet")) return;     // bots never press the button on their own
            if (_me.Alive && !_me.UsedButton && Vector2.Distance(WorldMap.EmergencyButtonPos, _me.Pos) <= WorldMap.EmergencyButtonRange)
                Cmd("REPORT|-1");
        }

        void UpdateTaskProgress()
        {
            if (_taskRunning < 0) return;
            if (_me == null || _phase != Phase.Playing || NearestTask(out _) != _taskRunning) { _taskRunning = -1; _taskHold = 0; return; }
            _taskHold += Time.deltaTime;
            if (_taskHold >= TaskHoldSeconds) { Cmd("TASK|" + _taskRunning); _taskRunning = -1; _taskHold = 0; }
        }

        PlayerInfo NearestPlayer(float range, Func<PlayerInfo, bool> filter)
        {
            PlayerInfo best = null; float bd = range;
            foreach (var p in _players.Values)
            {
                if (!filter(p)) continue;
                float d = Vector2.Distance(p.Pos, _me.Pos);
                if (d <= bd) { bd = d; best = p; }
            }
            return best;
        }

        void FollowCamera()
        {
            Vector3 target = (_me != null && _phase != Phase.Menu)
                ? new Vector3(_me.Pos.x, _me.Pos.y, -10)
                : new Vector3(LobbyOffset.x, LobbyOffset.y + 0.5f, -10);
            _cam.transform.position = Vector3.Lerp(_cam.transform.position, target, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        // =====================================================================================
        // Command-line automation (smoke testing of the LAN flow without a human)
        // =====================================================================================

        Vector2 BotInput()
        {
            _botDirTimer -= Time.deltaTime;
            if (_botDirTimer <= 0f)
            {
                _botDirTimer = UnityEngine.Random.Range(0.6f, 1.6f);
                if (_me != null && _phase == Phase.Playing && _me.Alive && _me.Impostor)
                {
                    var prey = NearestPlayer(999f, p => p.Alive && !p.Impostor);
                    if (prey != null) { _botDirTimer = 0.1f; return _botDir = (prey.Pos - _me.Pos).normalized; }
                }
                if (_me != null && _phase == Phase.Playing && _me.Alive && !_me.Impostor && _bodies.Count > 0)
                {
                    var any = _bodies.Values.First();
                    _botDirTimer = 0.2f;
                    return _botDir = ((Vector2)any.transform.position - _me.Pos).normalized;
                }
                if (_me != null && _phase == Phase.Playing && !_me.Impostor && _sab != SabKind.None && _sab != SabKind.Doors)
                {
                    foreach (var sc in SabConsoles())
                    {
                        if (_sab == SabKind.Reactor && sc.Idx != _me.Id % 2) continue;
                        var sc2 = WorldMap.FindConsole(sc.Name, sc.Room);
                        if (sc2 == null) continue;
                        if (_botTeleport && _me.Body != null) _me.Body.position = (Vector2)sc2.transform.position + new Vector2(0f, -0.3f);
                        _botDirTimer = 0.3f;
                        return _botDir = ((Vector2)sc2.transform.position - _me.Pos).normalized * 0.01f;
                    }
                }
                if (_me != null && _phase == Phase.Playing && !_me.Impostor)
                {
                    foreach (var t in _myTasks)
                    {
                        if (t.Complete) continue;
                        var c = WorldMap.FindConsole(t.Current.Console, t.Current.Room);
                        if (c == null) continue;
                        // Bots have no pathfinding: with -teleport they hop to the console instead of walking.
                        if (_botTeleport && _me.Body != null) _me.Body.position = (Vector2)c.transform.position + new Vector2(0f, -0.3f);
                        _botDirTimer = 0.3f;
                        return _botDir = ((Vector2)c.transform.position - _me.Pos).normalized * 0.01f;
                    }
                }
                _botDir = UnityEngine.Random.insideUnitCircle.normalized;
            }
            return _botDir;
        }

        void RunAutomation()
        {
            _autoTimer += Time.deltaTime;
            if (_phase == Phase.Menu && _autoHost && _autoTimer > 0.5f) HostGame();
            if (_phase == Phase.Menu && _autoJoin && _peer == null && _autoTimer > 2f) { _autoTimer = 1f; JoinGame(_ipField); }
            if (_isHost && _autoHost && _phase == Phase.Lobby && Environment.GetCommandLineArgs().Contains("-solo") && _autoTimer > 2f) HostStartGame();
            if (_isHost && _autoHost && _phase == Phase.Lobby && _players.Count >= (Environment.GetCommandLineArgs().Contains("-four") ? 4 : 2) && _autoTimer > 8f) HostStartGame();
            if (_phase == Phase.Playing && _autoTimer > 7f && !_ventDemo && Environment.GetCommandLineArgs().Contains("-ventdemo") && _me != null)
            {
                _ventDemo = true;
                var cv = _vents.Values.OrderBy(v => Vector2.Distance(v.transform.position, _me.Pos)).First();
                _me.Body.position = (Vector2)cv.transform.position + new Vector2(0.3f, 0f);
                _me.Pos = _me.TargetPos = _me.Body.position;
                Cmd("VENT|enter|" + cv.Id);
            }
            if (_phase == Phase.Playing && _autoTimer > 6f && !IntroActive && !_mapShown && Environment.GetCommandLineArgs().Contains("-openmap")) { _mapShown = true; var args2 = Environment.GetCommandLineArgs(); int mi2 = Array.IndexOf(args2, "-openmap"); ToggleMap((MapMode)Enum.Parse(typeof(MapMode), args2[mi2 + 1])); }
            if (_phase == Phase.Playing && _autoTimer > 7f && !_sabMenuShown && Environment.GetCommandLineArgs().Contains("-sabmenu")) { _sabMenuShown = true; _sabCooldown = 0; ToggleSabMenu(); }
            if (_phase == Phase.Playing && _autoTimer > 3f && !_mgOpened)
            {
                var args = Environment.GetCommandLineArgs();
                int mi = Array.IndexOf(args, "-openmg");
                if (mi >= 0 && mi + 1 < args.Length)
                {
                    _mgOpened = true;
                    Minigame.Open(args[mi + 1], ok => Debug.Log("[LAN] minigame closed ok=" + ok), 0, SystemTypes.Admin, "", _bot);
                }
            }
            if (_phase == Phase.Playing && Environment.GetCommandLineArgs().Contains("-meet") && _autoTimer > 4f && !_meetDone) { _meetDone = true; Cmd("REPORT|-1"); }
            if (_bot && _botSab != null && _phase == Phase.Playing && _me != null && _me.Impostor && _me.Alive && !IntroActive && _sabCooldown <= 0f && _sab == SabKind.None && _autoTimer > 12f)
                Cmd("SABOTAGE|" + _botSab + "|" + (_botSab == "Doors" ? ((int)SystemTypes.Cafeteria).ToString() : ""));
            if (_bot && _phase == Phase.Meeting && _me != null && _me.Alive && _myVote == -2)
            {
                _myVote = Environment.GetCommandLineArgs().Contains("-voteself") ? _myId : -1;
                Cmd("VOTE|" + _myVote);
            }
        }

        // =====================================================================================
        // IMGUI
        // =====================================================================================

        GUIStyle _big, _mid, _small, _center;

        void InitStyles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 22 };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            _center = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        }

        void OnGUI()
        {
            InitStyles();
            float s = Screen.height / 720f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(s, s, 1));
            float W = Screen.width / s, H = 720f;

            DrawWorldLabels(s);

            switch (_phase)
            {
                case Phase.Ended: DrawEnd(W, H); break;
            }
        }

        void DrawWorldLabels(float s)
        {
            if (_phase != Phase.Lobby && _phase != Phase.Playing) return;
            if (Minigame.Instance != null || IntroActive || _map != null) return;
            var st = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            foreach (var p in _players.Values)
            {
                if (p.Renderer == null || !p.Renderer.enabled) continue;
                var sp = _cam.WorldToScreenPoint(p.Pos + Vector2.up * 0.5f);
                var r = new Rect(sp.x / s - 80, (Screen.height - sp.y) / s - 12, 160, 24);
                bool knownImp = p.Impostor && _me != null && _me.Impostor;
                st.normal.textColor = knownImp ? new Color(1f, 0.25f, 0.25f) : Color.white;
                GUI.Label(r, p.Name, st);
            }
            if (_phase != Phase.Playing || _me == null || _me.Impostor) return;
            st.fontSize = 22;
            if (_sab != SabKind.None && _sab != SabKind.Doors && _me.Alive)
            {
                st.normal.textColor = new Color(1f, 0.2f, 0.2f);
                foreach (var sc in SabConsoles())
                {
                    var sc2 = WorldMap.FindConsole(sc.Name, sc.Room);
                    if (sc2 == null) continue;
                    var spos = _cam.WorldToScreenPoint(sc2.transform.position + Vector3.up * 0.5f);
                    GUI.Label(new Rect(spos.x / s - 20, (Screen.height - spos.y) / s - 14, 40, 28), "!", st);
                }
            }
            st.normal.textColor = new Color(1f, 0.9f, 0.2f);
            st.normal.textColor = new Color(1f, 0.9f, 0.2f);
            foreach (var t in _myTasks)
            {
                if (t.Complete) continue;
                var c = WorldMap.FindConsole(t.Current.Console, t.Current.Room);
                if (c == null) continue;
                var sp = _cam.WorldToScreenPoint(c.transform.position + Vector3.up * 0.5f);
                GUI.Label(new Rect(sp.x / s - 20, (Screen.height - sp.y) / s - 14, 40, 28), "!", st);
            }
        }

        void DrawMenu(float W, float H)
        {
            GUI.Box(new Rect(W / 2 - 330, 60, 660, 600), "");
            GUI.Label(new Rect(W / 2 - 330, 75, 660, 60), "AMONG US · LAN", _big);
            GUI.Label(new Rect(W / 2 - 300, 150, 120, 30), "Nombre:", _mid);
            _nameField = GUI.TextField(new Rect(W / 2 - 170, 150, 260, 32), _nameField, 10, _mid);
            if (GUI.Button(new Rect(W / 2 + 110, 148, 190, 36), "Crear partida", _mid)) { _nameField = Clean(_nameField); HostGame(); }

            GUI.Label(new Rect(W / 2 - 300, 215, 400, 30), "Partidas en la red local:", _mid);
            var found = _discovery.Snapshot();
            float y = 255;
            if (found.Count == 0) GUI.Label(new Rect(W / 2 - 300, y, 600, 30), "Buscando...", _small);
            foreach (var f in found)
            {
                GUI.Label(new Rect(W / 2 - 300, y, 360, 30), f.Name + "  (" + f.Players + " jug.)  " + f.Ip, _small);
                if (GUI.Button(new Rect(W / 2 + 100, y - 2, 200, 30), "Unirse", _small)) { _nameField = Clean(_nameField); JoinGame(f.Ip); }
                y += 34;
                if (y > 450) break;
            }
            GUI.Label(new Rect(W / 2 - 300, 500, 200, 30), "IP manual:", _mid);
            _ipField = GUI.TextField(new Rect(W / 2 - 170, 500, 200, 32), _ipField, 40, _mid);
            if (GUI.Button(new Rect(W / 2 + 50, 498, 250, 36), "Unirse por IP", _mid)) { _nameField = Clean(_nameField); JoinGame(_ipField.Trim()); }
            GUI.Label(new Rect(W / 2 - 300, 560, 600, 80),
                "Mueve: WASD/flechas · Tarea: mantén E · Matar: Q · Reportar: R · Botón de emergencia: F", _small);
            if (!string.IsNullOrEmpty(_status)) GUI.Label(new Rect(W / 2 - 300, 620, 600, 30), _status, _small);
        }

        void DrawLobby(float W, float H)
        {
            GUI.Box(new Rect(20, 20, 300, 60 + _players.Count * 28), "");
            GUI.Label(new Rect(30, 25, 280, 30), "Sala (" + _players.Count + "/12)", _mid);
            float y = 60;
            foreach (var p in _players.Values.OrderBy(p => p.Id))
            {
                var prev = GUI.color; GUI.color = Palette.Colors[p.Color % Palette.Colors.Length];
                GUI.Label(new Rect(30, y, 280, 28), p.Name + (p.Id == 0 ? " (anfitrión)" : ""), _small);
                GUI.color = prev; y += 28;
            }
            if (_isHost)
            {
                if (GUI.Button(new Rect(W / 2 - 120, H - 80, 240, 50), _players.Count >= 2 ? "EMPEZAR" : "EMPEZAR (solo, sin impostor)", _mid))
                    HostStartGame();
                GUI.Label(new Rect(W / 2 - 300, H - 130, 600, 30), "Los demás deben unirse desde la lista de partidas o por tu IP.", _center);
            }
            else GUI.Label(new Rect(W / 2 - 300, H - 80, 600, 40), "Esperando que el anfitrión empiece...", _center);
        }

        void DrawHud(float W, float H)
        {
            if (_me == null) return;
            GUI.Box(new Rect(20, 20, 360, 26), "");
            float frac = _tasksTotal > 0 ? (float)_tasksDone / _tasksTotal : 0f;
            var prev = GUI.color; GUI.color = new Color(0.3f, 0.9f, 0.4f);
            GUI.DrawTexture(new Rect(22, 22, 356 * frac, 22), _white);
            GUI.color = prev;
            GUI.Label(new Rect(24, 20, 360, 26), "  Tareas totales", _small);

            string role = _me.Impostor ? "IMPOSTOR" : "TRIPULANTE";
            var rolePrev = GUI.color; GUI.color = _me.Impostor ? new Color(1f, 0.3f, 0.3f) : new Color(0.5f, 0.9f, 1f);
            GUI.Label(new Rect(20, 52, 360, 30), role + (_me.Alive ? "" : " (fantasma)"), _mid);
            GUI.color = rolePrev;

            float y = 86;
            if (!_me.Impostor)
                foreach (var t in _myTasks)
                {
                    string room = t.RoomName(t.Current.Room);
                    string line = (t.Complete ? "[OK] " : "- ") + room + ": " + t.Title + (t.Steps.Length > 1 ? " (" + t.Done + "/" + t.Steps.Length + ")" : "");
                    var p2 = GUI.color; GUI.color = t.Complete ? new Color(0.4f, 1f, 0.4f) : Color.white;
                    GUI.Label(new Rect(20, y, 420, 24), line, _small); y += 22;
                    GUI.color = p2;
                }
            else
            {
                string partners = string.Join(", ", _players.Values.Where(p => p.Impostor && p != _me).Select(p => p.Name));
                if (partners.Length > 0) GUI.Label(new Rect(20, y, 380, 24), "Socios: " + partners, _small);
                y += 22;
                GUI.Label(new Rect(20, y, 380, 24), _killCd > 0 ? "Matar en " + Mathf.CeilToInt(_killCd) + "s" : "Matar: Q (listo)", _small);
            }

            if (_me.Alive || !_me.Impostor)
            {
                int near = _me.Impostor ? -1 : NearestTask(out _);
                if (near >= 0)
                {
                    GUI.Label(new Rect(W / 2 - 250, H - 110, 500, 30), "Mantén E: " + _myTasks[near].Title, _center);
                    GUI.Box(new Rect(W / 2 - 120, H - 75, 240, 20), "");
                    GUI.color = new Color(0.9f, 0.9f, 0.3f);
                    GUI.DrawTexture(new Rect(W / 2 - 118, H - 73, 236 * (_taskHold / TaskHoldSeconds), 16), _white);
                    GUI.color = Color.white;
                }
                else if (_me.Alive && _bodies.Any(kv => Vector2.Distance(kv.Value.transform.position, _me.Pos) <= ReportRange))
                    GUI.Label(new Rect(W / 2 - 250, H - 80, 500, 30), "R: Reportar cuerpo", _center);
                else if (_me.Alive && Vector2.Distance(WorldMap.EmergencyButtonPos, _me.Pos) <= WorldMap.EmergencyButtonRange && !_me.UsedButton)
                    GUI.Label(new Rect(W / 2 - 250, H - 80, 500, 30), "F: Reunión de emergencia", _center);
            }
        }

        void DrawMeeting(float W, float H)
        {
            GUI.Box(new Rect(W / 2 - 360, 40, 720, 640), "");
            string why = _meetingBody >= 0 ? "¡Cuerpo reportado!" : "Reunión de emergencia";
            var caller = Get(_meetingCaller);
            GUI.Label(new Rect(W / 2 - 360, 50, 720, 50), why, _big);
            GUI.Label(new Rect(W / 2 - 360, 100, 720, 30), "Convocó: " + (caller?.Name ?? "?") + "   ·   Tiempo: " + Mathf.Max(0, Mathf.CeilToInt(_meetingTimer)) + "s", _center);
            float y = 145;
            bool canVote = _me != null && _me.Alive && _myVote == -2;
            foreach (var p in _players.Values.OrderBy(p => p.Id))
            {
                var prev = GUI.color;
                GUI.color = p.Alive ? Palette.Colors[p.Color % Palette.Colors.Length] : new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(new Rect(W / 2 - 330, y, 400, 36), p.Name + (p.Alive ? "" : "  (muerto)") + (_votedIds.Contains(p.Id) ? "  [OK] voto" : ""), _mid);
                GUI.color = prev;
                if (canVote && p.Alive && GUI.Button(new Rect(W / 2 + 120, y, 200, 34), "Votar", _small))
                { _myVote = p.Id; Cmd("VOTE|" + p.Id); }
                y += 42;
            }
            if (canVote && GUI.Button(new Rect(W / 2 - 100, 620, 200, 40), "Saltar voto", _mid)) { _myVote = -1; Cmd("VOTE|-1"); }
            if (!canVote) GUI.Label(new Rect(W / 2 - 300, 625, 600, 30), _me != null && !_me.Alive ? "Eres un fantasma: no puedes votar." : "Voto enviado. Esperando...", _center);
        }

        void DrawResult(float W, float H)
        {
            GUI.Box(new Rect(0, 0, W, H), "");
            GUI.Label(new Rect(0, H / 2 - 80, W, 50), "Resultado de la votación", _big);
            GUI.Label(new Rect(0, H / 2 - 10, W, 100), _resultText, _center);
        }

        void DrawEnd(float W, float H)
        {
            GUI.Box(new Rect(0, 0, W, H), "");
            var prev = GUI.color;
            GUI.color = _endWinner == 1 ? new Color(1f, 0.3f, 0.3f) : new Color(0.5f, 0.9f, 1f);
            GUI.Label(new Rect(0, H / 2 - 120, W, 60), _endText, _big);
            GUI.color = prev;
            GUI.Label(new Rect(0, H / 2 - 40, W, 40), Loc.T("Impostors: ") + _endImpostors, _center);
            if (_isHost && GUI.Button(new Rect(W / 2 - 130, H / 2 + 40, 260, 50), Loc.T("Back to Lobby"), _mid))
                Emit("LOBBYBACK");
            else if (!_isHost) GUI.Label(new Rect(0, H / 2 + 40, W, 40), Loc.T("Waiting for the host..."), _center);
        }
    }
}
