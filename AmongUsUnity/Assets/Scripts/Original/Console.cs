using UnityEngine;

/// <summary>A task station. Serialized fields mirror the original so the migrated Skeld prefab binds to it.</summary>
public class Console : MonoBehaviour
{
    public float usableDistance;
    public int ConsoleId;
    public bool onlyFromBelow;
    public bool onlySameRoom;
    public bool checkWalls;
    public bool GhostsIgnored;
    public bool AllowImpostor;
    public SystemTypes Room;
    public TaskTypes[] TaskTypes;
    public SpriteRenderer Image;
}
