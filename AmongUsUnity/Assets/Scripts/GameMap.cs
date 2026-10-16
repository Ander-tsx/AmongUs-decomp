using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Ship map built from the original ShipMap prefab: crew map, admin counts and the impostor sabotage map.</summary>
    public partial class Game
    {
        enum MapMode { Normal, Sabotage, Count }

        GameObject _map;
        MapMode _mapMode;
        Transform _mapHere, _mapCount, _mapInfected, _mapTasks, _mapDots;
        readonly List<GameObject> _mapMarks = new List<GameObject>();
        TextMesh _mapNote;
        float _mapScale = 4.4f;
        SpriteRenderer _mapBtn;
        UiButton _mapBtnUi;

        static readonly Dictionary<string, SystemTypes> CounterRooms = new Dictionary<string, SystemTypes>
        {
            { "LEngine", SystemTypes.UpperEngine }, { "REngine", SystemTypes.LowerEngine }, { "Reactor", SystemTypes.Reactor }, { "Security", SystemTypes.Security },
            { "MedBay", SystemTypes.MedBay }, { "Cafeteria", SystemTypes.Cafeteria }, { "Weapons", SystemTypes.Weapons }, { "Nav", SystemTypes.Nav },
            { "Shields", SystemTypes.Shields }, { "Comms", SystemTypes.Comms }, { "Storage", SystemTypes.Storage }, { "Admin", SystemTypes.Admin },
            { "Electric", SystemTypes.Electrical }, { "LifeSupp", SystemTypes.LifeSupp },
        };

        void BuildMapButton()
        {
            float w = _ui.HalfW, h = _ui.HalfH;
            _mapBtn = _ui.Sprite("BUTTON-MapInactive", _hud, new Vector2(w - 0.55f, h - 0.55f), 5, 0.8f);
            _mapBtnUi = _ui.Button(_mapBtn, () => ToggleMap(MapMode.Normal), new Vector2(0.9f, 0.9f));
        }

        void ToggleMap(MapMode mode)
        {
            if (_map != null) { bool same = _mapMode == mode; CloseMap(); if (same) return; }
            OpenMap(mode);
        }

        void CloseMap() { if (_map != null) Destroy(_map); _map = null; _mapMarks.Clear(); }

        void OpenMap(MapMode mode)
        {
            if (_phase != Phase.Playing || _me == null || IntroActive || Minigame.Instance != null) return;
            if (mode == MapMode.Sabotage && (!_me.Impostor || !_me.Alive)) return;
            var prefab = PrefabCatalog.Get("ShipMap");
            if (prefab == null) return;
            _mapMode = mode;
            _map = Instantiate(prefab, _ui.Cam.transform);
            _map.transform.localPosition = new Vector3(0, 0, 40);
            _map.transform.localRotation = Quaternion.identity;
            foreach (var t in _map.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.layer = UiKit.Layer;
                if (t.name.Contains("Glyph")) t.gameObject.SetActive(false);
            }
            foreach (var r in _map.GetComponentsInChildren<Renderer>(true)) r.sortingOrder += 60;
            var fade = _map.transform.Find("FadedBackground"); if (fade != null) fade.gameObject.SetActive(true);
            var bgr = _map.transform.Find("Background")?.GetComponent<SpriteRenderer>();
            if (bgr != null) bgr.color = mode == MapMode.Sabotage ? new Color(0.95f, 0.1f, 0.1f) : new Color(0.28f, 0.6f, 0.72f);
            _mapHere = _map.transform.Find("HereIndicatorParent/HereIndicator");
            _mapCount = _map.transform.Find("CountOverlay");
            _mapInfected = _map.transform.Find("InfectedOverlay");
            _mapTasks = _map.transform.Find("HereIndicatorParent/TaskOverlay");
            if (_mapCount != null) _mapCount.gameObject.SetActive(mode == MapMode.Count);
            if (_mapInfected != null) _mapInfected.gameObject.SetActive(mode == MapMode.Sabotage);
            if (_mapHere != null) UiKit.Tint(_mapHere.GetComponent<SpriteRenderer>(), Palette.Body(_me.Color), Palette.Shade(_me.Color));
            _mapNote = _ui.Text("", _map.transform, new Vector2(0, -2.5f), 0.3f, Color.white, TextAnchor.MiddleCenter, 200, true);
            _mapNote.gameObject.layer = UiKit.Layer;
            foreach (var t in _map.GetComponentsInChildren<Transform>(true)) if (t.name == "RoomNames (1)") t.gameObject.SetActive(false);
            if (mode == MapMode.Sabotage) PrepareSabotageMap();
        }

        Vector2 ToMap(Vector2 world) => world / _mapScale;

        void PrepareSabotageMap()
        {
            // door buttons and special buttons are children of each MapRoom; hide the ones that don't apply
            foreach (var t in _mapInfected.GetComponentsInChildren<Transform>(true))
                if (t.name == "bomb" || t.name == "lightsOut" || t.name == "meltdown" || t.name == "closeDoors") t.gameObject.SetActive(true);
        }

        void UpdateMap()
        {
            // HUD map button
            if (_mapBtn != null)
            {
                bool show = _phase == Phase.Playing && _me != null && !IntroActive;
                _mapBtn.gameObject.SetActive(show);
                if (show) _mapBtn.transform.localPosition = new Vector2(_ui.HalfW - 0.55f, _ui.HalfH - 0.55f);
                if (show && !_chatOpen && Input.GetKeyDown(KeyCode.Tab)) ToggleMap(MapMode.Normal);
            }
            if (_map == null) return;
            if (_phase != Phase.Playing || Minigame.Instance != null || (_me != null && !_me.Alive && _mapMode == MapMode.Sabotage)) { CloseMap(); return; }
            if (Input.GetKeyDown(KeyCode.Escape)) { CloseMap(); return; }

            bool commsDown = _sab == SabKind.Comms && _mapMode != MapMode.Sabotage;
            if (_mapHere != null) _mapHere.parent.GetComponentsInChildren<Transform>(true);
            if (_mapHere != null)
            {
                _mapHere.localPosition = new Vector3(ToMap(_me.Pos).x, ToMap(_me.Pos).y, -1f);
                _mapHere.gameObject.SetActive(!commsDown);
            }
            if (commsDown) _mapNote.text = Loc.T("COMMS SABOTAGED");

            // task markers
            if (_mapTasks != null)
            {
                _mapTasks.gameObject.SetActive(_mapMode == MapMode.Normal && !commsDown);
                if (_mapMarks.Count == 0 && _mapMode == MapMode.Normal && !_me.Impostor)
                    foreach (var t in _myTasks)
                    {
                        if (t.Complete) continue;
                        var c = WorldMap.FindConsole(t.Current.Console, t.Current.Room);
                        if (c == null) continue;
                        var tm = _ui.Text("!", _mapTasks, ToMap(c.transform.position), 0.35f, new Color(1f, 0.9f, 0.2f), TextAnchor.MiddleCenter, 190, true);
                        tm.gameObject.layer = UiKit.Layer;
                        _mapMarks.Add(tm.gameObject);
                    }
            }

            if (_mapMode == MapMode.Count) UpdateCounts(commsDown);
            if (Input.GetMouseButtonDown(0)) MapClick();
        }

        void UpdateCounts(bool commsDown)
        {
            if (_mapCount == null) return;
            foreach (var m in _mapMarks.Where(m => m != null && m.name == "dot").ToList()) { Destroy(m); _mapMarks.Remove(m); }
            if (commsDown) return;
            var perRoom = new Dictionary<SystemTypes, List<PlayerInfo>>();
            foreach (var p in _players.Values)
            {
                if (!p.Alive || p.InVent >= 0) continue;
                var r = WorldMap.RoomAt(p.Pos);
                if (!perRoom.TryGetValue(r, out var l)) perRoom[r] = l = new List<PlayerInfo>();
                l.Add(p);
            }
            foreach (var area in _mapCount.GetComponentsInChildren<Transform>(true))
            {
                if (area.parent != _mapCount || !CounterRooms.TryGetValue(area.name, out var room) || !perRoom.TryGetValue(room, out var list)) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    var icon = _ui.PlayerIcon(area, new Vector2((i % 4) * 0.22f - 0.3f, -(i / 4) * 0.26f), 0.14f, list[i].Color, 195);
                    icon.gameObject.name = "dot";
                    icon.gameObject.layer = UiKit.Layer;
                    _mapMarks.Add(icon.gameObject);
                }
            }
        }

        void MapClick()
        {
            var wp = (Vector2)_ui.Cam.ScreenToWorldPoint(Input.mousePosition);
            foreach (var c in UiKit.PointAll(wp))
                if (c.transform.IsChildOf(_map.transform) && c.name == "CloseButton") { CloseMap(); return; }
            if (_mapMode != MapMode.Sabotage || _mapInfected == null) return;

            // hit-test the sabotage buttons by sprite bounds (independent of the original colliders), topmost-nearest first
            SpriteRenderer best = null; float bd = float.MaxValue;
            foreach (var sr in _mapInfected.GetComponentsInChildren<SpriteRenderer>(false))
            {
                var n = sr.name;
                if (n != "closeDoors" && n != "bomb" && n != "lightsOut" && n != "meltdown") continue;
                var bounds = sr.bounds; bounds.Expand(new Vector3(0.15f, 0.15f, 100f));
                if (!bounds.Contains(new Vector3(wp.x, wp.y, bounds.center.z))) continue;
                float d = Vector2.Distance(bounds.center, wp);
                if (d < bd) { bd = d; best = sr; }
            }
            if (best == null) return;
            if (_sabCooldown > 0f || _sab != SabKind.None && best.name != "closeDoors")
            {
                _mapNote.text = _sab != SabKind.None ? Loc.T("A sabotage is already active") : Loc.T("Recharging...") + " " + Mathf.CeilToInt(_sabCooldown) + "s";
                return;
            }
            var parent = best.transform.parent;
            MapRoomTypes.TryGetValue(parent != null ? parent.name : "", out var rt);
            switch (best.name)
            {
                case "closeDoors": DoSabotage("Doors", ((int)rt).ToString()); break;
                case "meltdown": DoSabotage("Reactor", ""); break;
                case "lightsOut": DoSabotage("Lights", ""); break;
                case "bomb": DoSabotage(rt == SystemTypes.Comms ? "Comms" : "O2", ""); break;
            }
            CloseMap();
        }

        static readonly Dictionary<string, SystemTypes> MapRoomTypes = new Dictionary<string, SystemTypes>
        {
            { "Cafeteria", SystemTypes.Cafeteria }, { "Medbay", SystemTypes.MedBay }, { "Electrical", SystemTypes.Electrical }, { "LifeSupp", SystemTypes.LifeSupp },
            { "LeftEngine", SystemTypes.UpperEngine }, { "RightEngine", SystemTypes.LowerEngine }, { "Storage", SystemTypes.Storage },
            { "Reactor", SystemTypes.Reactor }, { "Comms", SystemTypes.Comms }, { "Security", SystemTypes.Security },
        };
    }
}
