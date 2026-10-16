using System.Collections.Generic;
using UnityEngine;

/// <summary>Named sprites and fonts migrated from the original UI, resolved at runtime by name.</summary>
public class UiSprites : ScriptableObject
{
    public string[] Names;
    public Sprite[] Sprites;
    public Font Font;
    public Font FontBlack;

    static UiSprites _inst;
    static Dictionary<string, Sprite> _map;

    public static UiSprites Instance
    {
        get
        {
            if (_inst == null)
            {
                _inst = Resources.Load<UiSprites>("UiSprites");
                _map = new Dictionary<string, Sprite>();
                if (_inst != null)
                    for (int i = 0; i < _inst.Names.Length; i++) _map[_inst.Names[i]] = _inst.Sprites[i];
            }
            return _inst;
        }
    }

    public static Sprite Get(string name)
    {
        var _ = Instance;
        if (_map != null && _map.TryGetValue(name, out var s)) return s;
        Debug.LogWarning("UiSprites: missing sprite '" + name + "'");
        return null;
    }
}
