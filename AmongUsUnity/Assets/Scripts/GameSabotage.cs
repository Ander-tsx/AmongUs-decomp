using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    public enum SabKind { None, Reactor, O2, Lights, Comms, Doors }

    /// <summary>Sabotages: reactor meltdown, O2, lights, comms and door lockdowns, with the original fix minigames.</summary>
    public partial class Game
    {
        SabKind _sab;
        float _sabTimer;                       // seconds until a critical sabotage ends the game
        string _o2Code = "";
        readonly bool[] _o2Done = new bool[2];
        readonly bool[] _reactorHeld = new bool[2];
        float _sabCooldown;                    // host-authoritative; impostor UI mirrors it
        readonly float[] _hostHoldAt = { -10f, -10f };
        float _holdSendAt;
        const float SabCooldownSeconds = 30f, DoorSeconds = 10f;

        readonly List<KeyValuePair<PlainDoor, float>> _closedDoors = new List<KeyValuePair<PlainDoor, float>>();

        static readonly SystemTypes[] DoorRooms =
            { SystemTypes.Cafeteria, SystemTypes.MedBay, SystemTypes.UpperEngine, SystemTypes.LowerEngine, SystemTypes.Security, SystemTypes.Electrical, SystemTypes.Storage };

        static string RoomLabel(SystemTypes r) => r == SystemTypes.LifeSupp ? "O2" : r == SystemTypes.Nav ? "Navigation"
            : r == SystemTypes.UpperEngine ? "Upper Engine" : r == SystemTypes.LowerEngine ? "Lower Engine" : r == SystemTypes.MedBay ? "MedBay" : r.ToString();

        // ---------------------------------------------------------------- host

        void HostSabotage(int from, string kind, string arg)
        {
            if (_phase != Phase.Playing) return;
            var p = Get(from);
            if (p == null || !p.Impostor || !p.Alive || _sabCooldown > 0f) return;
            if (!Enum.TryParse(kind, out SabKind k) || k == SabKind.None) return;
            if (k != SabKind.Doors && _sab != SabKind.None) return;
            _sabCooldown = SabCooldownSeconds;
            switch (k)
            {
                case SabKind.Reactor: Emit("SAB|Reactor|45|"); break;
                case SabKind.O2: Emit("SAB|O2|45|" + UnityEngine.Random.Range(10000, 99999)); break;
                case SabKind.Lights: Emit("SAB|Lights|0|"); break;
                case SabKind.Comms: Emit("SAB|Comms|0|"); break;
                case SabKind.Doors: Emit("DOORS|" + int.Parse(arg) + "|" + DoorSeconds); break;
            }
        }

        void HostFix(int from, string kind, int which)
        {
            if (_phase != Phase.Playing || _sab == SabKind.None) return;
            var p = Get(from);
            if (p == null || !p.Alive || p.Impostor) return;
            if (!Enum.TryParse(kind, out SabKind k) || k != _sab) return;
            switch (k)
            {
                case SabKind.Lights:
                case SabKind.Comms:
                    Emit("SABFIXED|" + k); break;
                case SabKind.O2:
                    which = Mathf.Clamp(which, 0, 1);
                    _o2Done[which] = true;
                    Emit("SABPROG|O2|" + (_o2Done[0] ? 1 : 0) + "|" + (_o2Done[1] ? 1 : 0));
                    if (_o2Done[0] && _o2Done[1]) Emit("SABFIXED|O2");
                    break;
            }
        }

        void HostHold(int from, int idx, bool held)
        {
            if (_phase != Phase.Playing || _sab != SabKind.Reactor) return;
            var p = Get(from);
            if (p == null || !p.Alive || p.Impostor || idx < 0 || idx > 1) return;
            if (held) _hostHoldAt[idx] = Time.time;
        }

        void HostSabTick()
        {
            if (_phase != Phase.Playing) return;
            if (_sabCooldown > 0f) _sabCooldown -= Time.deltaTime;
            if (_sab == SabKind.Reactor)
            {
                bool a = Time.time - _hostHoldAt[0] < 0.6f, b = Time.time - _hostHoldAt[1] < 0.6f;
                if (a != _reactorHeld[0] || b != _reactorHeld[1]) Emit("SABPROG|Reactor|" + (a ? 1 : 0) + "|" + (b ? 1 : 0));
                if (a && b) { Emit("SABFIXED|Reactor"); return; }
            }
            if ((_sab == SabKind.Reactor || _sab == SabKind.O2) && _sabTimer <= 0f)
            {
                HostEnd(1);
            }
        }

        // ---------------------------------------------------------------- events (host + clients)

        void ApplySabEvent(string[] a)
        {
            Debug.Log("[LAN] " + string.Join(" ", a));
            switch (a[0])
            {
                case "SAB":
                    _sab = (SabKind)Enum.Parse(typeof(SabKind), a[1]);
                    _sabTimer = float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture);
                    _o2Code = a.Length > 3 ? a[3] : "";
                    Array.Clear(_o2Done, 0, 2); Array.Clear(_reactorHeld, 0, 2);
                    _sabCooldown = SabCooldownSeconds;
                    Array.Clear(_hostHoldAt, 0, 2); _hostHoldAt[0] = _hostHoldAt[1] = -10f;
                    Play("Alarm_sabotage");
                    break;
                case "SABPROG":
                    if (a[1] == "O2") { _o2Done[0] = a[2] == "1"; _o2Done[1] = a[3] == "1"; }
                    else { _reactorHeld[0] = a[2] == "1"; _reactorHeld[1] = a[3] == "1"; }
                    break;
                case "SABFIXED":
                    EndSabotage();
                    break;
                case "DOORS":
                    CloseDoors((SystemTypes)int.Parse(a[1]), float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture));
                    _sabCooldown = SabCooldownSeconds;
                    Play("door_close");
                    break;
            }
        }

        void EndSabotage()
        {
            _sab = SabKind.None; _sabTimer = 0;
            Array.Clear(_o2Done, 0, 2); Array.Clear(_reactorHeld, 0, 2);
            if (Minigame.Instance != null && _sabMinigameOpen) Minigame.Instance.Close(false);
            _sabMinigameOpen = false;
        }

        void ResetSabotage()
        {
            EndSabotage();
            _sabCooldown = 12f;
            foreach (var kv in _closedDoors) if (kv.Key != null) kv.Key.SetDoorway(true);
            _closedDoors.Clear();
        }

        void CloseDoors(SystemTypes room, float seconds)
        {
            foreach (var d in FindObjectsByType<PlainDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (d.Room != room) continue;
                d.SetDoorway(false);
                _closedDoors.Add(new KeyValuePair<PlainDoor, float>(d, Time.time + seconds));
            }
        }

        void UpdateSabotage()
        {
            // reopen doors
            for (int i = _closedDoors.Count - 1; i >= 0; i--)
                if (_closedDoors[i].Key == null || Time.time >= _closedDoors[i].Value) { if (_closedDoors[i].Key != null) _closedDoors[i].Key.SetDoorway(true); _closedDoors.RemoveAt(i); }
            if (_phase == Phase.Playing && (_sab == SabKind.Reactor || _sab == SabKind.O2)) _sabTimer -= Time.deltaTime;
            if (_phase == Phase.Playing && _sabCooldown > 0f && !_isHost) _sabCooldown -= Time.deltaTime;
            UpdateSabotageHud();
        }

        // ---------------------------------------------------------------- local fixing

        bool _sabMinigameOpen;

        struct SabConsole { public string Name; public SystemTypes Room; public int Idx; }

        IEnumerable<SabConsole> SabConsoles()
        {
            switch (_sab)
            {
                case SabKind.Lights: yield return new SabConsole { Name = "SwitchConsole", Room = SystemTypes.Electrical, Idx = 0 }; break;
                case SabKind.Comms: yield return new SabConsole { Name = "FixCommsConsole", Room = SystemTypes.Comms, Idx = 0 }; break;
                case SabKind.Reactor:
                    yield return new SabConsole { Name = "UpperHandConsole", Room = SystemTypes.Reactor, Idx = 0 };
                    yield return new SabConsole { Name = "LowerHandConsole", Room = SystemTypes.Reactor, Idx = 1 };
                    break;
                case SabKind.O2:
                    if (!_o2Done[0]) yield return new SabConsole { Name = "NoOxyConsole", Room = SystemTypes.LifeSupp, Idx = 0 };
                    if (!_o2Done[1]) yield return new SabConsole { Name = "NoOxyConsole", Room = SystemTypes.Admin, Idx = 1 };
                    break;
            }
        }

        bool TrySabotageFix()
        {
            if (_sab == SabKind.None || _sab == SabKind.Doors || _me == null || !_me.Alive || _me.Impostor || Minigame.Instance != null) return false;
            foreach (var sc in SabConsoles())
            {
                var c = WorldMap.FindConsole(sc.Name, sc.Room);
                if (c == null || Vector2.Distance(c.transform.position, _me.Pos) > Mathf.Max(c.usableDistance, 0.9f)) continue;
                var kind = _sab;
                int idx = sc.Idx;
                Minigame mg = null;
                switch (kind)
                {
                    case SabKind.Lights: mg = Minigame.Open("SwitchMinigame", ok => { _sabMinigameOpen = false; if (ok) Cmd("FIX|Lights|0"); }, 0, sc.Room, sc.Name, _bot); break;
                    case SabKind.Comms: mg = Minigame.Open("RadioMinigame", ok => { _sabMinigameOpen = false; if (ok) Cmd("FIX|Comms|0"); }, 0, sc.Room, sc.Name, _bot); break;
                    case SabKind.O2: mg = Minigame.Open("KeyPadDisarmMinigame", ok => { _sabMinigameOpen = false; if (ok) Cmd("FIX|O2|" + idx); }, 0, sc.Room, sc.Name, _bot, SystemTypes.Hallway, _o2Code); break;
                    case SabKind.Reactor:
                        mg = Minigame.Open("ResetReactorMinigame", ok => { _sabMinigameOpen = false; Cmd("HOLD|" + idx + "|0"); }, 0, sc.Room, sc.Name, _bot, SystemTypes.Hallway, null,
                            held => { if (held && Time.time - _holdSendAt > 0.2f) { _holdSendAt = Time.time; Cmd("HOLD|" + idx + "|1"); } });
                        break;
                }
                if (mg == null) return false;
                _sabMinigameOpen = true;
                if (_me.Body != null) _me.Body.linearVelocity = Vector2.zero;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- impostor menu + HUD

        Transform _sabMenu;
        SpriteRenderer _sabBtn, _alert;
        TextMesh _sabText, _sabHint;
        UiButton _sabUi;
        bool _doorsMenu;

        void BuildSabotageHud()
        {
            float w = _ui.HalfW, h = _ui.HalfH;
            _sabBtn = _ui.Sprite("sabotage", _hud, new Vector2(w - 2.0f, -h + 1.95f), 5, 0.9f);
            _sabUi = _ui.Button(_sabBtn, ToggleSabMenu, new Vector2(1.1f, 1.1f));
            _sabText = _ui.Text("", _hud, new Vector2(w - 2.0f, -h + 1.95f), 0.55f, Color.white, TextAnchor.MiddleCenter, 8, true);
            _alert = _ui.Rect(_hud, Vector2.zero, new Vector2(w * 2 + 2, h * 2 + 2), new Color(1f, 0f, 0f, 0f), 20);
            _sabHint = _ui.Text("", _hud, new Vector2(0, h - 0.9f), 0.32f, new Color(1f, 0.35f, 0.3f), TextAnchor.MiddleCenter, 22, true);
        }

        void ToggleSabMenu()
        {
            if (_phase != Phase.Playing || _me == null || !_me.Impostor || !_me.Alive) return;
            ToggleMap(MapMode.Sabotage);
        }

        void CloseSabMenu() { if (_sabMenu != null) Destroy(_sabMenu.gameObject); _sabMenu = null; }

        void BuildSabMenu()
        {
            if (_sabMenu != null) Destroy(_sabMenu.gameObject);
            _sabMenu = _ui.Group("SabMenu", _hud);
            float w = _ui.HalfW;
            var items = new List<KeyValuePair<string, Action>>();
            if (!_doorsMenu)
            {
                items.Add(new KeyValuePair<string, Action>(Loc.T("Reactor"), () => DoSabotage("Reactor", "")));
                items.Add(new KeyValuePair<string, Action>(Loc.T("O2"), () => DoSabotage("O2", "")));
                items.Add(new KeyValuePair<string, Action>(Loc.T("Lights"), () => DoSabotage("Lights", "")));
                items.Add(new KeyValuePair<string, Action>(Loc.T("Comms"), () => DoSabotage("Comms", "")));
                items.Add(new KeyValuePair<string, Action>(Loc.T("Doors >"), () => { _doorsMenu = true; BuildSabMenu(); }));
            }
            else
            {
                foreach (var r in DoorRooms) { var room = r; items.Add(new KeyValuePair<string, Action>(Loc.T("Doors: ") + Loc.T(RoomLabel(r)), () => DoSabotage("Doors", ((int)room).ToString()))); }
                items.Add(new KeyValuePair<string, Action>(Loc.T("< Back"), () => { _doorsMenu = false; BuildSabMenu(); }));
            }
            float y = items.Count * 0.28f - 0.1f;
            _ui.Rect(_sabMenu, new Vector2(w - 2.2f, y - items.Count * 0.28f + 0.3f), new Vector2(2.6f, items.Count * 0.56f + 0.3f), new Color(0, 0, 0, 0.75f), 24);
            for (int i = 0; i < items.Count; i++)
            {
                var act = items[i].Value;
                var sr = _ui.Sliced("buttonClick", _sabMenu, new Vector2(w - 2.2f, y - i * 0.56f), new Vector2(2.3f, 0.46f), 26);
                _ui.Button(sr, act, new Vector2(2.3f, 0.46f));
                _ui.Text(items[i].Key, _sabMenu, new Vector2(w - 2.2f, y - i * 0.56f), 0.22f, Color.white, TextAnchor.MiddleCenter, 28, true);
            }
        }

        void DoSabotage(string kind, string arg)
        {
            if (_sabCooldown > 0f) return;
            Cmd("SABOTAGE|" + kind + "|" + arg);
            CloseSabMenu();
        }

        void UpdateSabotageHud()
        {
            if (_sabBtn == null) return;
            bool imp = _phase == Phase.Playing && _me != null && _me.Impostor && _me.Alive && !IntroActive;
            _sabBtn.gameObject.SetActive(imp);
            _sabText.gameObject.SetActive(imp);
            if (imp)
            {
                float w = _ui.HalfW, h = _ui.HalfH;
                _sabBtn.transform.localPosition = new Vector2(w - 2.0f, -h + 1.95f);
                bool ready = _sabCooldown <= 0f && (_sab == SabKind.None);
                _sabUi.SetTint(ready ? Color.white : Disabled);
                _sabText.text = _sabCooldown > 0f ? Mathf.CeilToInt(_sabCooldown).ToString() : "";
            }
            else CloseSabMenu();

            // alert flash + status line
            bool active = _phase == Phase.Playing && _sab != SabKind.None && !IntroActive;
            float pulse = active && (_sab == SabKind.Reactor || _sab == SabKind.O2) ? (Mathf.Sin(Time.time * 6f) * 0.5f + 0.5f) * 0.28f : 0f;
            var c = _alert.color; c.a = pulse; _alert.color = c;
            string msg = "";
            if (active)
            {
                switch (_sab)
                {
                    case SabKind.Reactor: msg = Loc.T("REACTOR MELTDOWN") + "  " + Mathf.CeilToInt(Mathf.Max(0, _sabTimer)) + "s"; break;
                    case SabKind.O2: msg = Loc.T("OXYGEN DEPLETED") + "  " + Mathf.CeilToInt(Mathf.Max(0, _sabTimer)) + "s"; break;
                    case SabKind.Lights: msg = Loc.T("LIGHTS SABOTAGED"); break;
                    case SabKind.Comms: msg = Loc.T("COMMS SABOTAGED"); break;
                }
            }
            _sabHint.text = msg;
        }
    }
}
