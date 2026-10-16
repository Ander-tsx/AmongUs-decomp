using System.Collections.Generic;
using UnityEngine;

namespace AmongUsLan
{
    /// <summary>Read-only helpers over the migrated Skeld prefab (ShipStatus + its consoles/rooms).</summary>
    public static class WorldMap
    {
        static readonly Dictionary<string, Console> Consoles = new Dictionary<string, Console>();
        static PlainShipRoom[] _rooms;

        static string Key(string name, SystemTypes room) => name + "@" + (int)room;

        public static bool Ready => ShipStatus.Instance != null;

        public static void Init()
        {
            Consoles.Clear();
            foreach (var c in Object.FindObjectsByType<Console>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Consoles[Key(c.gameObject.name, c.Room)] = c;
            _rooms = Object.FindObjectsByType<PlainShipRoom>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        }

        public static Console FindConsole(string name, SystemTypes room)
        {
            Consoles.TryGetValue(Key(name, room), out var c);
            return c;
        }

        static Transform _adminMap;
        public static Transform AdminMap { get { if (_adminMap == null) { var g = GameObject.Find("MapRoomConsole"); if (g != null) _adminMap = g.transform; } return _adminMap; } }

        static Transform _surv;
        public static Transform SurvConsole { get { if (_surv == null) { var g = GameObject.Find("SurvConsole"); if (g != null) _surv = g.transform; } return _surv; } }

        public static Vector2 SpawnCenter => ShipStatus.Instance != null ? ShipStatus.Instance.InitialSpawnCenter : Vector2.zero;
        public static float SpawnRadius => ShipStatus.Instance != null && ShipStatus.Instance.SpawnRadius > 0 ? ShipStatus.Instance.SpawnRadius : 1.6f;

        /// <summary>Spawn ring around the cafeteria table, like the original start-of-round layout.</summary>
        public static Vector2 SpawnPos(int index, int count)
        {
            float ang = (index * 360f / Mathf.Max(count, 1) + 90f) * Mathf.Deg2Rad;
            return SpawnCenter + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * SpawnRadius;
        }

        public static Vector2 EmergencyButtonPos => ShipStatus.Instance != null && ShipStatus.Instance.EmergencyButton != null
            ? (Vector2)ShipStatus.Instance.EmergencyButton.transform.position : SpawnCenter;

        public static float EmergencyButtonRange => ShipStatus.Instance != null && ShipStatus.Instance.EmergencyButton != null
            ? Mathf.Max(1.5f, ShipStatus.Instance.EmergencyButton.usableDistance) : 1.75f;

        public static SystemTypes RoomAt(Vector2 p)
        {
            if (_rooms == null) return SystemTypes.Hallway;
            foreach (var r in _rooms)
                if (r.roomArea != null && r.roomArea.OverlapPoint(p)) return r.RoomId;
            return SystemTypes.Hallway;
        }
    }
}
