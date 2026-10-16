using System.Collections.Generic;
using UnityEngine;

/// <summary>Migrated original prefabs (minigames etc.) resolved by name at runtime.</summary>
public class PrefabCatalog : ScriptableObject
{
    public string[] Names;
    public GameObject[] Prefabs;
    static Dictionary<string, GameObject> _map;

    public static GameObject Get(string name)
    {
        if (_map == null)
        {
            _map = new Dictionary<string, GameObject>();
            var c = Resources.Load<PrefabCatalog>("PrefabCatalog");
            if (c != null) for (int i = 0; i < c.Names.Length; i++) _map[c.Names[i]] = c.Prefabs[i];
        }
        _map.TryGetValue(name, out var p);
        return p;
    }
}
