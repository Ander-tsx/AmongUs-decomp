using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Impostor vents: hide inside a vent and hop between connected vents.</summary>
    public partial class Game
    {
        Vent _myVent;
        SpriteRenderer _ventBtn;
        UiButton _ventUi;
        readonly Dictionary<int, Vent> _vents = new Dictionary<int, Vent>();
        const float VentRange = 1.0f;

        void InitVents()
        {
            _vents.Clear();
            foreach (var v in FindObjectsByType<Vent>(FindObjectsInactive.Include, FindObjectsSortMode.None)) _vents[v.Id] = v;
        }

        void BuildVentHud()
        {
            float w = _ui.HalfW, h = _ui.HalfH;
            _ventBtn = _ui.Sprite("vent", _hud, new Vector2(w - 3.2f, -h + 0.75f), 5, 0.9f);
            _ventUi = _ui.Button(_ventBtn, VentButton, new Vector2(1.2f, 0.8f));
        }

        Vent NearestVent()
        {
            if (_me == null) return null;
            Vent best = null; float bd = VentRange;
            foreach (var v in _vents.Values)
            {
                float d = Vector2.Distance(v.transform.position, _me.Pos);
                if (d <= bd) { bd = d; best = v; }
            }
            return best;
        }

        void VentButton()
        {
            if (_phase != Phase.Playing || _me == null || !_me.Impostor || !_me.Alive) return;
            if (_myVent != null) { Cmd("VENT|exit|" + _myVent.Id); return; }
            var v = NearestVent();
            if (v != null) Cmd("VENT|enter|" + v.Id);
        }

        void UpdateVentHud()
        {
            if (_ventBtn == null) return;
            bool imp = _phase == Phase.Playing && _me != null && _me.Impostor && _me.Alive && !IntroActive;
            _ventBtn.gameObject.SetActive(imp);
            if (!imp) return;
            float w = _ui.HalfW, h = _ui.HalfH;
            _ventBtn.transform.localPosition = new Vector2(w - 3.2f, -h + 0.75f);
            _ventUi.SetTint(_myVent != null || NearestVent() != null ? Color.white : Disabled);
            if (Input.GetKeyDown(KeyCode.V) && !_chatOpen) VentButton();
        }

        void UpdateVentInput()
        {
            if (_myVent == null) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { Cmd("VENT|exit|" + _myVent.Id); return; }
            if (Input.GetMouseButtonDown(0))
            {
                var wp = (Vector2)_cam.ScreenToWorldPoint(Input.mousePosition);
                var target = _myVent.ArrowAt(wp);
                if (target != null) Cmd("VENT|move|" + target.Id);
            }
        }

        // ---- host ----

        void HostVent(int from, string action, int id)
        {
            if (_phase != Phase.Playing) return;
            var p = Get(from);
            if (p == null || !p.Impostor || !p.Alive || !_vents.TryGetValue(id, out var v)) return;
            switch (action)
            {
                case "enter":
                    if (p.InVent >= 0 || Vector2.Distance(p.TargetPos, v.transform.position) > VentRange + 1f) return;
                    p.InVent = id; p.TargetPos = v.transform.position;
                    Emit("VENT|" + from + "|" + id + "|enter");
                    break;
                case "move":
                    if (p.InVent < 0 || !_vents.TryGetValue(p.InVent, out var cur) || !cur.Neighbours.Contains(v)) return;
                    p.InVent = id; p.TargetPos = v.transform.position;
                    Emit("VENT|" + from + "|" + id + "|move");
                    break;
                case "exit":
                    if (p.InVent != id) return;
                    p.InVent = -1;
                    Emit("VENT|" + from + "|" + id + "|exit");
                    break;
            }
        }

        // ---- events ----

        void ApplyVent(string[] a)
        {
            int pid = int.Parse(a[1]), id = int.Parse(a[2]);
            string action = a[3];
            var p = Get(pid);
            if (p == null || !_vents.TryGetValue(id, out var v)) return;
            var pos = (Vector2)v.transform.position;
            if (action == "enter" || action == "move") { p.InVent = id; p.Pos = p.TargetPos = pos; }
            else p.InVent = -1;
            v.PlayAnim(action != "exit");
            Play("Vent_Enter", 0.6f);
            if (p == _me)
            {
                if (_myVent != null) _myVent.ShowArrows(false);
                _myVent = p.InVent >= 0 ? v : null;
                if (_myVent != null)
                {
                    _myVent.ShowArrows(true);
                    if (_me.Go != null) _me.Go.transform.position = pos;
                    if (_me.Body != null) { _me.Body.linearVelocity = Vector2.zero; _me.Body.position = pos; }
                }
            }
        }

        void LeaveAllVents()
        {
            foreach (var p in _players.Values) p.InVent = -1;
            if (_myVent != null) _myVent.ShowArrows(false);
            _myVent = null;
        }
    }
}
